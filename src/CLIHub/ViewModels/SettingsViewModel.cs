namespace CLIHub.ViewModels;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
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

    private readonly IPreferencesStore _preferencesStore;
    private readonly IUpdateService _updateService;
    private readonly IStartupService _startupService;
    private readonly IPreferenceApplier _applier;
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
    /// <param name="preferencesStore"> Store used to load and save preferences. </param>
    /// <param name="updateService"> Update service used for the version and update checks. </param>
    /// <param name="startupService"> Startup service used by the start-with-Windows toggle. </param>
    /// <param name="applier"> Preference applier invoked on save. </param>
    /// <param name="logger"> Logger for unexpected settings update-check failures. </param>
    public SettingsViewModel(
        IPreferencesStore preferencesStore,
        IUpdateService updateService,
        IStartupService startupService,
        IPreferenceApplier applier,
        ILogger<SettingsViewModel> logger)
    {
        _preferencesStore = preferencesStore;
        _updateService = updateService;
        _startupService = startupService;
        _applier = applier;
        _logger = logger;

        Version = _updateService.GetCurrentVersion();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        CheckForUpdatesCommand = new RelayCommand(() => _ = RunUpdateCheckAsync());
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

    private Task RunUpdateCheckAsync() =>
        AsyncOperationRunner.RunAsync(
            "Settings update check",
            CheckForUpdatesAsync,
            _logger,
            message => UpdateMessage = message);

    /// <summary>
    ///   Reloads all fields from the stored configuration.
    /// </summary>
    public void Load()
    {
        var prefs = _preferencesStore.Load();

        _selectedRuntime = RuntimeOptions
            .FirstOrDefault(o => o.Kind == RuntimeKinds.Parse(prefs.DefaultRuntime)) ?? RuntimeOptions[2];
        _selectedPathDisplay = PathDisplayOptions
            .FirstOrDefault(o => o.Style == PathDisplayStyles.Parse(prefs.PathDisplayStyle)) ?? PathDisplayOptions[0];
        _hotkeyText = string.IsNullOrWhiteSpace(prefs.Hotkey)
            ? HotkeyParser.Format(HotkeyParser.Default)
            : prefs.Hotkey;
        _hotkeyParts = SplitHotkeyParts(_hotkeyText);
        _probeTtlText = prefs.AgentProbeTtlMinutes?.ToString() ?? string.Empty;
        _probeTimeoutText = prefs.AgentProbeTimeoutSeconds?.ToString() ?? string.Empty;
        _checkForUpdatesOnStartup = prefs.CheckForUpdatesOnStartup;
        _startWithWindows = _startupService.IsEnabled();
        _showWindowOnStartup = prefs.ShowWindowOnStartup;
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
    public async Task CheckForUpdatesAsync()
    {
        UpdateMessage = "Checking for updates...";

        var result = await _updateService.CheckForUpdatesAsync();

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
        if (!Validate(out var hotkeyDefinition, out var ttl, out var timeout))
        {
            return;
        }

        if (!_applier.ApplyStartWithWindows(StartWithWindows))
        {
            ValidationError = "Could not update the Windows startup registration. Changes were not saved.";
            StartWithWindows = _startupService.IsEnabled();
            return;
        }

        _preferencesStore.Update(prefs =>
        {
            prefs.DefaultRuntime = RuntimeKinds.ToToken(SelectedRuntime.Kind);
            prefs.Hotkey = HotkeyParser.Format(hotkeyDefinition!);
            prefs.AgentProbeTtlMinutes = ttl;
            prefs.AgentProbeTimeoutSeconds = timeout;
            prefs.CheckForUpdatesOnStartup = CheckForUpdatesOnStartup;
            prefs.StartWithWindows = StartWithWindows;
            prefs.ShowWindowOnStartup = ShowWindowOnStartup;
            prefs.PathDisplayStyle = PathDisplayStyles.ToToken(SelectedPathDisplay.Style);
        });

        _applier.ApplyRuntime(SelectedRuntime.Kind);
        _applier.ApplyPathDisplayStyle(SelectedPathDisplay.Style);
        _applier.ApplyHotkey(hotkeyDefinition!);
        _applier.ApplyStartupUpdateCheck(CheckForUpdatesOnStartup);

        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private bool Validate(out HotkeyDefinition? hotkeyDefinition, out int? ttl, out int? timeout)
    {
        hotkeyDefinition = null;
        ttl = null;
        timeout = null;
        ValidationError = null;

        if (!HotkeyParser.TryParse(HotkeyText, out hotkeyDefinition) || hotkeyDefinition == null)
        {
            ValidationError = "Hotkey needs a modifier (Ctrl, Shift, Alt, Win) and a key.";
            return false;
        }

        if (!TryParsePositive(ProbeTtlText, out ttl))
        {
            ValidationError = "Probe TTL must be a positive number of minutes, or empty for the default.";
            return false;
        }

        if (!TryParsePositive(ProbeTimeoutText, out timeout))
        {
            ValidationError = "Probe timeout must be a positive number of seconds, or empty for the default.";
            return false;
        }

        return true;
    }

    private static bool TryParsePositive(string? text, out int? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (int.TryParse(text.Trim(), out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
