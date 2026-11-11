namespace CLIHub.Core.Tests.Fakes;

using System.Collections.Concurrent;

internal sealed class QueueingSynchronizationContext : SynchronizationContext
{
    private readonly ConcurrentQueue<Action> posted = new();
    private readonly TaskCompletionSource firstPost = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int PendingCount => this.posted.Count;

    public Task FirstPost => this.firstPost.Task;

    public override void Post(SendOrPostCallback d, object? state)
    {
        this.posted.Enqueue(() => d(state));
        this.firstPost.TrySetResult();
    }

    public int RunPending()
    {
        var count = 0;
        while (this.posted.TryDequeue(out var callback))
        {
            callback();
            count++;
        }

        return count;
    }
}
