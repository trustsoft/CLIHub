namespace CLIHub;

using CLIHub.Views;

/// <summary>
///   Adapts the WPF tray and launch window to the application startup UI port.
/// </summary>
public sealed class WpfApplicationStartupUi : IApplicationStartupUi, IDisposable
{
    private readonly TrayIconController _tray;
    private readonly LaunchWindow _launchWindow;
    private bool _disposed;

    /// <summary>
    ///   Creates the startup UI adapter over the tray and launch window.
    /// </summary>
    /// <param name="tray"> The system tray controller. </param>
    /// <param name="launchWindow"> The launch window. </param>
    public WpfApplicationStartupUi(TrayIconController tray, LaunchWindow launchWindow)
    {
        _tray = tray ?? throw new ArgumentNullException(nameof(tray));
        _launchWindow = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));
        _tray.UpdateDownloadRequested += OnUpdateDownloadRequested;
    }

    /// <inheritdoc />
    public event EventHandler? UpdateDownloadRequested;

    /// <inheritdoc />
    public IStartupWindow LaunchWindow => _launchWindow;

    /// <inheritdoc />
    public void RefreshMenu() => _tray.RefreshMenu();

    /// <inheritdoc />
    public void ShowLaunchWindow() => _tray.ShowLaunchWindow();

    /// <inheritdoc />
    public void NotifyUpdateAvailable(string version) => _tray.NotifyUpdateAvailable(version);

    /// <inheritdoc />
    public void NotifyUpdateDownloaded(string version) => _tray.NotifyUpdateDownloaded(version);

    /// <inheritdoc />
    public void NotifyUpdateFailed(string version) => _tray.NotifyUpdateFailed(version);

    private void OnUpdateDownloadRequested(object? sender, EventArgs e) =>
        UpdateDownloadRequested?.Invoke(this, e);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tray.UpdateDownloadRequested -= OnUpdateDownloadRequested;
    }
}
