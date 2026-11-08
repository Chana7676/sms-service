namespace SmsService.Api.Data.Entities;

public class Message
{
    public int Id { get; set; }

    // Who the message was sent to (an employee instance).
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Snapshot of the number we actually sent to, even if the employee row changes later.
    public string Phone { get; set; } = "";

    public string Body { get; set; } = "";
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    // The provider's id for the message (once it accepted it).
    public string? ProviderMessageId { get; set; }

    // How many times we tried to send, and why the last try failed.
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
