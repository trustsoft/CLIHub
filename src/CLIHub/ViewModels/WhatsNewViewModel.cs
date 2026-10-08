namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Windows;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   View model for the What's New window: the release notes in display order, with empty groups
///   dropped so the window only shows what a version actually changed, plus the download-and-restart
///   action for an available update. The action itself runs in <see cref="App"/>, so the tray menu
///   and this window share one flow and one state.
/// </summary>
public sealed class WhatsNewViewModel : ObservableObject, IDisposable, IUpdateRequestSource
{
    /// <summary>
    ///   The group headings, in the order they are displayed.
    /// </summary>
    private static readonly string[] GroupLabels = { "New", "Improved", "Fixed" };

    private readonly IUpdateService _updates;
    private readonly IUpdateChecker _updateChecker;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private bool _isUpdateAvailable;
    private bool _isDownloading;
    private bool _isCheckingForUpdates;

    /// <summary>
    ///   Raised when the user asks to download the update and restart.
    /// </summary>
    public event EventHandler? UpdateRequested;

    /// <summary>
    ///   Loads the release notes and subscribes to update-state changes.
    /// </summary>
    /// <param name="releaseNotes"> Release notes service. </param>
    /// <param name="updates"> Update service used for the download action state. </param>
    /// <param name="updateChecker"> Update check workflow. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    public WhatsNewViewModel(
        IReleaseNotesService releaseNotes,
        IUpdateService updates,
        IUpdateChecker updateChecker,
        IApplicationOperationLifetime operationLifetime)
    {
        _updates = updates;
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));

        Versions = BuildVersions(releaseNotes.GetNotes());

        updates.UpdateStateChanged += OnUpdateStateChanged;
        RefreshUpdateState();
    }

    /// <summary>
    ///   The release notes, newest version first.
    /// </summary>
    public IReadOnlyList<WhatsNewVersion> Versions { get; }

    /// <summary>
    ///   Whether there is nothing to show, so the window displays its empty message instead.
    /// </summary>
    public bool HasNoNotes => Versions.Count == 0;

    /// <summary>
    ///   Whether an update is available to download.
    /// </summary>
    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set => SetProperty(ref _isUpdateAvailable, value);
    }

    /// <summary>
    ///   Whether the update download is currently running; the install action is disabled then.
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        private set => SetProperty(ref _isDownloading, value);
    }

    /// <summary>
    ///   Whether an update check is currently running.
    /// </summary>
    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set => SetProperty(ref _isCheckingForUpdates, value);
    }

    /// <summary>
    ///   Whether the install action can start a download right now.
    /// </summary>
    public bool CanDownloadUpdate => IsUpdateAvailable && !IsDownloading;

    /// <summary>
    ///   Whether the user can start a manual update check.
    /// </summary>
    public bool CanCheckForUpdates => !IsUpdateAvailable && !IsDownloading && !IsCheckingForUpdates;

    /// <summary>
    ///   Whether the manual update-check action should remain visible.
    /// </summary>
    public bool ShowCheckForUpdates => !IsUpdateAvailable;

    /// <summary>
    ///   The caption of the manual update-check action.
    /// </summary>
    public string CheckActionText => IsCheckingForUpdates ? "Checking for updates..." : "Check for updates";

    /// <summary>
    ///   The caption of the install action, naming the version or the running download.
    /// </summary>
    public string InstallActionText => IsDownloading
        ? "Downloading update…"
        : $"Download {_updates.LastKnownAvailableVersion} and restart";

    /// <summary>
    ///   Asks the application to download the update and restart; ignored while a download runs.
    /// </summary>
    public void RequestInstallUpdate()
    {
        if (!IsDownloading && IsUpdateAvailable)
        {
            UpdateRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///   Starts a tracked manual update check while the What's New window is idle.
    /// </summary>
    public void RequestCheckForUpdates()
    {
        if (!CanCheckForUpdates)
        {
            return;
        }

        IsCheckingForUpdates = true;
        NotifyCheckStateChanged();
        _ = _operationLifetime.RunAsync("Manual update check", CheckForUpdatesAsync);
    }

    /// <summary>
    ///   Unsubscribes from update-state changes.
    /// </summary>
    public void Dispose()
    {
        _updates.UpdateStateChanged -= OnUpdateStateChanged;
    }

    private void OnUpdateStateChanged(object? sender, EventArgs e) =>
        Application.Current?.Dispatcher.BeginInvoke(RefreshUpdateState);

    private void RefreshUpdateState()
    {
        IsUpdateAvailable = _updates.LastKnownAvailableVersion != null;
        IsDownloading = _updates.IsDownloading;
        NotifyCheckStateChanged();
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _updateChecker.CheckForUpdatesAsync(cancellationToken);
        }
        finally
        {
            IsCheckingForUpdates = false;
            RefreshUpdateState();
        }
    }

    private void NotifyCheckStateChanged()
    {
        OnPropertyChanged(nameof(CanDownloadUpdate));
        OnPropertyChanged(nameof(CanCheckForUpdates));
        OnPropertyChanged(nameof(ShowCheckForUpdates));
        OnPropertyChanged(nameof(CheckActionText));
        OnPropertyChanged(nameof(InstallActionText));
    }

    private static IReadOnlyList<WhatsNewVersion> BuildVersions(IReadOnlyList<ReleaseNote> notes)
    {
        var versions = new List<WhatsNewVersion>(notes.Count);

        foreach (var note in notes)
        {
            versions.Add(new WhatsNewVersion(note.Version, note.Date, BuildGroups(note)));
        }

        return versions;
    }

    private static IReadOnlyList<WhatsNewGroup> BuildGroups(ReleaseNote note)
    {
        var groups = new List<WhatsNewGroup>();
        var entries = new[] { note.New, note.Improved, note.Fixed };

        for (var index = 0; index < GroupLabels.Length; index++)
        {
            if (entries[index].Count > 0)
            {
                groups.Add(new WhatsNewGroup(GroupLabels[index], entries[index]));
            }
        }

        return groups;
    }
}
