namespace CLIHub;

using System.Windows;

/// <summary>
///   Dispatches update download notifications and tray refreshes to the WPF thread.
/// </summary>
public sealed class UpdateDownloadNotifier : IUpdateDownloadNotifier
{
    private readonly ITrayHost _tray;

    /// <summary>
    ///   Creates the notifier over the tray controller.
    /// </summary>
    /// <param name="tray"> Tray controller receiving notifications and refresh requests. </param>
    public UpdateDownloadNotifier(ITrayHost tray)
    {
        _tray = tray ?? throw new ArgumentNullException(nameof(tray));
    }

    /// <inheritdoc />
    public void NotifyDownloaded(string version) => Invoke(() => _tray.NotifyUpdateDownloaded(version));

    /// <inheritdoc />
    public void NotifyFailed(string version) => Invoke(() => _tray.NotifyUpdateFailed(version));

    /// <inheritdoc />
    public void RefreshMenu() => Invoke(_tray.RefreshMenu);

    private static void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
