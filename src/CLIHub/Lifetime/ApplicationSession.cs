namespace CLIHub.Lifetime;

using CLIHub.Core.Windows;
using CLIHub.Updates;
using CLIHub.Startup;
using CLIHub.Themes;

/// <summary>
///   Owns application-level event subscriptions for the running first-instance session.
///   Delegates shell creation and event wiring to the shell coordinator.
/// </summary>
public sealed class ApplicationSession : IApplicationSession
{
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly Func<IApplicationStartupUi> startupUiFactory;
    private readonly Func<IUpdateRequestSource> updateRequestSourceFactory;
    private readonly ISingleInstanceGuard singleInstanceGuard;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly SystemThemeFollower themeFollower;

    private IShellCoordinator? shellCoordinator;
    private bool disposed;

    /// <summary>
    ///   Creates the application session with lazy shell factories, update workflow, and the
    ///   theme follower disposed with the session.
    /// </summary>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="singleInstanceGuard"> Single-instance activation source. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="themeFollower"> Session-owned Windows personalization theme follower. </param>
    public ApplicationSession(IApplicationOperationLifetime operationLifetime,
                              Func<IApplicationStartupUi> startupUiFactory,
                              Func<IUpdateRequestSource> updateRequestSourceFactory,
                              ISingleInstanceGuard singleInstanceGuard,
                              IUpdateWorkflow updateWorkflow,
                              SystemThemeFollower themeFollower)
    {
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        this.updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        this.singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        this.updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        this.themeFollower = themeFollower ?? throw new ArgumentNullException(nameof(themeFollower));
    }

    /// <inheritdoc />
    public IApplicationStartupUi Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);

        if (this.disposed)
        {
            throw new ObjectDisposedException(nameof(ApplicationSession));
        }

        if (this.shellCoordinator is not null)
        {
            return this.shellCoordinator.StartupUi;
        }

        this.shellCoordinator = new ShellCoordinator(
            this.operationLifetime,
            this.startupUiFactory,
            this.updateRequestSourceFactory,
            this.singleInstanceGuard,
            this.updateWorkflow,
            this.themeFollower,
            context);

        return this.shellCoordinator.StartupUi;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        this.shellCoordinator?.Dispose();
    }
}
