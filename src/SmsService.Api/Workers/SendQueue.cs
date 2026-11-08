using System.Threading.Channels;

namespace SmsService.Api.Workers;

/// <summary>
/// In-memory queue of message ids waiting to be sent: POST /messages puts an id in, SmsWorker takes it out.
/// It is lost on restart, which is fine: the message is Pending in the DB and the retry round picks it up.
/// </summary>
public class SendQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int messageId) => _channel.Writer.TryWrite(messageId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
