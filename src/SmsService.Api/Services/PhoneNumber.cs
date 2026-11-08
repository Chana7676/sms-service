using System.Text.RegularExpressions;

namespace SmsService.Api.Services;

/// <summary>
/// Israeli mobile numbers: 05XXXXXXXX (10 digits). Dashes and spaces are accepted on input.
/// </summary>
public static partial class PhoneNumber
{
    [GeneratedRegex(@"^05\d{8}$")]
    private static partial Regex ValidFormat();

    public static string Normalize(string phone) =>
        phone.Replace("-", "").Replace(" ", "").Trim();

    public static bool IsValid(string phone) =>
        ValidFormat().IsMatch(Normalize(phone));
}
