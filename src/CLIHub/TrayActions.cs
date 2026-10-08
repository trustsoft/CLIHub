namespace CLIHub;

/// <summary>
///   Adapts focused tray state projection and command handlers to the tray host contract.
/// </summary>
public sealed class TrayActions : ITrayActions, IDisposable
{
    private readonly TrayStateProjection _projection;
    private readonly TrayCommandHandlers _handlers;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public event EventHandler? UpdateDownloadRequested;

    /// <inheritdoc />
    public TrayMenuCommands Commands { get; }

    /// <summary>
    ///   Creates the tray adapter over focused state and command services.
    /// </summary>
    /// <param name="projection"> State projection for tray menu snapshots. </param>
    /// <param name="handlers"> User-invoked tray command handlers. </param>
    public TrayActions(TrayStateProjection projection, TrayCommandHandlers handlers)
    {
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _projection.StateChanged += OnStateChanged;
        _handlers.UpdateDownloadRequested += OnUpdateDownloadRequested;
        Commands = _handlers.CreateCommands(_projection.SetCurrentProject, static () => { });
    }

    /// <inheritdoc />
    public TrayMenuState GetState() => _projection.GetState();

    /// <summary>
    ///   Releases event subscriptions to the composed tray services.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projection.StateChanged -= OnStateChanged;
        _handlers.UpdateDownloadRequested -= OnUpdateDownloadRequested;
    }

    private void OnStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, e);

    private void OnUpdateDownloadRequested(object? sender, EventArgs e) =>
        UpdateDownloadRequested?.Invoke(this, e);
}
