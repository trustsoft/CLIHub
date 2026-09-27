using CLIHub.Core.Interfaces;
using CLIHub.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CLIHub.Core;

/// <summary>
/// Registers the CLIHub core services (no UI dependencies).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration, project, plugin, and process-launch services as singletons.
    /// Logging is registered so services can resolve <c>ILogger&lt;T&gt;</c>; the host may
    /// add providers (for example Serilog) to the same logging pipeline.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="defaultLogoPath">Path to the fallback project logo, or null.</param>
    public static IServiceCollection AddClIHubCoreServices(
        this IServiceCollection services,
        string? defaultLogoPath = null)
    {
        services.AddLogging();

        services.AddSingleton<IConfigService, ConfigService>();

        services.AddSingleton<IProjectService>(sp => new ProjectService(
            sp.GetRequiredService<IConfigService>(),
            sp.GetRequiredService<ILogger<ProjectService>>())
        {
            DefaultLogoPath = defaultLogoPath
        });

        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddSingleton<IPluginSeeder, PluginSeeder>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        services.AddSingleton<IAgentCommandService, AgentCommandService>();
        services.AddSingleton<IAgentDetectionService, AgentDetectionService>();
        services.AddSingleton<IAgentVersionService, AgentVersionService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        return services;
    }
}
