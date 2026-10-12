namespace CLIHub.ViewModels;

using System.Windows;
using System.Windows.Input;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   State and command for an update control: shows the current version when idle, checks for
///   updates, downloads an available update, and applies it with a restart. Shared by every
///   window that hosts an update control; reflects checks and downloads started anywhere.
/// </summary>
public sealed class UpdateControlViewModel : ObservableObject, IDisposable
{
    private readonly IUpdateWorkflow workflow;
    private readonly ILogger<UpdateControlViewModel> logger;
    private readonly IApplicationOperationLifetime operationLifetime;

    private UpdateControlState state = UpdateControlState.Idle;
    private bool disposed;

    /// <summary>
    ///   Raised with a verbose outcome message that the host window shows in its status line.
    /// </summary>
    public event EventHandler<string>? OutcomeReported;

    /// <summary>
    ///   Creates the update control state for the given update service.
    /// </summary>
    /// <param name="workflow"> Shared application update workflow. </param>
    /// <param name="logger"> Logger for unexpected update control failures. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked update work. </param>
    public UpdateControlViewModel(
        IUpdateWorkflow workflow,
        ILogger<UpdateControlViewModel> logger,
        IApplicationOperationLifetime operationLifetime)
    {
        this.workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        this.logger = logger;
        this.operationLifetime = operationLifetime;

        this.CurrentVersion = workflow.GetCurrentVersion();

        this.UpdateControlCommand = new RelayCommand(
            () => _ = this.operationLifetime.RunAsync("Update control", this.RunUpdateControlAsync),
            () => this.state is UpdateControlState.Idle or UpdateControlState.Available or UpdateControlState.ReadyToApply);

        this.workflow.UpdateStateChanged += this.OnUpdateStateChanged;
        this.RefreshState();
    }

    private Task RunUpdateControlAsync(CancellationToken cancellationToken) =>
        AsyncOperationRunner.RunAsync(
            "Update control",
            () => this.HandleUpdateControlAsync(cancellationToken),
            this.logger,
            this.ReportOutcome,
            cancellationToken);

    /// <summary>
    ///   The current application version, shown while the control is idle.
    /// </summary>
    public string CurrentVersion { get; }

    /// <summary>
    ///   The label of the control for the current state: the running version when idle, the
    ///   progress state while busy, and the update action when one is available.
    /// </summary>
    public string UpdateButtonText => this.state switch
    {
        UpdateControlState.Available => $"Update to {this.workflow.LastKnownAvailableVersion}",
        UpdateControlState.Checking => "Checking…",
        UpdateControlState.Downloading => this.workflow.LastKnownAvailableVersion is { } downloading
            ? $"Downloading {downloading}…"
            : "Downloading…",
        UpdateControlState.ReadyToApply => this.workflow.LastKnownAvailableVersion is { } ready
            ? $"Restart to update to {ready}"
            : "Restart to update",
        _ => this.CurrentVersion,
    };

    /// <summary>
    ///   Whether the control currently offers an update action, which drives its accent styling.
    /// </summary>
    public bool IsUpdateActionAvailable =>
        this.state is UpdateControlState.Available or UpdateControlState.ReadyToApply;

    /// <summary>
    ///   Routes control clicks by state: checks for updates when idle, downloads the update when
    ///   one is available, and applies it with a restart once downloaded.
    /// </summary>
    public RelayCommand UpdateControlCommand { get; }

    private async Task HandleUpdateControlAsync(CancellationToken cancellationToken)
    {
        switch (this.state)
        {
            case UpdateControlState.Available:
                await this.DownloadUpdateAsync(cancellationToken);
                break;
            case UpdateControlState.ReadyToApply:
                this.ApplyDownloadedUpdate();
                break;
            default:
                await this.CheckForUpdatesAsync(cancellationToken);
                break;
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        this.SetState(UpdateControlState.Checking);
        this.ReportOutcome("Checking for updates...");

        UpdateCheckResult result;
        try
        {
            result = await this.workflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.SetState(UpdateControlState.Idle);
            return;
        }
        catch (Exception)
        {
            this.SetState(UpdateControlState.Idle);
            this.ReportOutcome("Update check failed or timed out.");
            return;
        }

        this.ReportOutcome(result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        });

        this.SetState(result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is not null
            ? UpdateControlState.Available
            : UpdateControlState.Idle);
    }

    private async Task DownloadUpdateAsync(CancellationToken cancellationToken)
    {
        this.SetState(UpdateControlState.Downloading);

        UpdateDownloadResult result;
        try
        {
            result = await this.workflow.DownloadUpdateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.SetState(UpdateControlState.Idle);
            return;
        }
        catch (Exception)
        {
            this.ReportOutcome("Update download failed. The current version keeps running.");
            this.RefreshState();
            return;
        }

        this.SetState(UpdateControlLogic.AfterDownload(
            result.Status,
            this.workflow.IsDownloading,
            this.workflow.LastKnownAvailableVersion is not null));

        this.ReportOutcome(result.Status switch
        {
            UpdateDownloadStatus.Downloaded => $"Update {result.AvailableVersion} downloaded. Restart CLIHub to apply it.",
            UpdateDownloadStatus.Failed => "Update download failed. The current version keeps running.",
            UpdateDownloadStatus.NoUpdate => "No update is available to download.",
            UpdateDownloadStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "An update download is already running."
        });
    }

    private void ApplyDownloadedUpdate()
    {
        try
        {
            this.workflow.ApplyDownloadedUpdateAndRestart();
        }
        catch (Exception ex)
        {
            this.ReportOutcome($"Could not apply the update: {ex.Message}");
            this.SetState(UpdateControlState.Available);
        }
    }

    private void OnUpdateStateChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(this.RefreshState);
            return;
        }

        this.RefreshState();
    }

    /// <summary>
    ///   Removes the shared update-state event subscription.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.workflow.UpdateStateChanged -= this.OnUpdateStateChanged;
    }

    private void RefreshState()
    {
        if (this.disposed || this.state == UpdateControlState.ReadyToApply)
        {
            return;
        }

        this.SetState(this.workflow.IsDownloading
            ? UpdateControlState.Downloading
            : this.workflow.IsCheckingForUpdates
                ? UpdateControlState.Checking
                : this.workflow.LastKnownAvailableVersion is not null
                    ? UpdateControlState.Available
                    : UpdateControlState.Idle);
    }

    private void SetState(UpdateControlState state)
    {
        if (this.state == state)
        {
            return;
        }

        this.state = state;
        this.OnPropertyChanged(nameof(this.UpdateButtonText));
        this.OnPropertyChanged(nameof(this.IsUpdateActionAvailable));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ReportOutcome(string message) => OutcomeReported?.Invoke(this, message);
}
