namespace CLIHub;

using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;

/// <summary>
///   Owns application-level event subscriptions for the running first-instance session.
///   Delegates shell creation and event wiring to the shell coordinator.
/// </summary>
public sealed class ApplicationSession : IApplicationSession
{
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly Func<IApplicationStartupUi> _startupUiFactory;
    private readonly Func<IUpdateRequestSource> _updateRequestSourceFactory;
    private readonly ISingleInstanceGuard _singleInstanceGuard;
    private readonly IUpdateWorkflow _updateWorkflow;

    private IShellCoordinator? _shellCoordinator;
    private bool _disposed;

    /// <summary>
    ///   Creates the application session with lazy shell factories and update workflow.
    /// </summary>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="singleInstanceGuard"> Single-instance activation source. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    public ApplicationSession(
        IApplicationOperationLifetime operationLifetime,
        Func<IApplicationStartupUi> startupUiFactory,
        Func<IUpdateRequestSource> updateRequestSourceFactory,
        ISingleInstanceGuard singleInstanceGuard,
        IUpdateWorkflow updateWorkflow)
    {
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        _updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        _singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        _updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
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

        if (_shellCoordinator is not null)
        {
            return _shellCoordinator.StartupUi;
        }

        _shellCoordinator = new ShellCoordinator(
            _operationLifetime,
            _startupUiFactory,
            _updateRequestSourceFactory,
            _singleInstanceGuard,
            _updateWorkflow,
            context);

        return _shellCoordinator.StartupUi;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _shellCoordinator?.Dispose();
    }
}

