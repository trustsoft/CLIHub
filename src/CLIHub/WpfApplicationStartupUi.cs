namespace CLIHub;

using CLIHub.Views;

/// <summary>
///   Adapts the WPF tray and launch window to the application startup UI port.
/// </summary>
public sealed class WpfApplicationStartupUi : IApplicationStartupUi, IDisposable
{
    private readonly TrayIconController tray;
    private readonly LaunchWindow launchWindow;
    private bool disposed;

    /// <summary>
    ///   Creates the startup UI adapter over the tray and launch window.
    /// </summary>
    /// <param name="tray"> The system tray controller. </param>
    /// <param name="launchWindow"> The launch window. </param>
    public WpfApplicationStartupUi(TrayIconController tray, LaunchWindow launchWindow)
    {
        this.tray = tray ?? throw new ArgumentNullException(nameof(tray));
        this.launchWindow = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));
        this.tray.UpdateDownloadRequested += this.OnUpdateDownloadRequested;
    }

    /// <inheritdoc />
    public event EventHandler? UpdateDownloadRequested;

    /// <inheritdoc />
    public IStartupWindow LaunchWindow => this.launchWindow;

    /// <inheritdoc />
    public void RefreshMenu() => this.tray.RefreshMenu();

    /// <inheritdoc />
    public void ShowLaunchWindow() => this.tray.ShowLaunchWindow();

    /// <inheritdoc />
    public void NotifyUpdateAvailable(string version) => this.tray.NotifyUpdateAvailable(version);

    /// <inheritdoc />
    public void NotifyUpdateDownloaded(string version) => this.tray.NotifyUpdateDownloaded(version);

    /// <inheritdoc />
    public void NotifyUpdateFailed(string version) => this.tray.NotifyUpdateFailed(version);

    private void OnUpdateDownloadRequested(object? sender, EventArgs e) =>
        UpdateDownloadRequested?.Invoke(this, e);

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.tray.UpdateDownloadRequested -= this.OnUpdateDownloadRequested;
    }
}
