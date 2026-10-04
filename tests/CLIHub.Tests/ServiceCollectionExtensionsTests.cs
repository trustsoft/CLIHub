namespace CLIHub.Tests;

using CLIHub.Core;
using CLIHub.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClIHubCoreServices_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IConfigService>());
        Assert.NotNull(provider.GetRequiredService<ILogoCacheService>());
        Assert.NotNull(provider.GetRequiredService<IProjectService>());
        Assert.NotNull(provider.GetRequiredService<IPluginManager>());
        Assert.NotNull(provider.GetRequiredService<IProcessLauncher>());
        Assert.NotNull(provider.GetRequiredService<IReleaseNotesService>());
    }

    [Fact]
    public void ConfigService_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IConfigService>();
        var second = provider.GetRequiredService<IConfigService>();

        Assert.Same(first, second);
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
