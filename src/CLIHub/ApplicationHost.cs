namespace CLIHub;

using System.IO;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Logging;

using Serilog;

/// <summary>
///   WPF lifecycle boundary that owns environment setup, DI composition, and shutdown.
/// </summary>
public sealed class ApplicationHost : IApplicationHost
{
    private ServiceProvider? _services;

    /// <inheritdoc />
    public void Start(Action<Action> dispatch, Action<object> setMainWindow)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(setMainWindow);

        DirectoryInitializer.EnsureAppDataLayout();
        ConfigureLogging();

        Log.Information("CLIHub starting");

        var services = new ServiceCollection();
        services.AddClIHubServices();
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<IApplicationBootstrapper>().Start(
            new ApplicationStartupContext(dispatch, setMainWindow));
    }

    /// <inheritdoc />
    public void Shutdown()
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
    }

    private static void ConfigureLogging()
    {
        var configPath = AppPaths.ConfigFile;

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        Log.Logger = LoggingSetup.CreateLogger(AppPaths.LogsDirectory, level);
    }
}
