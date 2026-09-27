using CLIHub.Core.Logging;
using CLIHub.Core.Services;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Windows;

namespace CLIHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private SingleInstanceGuard? _guard;
    private TrayIconController? _tray;

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

        _tray = _services.GetRequiredService<TrayIconController>();

        _guard.ActivationRequested += () =>
            Dispatcher.Invoke(() => _tray?.ShowMainWindow());

        _services.GetRequiredService<MainWindow>().Show();
        Log.Information("CLIHub started");
    }

    private static void ConfigureLogging()
    {
        var root = DirectoryInitializer.GetAppDataRoot();
        var logsDirectory = Path.Combine(root, "logs");
        var configPath = Path.Combine(root, "config.json");

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        Log.Logger = LoggingSetup.CreateLogger(logsDirectory, level);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CLIHub shutting down");

        _tray?.Dispose();
        _services?.Dispose();
        _guard?.Dispose();

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
