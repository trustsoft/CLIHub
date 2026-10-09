namespace CLIHub.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;

/// <summary>
///   Registers the CLIHub core services (no UI dependencies).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///   Adds configuration, project, plugin, and process-launch services as singletons.
    ///   Logging is registered so services can resolve <c>ILogger&lt;T&gt;</c>; the host may
    ///   add providers (for example Serilog) to the same logging pipeline.
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <param name="defaultLogoPath"> The path to the fallback project logo, or null. </param>
    public static IServiceCollection AddClIHubCoreServices(
        this IServiceCollection services,
        string? defaultLogoPath = null)
    {
        services.AddLogging();

        services.AddConfigurationServices();
        services.AddProjectServices(defaultLogoPath);
        services.AddPluginServices();
        services.AddAgentServices();
        services.AddUpdateServices();
        services.AddWindowsInfrastructureServices();

        return services;
    }

    private static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigMigration, LegacyTerminalPreferenceMigration>();
        services.AddSingleton<IConfigMigrationRunner, ConfigMigrationRunner>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
        services.AddSingleton<IPreferencesStore, PreferencesStore>();
        return services;
    }

    private static IServiceCollection AddProjectServices(
        this IServiceCollection services,
        string? defaultLogoPath)
    {
        services.AddSingleton<IProjectStateStore, ProjectStateStore>();
        services.AddSingleton<ILogoCacheService, LogoCacheService>();
        services.AddSingleton<IProjectService>(sp => new ProjectService(
            sp.GetRequiredService<IProjectStateStore>(),
            sp.GetRequiredService<ILogoCacheService>(),
            sp.GetRequiredService<ILogger<ProjectService>>())
        {
            DefaultLogoPath = defaultLogoPath
        });
        return services;
    }

    private static IServiceCollection AddPluginServices(this IServiceCollection services)
    {
        services.AddSingleton<IPluginDescriptorReader, PluginDescriptorReader>();
        services.AddSingleton<IPluginDescriptorValidator, PluginDescriptorValidator>();
        services.AddSingleton<PluginCatalog>();
        services.AddSingleton<IPluginCatalog>(sp => sp.GetRequiredService<PluginCatalog>());
        services.AddSingleton<IPluginSeeder, PluginSeeder>();
        return services;
    }

    private static IServiceCollection AddAgentServices(this IServiceCollection services)
    {
        services.AddSingleton<IAgentCommandService, AgentCommandService>();
        services.AddSingleton<IAgentDetectionService>(sp => new AgentDetectionService(
            sp.GetRequiredService<IPreferencesStore>()));
        services.AddSingleton<IAgentVersionService>(sp => new AgentVersionService(
            sp.GetRequiredService<ProcessLauncher>(),
            sp.GetRequiredService<IPreferencesStore>(),
            sp.GetRequiredService<ILogger<AgentVersionService>>()));
        services.AddSingleton<IAgentProcessInspector, WindowsAgentProcessInspector>();
        services.AddSingleton<AgentProcessMatcher>();
        services.AddSingleton<Services.AgentProcessMonitor>();
        return services;
    }

    private static IServiceCollection AddUpdateServices(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IUpdateVersionProvider>(sp => sp.GetRequiredService<IUpdateService>());
        services.AddSingleton<IUpdateChecker>(sp => sp.GetRequiredService<IUpdateService>());
        services.AddSingleton<IUpdateStateSource>(sp => sp.GetRequiredService<IUpdateService>());
        services.AddSingleton<IUpdateDownloader>(sp => sp.GetRequiredService<IUpdateService>());
        services.AddSingleton<IUpdateInstaller>(sp => sp.GetRequiredService<IUpdateService>());
        services.AddSingleton<IReleaseNotesService, ReleaseNotesService>();
        return services;
    }

    private static IServiceCollection AddWindowsInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<RuntimeSelector>();
        services.AddSingleton<IInteractiveProcessRunner, WindowsInteractiveProcessRunner>();
        services.AddSingleton<IProcessOutputRunner, WindowsProcessOutputRunner>();
        services.AddSingleton<ProcessLauncher>();
        services.AddSingleton<IStartupService>(sp => new StartupService(
            new CurrentUserRegistryStartup(),
            sp.GetRequiredService<ILogger<StartupService>>()));
        return services;
    }
}
