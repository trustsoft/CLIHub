namespace CLIHub;

using System.IO;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Services;
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

        _guard = new SingleInstanceGuard();

        if (!_guard.IsFirstInstance)
        {
            _guard.SignalActivation();
            Shutdown();
            return;
        }

        DirectoryInitializer.EnsureAppDataLayout();
        ConfigureLogging();

        Log.Information("CLIHub starting");

        var services = new ServiceCollection();
        services.AddClIHubServices();
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<IPluginSeeder>().SeedIfEmpty();
        _services.GetRequiredService<IPluginManager>().LoadPlugins();

        var preferences = _services.GetRequiredService<IPreferencesStore>().Load();
        _services.GetRequiredService<IProcessLauncher>().SetRuntime(RuntimeKinds.Parse(preferences.DefaultRuntime));
        _services.GetRequiredService<IStartupService>().SetEnabled(preferences.StartWithWindows);

        _tray = _services.GetRequiredService<TrayIconController>();

        var updates = _services.GetRequiredService<IUpdateService>();
        updates.UpdateStateChanged += (_, _) =>
            Dispatcher.Invoke(() => _tray?.RefreshMenu());
        _tray.UpdateDownloadRequested += (_, _) => _ = DownloadAndApplyUpdateAsync();
        _services.GetRequiredService<WhatsNewViewModel>().UpdateRequested += (_, _) =>
            _ = DownloadAndApplyUpdateAsync();

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

        RegisterGlobalHotkey();

        ShowReleaseNotesOnce();

        if (preferences.CheckForUpdatesOnStartup)
        {
            _ = CheckForUpdatesAsync();
        }
        else
        {
            Log.Information("Startup update check disabled in preferences");
        }

        Log.Information("CLIHub started");
    }

    /// <summary>
    ///   Opens the What's New window once when the running version differs from the one whose notes
    ///   were already shown; a version that has no notes is recorded without opening anything, so
    ///   the check does not repeat on every start.
    /// </summary>
    private void ShowReleaseNotesOnce()
    {
        try
        {
            var services = _services!;
            var currentVersion = services.GetRequiredService<IUpdateService>().GetCurrentVersion();
            var notes = services.GetRequiredService<IReleaseNotesService>();
            var recordedVersion = services.GetRequiredService<IPreferencesStore>()
                .Load().LastSeenReleaseNotesVersion;
            var launcher = services.GetRequiredService<IReleaseNotesLauncher>();

            var action = ReleaseNotesPrompt.Decide(
                recordedVersion,
                currentVersion,
                notes.GetNote(currentVersion) != null);

            if (action == ReleaseNotesPromptAction.Show)
            {
                launcher.ShowReleaseNotes();
            }
            else if (action == ReleaseNotesPromptAction.RecordOnly)
            {
                launcher.MarkReleaseNotesSeen();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "The release notes check failed");
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _services!.GetRequiredService<IUpdateService>().CheckForUpdatesAsync();

            if (result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is { } version)
            {
                Dispatcher.Invoke(() => _tray?.NotifyUpdateAvailable(version));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed");
        }
    }

    /// <summary>
    ///   Downloads the available update and restarts into it. Started from the tray menu or the
    ///   What's New window; the restart needs no confirmation because the user explicitly invoked
    ///   the action, and failures are reported via a tray notification.
    /// </summary>
    private async Task DownloadAndApplyUpdateAsync()
    {
        var updates = _services!.GetRequiredService<IUpdateService>();

        try
        {
            var result = await updates.DownloadUpdateAsync();

            if (result.Status == UpdateDownloadStatus.Downloaded && result.AvailableVersion is { } version)
            {
                Dispatcher.Invoke(() =>
                {
                    _tray?.NotifyUpdateDownloaded(version);
                    _tray?.RefreshMenu();
                });

                // Give the notification a moment before the process exits.
                await Task.Delay(TimeSpan.FromSeconds(2));
                updates.ApplyDownloadedUpdateAndRestart();
            }
            else if (result.Status == UpdateDownloadStatus.Failed)
            {
                Log.Warning("Update download failed (version {Version})", result.AvailableVersion);
                Dispatcher.Invoke(() =>
                {
                    if (result.AvailableVersion is { } failedVersion)
                    {
                        _tray?.NotifyUpdateFailed(failedVersion);
                    }

                    _tray?.RefreshMenu();
                });
            }
            else
            {
                Log.Information("Update download not applied: {Status}", result.Status);
                Dispatcher.Invoke(() => _tray?.RefreshMenu());
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Applying the downloaded update failed");
            Dispatcher.Invoke(() => _tray?.RefreshMenu());
        }
    }

    private static void ConfigureLogging()
    {
        var configPath = AppPaths.ConfigFile;

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        Log.Logger = LoggingSetup.CreateLogger(AppPaths.LogsDirectory, level);
    }

    private void RegisterGlobalHotkey()
    {
        var configured = _services!.GetRequiredService<IPreferencesStore>().Load().Hotkey;

        HotkeyDefinition definition;
        if (HotkeyParser.TryParse(configured, out var parsed) && parsed != null)
        {
            definition = parsed;
        }
        else
        {
            Log.Warning("Invalid hotkey '{Hotkey}' in config; using default Ctrl+Shift+A", configured);
            definition = HotkeyParser.Default;
        }

        _hotkey = _services!.GetRequiredService<GlobalHotkeyService>();
        _hotkey.Register(definition);
    }

    /// <summary>
    ///   Disposes the hotkey, tray icon, DI container, and single-instance guard, then flushes logs.
    /// </summary>
    /// <param name="e"> Exit event arguments. </param>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CLIHub shutting down");

        _hotkey?.Dispose();
        _tray?.Dispose();
        _services?.Dispose();
        _guard?.Dispose();

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}

