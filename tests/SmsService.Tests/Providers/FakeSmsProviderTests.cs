using Microsoft.Extensions.Options;
using SmsService.Api.Providers;

namespace SmsService.Tests.Providers;

public class FakeSmsProviderTests
{
    [Fact]
    public async Task Accepts_and_returns_an_id_when_error_rate_is_0() =>
        Assert.False(string.IsNullOrEmpty(await Provider(errorRate: 0).SendAsync("0522222223", "שלום")));

    [Fact]
    public async Task Throws_when_error_rate_is_1() =>
        await Assert.ThrowsAsync<SmsProviderException>(() => Provider(errorRate: 1).SendAsync("0522222223", "שלום"));

    private static FakeSmsProvider Provider(double errorRate) =>
        new(Options.Create(new FakeSmsProviderOptions { ErrorRate = errorRate, MaxLatencyMs = 0 }));
}
