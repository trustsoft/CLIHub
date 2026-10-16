namespace CLIHub.Startup;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;
using CLIHub.Composition;
using CLIHub.Lifetime;

using Serilog;

/// <summary>
///   WPF lifecycle boundary that owns environment setup, DI composition, and shutdown.
/// </summary>
public sealed class ApplicationHost : IApplicationHost
{
    private ServiceProvider? services;

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
        this.services = services.BuildServiceProvider();

        this.services.GetRequiredService<IApplicationBootstrapper>().Start(
            new ApplicationStartupContext(dispatch, setMainWindow));
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        Log.Information("CLIHub shutting down");

        try
        {
            this.services?.GetService<IApplicationSession>()?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application session shutdown failed");
        }

        try
        {
            this.services?
                .GetService<IApplicationOperationLifetime>()?
                .StopAsync(ApplicationOperationLifetime.DefaultShutdownTimeout)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application operation shutdown failed");
        }

        this.services?.Dispose();

        Log.CloseAndFlush();
    }

    private static void ConfigureLogging()
    {
        var configPath = AppPaths.ConfigFile;

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        var retentionDays = PreferenceReader.ReadLogRetentionDays(configPath) ?? 7;
        var sizeLimitMb = PreferenceReader.ReadLogFileSizeLimitMb(configPath) ?? 0;
        var sizeLimitBytes = sizeLimitMb > 0 ? sizeLimitMb * 1024L * 1024L : (long?)null;

        Log.Logger = LoggingSetup.CreateLogger(
            AppPaths.LogsDirectory,
            level,
            retentionDays,
            sizeLimitBytes);
    }
}
