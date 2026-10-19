namespace CLIHub.Composition;

using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core;
using CLIHub.Core.Windows;
using CLIHub.Core.Scanning;
using CLIHub.Core.Updates;
using CLIHub.Converters;
using CLIHub.Imaging;
using CLIHub.Hotkeys;
using CLIHub.ViewModels;
using CLIHub.Views;
using CLIHub.Updates;
using CLIHub.Settings;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Startup;
using CLIHub.Tray;

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
        => services.AddClIHubServices(guard: null);

    /// <summary>
    ///   Registers the WPF application's services (windows, view models, tray, hotkey).
    /// </summary>
    /// <param name="services"> The service collection to configure. </param>
    /// <param name="guard">
    ///   An already-acquired single-instance guard. When provided, it is registered so the provider
    ///   owns disposal; when omitted, a lazily constructed production guard is registered instead.
    /// </param>
    /// <returns> The configured service collection. </returns>
    public static IServiceCollection AddClIHubServices(this IServiceCollection services, SingleInstanceGuard? guard)
    {
        services.AddClIHubCoreServices(Path.Combine(AppContext.BaseDirectory, "default-logo.png"));
        services.AddWindowsInfrastructure(guard);

        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));

        services.AddSingleton<IApplicationLifetime, WpfApplicationLifetime>();
        services.AddSingleton<IApplicationOperationLifetime, ApplicationOperationLifetime>();
        services.AddSingleton<IInstanceCoordinator, InstanceCoordinator>();
        services.AddSingleton<IStartupStateLoader, StartupStateLoader>();
        services.AddSingleton<IOptionalStartupCoordinator, OptionalStartupCoordinator>();
        services.AddSingleton<IUpdateWorkflow, UpdateWorkflow>();
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
        services.AddSingleton<ProjectModelCatalog>();
        services.AddSingleton<LaunchCommandCoordinator>();
        services.AddSingleton<LaunchWindowActionBuilder>();
        services.AddSingleton<TrayMenuBuilder>();
        services.AddSingleton<TrayStateProjection>();
        services.AddSingleton<TrayCommandHandlers>();
        services.AddSingleton<ITrayActions, TrayActions>();
        services.AddSingleton<ProjectPaneController>();
        services.AddSingleton<AgentPaneController>();
        services.AddSingleton<LogoImageSourceCache>();
        services.AddSingleton<PathToImageConverter>();
        services.AddSingleton<UpdateControlViewModel>();
        services.AddSingleton<StatusMessageCoordinator>();
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

        services.AddSingleton<IPreferenceApplier, PreferenceApplier>();
        services.AddSingleton<IAccentPreferenceTarget, WpfAccentPreferenceTarget>();
        services.AddTransient<IAgentManagementSection, AgentManagementViewModel>();
        services.AddTransient<IProjectRegistrySection, ProjectRegistryViewModel>();
        services.AddSingleton<SettingsApplicationService>();
        services.AddSingleton<AccentSelectionMemory>();
        services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<SettingsApplicationService>(),
            sp.GetRequiredService<IUpdateVersionProvider>(),
            sp.GetRequiredService<IUpdateWorkflow>(),
            sp.GetRequiredService<IReleaseNotesLauncher>(),
            sp.GetRequiredService<IApplicationOperationLifetime>(),
            sp.GetRequiredService<IExternalLauncher>(),
            sp.GetRequiredService<IProjectDialogService>(),
            sp.GetRequiredService<IProjectScanner>(),
            sp.GetRequiredService<IAgentManagementSection>(),
            sp.GetRequiredService<IProjectRegistrySection>(),
            sp.GetRequiredService<AccentSelectionMemory>(),
            sp.GetRequiredService<ILogger<SettingsViewModel>>()));
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
