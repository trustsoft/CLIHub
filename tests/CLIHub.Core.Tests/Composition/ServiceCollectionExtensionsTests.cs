namespace CLIHub.Core.Tests.Composition;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Tests.Fakes;
using CLIHub.Core.Updates;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClIHubCoreServices_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();
        // The version probe resolves ProcessLauncher, which the Windows module normally
        // registers; stand in the runner fakes so the Core-only container can resolve the
        // agent chain under test here.
        services.AddSingleton<ProcessLauncher>(sp => new ProcessLauncher(
            sp.GetRequiredService<ILogger<ProcessLauncher>>(),
            new FakeInteractiveProcessRunner(),
            new FakeProcessOutputRunner(),
            new FakeRuntimeExecutableLocator()));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IConfigurationRepository>());
        Assert.NotNull(provider.GetRequiredService<IPreferencesStore>());
        Assert.NotNull(provider.GetRequiredService<ILogoCacheService>());
        Assert.NotNull(provider.GetRequiredService<LogoOptions>());
        Assert.NotNull(provider.GetRequiredService<ProjectLogoResolver>());
        Assert.NotNull(provider.GetRequiredService<PluginLogoResolver>());
        Assert.NotNull(provider.GetRequiredService<IProjectService>());
        Assert.NotNull(provider.GetRequiredService<IPluginCatalog>());
        Assert.NotNull(provider.GetRequiredService<IPluginDescriptorReader>());
        Assert.NotNull(provider.GetRequiredService<IPluginDescriptorValidator>());
        Assert.NotNull(provider.GetRequiredService<IAgentDetectionService>());
        Assert.NotNull(provider.GetRequiredService<IAgentReleaseClient>());
        Assert.NotNull(provider.GetRequiredService<IAgentUpdateAvailabilityService>());
        Assert.NotNull(provider.GetRequiredService<IReleaseNotesService>());
    }

    [Fact]
    public void AddClIHubCoreServices_MapsUpdatePortsToOneService()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<IUpdateService>();

        Assert.Same(service, provider.GetRequiredService<IUpdateVersionProvider>());
        Assert.Same(service, provider.GetRequiredService<IUpdateChecker>());
        Assert.Same(service, provider.GetRequiredService<IUpdateStateSource>());
        Assert.Same(service, provider.GetRequiredService<IUpdateDownloader>());
        Assert.Same(service, provider.GetRequiredService<IUpdateInstaller>());
    }

    [Fact]
    public void ConfigurationRepository_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IConfigurationRepository>();
        var second = provider.GetRequiredService<IConfigurationRepository>();

        Assert.Same(first, second);
        Assert.True(provider.GetRequiredService<IConfigMigrationRunner>().HasMigrations);
    }

    [Fact]
    public void LogoCacheService_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ILogoCacheService>();
        var second = provider.GetRequiredService<ILogoCacheService>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddClIHubCoreServices_DefaultLogoPath_FlowsIntoSharedLogoOptions()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices(defaultLogoPath: "custom-logo.png");

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<LogoOptions>();

        Assert.Equal("custom-logo.png", options.DefaultLogoPath);
        Assert.NotNull(provider.GetRequiredService<ProjectLogoResolver>());
        Assert.NotNull(provider.GetRequiredService<PluginLogoResolver>());
    }
}
