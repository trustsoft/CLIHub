namespace CLIHub.Core.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Registers the Windows platform infrastructure: process runners, agent process
///   inspection, per-user startup registration, and the single-instance guard.
/// </summary>
public static class WindowsServiceCollectionExtensions
{
    /// <summary>
    ///   Adds the Windows platform adapters as singletons. Combine with <c> AddClIHubCoreServices </c>,
    ///   which registers the platform-neutral services and the process ports these adapters implement.
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <returns> The configured service collection. </returns>
    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<RuntimeSelector>();
        services.AddSingleton<IInteractiveProcessRunner, WindowsInteractiveProcessRunner>();
        services.AddSingleton<IProcessOutputRunner, WindowsProcessOutputRunner>();
        services.AddSingleton<ProcessLauncher>();
        services.AddSingleton<IAgentProcessInspector, WindowsAgentProcessInspector>();
        services.AddSingleton<IStartupService>(sp => new StartupService(
            new CurrentUserRegistryStartup(),
            sp.GetRequiredService<ILogger<StartupService>>()));
        services.AddSingleton<SingleInstanceGuard>();
        services.AddSingleton<ISingleInstanceGuard>(sp => sp.GetRequiredService<SingleInstanceGuard>());
        return services;
    }
}
