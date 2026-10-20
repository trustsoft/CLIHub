namespace CLIHub.Core.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Processes;

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

        services.AddSingleton<RuntimeSelector>();
        services.AddSingleton<IInteractiveProcessRunner, WindowsInteractiveProcessRunner>();
        services.AddSingleton<IProcessOutputRunner, WindowsProcessOutputRunner>();
        services.AddSingleton<ProcessLauncher>();
        services.AddSingleton<IRuntimeExecutableLocator>(sp => new WindowsRuntimeLocator(
            new WindowsRuntimeEnvironment(),
            sp.GetRequiredService<ILogger<WindowsRuntimeLocator>>()));
        services.AddSingleton<IAgentProcessInspector, WindowsAgentProcessInspector>();
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
