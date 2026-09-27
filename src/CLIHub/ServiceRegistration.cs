using CLIHub.Core;
using CLIHub.Core.Services;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
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

        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));

        services.AddSingleton<SingleInstanceGuard>();
        services.AddSingleton<TrayIconController>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
