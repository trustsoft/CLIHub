namespace CLIHub.Startup;

using CLIHub.Core.Windows;
using CLIHub.Updates;
using CLIHub.Lifetime;

/// <summary>
///   Coordinates application shell creation (tray and launch window) and event wiring.
/// </summary>
public sealed class ShellCoordinator : IShellCoordinator
{
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly Func<IApplicationStartupUi> startupUiFactory;
    private readonly Func<IUpdateRequestSource> updateRequestSourceFactory;
    private readonly ISingleInstanceGuard singleInstanceGuard;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly ApplicationStartupContext context;

    private IApplicationStartupUi? startupUi;
    private IUpdateRequestSource? updateRequestSource;
    private EventHandler? updateStateChangedHandler;
    private EventHandler? startupUiUpdateDownloadRequestedHandler;
    private EventHandler? updateRequestedHandler;
    private Action? activationRequestedHandler;
    private EventHandler<string>? updateDownloadedHandler;
    private EventHandler<string>? updateDownloadFailedHandler;
    private bool disposed;

    /// <summary>
    ///   Creates the shell coordinator with lazy UI factories and event sources.
    /// </summary>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="singleInstanceGuard"> Single-instance activation source. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="context"> WPF lifecycle callbacks for dispatcher and window registration. </param>
    public ShellCoordinator(IApplicationOperationLifetime operationLifetime,
                            Func<IApplicationStartupUi> startupUiFactory,
                            Func<IUpdateRequestSource> updateRequestSourceFactory,
                            ISingleInstanceGuard singleInstanceGuard,
                            IUpdateWorkflow updateWorkflow,
                            ApplicationStartupContext context)
    {
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        this.updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        this.singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        this.updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        this.context = context ?? throw new ArgumentNullException(nameof(context));

        this.InitializeShell();
    }

    /// <inheritdoc />
    public IApplicationStartupUi StartupUi
    {
        get
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(ShellCoordinator));
            }

            return this.startupUi ?? throw new InvalidOperationException("Startup UI not initialized");
        }
    }

    private void InitializeShell()
    {
        var startupUi = this.startupUiFactory();
        var updateRequestSource = this.updateRequestSourceFactory();

        EventHandler updateStateChanged = (_, _) => this.context.Dispatch(startupUi.RefreshMenu);
        EventHandler startupUiUpdateDownloadRequested = (_, _) =>
            _ = this.operationLifetime.RunAsync(
                "Update download",
                cancellationToken => this.updateWorkflow.DownloadAndApplyAsync(cancellationToken));
        EventHandler updateRequested = (_, _) =>
            _ = this.operationLifetime.RunAsync(
                "Update download",
                cancellationToken => this.updateWorkflow.DownloadAndApplyAsync(cancellationToken));
        Action activationRequested = () => this.context.Dispatch(startupUi.ShowLaunchWindow);
        EventHandler<string> updateDownloaded = (_, version) =>
            this.context.Dispatch(() => startupUi.NotifyUpdateDownloaded(version));
        EventHandler<string> updateDownloadFailed = (_, version) =>
            this.context.Dispatch(() => startupUi.NotifyUpdateFailed(version));

        this.startupUi = startupUi;
        this.updateRequestSource = updateRequestSource;
        this.updateStateChangedHandler = updateStateChanged;
        this.startupUiUpdateDownloadRequestedHandler = startupUiUpdateDownloadRequested;
        this.updateRequestedHandler = updateRequested;
        this.activationRequestedHandler = activationRequested;
        this.updateDownloadedHandler = updateDownloaded;
        this.updateDownloadFailedHandler = updateDownloadFailed;

        this.updateWorkflow.UpdateStateChanged += updateStateChanged;
        startupUi.UpdateDownloadRequested += startupUiUpdateDownloadRequested;
        updateRequestSource.UpdateRequested += updateRequested;
        this.singleInstanceGuard.ActivationRequested += activationRequested;
        this.updateWorkflow.UpdateDownloaded += updateDownloaded;
        this.updateWorkflow.UpdateDownloadFailed += updateDownloadFailed;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        if (this.updateStateChangedHandler is not null)
        {
            this.updateWorkflow.UpdateStateChanged -= this.updateStateChangedHandler;
        }

        if (this.startupUi is not null && this.startupUiUpdateDownloadRequestedHandler is not null)
        {
            this.startupUi.UpdateDownloadRequested -= this.startupUiUpdateDownloadRequestedHandler;
        }

        if (this.updateRequestSource is not null && this.updateRequestedHandler is not null)
        {
            this.updateRequestSource.UpdateRequested -= this.updateRequestedHandler;
        }

        if (this.activationRequestedHandler is not null)
        {
            this.singleInstanceGuard.ActivationRequested -= this.activationRequestedHandler;
        }

        if (this.updateDownloadedHandler is not null)
        {
            this.updateWorkflow.UpdateDownloaded -= this.updateDownloadedHandler;
        }

        if (this.updateDownloadFailedHandler is not null)
        {
            this.updateWorkflow.UpdateDownloadFailed -= this.updateDownloadFailedHandler;
        }

        if (this.startupUi is IDisposable disposableStartupUi)
        {
            disposableStartupUi.Dispose();
        }
    }
}
