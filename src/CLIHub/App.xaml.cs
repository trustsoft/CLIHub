using CLIHub.Core.Services;
using CLIHub.Windows;
using Microsoft.Extensions.DependencyInjection;
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

        var services = new ServiceCollection();
        services.AddClIHubServices();
        _services = services.BuildServiceProvider();

        _tray = _services.GetRequiredService<TrayIconController>();

        _guard.ActivationRequested += () =>
            Dispatcher.Invoke(() => _tray?.ShowMainWindow());

        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _services?.Dispose();
        _guard?.Dispose();

        base.OnExit(e);
    }
}
