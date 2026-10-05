namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

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
        var processRunner = new Mock<IInteractiveProcessRunner>(MockBehavior.Strict);
        var startupService = new Mock<IStartupService>(MockBehavior.Strict);

        processRunner.InSequence(sequence).Setup(x => x.SetRuntime(RuntimeKind.PowerShell));
        startupService.InSequence(sequence).Setup(x => x.SetEnabled(true)).Returns(true);

        var applier = new StartupPreferencesApplier(processRunner.Object, startupService.Object);

        applier.Apply(new AppPreferences
        {
            DefaultRuntime = "ps",
            StartWithWindows = true
        });

        processRunner.Verify(x => x.SetRuntime(RuntimeKind.PowerShell), Times.Once);
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
}
