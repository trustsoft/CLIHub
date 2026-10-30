namespace CLIHub.Tray;

/// <summary>
///   Adapts focused tray state projection and command handlers to the tray host contract.
/// </summary>
public sealed class TrayActions : ITrayActions, IDisposable
{
    private readonly TrayStateProjection projection;
    private readonly TrayCommandHandlers handlers;
    private bool disposed;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public TrayMenuCommands Commands { get; }

    /// <summary>
    ///   Creates the tray adapter over focused state and command services.
    /// </summary>
    /// <param name="projection"> State projection for tray menu snapshots. </param>
    /// <param name="handlers"> User-invoked tray command handlers. </param>
    public TrayActions(TrayStateProjection projection, TrayCommandHandlers handlers)
    {
        this.projection = projection ?? throw new ArgumentNullException(nameof(projection));
        this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        this.projection.StateChanged += this.OnStateChanged;
        this.Commands = this.handlers.CreateCommands(this.projection.SetCurrentProject);
    }

    /// <inheritdoc />
    public TrayMenuState GetState() => this.projection.GetState();

    /// <summary>
    ///   Releases event subscriptions to the composed tray services.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projection.StateChanged -= this.OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e) => this.StateChanged?.Invoke(this, e);
}
