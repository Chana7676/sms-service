using SmsService.Api.Data.Entities;

namespace SmsService.Api.Contracts;

/// <summary>
/// What the API returns for a message. Internal fields (EmployeeId, ProviderMessageId, ...) are not exposed.
/// </summary>
public record MessageResponse(
    int Id,
    RecipientDto To,
    string Body,
    string Status,
    string? Error,
    int Attempts,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // Expects message.Employee to be loaded.
    public static MessageResponse From(Message message) => new(
        message.Id,
        new RecipientDto
        {
            FirstName = message.Employee.FirstName,
            LastName = message.Employee.LastName,
            Address = message.Employee.Address,
            Phone = message.Phone
        },
        message.Body,
        message.Status.ToString(),
        message.LastError,
        message.Attempts,
        message.CreatedAt,
        message.UpdatedAt);
}
