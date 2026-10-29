namespace CLIHub.Core.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;

/// <summary>
///   Registers the Windows platform infrastructure: process runners, runtime executable resolution,
///   agent process inspection, per-user startup registration, and the single-instance guard.
/// </summary>
public static class WindowsServiceCollectionExtensions
{
    /// <summary>
    ///   Adds the Windows platform adapters as singletons. Combine with <c> AddClIHubCoreServices </c>,
    ///   which registers the platform-neutral services and the process ports these adapters implement,
    ///   including runtime executable resolution.
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <returns> The configured service collection. </returns>
    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
        => services.AddWindowsInfrastructure(guard: null);

    /// <summary>
    ///   Adds the Windows platform adapters as singletons. Combine with <c> AddClIHubCoreServices </c>,
    ///   which registers the platform-neutral services and the process ports these adapters implement.
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <param name="guard">
    ///   An already-acquired single-instance guard. When provided, it is registered so the provider
    ///   owns disposal; when omitted, a lazily constructed production guard is registered instead.
    /// </param>
    /// <returns> The configured service collection. </returns>
    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services, SingleInstanceGuard? guard)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IInteractiveProcessRunner, WindowsInteractiveProcessRunner>();
        services.AddSingleton<IProcessOutputRunner, WindowsProcessOutputRunner>();
        services.AddSingleton<IRuntimeExecutableLocator>(sp => new WindowsRuntimeLocator(
            new WindowsRuntimeEnvironment(),
            sp.GetRequiredService<ILogger<WindowsRuntimeLocator>>()));
        services.AddSingleton<IRuntimeResolver>(sp => new RuntimeResolver(
            sp.GetRequiredService<IRuntimeExecutableLocator>()));
        services.AddSingleton<ICommandLineBuilder, CommandLineBuilderAdapter>();
        services.AddSingleton<IAgentProcessInspector>(sp => new WindowsAgentProcessInspector(
            sp.GetRequiredService<ILogger<WindowsAgentProcessInspector>>(),
            new WmiProcessCommandLineSource(sp.GetRequiredService<ILogger<WmiProcessCommandLineSource>>())));
        services.AddSingleton<IAgentProcessFinder>(sp => new AgentProcessFinder(
            sp.GetRequiredService<IAgentProcessInspector>(),
            new AgentProcessMatcher(),
            sp.GetRequiredService<IPluginCatalog>()));
        services.AddSingleton<IProcessOrchestrator>(sp => new ProcessOrchestrator(
            sp.GetRequiredService<IRuntimeResolver>(),
            sp.GetRequiredService<ICommandLineBuilder>(),
            sp.GetRequiredService<IInteractiveProcessRunner>(),
            sp.GetRequiredService<IProcessOutputRunner>(),
            sp.GetRequiredService<IAgentProcessFinder>(),
            sp.GetRequiredService<ILoggingContextProvider>(),
            sp.GetRequiredService<ILogger<ProcessOrchestrator>>()));
        services.AddSingleton<IStartupService>(sp => new StartupService(
            new CurrentUserRegistryStartup(),
            sp.GetRequiredService<ILogger<StartupService>>()));

        if (guard is null)
        {
            services.AddSingleton<SingleInstanceGuard>();
        }
        else
        {
            services.AddSingleton(guard);
        }

        services.AddSingleton<ISingleInstanceGuard>(sp => sp.GetRequiredService<SingleInstanceGuard>());
        return services;
    }
}
