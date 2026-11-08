using System.ComponentModel.DataAnnotations;

namespace SmsService.Api.Contracts;

// Fields are non-nullable with empty defaults: a field missing from the JSON stays "" and fails [Required] (400),
// so the service can use the values without null checks.
public class SendMessageRequest
{
    [Required]
    public RecipientDto To { get; set; } = new();

    // 335 = 5 SMS parts in Hebrew (UCS-2: 67 characters per part).
    [Required, MaxLength(335)]
    public string Body { get; set; } = "";
}

/// <summary>
/// An employee instance: who (first name + last name + address) and which phone.
/// </summary>
public class RecipientDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required, MaxLength(100)]
    public string Address { get; set; } = "";

    [Required, IsraeliPhone]
    public string Phone { get; set; } = "";
}
