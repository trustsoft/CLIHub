namespace CLIHub;

using CLIHub.Core;
using CLIHub.Core.Services;
using CLIHub.Hotkeys;
using CLIHub.ViewModels;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;

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
        services.AddSingleton<PromptState>();
        services.AddSingleton<LaunchWindowViewModel>();
        services.AddSingleton<LaunchWindow>();
        services.AddSingleton<TrayIconController>();

        services.AddSingleton<GlobalHotkeyService>(sp => new GlobalHotkeyService(
            sp.GetRequiredService<LaunchWindow>(),
            () => sp.GetRequiredService<TrayIconController>().ToggleLaunchWindow(),
            sp.GetRequiredService<ILogger<GlobalHotkeyService>>()));

        services.AddSingleton<IPreferenceApplier, PreferenceApplier>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => () => sp.GetRequiredService<SettingsWindow>());
        services.AddSingleton<ISettingsLauncher, SettingsLauncher>();

        services.AddTransient<WhatsNewViewModel>();
        services.AddTransient<WhatsNewWindow>();
        services.AddSingleton<Func<WhatsNewWindow>>(sp => () => sp.GetRequiredService<WhatsNewWindow>());
        services.AddSingleton<IReleaseNotesLauncher, ReleaseNotesLauncher>();

        return services;
    }
}
