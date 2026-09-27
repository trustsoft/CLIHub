using CLIHub.Core;
using CLIHub.Core.Services;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace CLIHub;

/// <summary>
/// Registers CLIHub services for the WPF application.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddClIHubServices(this IServiceCollection services)
    {
        services.AddClIHubCoreServices(
            Path.Combine(AppContext.BaseDirectory, "default-project.png"));

        services.AddSingleton<SingleInstanceGuard>();
        services.AddSingleton<TrayIconController>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
