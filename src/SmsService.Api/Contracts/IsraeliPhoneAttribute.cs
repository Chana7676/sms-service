using System.ComponentModel.DataAnnotations;
using SmsService.Api.Services;

namespace SmsService.Api.Contracts;

/// <summary>
/// Validation attribute that delegates to PhoneNumber, so all phone rules live in one place.
/// A missing or empty value is handled by [Required], not here.
/// </summary>
public class IsraeliPhoneAttribute : ValidationAttribute
{
    public IsraeliPhoneAttribute() : base("Phone must be an Israeli mobile number: 05XXXXXXXX.") { }

    public override bool IsValid(object? value) =>
        value is not string phone || phone.Length == 0 || PhoneNumber.IsValid(phone);
}
