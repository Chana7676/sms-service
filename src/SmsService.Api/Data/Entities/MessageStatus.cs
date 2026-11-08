namespace SmsService.Api.Data.Entities;

/// <summary>
/// Pending -> Sent, or Pending -> Failed after the last retry.
/// Sent = the provider accepted the message (delivery to the phone is not tracked).
/// The numeric values are the Ids in the MessageStatuses lookup table.
/// </summary>
public enum MessageStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

/// <summary>
/// Row in the MessageStatuses lookup table, so the DB itself rejects an unknown status.
/// </summary>
public class MessageStatusInfo
{
    public MessageStatus Id { get; set; }
    public string Name { get; set; } = "";
}
