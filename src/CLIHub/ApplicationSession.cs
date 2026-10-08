namespace CLIHub;

using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;

/// <summary>
///   Owns application-level event subscriptions for the running first-instance session.
/// </summary>
public sealed class ApplicationSession : IApplicationSession
{
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IUpdateStateSource _updateState;
    private readonly Func<IApplicationStartupUi> _startupUiFactory;
    private readonly Func<IUpdateDownloadCoordinator> _updateDownloadFactory;
    private readonly Func<IUpdateRequestSource> _updateRequestSourceFactory;
    private readonly ISingleInstanceGuard _singleInstanceGuard;

    private IApplicationStartupUi? _startupUi;
    private IUpdateDownloadCoordinator? _updateDownload;
    private IUpdateRequestSource? _updateRequestSource;
    private EventHandler? _updateStateChangedHandler;
    private EventHandler? _startupUiUpdateDownloadRequestedHandler;
    private EventHandler? _updateRequestedHandler;
    private Action? _activationRequestedHandler;
    private bool _disposed;

    /// <summary>
    ///   Creates the application session with lazy shell and update workflows.
    /// </summary>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    /// <param name="updateState"> Shared update-state notification source. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateDownloadFactory"> Lazy update download workflow factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="singleInstanceGuard"> Single-instance activation source. </param>
    public ApplicationSession(
        IApplicationOperationLifetime operationLifetime,
        IUpdateStateSource updateState,
        Func<IApplicationStartupUi> startupUiFactory,
        Func<IUpdateDownloadCoordinator> updateDownloadFactory,
        Func<IUpdateRequestSource> updateRequestSourceFactory,
        ISingleInstanceGuard singleInstanceGuard)
    {
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _updateState = updateState ?? throw new ArgumentNullException(nameof(updateState));
        _startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        _updateDownloadFactory = updateDownloadFactory ?? throw new ArgumentNullException(nameof(updateDownloadFactory));
        _updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        _singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
    }

    /// <inheritdoc />
    public IApplicationStartupUi Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ApplicationSession));
        }

        if (_startupUi is not null)
        {
            return _startupUi;
        }

        var startupUi = _startupUiFactory();
        var updateDownload = _updateDownloadFactory();
        var updateRequestSource = _updateRequestSourceFactory();

        EventHandler updateStateChanged = (_, _) => context.Dispatch(startupUi.RefreshMenu);
        EventHandler startupUiUpdateDownloadRequested = (_, _) =>
            _ = _operationLifetime.RunAsync(
                "Update download",
                cancellationToken => updateDownload.DownloadAndApplyAsync(cancellationToken));
        EventHandler updateRequested = (_, _) =>
            _ = _operationLifetime.RunAsync(
                "Update download",
                cancellationToken => updateDownload.DownloadAndApplyAsync(cancellationToken));
        Action activationRequested = () => context.Dispatch(startupUi.ShowLaunchWindow);

        _startupUi = startupUi;
        _updateDownload = updateDownload;
        _updateRequestSource = updateRequestSource;
        _updateStateChangedHandler = updateStateChanged;
        _startupUiUpdateDownloadRequestedHandler = startupUiUpdateDownloadRequested;
        _updateRequestedHandler = updateRequested;
        _activationRequestedHandler = activationRequested;

        _updateState.UpdateStateChanged += updateStateChanged;
        startupUi.UpdateDownloadRequested += startupUiUpdateDownloadRequested;
        updateRequestSource.UpdateRequested += updateRequested;
        _singleInstanceGuard.ActivationRequested += activationRequested;

        return startupUi;
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
            _updateState.UpdateStateChanged -= _updateStateChangedHandler;
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

        if (_startupUi is IDisposable disposableStartupUi)
        {
            disposableStartupUi.Dispose();
        }
    }
}
