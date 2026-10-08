namespace CLIHub;

using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates application shell creation (tray and launch window) and event wiring.
/// </summary>
public sealed class ShellCoordinator : IShellCoordinator
{
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly Func<IApplicationStartupUi> _startupUiFactory;
    private readonly Func<IUpdateRequestSource> _updateRequestSourceFactory;
    private readonly ISingleInstanceGuard _singleInstanceGuard;
    private readonly IUpdateWorkflow _updateWorkflow;
    private readonly ApplicationStartupContext _context;

    private IApplicationStartupUi? _startupUi;
    private IUpdateRequestSource? _updateRequestSource;
    private EventHandler? _updateStateChangedHandler;
    private EventHandler? _startupUiUpdateDownloadRequestedHandler;
    private EventHandler? _updateRequestedHandler;
    private Action? _activationRequestedHandler;
    private EventHandler<string>? _updateDownloadedHandler;
    private EventHandler<string>? _updateDownloadFailedHandler;
    private bool _disposed;

    /// <summary>
    ///   Creates the shell coordinator with lazy UI factories and event sources.
    /// </summary>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="singleInstanceGuard"> Single-instance activation source. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="context"> WPF lifecycle callbacks for dispatcher and window registration. </param>
    public ShellCoordinator(
        IApplicationOperationLifetime operationLifetime,
        Func<IApplicationStartupUi> startupUiFactory,
        Func<IUpdateRequestSource> updateRequestSourceFactory,
        ISingleInstanceGuard singleInstanceGuard,
        IUpdateWorkflow updateWorkflow,
        ApplicationStartupContext context)
    {
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        _updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        _singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        _updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        InitializeShell();
    }

    /// <inheritdoc />
    public IApplicationStartupUi StartupUi
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ShellCoordinator));
            }

            return _startupUi ?? throw new InvalidOperationException("Startup UI not initialized");
        }
    }

    private void InitializeShell()
    {
        var startupUi = _startupUiFactory();
        var updateRequestSource = _updateRequestSourceFactory();

        EventHandler updateStateChanged = (_, _) => _context.Dispatch(startupUi.RefreshMenu);
        EventHandler startupUiUpdateDownloadRequested = (_, _) =>
            _ = _operationLifetime.RunAsync(
                "Update download",
                cancellationToken => _updateWorkflow.DownloadAndApplyAsync(cancellationToken));
        EventHandler updateRequested = (_, _) =>
            _ = _operationLifetime.RunAsync(
                "Update download",
                cancellationToken => _updateWorkflow.DownloadAndApplyAsync(cancellationToken));
        Action activationRequested = () => _context.Dispatch(startupUi.ShowLaunchWindow);
        EventHandler<string> updateDownloaded = (_, version) =>
            _context.Dispatch(() => startupUi.NotifyUpdateDownloaded(version));
        EventHandler<string> updateDownloadFailed = (_, version) =>
            _context.Dispatch(() => startupUi.NotifyUpdateFailed(version));

        _startupUi = startupUi;
        _updateRequestSource = updateRequestSource;
        _updateStateChangedHandler = updateStateChanged;
        _startupUiUpdateDownloadRequestedHandler = startupUiUpdateDownloadRequested;
        _updateRequestedHandler = updateRequested;
        _activationRequestedHandler = activationRequested;
        _updateDownloadedHandler = updateDownloaded;
        _updateDownloadFailedHandler = updateDownloadFailed;

        _updateWorkflow.UpdateStateChanged += updateStateChanged;
        startupUi.UpdateDownloadRequested += startupUiUpdateDownloadRequested;
        updateRequestSource.UpdateRequested += updateRequested;
        _singleInstanceGuard.ActivationRequested += activationRequested;
        _updateWorkflow.UpdateDownloaded += updateDownloaded;
        _updateWorkflow.UpdateDownloadFailed += updateDownloadFailed;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_updateStateChangedHandler is not null)
        {
            _updateWorkflow.UpdateStateChanged -= _updateStateChangedHandler;
        }

        if (_startupUi is not null && _startupUiUpdateDownloadRequestedHandler is not null)
        {
            _startupUi.UpdateDownloadRequested -= _startupUiUpdateDownloadRequestedHandler;
        }

        if (_updateRequestSource is not null && _updateRequestedHandler is not null)
        {
            _updateRequestSource.UpdateRequested -= _updateRequestedHandler;
        }

        if (_activationRequestedHandler is not null)
        {
            _singleInstanceGuard.ActivationRequested -= _activationRequestedHandler;
        }

        if (_updateDownloadedHandler is not null)
        {
            _updateWorkflow.UpdateDownloaded -= _updateDownloadedHandler;
        }

        if (_updateDownloadFailedHandler is not null)
        {
            _updateWorkflow.UpdateDownloadFailed -= _updateDownloadFailedHandler;
        }

        if (_startupUi is IDisposable disposableStartupUi)
        {
            disposableStartupUi.Dispose();
        }
    }
}
