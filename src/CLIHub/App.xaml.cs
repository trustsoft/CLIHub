namespace CLIHub;

using System.IO;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;

using Serilog;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    /// <summary>
    ///   Prepares the environment and delegates application startup to the bootstrapper.
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

        _services.GetRequiredService<IApplicationBootstrapper>().Start(
            new ApplicationStartupContext(
                action => Dispatcher.Invoke(action),
                window => MainWindow = (Window)window));
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

        try
        {
            _services?.GetService<IApplicationSession>()?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application session shutdown failed");
        }

        try
        {
            _services?
                .GetService<IApplicationOperationLifetime>()?
                .StopAsync(ApplicationOperationLifetime.DefaultShutdownTimeout)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application operation shutdown failed");
        }

        _services?.Dispose();

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
