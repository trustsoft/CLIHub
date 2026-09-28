namespace CLIHub.ViewModels;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
/// A selectable default runtime with a friendly label.
/// </summary>
/// <param name="Kind">The runtime kind.</param>
/// <param name="Name">Display name shown in the selector.</param>
public sealed record RuntimeOption(RuntimeKind Kind, string Name);

/// <summary>
/// View model for the Settings window: loads preferences, validates input, and applies
/// changes on save.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private static readonly RuntimeOption[] RuntimeOptions =
    {
        new(RuntimeKind.CommandPrompt, "cmd — Command Prompt"),
        new(RuntimeKind.PowerShell, "ps — PowerShell"),
        new(RuntimeKind.WindowsTerminal, "wt — Windows Terminal")
    };

    private readonly IConfigService _configService;
    private readonly IUpdateService _updateService;
    private readonly IPreferenceApplier _applier;

    private RuntimeOption _selectedRuntime = RuntimeOptions[2];
    private string _hotkeyText = string.Empty;
    private string _probeTtlText = string.Empty;
    private string _probeTimeoutText = string.Empty;
    private bool _checkForUpdatesOnStartup = true;
    private string _updateMessage = string.Empty;
    private string? _validationError;

    public SettingsViewModel(
        IConfigService configService,
        IUpdateService updateService,
        IPreferenceApplier applier)
    {
        _configService = configService;
        _updateService = updateService;
        _applier = applier;

        Version = _updateService.GetCurrentVersion();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        CheckForUpdatesCommand = new RelayCommand(() => _ = CheckForUpdatesAsync());
    }

    /// <summary>Raised when the window should close (on Save or Cancel).</summary>
    public event EventHandler? RequestClose;

    public IReadOnlyList<RuntimeOption> Runtimes => RuntimeOptions;

    public RuntimeOption SelectedRuntime
    {
        get => _selectedRuntime;
        set => SetProperty(ref _selectedRuntime, value);
    }

    public string HotkeyText
    {
        get => _hotkeyText;
        set => SetProperty(ref _hotkeyText, value);
    }

    public string ProbeTtlText
    {
        get => _probeTtlText;
        set => SetProperty(ref _probeTtlText, value);
    }

    public string ProbeTimeoutText
    {
        get => _probeTimeoutText;
        set => SetProperty(ref _probeTimeoutText, value);
    }

    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set => SetProperty(ref _checkForUpdatesOnStartup, value);
    }

    public string UpdateMessage
    {
        get => _updateMessage;
        private set => SetProperty(ref _updateMessage, value);
    }

    public string? ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    public string Version { get; }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand CheckForUpdatesCommand { get; }

    /// <summary>Reloads all fields from the stored configuration.</summary>
    public void Load()
    {
        var prefs = _configService.Load().Preferences;

        _selectedRuntime = RuntimeOptions
            .FirstOrDefault(o => o.Kind == RuntimeKinds.Parse(prefs.DefaultRuntime)) ?? RuntimeOptions[2];
        _hotkeyText = string.IsNullOrWhiteSpace(prefs.Hotkey)
            ? HotkeyParser.Format(HotkeyParser.Default)
            : prefs.Hotkey;
        _probeTtlText = prefs.AgentProbeTtlMinutes?.ToString() ?? string.Empty;
        _probeTimeoutText = prefs.AgentProbeTimeoutSeconds?.ToString() ?? string.Empty;
        _checkForUpdatesOnStartup = prefs.CheckForUpdatesOnStartup;
        _updateMessage = string.Empty;
        _validationError = null;

        OnPropertyChanged(nameof(SelectedRuntime));
        OnPropertyChanged(nameof(HotkeyText));
        OnPropertyChanged(nameof(ProbeTtlText));
        OnPropertyChanged(nameof(ProbeTimeoutText));
        OnPropertyChanged(nameof(CheckForUpdatesOnStartup));
        OnPropertyChanged(nameof(UpdateMessage));
        OnPropertyChanged(nameof(ValidationError));
    }

    /// <summary>Sets the hotkey field from a captured combination; clears any error.</summary>
    public void SetCapturedHotkey(HotkeyDefinition definition)
    {
        HotkeyText = HotkeyParser.Format(definition);
        ValidationError = null;
    }

    /// <summary>Reports an invalid hotkey capture without changing the current value.</summary>
    public void SetHotkeyError(string message) => ValidationError = message;

    public void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

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

        var config = _configService.Load();
        var prefs = config.Preferences;
        prefs.DefaultRuntime = RuntimeKinds.ToToken(SelectedRuntime.Kind);
        prefs.Hotkey = HotkeyParser.Format(hotkeyDefinition!);
        prefs.AgentProbeTtlMinutes = ttl;
        prefs.AgentProbeTimeoutSeconds = timeout;
        prefs.CheckForUpdatesOnStartup = CheckForUpdatesOnStartup;
        _configService.Save(config);

        _applier.ApplyRuntime(SelectedRuntime.Kind);
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
