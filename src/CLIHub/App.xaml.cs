namespace CLIHub;

using System.IO;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;
using CLIHub.Core.Logging;
using CLIHub.Hotkeys;
using CLIHub.ViewModels;
using CLIHub.Views;

using Serilog;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private SingleInstanceGuard? _guard;
    private TrayIconController? _tray;
    private GlobalHotkeyService? _hotkey;

    /// <summary>
    ///   Enforces single-instance startup, builds the DI container, and shows the tray icon
    ///   and (optionally) the launch window.
    /// </summary>
    /// <param name="e"> Startup event arguments. </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DirectoryInitializer.EnsureAppDataLayout();
        ConfigureLogging();

        Log.Information("CLIHub starting");

        var services = new ServiceCollection();
        services.AddClIHubServices();
        _services = services.BuildServiceProvider();

        _guard = _services.GetRequiredService<SingleInstanceGuard>();

        if (!_guard.IsFirstInstance)
        {
            _guard.SignalActivation();
            Shutdown();
            return;
        }

        _services.GetRequiredService<IPluginInitializationService>().Initialize();

        var preferences = _services.GetRequiredService<IPreferencesStore>().Load();
        _services.GetRequiredService<IStartupPreferencesApplier>().Apply(preferences);

        _tray = _services.GetRequiredService<TrayIconController>();

        var updates = _services.GetRequiredService<IUpdateService>();
        var updateDownloads = _services.GetRequiredService<IUpdateDownloadCoordinator>();
        updates.UpdateStateChanged += (_, _) =>
            Dispatcher.Invoke(() => _tray?.RefreshMenu());
        _tray.UpdateDownloadRequested += (_, _) => _ = updateDownloads.DownloadAndApplyAsync();
        _services.GetRequiredService<WhatsNewViewModel>().UpdateRequested += (_, _) =>
            _ = updateDownloads.DownloadAndApplyAsync();

        _guard.ActivationRequested += () =>
            Dispatcher.Invoke(() => _tray?.ShowLaunchWindow());

        var launchWindow = _services.GetRequiredService<LaunchWindow>();
        MainWindow = launchWindow;

        if (preferences.ShowWindowOnStartup)
        {
            launchWindow.ShowOnPointerMonitor();
        }
        else
        {
            Log.Information("Starting in the system tray (show window on startup disabled)");
        }

        _hotkey = _services.GetRequiredService<GlobalHotkeyService>();
        _services.GetRequiredService<IHotkeyStartupRegistrar>().Register(preferences);

        _services.GetRequiredService<IReleaseNotesStartupCoordinator>().Evaluate(preferences);

        _ = _services.GetRequiredService<IUpdateStartupCoordinator>().CheckAsync(
            preferences.CheckForUpdatesOnStartup,
            version => Dispatcher.Invoke(() => _tray?.NotifyUpdateAvailable(version)));

        Log.Information("CLIHub started");
    }

    private static void ConfigureLogging()
    {
        var configPath = AppPaths.ConfigFile;

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        Log.Logger = LoggingSetup.CreateLogger(AppPaths.LogsDirectory, level);
    }

    /// <summary>
    ///   Disposes the hotkey, tray icon, and DI container, then flushes logs.
    /// </summary>
    /// <param name="e"> Exit event arguments. </param>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CLIHub shutting down");

        _hotkey?.Dispose();
        _tray?.Dispose();
        _services?.Dispose();

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
