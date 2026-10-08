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
        services.AddSingleton<ISingleInstanceGuard>(sp => sp.GetRequiredService<SingleInstanceGuard>());
        services.AddSingleton<IApplicationLifetime, WpfApplicationLifetime>();
        services.AddSingleton<IApplicationOperationLifetime, ApplicationOperationLifetime>();
        services.AddSingleton<IApplicationSession, ApplicationSession>();
        services.AddSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        services.AddSingleton<IApplicationStartupUi, WpfApplicationStartupUi>();
        services.AddSingleton<Func<IApplicationStartupUi>>(sp =>
            () => sp.GetRequiredService<IApplicationStartupUi>());
        services.AddSingleton<Func<IHotkeyStartupRegistrar>>(sp =>
            () => sp.GetRequiredService<IHotkeyStartupRegistrar>());
        services.AddSingleton<IAgentCommandWorkflow, AgentCommandWorkflow>();
        services.AddSingleton<IPluginInitializationService, PluginInitializationService>();
        services.AddSingleton<IStartupPreferencesApplier, StartupPreferencesApplier>();
        services.AddSingleton<PromptState>();
        services.AddSingleton<IProjectDialogService, ProjectDialogService>();
        services.AddSingleton<IUserNotificationService, UserNotificationService>();
        services.AddSingleton<IExternalLauncher, ExternalLauncher>();
        services.AddSingleton<LaunchCommandCoordinator>();
        services.AddSingleton<LaunchWindowActionBuilder>();
        services.AddSingleton<TrayMenuBuilder>();
        services.AddSingleton<TrayStateProjection>();
        services.AddSingleton<TrayCommandHandlers>();
        services.AddSingleton<ITrayActions, TrayActions>();
        services.AddSingleton<ProjectPaneController>();
        services.AddSingleton<AgentPaneController>();
        services.AddSingleton<UpdateControlViewModel>();
        services.AddSingleton<LaunchWindowViewModel>();
        services.AddSingleton<IPathDisplayStyleTarget>(sp => sp.GetRequiredService<LaunchWindowViewModel>());
        services.AddSingleton<LaunchWindow>();
        services.AddSingleton<TrayIconController>();
        services.AddSingleton<ITrayHost>(sp => sp.GetRequiredService<TrayIconController>());

        services.AddSingleton<GlobalHotkeyService>(sp => new GlobalHotkeyService(
            sp.GetRequiredService<LaunchWindow>(),
            () => sp.GetRequiredService<TrayIconController>().ToggleLaunchWindow(),
            sp.GetRequiredService<ILogger<GlobalHotkeyService>>()));
        services.AddSingleton<IGlobalHotkeyService>(sp => sp.GetRequiredService<GlobalHotkeyService>());
        services.AddSingleton<IRuntimePreferenceTarget, ProcessRuntimePreferenceTarget>();
        services.AddSingleton<IHotkeyStartupRegistrar, HotkeyStartupRegistrar>();
        services.AddSingleton<IReleaseNotesStartupCoordinator, ReleaseNotesStartupCoordinator>();
        services.AddSingleton<IUpdateStartupCoordinator, UpdateStartupCoordinator>();
        services.AddSingleton<IUpdateDownloadNotifier, UpdateDownloadNotifier>();
        services.AddSingleton<IUpdateDownloadCoordinator, UpdateDownloadCoordinator>();
        services.AddSingleton<Func<IUpdateDownloadCoordinator>>(sp =>
            () => sp.GetRequiredService<IUpdateDownloadCoordinator>());

        services.AddSingleton<IPreferenceApplier, PreferenceApplier>();
        services.AddSingleton<SettingsApplicationService>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => () => sp.GetRequiredService<SettingsWindow>());
        services.AddSingleton<ISettingsLauncher, SettingsLauncher>();

        services.AddSingleton<WhatsNewViewModel>();
        services.AddSingleton<Func<IUpdateRequestSource>>(sp =>
            () => sp.GetRequiredService<WhatsNewViewModel>());
        services.AddTransient<WhatsNewWindow>();
        services.AddSingleton<Func<WhatsNewWindow>>(sp => () => sp.GetRequiredService<WhatsNewWindow>());
        services.AddSingleton<IReleaseNotesLauncher, ReleaseNotesLauncher>();

        return services;
    }
}
