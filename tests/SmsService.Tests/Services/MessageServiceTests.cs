using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmsService.Api.Contracts;
using SmsService.Api.Data.Entities;
using SmsService.Api.Providers;
using SmsService.Api.Services;
using SmsService.Api.Workers;

namespace SmsService.Tests.Services;

public class MessageServiceTests : TestDb
{
    // --- Validation of the recipient ---

    [Fact]
    public async Task Unknown_employee_is_rejected()
    {
        var result = await Service().CreateAsync(Request("אין", "כזה", "שום מקום", "0522222223"));

        Assert.Equal("Employee not found.", result.Error);
        Assert.Empty(Db.Messages);
    }

    [Theory]
    [InlineData("0522222221")] // Sarah's old phone
    [InlineData("0501111111")] // another employee's phone
    public async Task Phone_that_is_not_the_current_one_is_rejected(string phone)
    {
        var result = await Service().CreateAsync(Request("שרה", "כהן", "הנשיא 5, ירושלים", phone));

        Assert.Equal("Phone is not the employee's current phone.", result.Error);
        Assert.Empty(Db.Messages);
    }

    [Fact]
    public async Task Phone_with_dashes_and_names_with_spaces_are_accepted()
    {
        var result = await Service().CreateAsync(Request(" שרה ", "כהן", "הנשיא 5, ירושלים ", "052-2222223"));

        Assert.Null(result.Error);
        Assert.Equal("0522222223", result.Message!.Phone);
    }

    [Fact]
    public async Task Shared_phone_gives_a_separate_message_per_employee()
    {
        await Service().CreateAsync(Request("משה", "אברהם", "הדקל 7, לוד", "0544444444"));
        await Service().CreateAsync(Request("רחל", "אברהם", "הדקל 7, לוד", "0544444444"));

        var names = await Db.Messages.Select(m => m.Employee.FirstName).OrderBy(n => n).ToListAsync();
        Assert.Equal(["משה", "רחל"], names);
    }

    // --- Sending ---

    // --- Create: saves and queues, does not send ---

    [Fact]
    public async Task Create_saves_pending_and_queues_it_without_sending()
    {
        var result = await Service().CreateAsync(SarahRequest());

        var saved = await Db.Messages.Include(m => m.Employee).SingleAsync();
        Assert.Equal(MessageStatus.Pending, saved.Status);
        Assert.Equal(0, saved.Attempts);
        Assert.Equal("שרה", saved.Employee.FirstName);
        Assert.Equal([saved.Id], await Queued());
        Assert.Equal(saved.Id, result.Message!.Id);
    }

    // --- First send (from the queue) ---

    [Fact]
    public async Task Provider_accepts_so_message_is_sent()
    {
        var id = (await Service().CreateAsync(SarahRequest())).Message!.Id;

        await Service(errorRate: 0).SendAsync(id);

        var saved = await Db.Messages.SingleAsync();
        Assert.Equal(MessageStatus.Sent, saved.Status);
        Assert.Equal(1, saved.Attempts);
        Assert.NotNull(saved.ProviderMessageId);
    }

    [Fact]
    public async Task Provider_fails_so_message_stays_pending_for_retry()
    {
        var id = (await Service().CreateAsync(SarahRequest())).Message!.Id;

        await Service(errorRate: 1).SendAsync(id);

        var saved = await Db.Messages.SingleAsync();
        Assert.Equal(MessageStatus.Pending, saved.Status);
        Assert.Equal(1, saved.Attempts);
        Assert.NotNull(saved.LastError);
    }

    [Fact]
    public async Task Unexpected_provider_exception_still_counts_as_a_failed_attempt()
    {
        var id = (await Service().CreateAsync(SarahRequest())).Message!.Id;
        var service = new MessageService(Db, new ThrowingProvider(), _queue,
            Options.Create(new SmsWorkerOptions { RetryIntervalMinutes = 1, MaxAttempts = 3 }));

        await service.SendAsync(id);

        var saved = await Db.Messages.SingleAsync();
        Assert.Equal(MessageStatus.Pending, saved.Status);
        Assert.Equal(1, saved.Attempts);
        Assert.Equal("Network down", saved.LastError);
    }

    private class ThrowingProvider : ISmsProvider
    {
        public Task<string> SendAsync(string phone, string text, CancellationToken ct = default) =>
            throw new HttpRequestException("Network down");
    }

