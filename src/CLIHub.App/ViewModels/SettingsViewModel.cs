using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsStore _settings;
    private readonly Func<string, string?> _applyHotkey;
    private readonly Func<Task<UpdateCheckOutcome>> _checkUpdates;
    private readonly Action<Action> _postToUi;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _checkUpdatesCommand;

    private string _runtime;
    private string _hotkey;
    private string _ttlMinutes;
    private string _timeoutSeconds;
    private bool _checkOnStartup;
    private bool _checkRunning;
    private string _statusText = string.Empty;

    public SettingsViewModel(
        SettingsStore settings,
        Func<string, string?> applyHotkey,
        Func<Task<UpdateCheckOutcome>> checkUpdates,
        Action<Action> postToUi)
    {
        _settings = settings;
        _applyHotkey = applyHotkey;
        _checkUpdates = checkUpdates;
        _postToUi = postToUi;

        _runtime = settings.Runtime ?? RuntimeResolver.DefaultRuntime;
        _hotkey = settings.Hotkey;
        _ttlMinutes = settings.Probe.TtlMinutes?.ToString() ?? string.Empty;
        _timeoutSeconds = settings.Probe.TimeoutSeconds?.ToString() ?? string.Empty;
        _checkOnStartup = settings.Update.EffectiveCheckOnStartup;

        _saveCommand = new RelayCommand(_ => Save(), _ => !_checkRunning);
        _checkUpdatesCommand = new RelayCommand(_ => _ = RunCheckAsync(), _ => !_checkRunning);
    }

    public IReadOnlyList<string> RuntimeOptions { get; } = new[] { "cmd", "ps", "wt" };

    public ICommand SaveCommand => _saveCommand;

    public ICommand CheckUpdatesCommand => _checkUpdatesCommand;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Runtime
    {
        get => _runtime;
        set
        {
            if (_runtime == value)
            {
                return;
            }

            _runtime = value;
            OnPropertyChanged();
        }
    }

    public string Hotkey
    {
        get => _hotkey;
        set
        {
            if (_hotkey == value)
            {
                return;
            }

            _hotkey = value;
            OnPropertyChanged();
        }
    }

    public string TtlMinutes
    {
        get => _ttlMinutes;
        set
        {
            if (_ttlMinutes == value)
            {
                return;
            }

            _ttlMinutes = value;
            OnPropertyChanged();
        }
    }

    public string TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (_timeoutSeconds == value)
            {
                return;
            }

            _timeoutSeconds = value;
            OnPropertyChanged();
        }
    }

    public bool CheckOnStartup
    {
        get => _checkOnStartup;
        set
        {
            if (_checkOnStartup == value)
            {
                return;
            }

            _checkOnStartup = value;
            OnPropertyChanged();
        }
    }

    public bool CheckRunning
    {
        get => _checkRunning;
        private set
        {
            if (_checkRunning == value)
            {
                return;
            }

            _checkRunning = value;
            OnPropertyChanged();
            _saveCommand.RaiseCanExecuteChanged();
            _checkUpdatesCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public void OnUpdateReady(string version) => _postToUi(() =>
        StatusText = string.IsNullOrWhiteSpace(version)
            ? "Update ready to install — see the tray."
            : $"Update {version} is ready to install — see the tray.");

    private void Save()
    {
        if (!TryBuildDocument(out var document, out var error))
        {
            StatusText = error ?? "Check the entered values.";
            return;
        }

        string? hotkeyError = null;
        if (document.Hotkey != _settings.Hotkey)
        {
            hotkeyError = _applyHotkey(document.Hotkey);
            if (hotkeyError is not null)
            {
                document.Hotkey = _settings.Hotkey;
            }
        }

        var result = _settings.Save(document);
        if (!result.Saved)
        {
            if (hotkeyError is null && document.Hotkey != _settings.Hotkey)
            {
                _applyHotkey(_settings.Hotkey);
            }

            StatusText = string.Join(" ", result.Errors);
            return;
        }

        if (hotkeyError is not null)
        {
            Hotkey = document.Hotkey;
            StatusText = $"Saved. Hotkey unchanged: {hotkeyError}";
            return;
        }

        StatusText = "Settings saved.";
    }

    private bool TryBuildDocument(out SettingsDocument document, out string? error)
    {
        error = null;

        if (!TryParseProbeValue(TtlMinutes, "Probe TTL", out var ttl, out error) ||
            !TryParseProbeValue(TimeoutSeconds, "Probe timeout", out var timeout, out error))
        {
            document = new SettingsDocument();
            return false;
        }

        document = new SettingsDocument
        {
            Runtime = Runtime,
            Hotkey = Hotkey,
            Probe = new ProbeConfig { TtlMinutes = ttl, TimeoutSeconds = timeout },
            Update = new UpdateConfig { CheckOnStartup = CheckOnStartup }
        };
        return true;
    }

    private static bool TryParseProbeValue(string text, string label, out int? value, out string? error)
    {
        value = null;
        error = null;

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        if (!int.TryParse(trimmed, out var parsed) || parsed <= 0)
        {
            error = $"{label} must be a positive number.";
            return false;
        }

        value = parsed;
        return true;
    }

    private async Task RunCheckAsync()
    {
        CheckRunning = true;
        StatusText = "Checking for updates…";
        try
        {
            var outcome = await _checkUpdates();
            StatusText = outcome switch
            {
                UpdateCheckOutcome.UpToDate => "You have the latest version.",
                UpdateCheckOutcome.Ready => "Update ready to install — see the tray.",
                UpdateCheckOutcome.NotInstalled => "Update checks are available only in the installed app.",
                _ => "Could not check for updates."
            };
        }
        catch (Exception)
        {
            StatusText = "Could not check for updates.";
        }
        finally
        {
            CheckRunning = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
