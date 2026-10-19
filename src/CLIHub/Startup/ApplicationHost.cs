namespace CLIHub.Startup;

using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;
using CLIHub.Composition;
using CLIHub.Converters;
using CLIHub.Core.Windows;
using CLIHub.Lifetime;

using Serilog;

/// <summary>
///   WPF lifecycle boundary that owns environment setup, DI composition, and shutdown.
/// </summary>
public sealed class ApplicationHost : IApplicationHost
{
    private readonly Func<SingleInstanceGuard> guardFactory;
    private ServiceProvider? services;
    private SingleInstanceGuard? guard;

    /// <summary>
    ///   Creates the application host.
    /// </summary>
    public ApplicationHost() : this(() => new SingleInstanceGuard())
    {
    }

    /// <summary>
    ///   Creates the application host with a single-instance guard factory for testing.
    /// </summary>
    /// <param name="guardFactory"> Factory that acquires the single-instance guard. </param>
    internal ApplicationHost(Func<SingleInstanceGuard> guardFactory)
    {
        this.guardFactory = guardFactory ?? throw new ArgumentNullException(nameof(guardFactory));
    }

    /// <summary>
    ///   True when the service provider has been built; visible to test assemblies for lifecycle assertions.
    /// </summary>
    internal bool HasBuiltServiceProvider => this.services is not null;

    /// <inheritdoc />
    public void Start(Action<Action> dispatch, Action<object> setMainWindow)
    {
        this.guard = this.guardFactory();

        if (!this.guard.IsFirstInstance)
        {
            this.guard.SignalActivation();
            new WpfApplicationLifetime().Shutdown();
            return;
        }

        try
        {
            ArgumentNullException.ThrowIfNull(dispatch);
            ArgumentNullException.ThrowIfNull(setMainWindow);

            DirectoryInitializer.EnsureAppDataLayout();
            ConfigureLogging();

            Log.Information("CLIHub starting");

            var collection = new ServiceCollection();
            collection.AddClIHubServices(this.guard);
            this.services = collection.BuildServiceProvider();
            this.guard = null;

            InstallSharedUiResources(this.services);

            this.services.GetRequiredService<IApplicationBootstrapper>().Start(
                new ApplicationStartupContext(dispatch, setMainWindow));
        }
        catch (Exception)
        {
            this.guard?.Dispose();
            this.guard = null;
            throw;
        }
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        if (this.services is null)
        {
            // The provider was never built (second instance or failed startup), so the host still owns the guard.
            this.guard?.Dispose();
            this.guard = null;
            return;
        }

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

    /// <summary>
    ///   Publishes the DI-constructed shared UI pieces into the application resource dictionary
    ///    before any window is created, so XAML <c> StaticResource </c> lookups resolve the
    ///    cache-backed instances (the XAML declarations were removed on purpose).
    /// </summary>
    private static void InstallSharedUiResources(ServiceProvider services)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        application.Resources[PathToImageConverter.ResourceKey] = services.GetRequiredService<PathToImageConverter>();
    }

    private static void ConfigureLogging()
    {
        var configPath = AppPaths.ConfigFile;

        var level = LogLevelParser.Parse(PreferenceReader.ReadLogLevel(configPath));
        var retentionDays = PreferenceReader.ReadLogRetentionDays(configPath) ?? 7;
        var sizeLimitMb = PreferenceReader.ReadLogFileSizeLimitMb(configPath) ?? 0;
        var sizeLimitBytes = sizeLimitMb > 0
                                 ? sizeLimitMb * 1024L * 1024L
                                 : (long?)null;

        Log.Logger = LoggingSetup.CreateLogger(AppPaths.LogsDirectory,
            level,
            retentionDays,
            sizeLimitBytes);
    }
}
