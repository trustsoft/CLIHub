namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.ViewModels;
using CLIHub.Lifetime;
using CLIHub.Launching;
using CLIHub.Tests.Builders;

using static CLIHub.Tests.Support.TestHelpers;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void Dispose_CancelsInFlightPopulation()
    {
        var a = Launchable("a");
        var versionCompletion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var capturedToken = CancellationToken.None;
        var version = new Mock<IAgentVersionService>();
        version
            .Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, token) =>
            {
                capturedToken = token;
                return versionCompletion.Task;
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(version).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);

        viewModel.Dispose();
        versionCompletion.SetResult("1.0");

        Assert.True(capturedToken.IsCancellationRequested);
        Assert.Null(viewModel.Cards[0].Version);
    }


    [Fact]
    public void Dispose_CancelsBlockedStatePass()
    {
        var a = Launchable("a", withUpdate: true);
        var gate = new ManualResetEventSlim(false);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(GatedProcesses(gate, new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now))).WithOperationLifetime(new DeferredOperationLifetime()).Build();

        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        viewModel.Dispose();
        gate.Set();

        // Give a broken guard time to manifest: after Dispose the pass's token and generation
        // are stale, so its blocking write must never reach the card.
        SpinWait.SpinUntil(() => !card.CanUpdate, TimeSpan.FromMilliseconds(250));

        Assert.True(card.CanUpdate);
        Assert.Null(card.Message);
    }


    [Fact]
    public void Dispose_WhilePerCardCheckHeldOpen_DiscardsWritesAndResetsFlags()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: complete it after Dispose
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.CheckCardUpdatesCommand.Execute(card);
        Assert.True(viewModel.IsCardActionRunning);

        viewModel.Dispose();
        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"));

        Assert.True(SpinWait.SpinUntil(
            () => !viewModel.IsCardActionRunning && !card.IsCheckingUpdates,
            TimeSpan.FromSeconds(5)));

        // The late result and message writes are discarded; both busy flags still reset.
        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.Null(card.Message);
    }

}