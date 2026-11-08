namespace CLIHub.Tests.Services;

using CLIHub.Composition;
using CLIHub.Core.Models;
using CLIHub.Core.Windows;
using CLIHub.Startup;

using Microsoft.Extensions.DependencyInjection;

using Moq;

public class StartupPreferencesApplierTests
{
    [Fact]
    public void Apply_CallsStartupServiceWithPreferenceValue()
    {
        var startupService = new Mock<IStartupService>();

        var applier = new StartupPreferencesApplier(startupService.Object);

        applier.Apply(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.PowerShell,
            StartWithWindows = true
        });

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