    [Fact]
    public async Task Already_sent_message_is_not_sent_again()
    {
        var id = (await Service().CreateAsync(SarahRequest())).Message!.Id;
        await Service(errorRate: 0).SendAsync(id);

        await Service(errorRate: 0).SendAsync(id);

        Assert.Equal(1, (await Db.Messages.SingleAsync()).Attempts);
    }

    // --- Retry round ---

    [Fact]
    public async Task Retry_sends_a_pending_message_once_the_interval_passed()
    {
        await FailedFirstAttempt();
        await MakeLastAttemptOld();

        await Service(errorRate: 0).RetryPendingAsync();

        var saved = await Db.Messages.SingleAsync();
        Assert.Equal(MessageStatus.Sent, saved.Status);
        Assert.Equal(2, saved.Attempts);
    }

    [Fact]
    public async Task Retry_skips_a_message_whose_last_attempt_was_recent()
    {
        await FailedFirstAttempt();

        await Service(errorRate: 0).RetryPendingAsync();

        Assert.Equal(1, (await Db.Messages.SingleAsync()).Attempts);
    }

    [Fact]
    public async Task Retry_sends_a_message_left_pending_by_a_restart()
    {
        await Service().CreateAsync(SarahRequest()); // queued, but the queue was lost (never sent)
        await MakeLastAttemptOld();

        await Service(errorRate: 0).RetryPendingAsync();

        Assert.Equal(MessageStatus.Sent, (await Db.Messages.SingleAsync()).Status);
    }

    [Fact]
    public async Task Retry_round_sends_at_most_one_batch()
    {
        for (var i = 0; i < 3; i++)
            await Service().CreateAsync(SarahRequest()); // never sent: the queue is not read in these tests
        await MakeLastAttemptOld();

        await Service(errorRate: 0, retryBatchSize: 2).RetryPendingAsync();

        Assert.Equal(2, await Db.Messages.CountAsync(m => m.Status == MessageStatus.Sent));
        Assert.Equal(1, await Db.Messages.CountAsync(m => m.Status == MessageStatus.Pending));
    }

    [Fact]
    public async Task Third_failed_attempt_marks_it_failed()
    {
        await FailedFirstAttempt();                       // attempt 1
        await MakeLastAttemptOld();
        await Service(errorRate: 1).RetryPendingAsync();  // attempt 2
        await MakeLastAttemptOld();
        await Service(errorRate: 1).RetryPendingAsync();  // attempt 3

        var saved = await Db.Messages.SingleAsync();
        Assert.Equal(MessageStatus.Failed, saved.Status);
        Assert.Equal(3, saved.Attempts);
    }

    private readonly SendQueue _queue = new();

    private MessageService Service(double errorRate = 0, int retryBatchSize = 50) => new(
        Db,
        new FakeSmsProvider(Options.Create(new FakeSmsProviderOptions { ErrorRate = errorRate, MaxLatencyMs = 0 })),
        _queue,
        Options.Create(new SmsWorkerOptions { RetryIntervalMinutes = 1, MaxAttempts = 3, RetryBatchSize = retryBatchSize }));

    private async Task FailedFirstAttempt()
    {
        var id = (await Service().CreateAsync(SarahRequest())).Message!.Id;
        await Service(errorRate: 1).SendAsync(id);
    }

    private async Task<List<int>> Queued()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var ids = new List<int>();
        try
        {
            await foreach (var id in _queue.ReadAllAsync(cts.Token))
                ids.Add(id);
        }
        catch (OperationCanceledException) { }
        return ids;
    }

    // Pretends the last attempt was 10 minutes ago, instead of waiting for the real retry interval.
    private async Task MakeLastAttemptOld()
    {
        foreach (var m in Db.Messages)
            m.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        await Db.SaveChangesAsync();
    }

    private static SendMessageRequest SarahRequest() => Request("שרה", "כהן", "הנשיא 5, ירושלים", "0522222223");

    private static SendMessageRequest Request(string first, string last, string address, string phone) => new()
    {
        To = new RecipientDto { FirstName = first, LastName = last, Address = address, Phone = phone },
        Body = "תזכורת: ישיבת צוות מחר ב-9:00"
    };
}
