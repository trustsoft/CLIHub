namespace CLIHub;

using System.Windows;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IApplicationHost? _host;

    /// <summary>
    ///   Prepares the environment and delegates application startup to the host.
    /// </summary>
    /// <param name="e"> Startup event arguments. </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = new ApplicationHost();
        _host.Start(
            action => Dispatcher.Invoke(action),
            window => MainWindow = (Window)window);
    }

    /// <summary>
    ///   Disposes the hotkey, tray icon, and DI container, then flushes logs.
    /// </summary>
    /// <param name="e"> Exit event arguments. </param>
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Shutdown();

        base.OnExit(e);
    }
}
