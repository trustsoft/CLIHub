namespace CLIHub.ViewModels;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Agents;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;

/// <summary>
///   A selectable default runtime with a friendly label and a short segment label.
/// </summary>
/// <param name="Kind"> The runtime kind. </param>
/// <param name="Name"> Display name shown in tooltips and diagnostics. </param>
/// <param name="Segment"> Short label rendered by the segmented selector. </param>
public sealed record RuntimeOption(RuntimeKind Kind, string Name, string Segment);

/// <summary>
///   A selectable path display style with a friendly label and a short segment label.
/// </summary>
/// <param name="Style"> The path display style. </param>
/// <param name="Name"> Display name shown in tooltips and diagnostics. </param>
/// <param name="Segment"> Short label rendered by the segmented selector. </param>
public sealed record PathDisplayOption(PathDisplayStyle Style, string Name, string Segment);

/// <summary>
///   View model for the Settings window: loads preferences, validates input, and applies
///   changes on save.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private static readonly RuntimeOption[] RuntimeOptions =
    {
        new(RuntimeKind.CommandPrompt, "cmd — Command Prompt", "cmd"),
        new(RuntimeKind.PowerShell, "ps — PowerShell", "ps"),
        new(RuntimeKind.WindowsTerminal, "wt — Windows Terminal", "wt")
    };

    private static readonly PathDisplayOption[] PathDisplayOptions =
    {
        new(PathDisplayStyle.LeftTrim, "Left trim — keep the end of the path", "Left trim"),
        new(PathDisplayStyle.MiddleEllipsis, "Middle ellipsis — keep both ends", "Middle ellipsis")
    };

    private readonly SettingsApplicationService _settingsApplication;
    private readonly IUpdateService _updateService;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly ILogger<SettingsViewModel> _logger;

    private bool _startWithWindows;
    private bool _showWindowOnStartup = true;
    private RuntimeOption _selectedRuntime = RuntimeOptions[2];
    private PathDisplayOption _selectedPathDisplay = PathDisplayOptions[0];
    private string _hotkeyText = string.Empty;
    private IReadOnlyList<string> _hotkeyParts = [];
    private string _probeTtlText = string.Empty;
    private string _probeTimeoutText = string.Empty;
    private bool _checkForUpdatesOnStartup = true;
    private string _updateMessage = string.Empty;
    private string? _validationError;

    /// <summary>
    ///   Creates the view model with its services.
    /// </summary>
    /// <param name="settingsApplication"> Typed Settings draft and application service. </param>
    /// <param name="updateService"> Update service used for the version and update checks. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked update checks. </param>
    /// <param name="logger"> Logger for unexpected settings update-check failures. </param>
    public SettingsViewModel(
        SettingsApplicationService settingsApplication,
        IUpdateService updateService,
        IApplicationOperationLifetime operationLifetime,
        ILogger<SettingsViewModel> logger)
    {
        _settingsApplication = settingsApplication;
        _updateService = updateService;
        _operationLifetime = operationLifetime;
        _logger = logger;

        Version = _updateService.GetCurrentVersion();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        CheckForUpdatesCommand = new RelayCommand(() =>
            _ = _operationLifetime.RunAsync("Settings update check", RunUpdateCheckAsync));
    }

    /// <summary>
    ///   Raised when the window should close (on Save or Cancel).
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///   The runtime options offered by the segmented control.
    /// </summary>
    public IReadOnlyList<RuntimeOption> Runtimes => RuntimeOptions;

    /// <summary>
    ///   The path display options offered by the segmented control.
    /// </summary>
    public IReadOnlyList<PathDisplayOption> PathDisplays => PathDisplayOptions;

    /// <summary>
    ///   The selected path display style.
    /// </summary>
    public PathDisplayOption SelectedPathDisplay
    {
        get => _selectedPathDisplay;
        set => SetProperty(ref _selectedPathDisplay, value);
    }

    /// <summary>
    ///   Whether the application starts with Windows.
    /// </summary>
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    /// <summary>
    ///   Whether the launch window is shown at startup.
    /// </summary>
    public bool ShowWindowOnStartup
    {
        get => _showWindowOnStartup;
        set => SetProperty(ref _showWindowOnStartup, value);
    }

    /// <summary>
    ///   The selected default runtime.
    /// </summary>
    public RuntimeOption SelectedRuntime
    {
        get => _selectedRuntime;
        set => SetProperty(ref _selectedRuntime, value);
    }

    /// <summary>
    ///   The global hotkey in its canonical textual form.
    /// </summary>
    public string HotkeyText
    {
        get => _hotkeyText;
        set
        {
            if (SetProperty(ref _hotkeyText, value))
            {
                HotkeyParts = SplitHotkeyParts(value);
            }
        }
    }

    /// <summary>
    ///   The captured combination split into key-chip tokens, for the segmented hotkey field.
    /// </summary>
    public IReadOnlyList<string> HotkeyParts
    {
        get => _hotkeyParts;
        private set => SetProperty(ref _hotkeyParts, value);
    }

    private static IReadOnlyList<string> SplitHotkeyParts(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    ///   The agent probe TTL as text; empty means the built-in default.
    /// </summary>
    public string ProbeTtlText
    {
        get => _probeTtlText;
        set => SetProperty(ref _probeTtlText, value);
    }

    /// <summary>
    ///   The agent probe timeout as text; empty means the built-in default.
    /// </summary>
    public string ProbeTimeoutText
    {
        get => _probeTimeoutText;
        set => SetProperty(ref _probeTimeoutText, value);
    }

    /// <summary>
    ///   Watermark for the TTL field: the built-in default shown while the field is empty.
    /// </summary>
    public string ProbeTtlWatermark => ((int)AgentVersionService.DefaultTtl.TotalMinutes).ToString();

    /// <summary>
    ///   Watermark for the timeout field: the built-in default shown while the field is empty.
    /// </summary>
    public string ProbeTimeoutWatermark => ((int)AgentVersionService.DefaultProbeTimeout.TotalSeconds).ToString();

    /// <summary>
    ///   Whether updates are checked on startup.
    /// </summary>
    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set => SetProperty(ref _checkForUpdatesOnStartup, value);
    }

    /// <summary>
    ///   The status line of the update check.
    /// </summary>
    public string UpdateMessage
    {
        get => _updateMessage;
        private set => SetProperty(ref _updateMessage, value);
    }

    /// <summary>
    ///   The current validation error, or null when the form is valid.
    /// </summary>
    public string? ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    /// <summary>
    ///   The running application version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///   Persists the form and applies the preferences.
    /// </summary>
    public RelayCommand SaveCommand { get; }

    /// <summary>
    ///   Closes the window without saving.
    /// </summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>
    ///   Runs an update check and reports the outcome in <see cref="UpdateMessage"/>.
    /// </summary>
    public RelayCommand CheckForUpdatesCommand { get; }

    private Task RunUpdateCheckAsync(CancellationToken cancellationToken) =>
        AsyncOperationRunner.RunAsync(
            "Settings update check",
            () => CheckForUpdatesAsync(cancellationToken),
            _logger,
            message => UpdateMessage = message,
            cancellationToken);

    /// <summary>
    ///   Reloads all fields from the stored configuration.
    /// </summary>
    public void Load()
    {
        var draft = _settingsApplication.Load();

        _selectedRuntime = RuntimeOptions
            .FirstOrDefault(o => o.Kind == draft.Runtime) ?? RuntimeOptions[2];
        _selectedPathDisplay = PathDisplayOptions
            .FirstOrDefault(o => o.Style == draft.PathDisplayStyle) ?? PathDisplayOptions[0];
        _hotkeyText = HotkeyParser.Format(draft.Hotkey);
        _hotkeyParts = SplitHotkeyParts(_hotkeyText);
        _probeTtlText = draft.ProbeTtlMinutes?.ToString() ?? string.Empty;
        _probeTimeoutText = draft.ProbeTimeoutSeconds?.ToString() ?? string.Empty;
        _checkForUpdatesOnStartup = draft.CheckForUpdatesOnStartup;
        _startWithWindows = draft.StartWithWindows;
        _showWindowOnStartup = draft.ShowWindowOnStartup;
        _updateMessage = string.Empty;
        _validationError = null;

        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(ShowWindowOnStartup));
        OnPropertyChanged(nameof(SelectedRuntime));
        OnPropertyChanged(nameof(SelectedPathDisplay));
        OnPropertyChanged(nameof(HotkeyText));
        OnPropertyChanged(nameof(HotkeyParts));
        OnPropertyChanged(nameof(ProbeTtlText));
        OnPropertyChanged(nameof(ProbeTimeoutText));
        OnPropertyChanged(nameof(CheckForUpdatesOnStartup));
        OnPropertyChanged(nameof(UpdateMessage));
        OnPropertyChanged(nameof(ValidationError));
    }

    /// <summary>
    ///   Sets the hotkey field from a captured combination; clears any error.
    /// </summary>
    public void SetCapturedHotkey(HotkeyDefinition definition)
    {
        HotkeyText = HotkeyParser.Format(definition);
        ValidationError = null;
    }

    /// <summary>
    ///   Reports an invalid hotkey capture without changing the current value.
    /// </summary>
    public void SetHotkeyError(string message) => ValidationError = message;

    /// <summary>
    ///   Closes the window without saving.
    /// </summary>
    public void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Checks for updates and reports the outcome in <see cref="UpdateMessage"/>.
    /// </summary>
    /// <returns> A task that completes when the check finishes. </returns>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        UpdateMessage = "Checking for updates...";

        UpdateCheckResult result;
        try
        {
            result = await _updateService.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UpdateMessage = "Update check cancelled.";
            return;
        }

        UpdateMessage = result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };
    }

    private void Save()
    {
        var input = new SettingsInput(
            SelectedRuntime.Kind,
            SelectedPathDisplay.Style,
            HotkeyText,
            ProbeTtlText,
            ProbeTimeoutText,
            StartWithWindows,
            ShowWindowOnStartup,
            CheckForUpdatesOnStartup);

        if (!_settingsApplication.TryCreateDraft(input, out var draft, out var validationError))
        {
            ValidationError = validationError;
            return;
        }

        var result = _settingsApplication.Save(draft!);
        if (!result.Success)
        {
            ValidationError = result.Error;
            return;
        }

        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
