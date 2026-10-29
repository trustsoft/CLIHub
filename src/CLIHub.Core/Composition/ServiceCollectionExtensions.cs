namespace CLIHub.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.Core.Projects;
using CLIHub.Core.Scanning;
using CLIHub.Core.Updates;

/// <summary>
///   Registers the CLIHub core services (no UI dependencies).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///   Adds configuration, project, plugin, and process-launch services as singletons.
    ///   Logging is registered so services can resolve <c> ILogger&lt;T&gt; </c>; the host may
    ///   add providers (for example Serilog) to the same logging pipeline.
    ///   Windows platform adapters are registered separately via
    ///   <c> CLIHub.Core.Windows.WindowsServiceCollectionExtensions.AddWindowsInfrastructure </c>.
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <param name="defaultLogoPath"> The path to the fallback logo, or null. </param>
    public static IServiceCollection AddClIHubCoreServices(this IServiceCollection services,
                                                           string? defaultLogoPath = null)
    {
        services.AddLogging();
        services.AddSingleton<ILoggingContextProvider, LoggingContextProvider>();

        services.AddEventServices();
        services.AddConfigurationServices();
        services.AddLogoServices(defaultLogoPath);
        services.AddProjectServices();
        services.AddPluginServices();
        services.AddAgentServices();
        services.AddScanningServices();
        services.AddUpdateServices();

        return services;
    }

    private static IServiceCollection AddScanningServices(this IServiceCollection services)
    {
        services.AddSingleton<IRepositoryWatcherFactory, FileSystemRepositoryWatcherFactory>();
        services.AddSingleton<IScanTimerFactory, SystemScanTimerFactory>();
        services.AddSingleton<IProjectScanner>(sp => new ProjectScanningService(
            sp.GetRequiredService<IPreferencesStore>(),
            sp.GetRequiredService<IProjectService>(),
            sp.GetRequiredService<IPluginCatalog>(),
            sp.GetRequiredService<IRepositoryWatcherFactory>(),
            sp.GetRequiredService<IScanTimerFactory>(),
            sp.GetRequiredService<ILoggingContextProvider>(),
            sp.GetRequiredService<ILogger<ProjectScanningService>>()));
        return services;
    }

    private static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigMigration, LegacyTerminalPreferenceMigration>();
        services.AddSingleton<IConfigMigration, StartMinimizedMigration>();
        services.AddSingleton<IConfigMigrationRunner, ConfigMigrationRunner>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
        services.AddSingleton<IPreferencesStore, PreferencesStore>();
        return services;
    }

    /// <summary>
    ///   Registers the shared logo infrastructure: the fallback options, the persistent logo
    ///   cache, and both domain logo resolvers, all as singletons.
    /// </summary>
    private static IServiceCollection AddLogoServices(this IServiceCollection services,
                                                      string? defaultLogoPath)
    {
        services.AddSingleton(new LogoOptions(defaultLogoPath));
        services.AddSingleton<ILogoCacheService, LogoCacheService>();
        services.AddSingleton<ProjectLogoResolver>();
        services.AddSingleton<PluginLogoResolver>();
        return services;
    }

    private static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddSingleton<IProjectStateStore, ProjectStateStore>();
        services.AddSingleton<ProjectEnricher>();
        services.AddSingleton<IProjectService>(sp => new ProjectService(
            sp.GetRequiredService<IProjectStateStore>(),
            sp.GetRequiredService<ProjectLogoResolver>(),
            sp.GetRequiredService<ILogger<ProjectService>>(),
            sp.GetRequiredService<IDomainEventPublisher>()));
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
        services.AddSingleton<IAgentCommandService>(sp => new AgentCommandService(
            sp.GetRequiredService<IProcessOrchestrator>(),
            sp.GetRequiredService<IPreferencesStore>(),
            sp.GetRequiredService<ILogger<AgentCommandService>>(),
            sp.GetRequiredService<IDomainEventPublisher>()));
        services.AddSingleton<IAgentManagementService, AgentManagementService>();
        services.AddSingleton<IAgentDetectionService>(sp => new AgentDetectionService(
            sp.GetRequiredService<IPreferencesStore>()));
        services.AddSingleton<IAgentVersionService>(sp => new AgentVersionService(
            sp.GetRequiredService<IProcessOrchestrator>(),
            sp.GetRequiredService<IPreferencesStore>(),
            sp.GetRequiredService<ILogger<AgentVersionService>>()));
        services.AddSingleton(_ =>
        {
            var client = new HttpClient();

            // GitHub rejects requests without a User-Agent; identify every source request.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CLIHub");
            return client;
        });
        services.AddSingleton<IAgentReleaseClient, AgentReleaseClient>();
        services.AddSingleton<IAgentUpdateAvailabilityService, AgentUpdateAvailabilityService>();
        services.AddSingleton<AgentProcessMatcher>();
        services.AddSingleton<AgentProcessMonitor>();
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

    private static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddSingleton<DomainEventBus>();
        services.AddSingleton<IDomainEventSubscriber>(sp => sp.GetRequiredService<DomainEventBus>());
        services.AddSingleton<ProjectSelectionLoggingSubscriber>();
        services.AddSingleton<AgentLaunchAuditSubscriber>();

        // Subscribers self-subscribe in their constructors, and DI creates singletons lazily —
        // registering them alone would leave every subscriber unconstructed (and therefore
        // unsubscribed) forever. Resolving the publisher activates the subscriber singletons
        // exactly once; every publisher consumer (ProjectService, AgentCommandService) resolves
        // it at construction, so subscriptions exist before the first event is published.
        services.AddSingleton<IDomainEventPublisher>(sp =>
        {
            _ = sp.GetRequiredService<ProjectSelectionLoggingSubscriber>();
            _ = sp.GetRequiredService<AgentLaunchAuditSubscriber>();
            return sp.GetRequiredService<DomainEventBus>();
        });

        return services;
    }
}
