namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Tests for the launch window update control state transitions.
/// </summary>
public class UpdateControlLogicTests
{
    [Theory]
    [InlineData(UpdateControlState.Idle, false, false, UpdateControlState.Idle)]
    [InlineData(UpdateControlState.Idle, false, true, UpdateControlState.Available)]
    [InlineData(UpdateControlState.Idle, true, true, UpdateControlState.Downloading)]
    [InlineData(UpdateControlState.Downloading, false, false, UpdateControlState.Idle)]
    [InlineData(UpdateControlState.Downloading, false, true, UpdateControlState.Available)]
    [InlineData(UpdateControlState.Available, true, true, UpdateControlState.Downloading)]
    public void Derive_ServiceFacts_ChangeStateAsReported(
        UpdateControlState current,
        bool isDownloading,
        bool isUpdateAvailable,
        UpdateControlState expected)
    {
        var state = UpdateControlLogic.Derive(current, isDownloading, isUpdateAvailable);

        Assert.Equal(expected, state);
    }

    [Theory]
    [InlineData(UpdateControlState.Checking, false, false)]
    [InlineData(UpdateControlState.Checking, true, true)]
    [InlineData(UpdateControlState.ReadyToApply, false, true)]
    [InlineData(UpdateControlState.ReadyToApply, true, true)]
    public void Derive_SelfDrivenStates_AreNeverOverriddenByExternalFacts(
        UpdateControlState current,
        bool isDownloading,
        bool isUpdateAvailable)
    {
        var state = UpdateControlLogic.Derive(current, isDownloading, isUpdateAvailable);

        Assert.Equal(current, state);
    }

    [Fact]
    public void AfterDownload_Downloaded_BecomesReadyToApply()
    {
        var state = UpdateControlLogic.AfterDownload(UpdateDownloadStatus.Downloaded, false, true);

        Assert.Equal(UpdateControlState.ReadyToApply, state);
    }

    [Fact]
    public void AfterDownload_Failed_WithKnownUpdate_BecomesAvailable()
    {
        var state = UpdateControlLogic.AfterDownload(UpdateDownloadStatus.Failed, false, true);

        Assert.Equal(UpdateControlState.Available, state);
    }

    [Fact]
    public void AfterDownload_Failed_WithoutKnownUpdate_BecomesIdle()
    {
        var state = UpdateControlLogic.AfterDownload(UpdateDownloadStatus.Failed, false, false);

        Assert.Equal(UpdateControlState.Idle, state);
    }

    [Fact]
    public void AfterDownload_AlreadyDownloading_WhileStillDownloading_BecomesDownloading()
    {
        var state = UpdateControlLogic.AfterDownload(UpdateDownloadStatus.AlreadyDownloading, true, true);

        Assert.Equal(UpdateControlState.Downloading, state);
    }

    [Fact]
    public void AfterDownload_NotInstalled_BecomesIdle()
    {
        var state = UpdateControlLogic.AfterDownload(UpdateDownloadStatus.NotInstalled, false, false);

        Assert.Equal(UpdateControlState.Idle, state);
    }
}
