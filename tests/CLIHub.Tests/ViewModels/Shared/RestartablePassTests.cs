namespace CLIHub.Tests.ViewModels.Shared;

using CLIHub.ViewModels;

public class RestartablePassTests
{
    [Fact]
    public void Begin_ReturnsLeaseWithIncrementedGeneration()
    {
        using var pass = new RestartablePass();

        Assert.Equal(1, pass.Begin().Generation);
        Assert.Equal(2, pass.Begin().Generation);
    }

    [Fact]
    public void Begin_CancelsPreviousToken()
    {
        using var pass = new RestartablePass();
        var first = pass.Begin();

        pass.Begin();

        Assert.True(first.RefreshToken.IsCancellationRequested);
    }

    [Fact]
    public void IsCurrent_ReturnsTrueWhenGenerationMatchesAndTokenNotCancelled()
    {
        using var pass = new RestartablePass();
        var lease = pass.Begin();

        Assert.True(pass.IsCurrent(lease.Generation, lease.RefreshToken));
    }

    [Fact]
    public void IsCurrent_ReturnsFalseWhenSuperseded()
    {
        using var pass = new RestartablePass();
        var first = pass.Begin();
        pass.Begin();

        Assert.False(pass.IsCurrent(first.Generation, first.RefreshToken));
    }

    [Fact]
    public void IsCurrent_ReturnsFalseWhenTokenCancelled()
    {
        using var pass = new RestartablePass();
        var lease = pass.Begin();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lease.RefreshToken);
        linked.Cancel();

        Assert.False(pass.IsCurrent(lease.Generation, linked.Token));
    }

    [Fact]
    public void Dispose_InvalidatesGenerationAndCancelsToken()
    {
        using var pass = new RestartablePass();
        var lease = pass.Begin();

        pass.Dispose();

        Assert.False(pass.IsCurrent(lease.Generation, lease.RefreshToken));
        Assert.True(lease.RefreshToken.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var pass = new RestartablePass();
        var first = pass.Begin();

        pass.Dispose();
        pass.Dispose();

        Assert.True(first.RefreshToken.IsCancellationRequested);
        Assert.Equal(4, pass.Begin().Generation);
    }
}
