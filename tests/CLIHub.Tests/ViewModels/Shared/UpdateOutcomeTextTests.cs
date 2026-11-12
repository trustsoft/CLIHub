namespace CLIHub.Tests.ViewModels.Shared;

using CLIHub.Core.Models;
using CLIHub.ViewModels;

public class UpdateOutcomeTextTests
{
    [Theory]
    [InlineData(UpdateStatus.UpdateAvailable, "Update available: 2.0 (current v1.0)")]
    [InlineData(UpdateStatus.UpToDate, "Up to date (v1.0)")]
    [InlineData(UpdateStatus.NotInstalled, "Updates apply to installed builds only.")]
    [InlineData(UpdateStatus.Failed, "Update check failed or timed out.")]
    public void ForCheck_KnownStatus_ReturnsSharedOutcomeText(UpdateStatus status, string expected)
    {
        Assert.Equal(expected, UpdateOutcomeText.ForCheck(new UpdateCheckResult(status, "1.0", "2.0")));
    }
}
