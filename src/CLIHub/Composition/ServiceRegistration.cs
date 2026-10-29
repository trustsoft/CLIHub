namespace CLIHub.Composition;

using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core;
using CLIHub.Core.Windows;
using CLIHub.Core.Infrastructure.Processes;
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
using CLIHub.Themes;
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

        // Process orchestrator and adapters
        services.AddSingleton<CLIHub.Core.Processes.IRuntimeResolver, CLIHub.Core.Processes.RuntimeResolver>();
        services.AddSingleton<CLIHub.Core.Processes.ICommandLineBuilder, CLIHub.Core.Processes.CommandLineBuilderAdapter>();
        services.AddSingleton<CLIHub.Core.Processes.IAgentProcessFinder, CLIHub.Core.Processes.AgentProcessFinder>();
        services.AddSingleton<CLIHub.Core.Processes.IProcessOrchestrator, CLIHub.Core.Processes.ProcessOrchestrator>();

        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));

        services.AddStartupServices();
        services.AddLaunchWindowServices();
        services.AddTrayServices();
        services.AddHotkeyServices();
        services.AddSettingsServices();
        services.AddUpdateServices();
        services.AddSharedUiServices();

        return services;
    }

    /// <summary>
    ///   Registers the boot and shutdown pipeline: application lifetime and single-instance
    ///   coordination, startup state loading, the session and bootstrapper, the session-owned
    ///   Windows personalization theme follower, startup UI, plugin initialization, startup
    ///   preferences, and the optional startup coordinators executed during boot.
    /// </summary>
    private static IServiceCollection AddStartupServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationLifetime, WpfApplicationLifetime>();
        services.AddSingleton<IApplicationOperationLifetime, ApplicationOperationLifetime>();
        services.AddSingleton<IInstanceCoordinator, InstanceCoordinator>();
        services.AddSingleton<IStartupStateLoader, StartupStateLoader>();
        services.AddSingleton<IOptionalStartupCoordinator, OptionalStartupCoordinator>();
        services.AddSingleton<IApplicationSession, ApplicationSession>();
        services.AddSingleton<SystemThemeFollower>();
        services.AddSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        services.AddSingleton<IApplicationStartupUi, WpfApplicationStartupUi>();
        services.AddSingleton<Func<IApplicationStartupUi>>(sp =>
            () => sp.GetRequiredService<IApplicationStartupUi>());
        services.AddSingleton<IPluginInitializationService, PluginInitializationService>();
        services.AddSingleton<IStartupPreferencesApplier, StartupPreferencesApplier>();
        services.AddSingleton<IReleaseNotesStartupCoordinator, ReleaseNotesStartupCoordinator>();
        services.AddSingleton<IUpdateStartupCoordinator, UpdateStartupCoordinator>();
        return services;
    }

    /// <summary>
    ///   Registers the active WPF shell: the agent command workflow, launch prompt state, the
    ///   launch window's catalog, coordinators, builders, pane controllers, and view models, and
    ///   the <c> LaunchWindow </c> itself with its <c> IPathDisplayStyleTarget </c> forwarder.
    ///   <c> AgentCommandWorkflow </c> is consumed cross-module by the tray command handlers.
    /// </summary>
    private static IServiceCollection AddLaunchWindowServices(this IServiceCollection services)
    {
        services.AddSingleton<IAgentCommandWorkflow, AgentCommandWorkflow>();
        services.AddSingleton<PromptState>();
        services.AddSingleton<ProjectModelCatalog>();
        services.AddSingleton<LaunchCommandCoordinator>();
        services.AddSingleton<LaunchWindowActionBuilder>();
        services.AddSingleton<ProjectPaneController>();
        services.AddSingleton<AgentPaneController>();
        services.AddSingleton<UpdateControlViewModel>();
        services.AddSingleton<StatusMessageCoordinator>();
        services.AddSingleton<LaunchWindowViewModel>();
        services.AddSingleton<IPathDisplayStyleTarget>(sp => sp.GetRequiredService<LaunchWindowViewModel>());
        services.AddSingleton<LaunchWindow>();
        return services;
    }

    /// <summary>
    ///   Registers the tray stack: the menu builder, state projection, command handlers, the
    ///   <c> ITrayActions </c> port and the tray icon controller.
    /// </summary>
    private static IServiceCollection AddTrayServices(this IServiceCollection services)
    {
        services.AddSingleton<TrayMenuBuilder>();
        services.AddSingleton<TrayStateProjection>();
        services.AddSingleton<TrayCommandHandlers>();
        services.AddSingleton<ITrayActions, TrayActions>();
        services.AddSingleton<TrayIconController>();
        return services;
    }

    /// <summary>
    ///   Registers the global hotkey stack: the hotkey service whose factory joins the launch
    ///   window with the tray toggle callback, its <c> IGlobalHotkeyService </c> forwarder, and
    ///   the hotkey startup registrar with its <c> Func </c> accessor.
    /// </summary>
    private static IServiceCollection AddHotkeyServices(this IServiceCollection services)
    {
        services.AddSingleton<GlobalHotkeyService>(sp => new GlobalHotkeyService(
            sp.GetRequiredService<LaunchWindow>(),
            () => sp.GetRequiredService<TrayIconController>().ToggleLaunchWindow(),
            sp.GetRequiredService<ILogger<GlobalHotkeyService>>()));
        services.AddSingleton<IGlobalHotkeyService>(sp => sp.GetRequiredService<GlobalHotkeyService>());
        services.AddSingleton<Func<IHotkeyStartupRegistrar>>(sp =>
            () => sp.GetRequiredService<IHotkeyStartupRegistrar>());
        services.AddSingleton<IHotkeyStartupRegistrar, HotkeyStartupRegistrar>();
        return services;
    }

    /// <summary>
    ///   Registers the settings window stack (application service, section view models, the
    ///   settings view model with its thirteen dependencies, and the window with its accessor)
    ///   plus the runtime preference-application chain (<c> PreferenceApplier </c> with its process,
    ///   accent, and theme preference targets, and the shared palette switcher they use) consumed at
    ///   startup and on preference change.
    /// </summary>
    private static IServiceCollection AddSettingsServices(this IServiceCollection services)
    {
        services.AddSingleton<IPreferenceApplier, PreferenceApplier>();
        services.AddSingleton<IRuntimePreferenceTarget, ProcessRuntimePreferenceTarget>();
        services.AddSingleton<IAccentPreferenceTarget, WpfAccentPreferenceTarget>();
        services.AddSingleton<IThemePreferenceTarget, WpfThemePreferenceTarget>();
        services.AddSingleton(PaletteSwitcher.Shared);
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
            sp.GetRequiredService<IRuntimeExecutableLocator>(),
            sp.GetRequiredService<ILogger<SettingsViewModel>>()));
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => () => sp.GetRequiredService<SettingsWindow>());
        services.AddSingleton<ISettingsLauncher, SettingsLauncher>();
        return services;
    }

    /// <summary>
    ///   Registers the update and whats-new stack: the <c> IUpdateWorkflow </c> anchor (shared by
    ///   the tray projection, tray handlers, launch window, session, settings, and startup
    ///   coordinators), the whats-new view model with its update request source accessor, and the
    ///   transient whats-new window with its accessor and the release notes launcher.
    /// </summary>
    private static IServiceCollection AddUpdateServices(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateWorkflow, UpdateWorkflow>();
        services.AddSingleton<WhatsNewViewModel>();
        services.AddSingleton<Func<IUpdateRequestSource>>(sp =>
            () => sp.GetRequiredService<WhatsNewViewModel>());
        services.AddTransient<WhatsNewWindow>();
        services.AddSingleton<Func<WhatsNewWindow>>(sp => () => sp.GetRequiredService<WhatsNewWindow>());
        services.AddSingleton<IReleaseNotesLauncher, ReleaseNotesLauncher>();
        return services;
    }

    /// <summary>
    ///   Registers UI services with no single owning feature: project dialogs, user
    ///   notifications, external launching, the logo image cache, and the path-to-image
    ///   converter.
    /// </summary>
    private static IServiceCollection AddSharedUiServices(this IServiceCollection services)
    {
        services.AddSingleton<IProjectDialogService, ProjectDialogService>();
        services.AddSingleton<IUserNotificationService, UserNotificationService>();
        services.AddSingleton<IExternalLauncher, ExternalLauncher>();
        services.AddSingleton<LogoImageSourceCache>();
        services.AddSingleton<PathToImageConverter>();
        return services;
    }
}
