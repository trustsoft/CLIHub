namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

public class StartupPreferencesApplierTests
{
    [Fact]
    public void Apply_SetsRuntimeBeforeStartupRegistration()
    {
        var sequence = new MockSequence();
        var processLauncher = CreateProcessLauncher();
        var startupService = new Mock<IStartupService>(MockBehavior.Strict);

        startupService.InSequence(sequence).Setup(x => x.SetEnabled(true)).Returns(true);

        var applier = new StartupPreferencesApplier(processLauncher, startupService.Object);

        applier.Apply(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.PowerShell,
            StartWithWindows = true
        });

        Assert.Equal(RuntimeKind.PowerShell, processLauncher.GetRuntime());
        startupService.Verify(x => x.SetEnabled(true), Times.Once);
    }

    [Fact]
    public void AddClIHubServices_RegistersStartupPreferencesApplier()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<StartupPreferencesApplier>(provider.GetRequiredService<IStartupPreferencesApplier>());
    }

    private static ProcessLauncher CreateProcessLauncher()
    {
        return new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new RuntimeSelector(),
            new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance),
            new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance));
    }
}
