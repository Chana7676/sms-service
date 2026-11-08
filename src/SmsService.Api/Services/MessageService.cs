using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmsService.Api.Contracts;
using SmsService.Api.Data;
using SmsService.Api.Data.Entities;
using SmsService.Api.Providers;
using SmsService.Api.Workers;

namespace SmsService.Api.Services;

/// <summary>Either the new message or why it was rejected.</summary>
public record CreateMessageResult(Message? Message, string? Error);

public class SmsWorkerOptions
{
    public bool Enabled { get; set; } = true;
    public double RetryIntervalMinutes { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;

    /// <summary>At most this many messages per retry round, so a big backlog doesn't flood the provider.</summary>
    public int RetryBatchSize { get; set; } = 50;
}

public class MessageService(SmsDbContext db, ISmsProvider provider, SendQueue queue, IOptions<SmsWorkerOptions> options)
{
    private readonly SmsWorkerOptions _options = options.Value;

    /// <summary>
    /// Saves the message as Pending and puts it in the send queue. Does not call the provider,
    /// so the request returns right away; SmsWorker sends it in the background.
    /// Expects a request that already passed model validation.
    /// </summary>
    public async Task<CreateMessageResult> CreateAsync(SendMessageRequest request)
    {
        var to = request.To;
        var phone = PhoneNumber.Normalize(to.Phone);

        var employee = await CurrentEmployees.FindAsync(db.Employees, to.FirstName.Trim(), to.LastName.Trim(), to.Address.Trim());
        if (employee == null)
            return new CreateMessageResult(null, "Employee not found.");
        if (employee.Phone != phone)
            return new CreateMessageResult(null, "Phone is not the employee's current phone.");

        var now = DateTime.UtcNow;
        var message = new Message
        {
            Employee = employee,
            Phone = phone,
            Body = request.Body,
            Status = MessageStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Saved first, then queued: if the server restarts before it is sent, the Pending row remains
        // and the retry round sends it.
        queue.Enqueue(message.Id);
        return new CreateMessageResult(message, null);
    }

    /// <summary>Called by SmsWorker for a message taken from the queue: first send attempt.</summary>
    public async Task SendAsync(int messageId, CancellationToken ct = default)
    {
        var message = await db.Messages.FindAsync([messageId], ct);
        if (message?.Status == MessageStatus.Pending)
            await TrySendAsync(message, ct);
    }

    /// <summary>
    /// Called by SmsWorker every retry interval: tries again every Pending message whose last attempt
    /// was at least one interval ago (failed sends, and messages left Pending by a restart).
    /// </summary>
    public async Task RetryPendingAsync(CancellationToken ct = default)
    {
        var lastAttemptBefore = DateTime.UtcNow.AddMinutes(-_options.RetryIntervalMinutes);
        var pending = await db.Messages
            .Where(m => m.Status == MessageStatus.Pending && m.UpdatedAt <= lastAttemptBefore)
            .OrderBy(m => m.Id)
            .Take(_options.RetryBatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
            await TrySendAsync(message, ct);
    }

    /// <summary>
    /// One attempt: success -> Sent. Failure -> stays Pending (the retry round tries again),
    /// or Failed when this was the last allowed attempt.
    /// </summary>
    private async Task TrySendAsync(Message message, CancellationToken ct = default)
    {
        message.Attempts++;
        try
        {
            message.ProviderMessageId = await provider.SendAsync(message.Phone, message.Body, ct);
            message.Status = MessageStatus.Sent;
            message.LastError = null;
        }
        // Any provider failure counts as an attempt (not only SmsProviderException), so a message can't stay
        // Pending forever because of an unexpected exception type. Cancellation (app shutdown) is not a failure.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.LastError = ex.Message;
            if (message.Attempts >= _options.MaxAttempts)
                message.Status = MessageStatus.Failed;
        }

        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public Task<Message?> GetAsync(int id) =>
        db.Messages.Include(m => m.Employee).FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>Newest first, optionally filtered by status. Uses the (Status, CreatedAt) index.</summary>
    public async Task<(List<Message> Items, int TotalCount)> ListAsync(MessageStatus? status, int page, int pageSize)
    {
        var query = db.Messages.AsQueryable();
        if (status != null)
            query = query.Where(m => m.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(m => m.Employee)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
