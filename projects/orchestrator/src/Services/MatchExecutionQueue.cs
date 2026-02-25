using System.Threading.Channels;

namespace SemanticPoker.Api.Services;

public class MatchExecutionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public async ValueTask EnqueueAsync(Guid matchId, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(matchId, ct);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}
