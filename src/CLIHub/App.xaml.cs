namespace CLIHub;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Services;
using CLIHub.Hotkeys;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private SingleInstanceGuard? _guard;
    private TrayIconController? _tray;
    private GlobalHotkeyService? _hotkey;

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

        var preferences = _services.GetRequiredService<IConfigService>().Load().Preferences;
        _services.GetRequiredService<IProcessLauncher>().SetRuntime(RuntimeKinds.Parse(preferences.DefaultRuntime));
        _services.GetRequiredService<IStartupService>().SetEnabled(preferences.StartWithWindows);

        _tray = _services.GetRequiredService<TrayIconController>();

        _guard.ActivationRequested += () =>
            Dispatcher.Invoke(() => _tray?.ShowMainWindow());

        var mainWindow = _services.GetRequiredService<MainWindow>();

        if (preferences.ShowWindowOnStartup)
        {
            mainWindow.Show();
        }
        else
        {
            Log.Information("Starting in the system tray (show window on startup disabled)");
        }

        RegisterGlobalHotkey();

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

    private static void ConfigureLogging()
    {
        var root = DirectoryInitializer.GetAppDataRoot();
        var logsDirectory = Path.Combine(root, "logs");
        var configPath = Path.Combine(root, "config.json");

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        Log.Logger = LoggingSetup.CreateLogger(logsDirectory, level);
    }

    private void RegisterGlobalHotkey()
    {
        var configured = _services!.GetRequiredService<IConfigService>().Load().Preferences.Hotkey;

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
