namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub;

public class ApplicationOperationLifetimeTests
{
    [Fact]
    public async Task RunAsync_PassesApplicationTokenAndCompletes()
    {
        using var lifetime = new ApplicationOperationLifetime(
            NullLogger<ApplicationOperationLifetime>.Instance);
        CancellationToken receivedToken = default;

        await lifetime.RunAsync(
            "test",
            token =>
            {
                receivedToken = token;
                return Task.CompletedTask;
            });

        Assert.Equal(lifetime.Token, receivedToken);
        await lifetime.StopAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task StopAsync_CancelsAndAwaitsTrackedOperation()
    {
        using var lifetime = new ApplicationOperationLifetime(
            NullLogger<ApplicationOperationLifetime>.Instance);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var operation = lifetime.RunAsync(
            "test",
            async token =>
            {
                started.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled.SetResult();
                    throw;
                }
            });

        await started.Task;
        await lifetime.StopAsync(TimeSpan.FromSeconds(1));
        await operation;

        Assert.True(cancelled.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task RunAsync_ObservesUnexpectedException()
    {
        using var lifetime = new ApplicationOperationLifetime(
            NullLogger<ApplicationOperationLifetime>.Instance);

        var exception = await Record.ExceptionAsync(
            () => lifetime.RunAsync(
                "test",
                _ => Task.FromException(new InvalidOperationException("test"))));

        Assert.Null(exception);
        await lifetime.StopAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task StopAsync_IsIdempotentAndBoundsCancellationWait()
    {
        using var lifetime = new ApplicationOperationLifetime(
            NullLogger<ApplicationOperationLifetime>.Instance);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = lifetime.RunAsync("test", _ => release.Task);

        var firstStop = lifetime.StopAsync(TimeSpan.FromMilliseconds(10));
        var secondStop = lifetime.StopAsync(TimeSpan.FromSeconds(1));

        Assert.Same(firstStop, secondStop);
        await firstStop;

        release.SetResult();
        await operation;
    }
}
