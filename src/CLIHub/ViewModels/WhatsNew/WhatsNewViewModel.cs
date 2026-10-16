namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Windows;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;
using CLIHub.Lifetime;
using CLIHub.Updates;

/// <summary>
///   View model for the What's New window: the release notes in display order, with empty groups
///   dropped so the window only shows what a version actually changed, plus the download-and-restart
///   action for an available update. The action is wired by <see cref="ApplicationSession" />, so the tray menu
///   and this window share one flow and one state.
/// </summary>
public sealed class WhatsNewViewModel : ObservableObject, IDisposable, IUpdateRequestSource
{
    /// <summary>
    ///   The group headings, in the order they are displayed.
    /// </summary>
    private static readonly string[] GroupLabels = { "New", "Improved", "Fixed" };

    private readonly IUpdateWorkflow updates;
    private readonly IApplicationOperationLifetime operationLifetime;
    private bool disposed;

    /// <summary>
    ///   Raised when the user asks to download the update and restart.
    /// </summary>
    public event EventHandler? UpdateRequested;

    /// <summary>
    ///   Loads the release notes and subscribes to update-state changes.
    /// </summary>
    /// <param name="releaseNotes"> Release notes service. </param>
    /// <param name="updates"> Shared application update workflow. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked asynchronous work. </param>
    public WhatsNewViewModel(
        IReleaseNotesService releaseNotes,
        IUpdateWorkflow updates,
        IApplicationOperationLifetime operationLifetime)
    {
        this.updates = updates;
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));

        this.Versions = BuildVersions(releaseNotes.GetNotes());

        updates.UpdateStateChanged += this.OnUpdateStateChanged;
        this.RefreshUpdateState();
    }

    /// <summary>
    ///   The release notes, newest version first.
    /// </summary>
    public IReadOnlyList<WhatsNewVersion> Versions { get; }

    /// <summary>
    ///   Whether there is nothing to show, so the window displays its empty message instead.
    /// </summary>
    public bool HasNoNotes => this.Versions.Count == 0;

    /// <summary>
    ///   Whether an update is available to download.
    /// </summary>
    public bool IsUpdateAvailable
    {
        get;
        private set => this.SetProperty(ref field, value);
    }

    /// <summary>
    ///   Whether the update download is currently running; the install action is disabled then.
    /// </summary>
    public bool IsDownloading
    {
        get;
        private set => this.SetProperty(ref field, value);
    }

    /// <summary>
    ///   Whether an update check is currently running.
    /// </summary>
    public bool IsCheckingForUpdates => this.updates.IsCheckingForUpdates;

    /// <summary>
    ///   Whether the install action can start a download right now.
    /// </summary>
    public bool CanDownloadUpdate => this.IsUpdateAvailable && !this.IsDownloading;

    /// <summary>
    ///   Whether the user can start a manual update check.
    /// </summary>
    public bool CanCheckForUpdates => !this.IsUpdateAvailable && !this.IsDownloading && !this.IsCheckingForUpdates;

    /// <summary>
    ///   Whether the manual update-check action should remain visible.
    /// </summary>
    public bool ShowCheckForUpdates => !this.IsUpdateAvailable;

    /// <summary>
    ///   The caption of the manual update-check action.
    /// </summary>
    public string CheckActionText => this.IsCheckingForUpdates ? "Checking for updates..." : "Check for updates";

    /// <summary>
    ///   The caption of the install action, naming the version or the running download.
    /// </summary>
    public string InstallActionText => this.IsDownloading
        ? "Downloading update…"
        : $"Download {this.updates.LastKnownAvailableVersion} and restart";

    /// <summary>
    ///   Asks the application to download the update and restart; ignored while a download runs.
    /// </summary>
    public void RequestInstallUpdate()
    {
        if (!this.IsDownloading && this.IsUpdateAvailable)
        {
            UpdateRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///   Starts a tracked manual update check while the What's New window is idle.
    /// </summary>
    public void RequestCheckForUpdates()
    {
        if (!this.CanCheckForUpdates)
        {
            return;
        }

        _ = this.operationLifetime.RunAsync("Manual update check", this.CheckForUpdatesAsync);
    }

    /// <summary>
    ///   Unsubscribes from update-state changes.
    /// </summary>
    public void Dispose()
    {
        this.disposed = true;
        this.updates.UpdateStateChanged -= this.OnUpdateStateChanged;
    }

    private void OnUpdateStateChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(this.RefreshUpdateState);
            return;
        }

        this.RefreshUpdateState();
    }

    private void RefreshUpdateState()
    {
        if (this.disposed)
        {
            return;
        }

        this.IsUpdateAvailable = this.updates.LastKnownAvailableVersion != null;
        this.IsDownloading = this.updates.IsDownloading;
        this.NotifyCheckStateChanged();
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this.updates.CheckForUpdatesAsync(cancellationToken);
        }
        finally
        {
            this.RefreshUpdateState();
        }
    }

    private void NotifyCheckStateChanged()
    {
        this.OnPropertyChanged(nameof(this.CanDownloadUpdate));
        this.OnPropertyChanged(nameof(this.IsCheckingForUpdates));
        this.OnPropertyChanged(nameof(this.CanCheckForUpdates));
        this.OnPropertyChanged(nameof(this.ShowCheckForUpdates));
        this.OnPropertyChanged(nameof(this.CheckActionText));
        this.OnPropertyChanged(nameof(this.InstallActionText));
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
