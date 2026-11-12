namespace CLIHub.Lifetime;

using CLIHub.Startup;

/// <summary>
///   Owns application-level event subscriptions for the running first-instance session.
///   Delegates shell creation and event wiring to the shell coordinator.
/// </summary>
public sealed class ApplicationSession : IApplicationSession
{
    private readonly Func<ApplicationStartupContext, IShellCoordinator> shellFactory;

    private IShellCoordinator? shellCoordinator;
    private bool disposed;

    /// <summary>
    ///   Creates the application session with the lazy shell coordinator factory invoked on first
    ///   start.
    /// </summary>
    /// <param name="shellFactory"> Lazy shell coordinator factory. </param>
    public ApplicationSession(Func<ApplicationStartupContext, IShellCoordinator> shellFactory)
    {
        this.shellFactory = shellFactory ?? throw new ArgumentNullException(nameof(shellFactory));
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

        this.shellCoordinator = this.shellFactory(context);

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
