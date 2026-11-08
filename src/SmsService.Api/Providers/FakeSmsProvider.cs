using Microsoft.Extensions.Options;

namespace SmsService.Api.Providers;

public class FakeSmsProviderOptions
{
    /// <summary>Share of calls that fail, like a 500 / timeout from a real provider.</summary>
    public double ErrorRate { get; set; } = 0.1;

    public int MinLatencyMs { get; set; } = 50;
    public int MaxLatencyMs { get; set; } = 300;
}

/// <summary>
/// Stand-in for the SMS provider (the real mock server was not received).
/// Simulates network latency and random provider errors.
/// </summary>
public class FakeSmsProvider(IOptions<FakeSmsProviderOptions> options) : ISmsProvider
{
    private readonly FakeSmsProviderOptions _options = options.Value;

    public async Task<string> SendAsync(string phone, string text, CancellationToken ct = default)
    {
        if (_options.MaxLatencyMs > 0)
            await Task.Delay(Random.Shared.Next(_options.MinLatencyMs, _options.MaxLatencyMs + 1), ct);

        if (Random.Shared.NextDouble() < _options.ErrorRate)
            throw new SmsProviderException("Provider unavailable (simulated).");

        return Guid.NewGuid().ToString("N");
    }
}
