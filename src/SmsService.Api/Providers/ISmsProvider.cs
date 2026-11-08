namespace SmsService.Api.Providers;

/// <summary>
/// The contract with an SMS provider. The rest of the system only knows this interface,
/// so switching to a real provider (or adding a second one) means a new class + one DI registration.
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Hands the SMS to the provider and returns the provider's id for it.
    /// Success means the provider accepted it, not that it reached the phone.
    /// Throws SmsProviderException when the provider fails.
    /// </summary>
    Task<string> SendAsync(string phone, string text, CancellationToken ct = default);
}

/// <summary>The provider could not handle the request (server error, timeout, ...). Worth retrying.</summary>
public class SmsProviderException(string message) : Exception(message);
