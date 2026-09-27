using CLIHub.Core.Interfaces;
using CLIHub.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CLIHub.Core;

/// <summary>
/// Registers the CLIHub core services (no UI dependencies).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration, project, plugin, and process-launch services as singletons.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="defaultLogoPath">Path to the fallback project logo, or null.</param>
    public static IServiceCollection AddClIHubCoreServices(
        this IServiceCollection services,
        string? defaultLogoPath = null)
    {
        services.AddSingleton<IConfigService, ConfigService>();

        services.AddSingleton<IProjectService>(sp => new ProjectService(
            sp.GetRequiredService<IConfigService>())
        {
            DefaultLogoPath = defaultLogoPath
        });

        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();

        return services;
    }
}
