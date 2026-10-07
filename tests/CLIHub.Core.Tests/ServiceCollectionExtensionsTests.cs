namespace CLIHub.Tests;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClIHubCoreServices_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IConfigurationRepository>());
        Assert.NotNull(provider.GetRequiredService<IPreferencesStore>());
        Assert.NotNull(provider.GetRequiredService<ILogoCacheService>());
        Assert.NotNull(provider.GetRequiredService<IProjectService>());
        Assert.NotNull(provider.GetRequiredService<IPluginManager>());
        Assert.NotNull(provider.GetRequiredService<IPluginCatalog>());
        Assert.NotNull(provider.GetRequiredService<IPluginDescriptorReader>());
        Assert.NotNull(provider.GetRequiredService<IPluginDescriptorValidator>());
        Assert.NotNull(provider.GetRequiredService<IAgentCommandService>());
        Assert.NotNull(provider.GetRequiredService<IAgentDetectionService>());
        Assert.NotNull(provider.GetRequiredService<IAgentVersionService>());
        Assert.NotNull(provider.GetRequiredService<IInteractiveProcessRunner>());
        Assert.NotNull(provider.GetRequiredService<IProcessOutputRunner>());
        Assert.NotNull(provider.GetRequiredService<IProcessLauncher>());
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
    public void PluginManager_DelegatesToSharedCatalog()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetRequiredService<IPluginCatalog>();
        var manager = provider.GetRequiredService<IPluginManager>();

        Assert.Same(catalog, provider.GetRequiredService<PluginCatalog>());
        Assert.Same(provider.GetRequiredService<PluginManager>(), manager);
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
    public void ProjectService_UsesInjectedDefaultLogoPath()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices(defaultLogoPath: "custom-logo.png");

        using var provider = services.BuildServiceProvider();

        var projects = provider.GetRequiredService<IProjectService>();
        Assert.Equal("custom-logo.png", projects.DefaultLogoPath);
    }
}
