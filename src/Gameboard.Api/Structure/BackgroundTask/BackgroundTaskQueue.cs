using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

internal class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue()
    {
        // number of items the channel is permitted to store (this is currently pretty arbitrary)
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(5)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem is null)
            throw new ArgumentNullException(nameof(workItem));

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
