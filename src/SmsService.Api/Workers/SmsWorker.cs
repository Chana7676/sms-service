using Microsoft.Extensions.Options;
using SmsService.Api.Services;

namespace SmsService.Api.Workers;

/// <summary>
/// Background worker inside the API process. Two jobs, run one at a time:
/// 1. Send: takes message ids from SendQueue and sends them right away.
/// 2. Retry: every RetryIntervalMinutes, sends again the messages still Pending (up to MaxAttempts in total).
/// </summary>
public class SmsWorker(
    SendQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<SmsWorkerOptions> options,
    ILogger<SmsWorker> logger) : BackgroundService
{
    // Keeps the two jobs from sending the same message at the same moment.
    private readonly SemaphoreSlim _oneAtATime = new(1, 1);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        options.Value.Enabled
            ? Task.WhenAll(SendQueuedAsync(stoppingToken), RetryPeriodicallyAsync(stoppingToken))
            : Task.CompletedTask;

    private async Task SendQueuedAsync(CancellationToken ct)
    {
        await foreach (var messageId in queue.ReadAllAsync(ct))
            await RunAsync(service => service.SendAsync(messageId, ct), ct);
    }

    private async Task RetryPeriodicallyAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.RetryIntervalMinutes));
        while (await timer.WaitForNextTickAsync(ct))
            await RunAsync(service => service.RetryPendingAsync(ct), ct);
    }

    private async Task RunAsync(Func<MessageService, Task> work, CancellationToken ct)
    {
        await _oneAtATime.WaitAsync(ct);
        try
        {
            // The worker lives for the whole app, but MessageService/DbContext are per unit of work.
            using var scope = scopeFactory.CreateScope();
            await work(scope.ServiceProvider.GetRequiredService<MessageService>());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Sending failed unexpectedly; the message stays Pending for the retry round.");
        }
        finally
        {
            _oneAtATime.Release();
        }
    }
}
