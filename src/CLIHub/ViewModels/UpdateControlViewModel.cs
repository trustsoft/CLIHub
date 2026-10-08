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
    private readonly IUpdateWorkflow _workflow;
    private readonly ILogger<UpdateControlViewModel> _logger;
    private readonly IApplicationOperationLifetime _operationLifetime;

    private UpdateControlState _state = UpdateControlState.Idle;
    private bool _disposed;

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
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _logger = logger;
        _operationLifetime = operationLifetime;

        CurrentVersion = workflow.GetCurrentVersion();

        UpdateControlCommand = new RelayCommand(
            () => _ = _operationLifetime.RunAsync("Update control", RunUpdateControlAsync),
            () => _state is UpdateControlState.Idle or UpdateControlState.Available or UpdateControlState.ReadyToApply);

        _workflow.UpdateStateChanged += OnUpdateStateChanged;
        RefreshState();
    }

    private Task RunUpdateControlAsync(CancellationToken cancellationToken) =>
        AsyncOperationRunner.RunAsync(
            "Update control",
            () => HandleUpdateControlAsync(cancellationToken),
            _logger,
            ReportOutcome,
            cancellationToken);

    /// <summary>
    ///   The current application version, shown while the control is idle.
    /// </summary>
    public string CurrentVersion { get; }

    /// <summary>
    ///   The label of the control for the current state: the running version when idle, the
    ///   progress state while busy, and the update action when one is available.
    /// </summary>
    public string UpdateButtonText => _state switch
    {
        UpdateControlState.Available => $"Update to {_workflow.LastKnownAvailableVersion}",
        UpdateControlState.Checking => "Checking…",
        UpdateControlState.Downloading => _workflow.LastKnownAvailableVersion is { } downloading
            ? $"Downloading {downloading}…"
            : "Downloading…",
        UpdateControlState.ReadyToApply => _workflow.LastKnownAvailableVersion is { } ready
            ? $"Restart to update to {ready}"
            : "Restart to update",
        _ => CurrentVersion,
    };

    /// <summary>
    ///   Whether the control currently offers an update action, which drives its accent styling.
    /// </summary>
    public bool IsUpdateActionAvailable =>
        _state is UpdateControlState.Available or UpdateControlState.ReadyToApply;

    /// <summary>
    ///   Routes control clicks by state: checks for updates when idle, downloads the update when
    ///   one is available, and applies it with a restart once downloaded.
    /// </summary>
    public RelayCommand UpdateControlCommand { get; }

    private async Task HandleUpdateControlAsync(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case UpdateControlState.Available:
                await DownloadUpdateAsync(cancellationToken);
                break;
            case UpdateControlState.ReadyToApply:
                ApplyDownloadedUpdate();
                break;
            default:
                await CheckForUpdatesAsync(cancellationToken);
                break;
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        SetState(UpdateControlState.Checking);
        ReportOutcome("Checking for updates...");

        UpdateCheckResult result;
        try
        {
            result = await _workflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(UpdateControlState.Idle);
            return;
        }
        catch (Exception)
        {
            SetState(UpdateControlState.Idle);
            ReportOutcome("Update check failed or timed out.");
            return;
        }

        ReportOutcome(result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        });

        SetState(result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is not null
            ? UpdateControlState.Available
            : UpdateControlState.Idle);
    }

    private async Task DownloadUpdateAsync(CancellationToken cancellationToken)
    {
        SetState(UpdateControlState.Downloading);

        UpdateDownloadResult result;
        try
        {
            result = await _workflow.DownloadUpdateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(UpdateControlState.Idle);
            return;
        }
        catch (Exception)
        {
            ReportOutcome("Update download failed. The current version keeps running.");
            RefreshState();
            return;
        }

        SetState(UpdateControlLogic.AfterDownload(
            result.Status,
            _workflow.IsDownloading,
            _workflow.LastKnownAvailableVersion is not null));

        ReportOutcome(result.Status switch
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
        _workflow.ApplyDownloadedUpdateAndRestart();
        }
        catch (Exception ex)
        {
            ReportOutcome($"Could not apply the update: {ex.Message}");
            SetState(UpdateControlState.Available);
        }
    }

    private void OnUpdateStateChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(RefreshState);
            return;
        }

        RefreshState();
    }

    /// <summary>
    ///   Removes the shared update-state event subscription.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _workflow.UpdateStateChanged -= OnUpdateStateChanged;
    }

    private void RefreshState()
    {
        if (_disposed || _state == UpdateControlState.ReadyToApply)
        {
            return;
        }

        SetState(_workflow.IsDownloading
            ? UpdateControlState.Downloading
            : _workflow.IsCheckingForUpdates
                ? UpdateControlState.Checking
                : _workflow.LastKnownAvailableVersion is not null
                    ? UpdateControlState.Available
                    : UpdateControlState.Idle);
    }

    private void SetState(UpdateControlState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        OnPropertyChanged(nameof(UpdateButtonText));
        OnPropertyChanged(nameof(IsUpdateActionAvailable));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ReportOutcome(string message) => OutcomeReported?.Invoke(this, message);
}
