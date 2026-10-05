namespace CLIHub;

using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Hotkeys;
using CLIHub.ViewModels;
using CLIHub.Views;

using Serilog;

/// <summary>
///   Registers CLIHub services for the WPF application.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///   Registers the WPF application's services (windows, view models, tray, hotkey).
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <returns> The configured service collection. </returns>
    public static IServiceCollection AddClIHubServices(this IServiceCollection services)
    {
        services.AddClIHubCoreServices(
            Path.Combine(AppContext.BaseDirectory, "default-project.png"));

        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));

        services.AddSingleton<SingleInstanceGuard>();
        services.AddSingleton<IPluginInitializationService, PluginInitializationService>();
        services.AddSingleton<IStartupPreferencesApplier, StartupPreferencesApplier>();
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

        services.AddSingleton<WhatsNewViewModel>();
        services.AddTransient<WhatsNewWindow>();
        services.AddSingleton<Func<WhatsNewWindow>>(sp => () => sp.GetRequiredService<WhatsNewWindow>());
        services.AddSingleton<IReleaseNotesLauncher, ReleaseNotesLauncher>();

        return services;
    }
}
