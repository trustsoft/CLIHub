namespace CLIHub.ViewModels;

using System.Windows;
using System.Windows.Input;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;

/// <summary>
///   State and command for an update control: shows the current version when idle, checks for
///   updates, downloads an available update, and applies it with a restart. Shared by every
///   window that hosts an update control; reflects checks and downloads started anywhere.
/// </summary>
public sealed class UpdateControlViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdateControlViewModel> _logger;

    private UpdateControlState _state = UpdateControlState.Idle;

    /// <summary>
    ///   Raised with a verbose outcome message that the host window shows in its status line.
    /// </summary>
    public event EventHandler<string>? OutcomeReported;

    /// <summary>
    ///   Creates the update control state for the given update service.
    /// </summary>
    /// <param name="updateService"> Update service backing the checks, downloads, and applies. </param>
    /// <param name="logger"> Logger for unexpected update control failures. </param>
    public UpdateControlViewModel(
        IUpdateService updateService,
        ILogger<UpdateControlViewModel> logger)
    {
        _updateService = updateService;
        _logger = logger;

        CurrentVersion = updateService.GetCurrentVersion();

        UpdateControlCommand = new RelayCommand(
            () => _ = RunUpdateControlAsync(),
            () => _state is UpdateControlState.Idle or UpdateControlState.Available or UpdateControlState.ReadyToApply);

        _updateService.UpdateStateChanged += OnUpdateStateChanged;
        RefreshState();
    }

    private Task RunUpdateControlAsync() =>
        AsyncOperationRunner.RunAsync(
            "Update control",
            HandleUpdateControlAsync,
            _logger,
            ReportOutcome);

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
        UpdateControlState.Available => $"Update to {_updateService.LastKnownAvailableVersion}",
        UpdateControlState.Checking => "Checking…",
        UpdateControlState.Downloading => _updateService.LastKnownAvailableVersion is { } downloading
            ? $"Downloading {downloading}…"
            : "Downloading…",
        UpdateControlState.ReadyToApply => _updateService.LastKnownAvailableVersion is { } ready
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

    private async Task HandleUpdateControlAsync()
    {
        switch (_state)
        {
            case UpdateControlState.Available:
                await DownloadUpdateAsync();
                break;
            case UpdateControlState.ReadyToApply:
                ApplyDownloadedUpdate();
                break;
            default:
                await CheckForUpdatesAsync();
                break;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        SetState(UpdateControlState.Checking);
        ReportOutcome("Checking for updates...");

        UpdateCheckResult result;
        try
        {
            result = await _updateService.CheckForUpdatesAsync();
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

    private async Task DownloadUpdateAsync()
    {
        SetState(UpdateControlState.Downloading);

        UpdateDownloadResult result;
        try
        {
            result = await _updateService.DownloadUpdateAsync();
        }
        catch (Exception)
        {
            ReportOutcome("Update download failed. The current version keeps running.");
            RefreshState();
            return;
        }

        SetState(UpdateControlLogic.AfterDownload(
            result.Status,
            _updateService.IsDownloading,
            _updateService.LastKnownAvailableVersion is not null));

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
            _updateService.ApplyDownloadedUpdateAndRestart();
        }
        catch (Exception ex)
        {
            ReportOutcome($"Could not apply the update: {ex.Message}");
            SetState(UpdateControlState.Available);
        }
    }

    private void OnUpdateStateChanged(object? sender, EventArgs e) =>
        Application.Current?.Dispatcher.BeginInvoke(RefreshState);

    private void RefreshState() =>
        SetState(UpdateControlLogic.Derive(
            _state,
            _updateService.IsDownloading,
            _updateService.LastKnownAvailableVersion is not null));

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
