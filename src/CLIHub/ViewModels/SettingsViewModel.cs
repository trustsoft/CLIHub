namespace CLIHub.ViewModels;

using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;

/// <summary>
///   A selectable default runtime with a friendly label, a short segment label, and an icon.
/// </summary>
/// <param name="Kind"> The runtime kind. </param>
/// <param name="Name"> Display name shown in tooltips and diagnostics. </param>
/// <param name="Segment"> Short label rendered by the segmented selector. </param>
/// <param name="Icon"> Pack URI of the icon rendered beside the segment label. </param>
public sealed record RuntimeOption(RuntimeKind Kind, string Name, string Segment, string Icon);

/// <summary>
///   A selectable path display style with a friendly label and a short segment label.
/// </summary>
/// <param name="Style"> The path display style. </param>
/// <param name="Name"> Display name shown in tooltips and diagnostics. </param>
/// <param name="Segment"> Short label rendered by the segmented selector. </param>
public sealed record PathDisplayOption(PathDisplayStyle Style, string Name, string Segment);

/// <summary>
///   A selectable log level.
/// </summary>
/// <param name="Value"> The stored level name. </param>
/// <param name="Name"> Display name. </param>
public sealed record LogLevelOption(string Value, string Name);

/// <summary>
///   A selectable popup screen position.
/// </summary>
/// <param name="Position"> The position. </param>
/// <param name="Name"> Display name. </param>
public sealed record PopupPositionOption(PopupScreenPosition Position, string Name);

/// <summary>
///   A selectable application theme.
/// </summary>
/// <param name="Theme"> The theme. </param>
/// <param name="Name"> Display name. </param>
public sealed record AppThemeOption(AppTheme Theme, string Name);

/// <summary>
///   A Settings navigation page: a stable key, a display title, the group it belongs to, and
///   whether its settings are implemented yet.
/// </summary>
/// <param name="Key"> Stable page key used to route the page view. </param>
/// <param name="Title"> Display title shown in the sidebar and page header. </param>
/// <param name="Group"> The navigation group the page belongs to. </param>
/// <param name="IsImplemented"> Whether the page has implemented settings. </param>
public sealed record SettingsPage(string Key, string Title, string Group, bool IsImplemented);

/// <summary>
///   A Settings navigation group: an uppercase label and its pages.
/// </summary>
/// <param name="Title"> The group label. </param>
/// <param name="Pages"> The pages in the group, in display order. </param>
public sealed record SettingsGroup(string Title, IReadOnlyList<SettingsPage> Pages);

/// <summary>
///   View model for the Settings window: loads preferences, validates input, and applies
///   changes on save.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private static readonly RuntimeOption[] RuntimeOptions =
    {
        new(RuntimeKind.PowerShell, "PowerShell", "PowerShell", "pack://application:,,,/Assets/powershell.png"),
        new(RuntimeKind.PowerShell7, "PowerShell 7", "PowerShell 7", "pack://application:,,,/Assets/powershell7.png"),
        new(RuntimeKind.CommandPrompt, "Command Prompt", "Command Prompt", "pack://application:,,,/Assets/cmd.png"),
        new(RuntimeKind.GitBash, "Git Bash", "Git Bash", "pack://application:,,,/Assets/gitbash.png")
    };

    private static readonly PathDisplayOption[] PathDisplayOptions =
    {
        new(PathDisplayStyle.LeftTrim, "Left trim — keep the end of the path", "Left trim"),
        new(PathDisplayStyle.MiddleEllipsis, "Middle ellipsis — keep both ends", "Middle ellipsis")
    };

    private static readonly SettingsPage[] AllPages =
    {
        new("general", "General & Startup", "General", true),
        new("hotkeys", "Hotkeys & Launchers", "General", true),
        new("projects", "Projects & Paths", "Engines & Repos", false),
        new("agents", "CLI Agents", "Engines & Repos", true),
        new("terminal", "Terminal Profiles", "Engines & Repos", true),
        new("appearance", "Appearance", "System", true),
        new("telemetry", "Telemetry & Logs", "System", true),
        new("updates", "Updates", "System", true)
    };

    private static readonly LogLevelOption[] LogLevelOptions =
    {
        new("Verbose", "Verbose"),
        new("Debug", "Debug"),
        new("Information", "Information"),
        new("Warning", "Warning"),
        new("Error", "Error")
    };

    private static readonly PopupPositionOption[] PopupPositionOptions =
    {
        new(PopupScreenPosition.Center, "Center of active monitor"),
        new(PopupScreenPosition.TopCenter, "Top-center"),
        new(PopupScreenPosition.CursorRelative, "Cursor-relative"),
        new(PopupScreenPosition.PrimaryScreen, "Primary screen")
    };

    private static readonly AppThemeOption[] AppThemeOptions =
    {
        new(AppTheme.Dark, "Dark"),
        new(AppTheme.Light, "Light"),
        new(AppTheme.System, "System")
    };

    private static readonly SettingsGroup[] PagesByGroup = AllPages
        .GroupBy(page => page.Group)
        .Select(group => new SettingsGroup(group.Key, group.ToArray()))
        .ToArray();

    private readonly SettingsApplicationService _settingsApplication;
    private readonly IUpdateVersionProvider _versionProvider;
    private readonly IUpdateWorkflow _updateWorkflow;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly ILogger<SettingsViewModel> _logger;

    private bool _startWithWindows;
    private bool _startMinimized = true;
    private RuntimeOption _selectedRuntime = RuntimeOptions[0];
    private PathDisplayOption _selectedPathDisplay = PathDisplayOptions[0];
    private string _hotkeyText = string.Empty;
    private IReadOnlyList<string> _hotkeyParts = [];
    private string _probeTtlText = string.Empty;
    private string _probeTimeoutText = string.Empty;
    private bool _checkForUpdatesOnStartup = true;
    private bool _autoDownloadUpdates = true;
    private bool _notifyStableUpdates = true;
    private bool _notifyPrereleaseUpdates;
    private string _updateMessage = string.Empty;
    private string? _validationError;
    private SettingsPage _selectedPage = AllPages[0];
    private readonly IExternalLauncher _externalLauncher;
    private LogLevelOption _selectedLogLevel = LogLevelOptions[2];
    private string _logRetentionDaysText = "7";
    private string _logFileSizeLimitText = "0";
    private PopupPositionOption _selectedPopupPosition = PopupPositionOptions[0];
    private AppThemeOption _selectedAppTheme = AppThemeOptions[0];
    private string _accentText = AccentPalette.DefaultHex;

    /// <summary>
    ///   Creates the view model with its services.
    /// </summary>
    /// <param name="settingsApplication"> Typed Settings draft and application service. </param>
    /// <param name="versionProvider"> Provides the current application version. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked update checks. </param>
    /// <param name="externalLauncher"> Operating-system path launcher for opening the log folder. </param>
    /// <param name="logger"> Logger for unexpected settings update-check failures. </param>
    public SettingsViewModel(
        SettingsApplicationService settingsApplication,
        IUpdateVersionProvider versionProvider,
        IUpdateWorkflow updateWorkflow,
        IApplicationOperationLifetime operationLifetime,
        IExternalLauncher externalLauncher,
        ILogger<SettingsViewModel> logger)
    {
        _settingsApplication = settingsApplication;
        _versionProvider = versionProvider;
        _updateWorkflow = updateWorkflow;
        _operationLifetime = operationLifetime;
        _externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        _logger = logger;

        Version = _versionProvider.GetCurrentVersion();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        CheckForUpdatesCommand = new RelayCommand(() =>
            _ = _operationLifetime.RunAsync("Settings update check", RunUpdateCheckAsync));
        CopyLogPathCommand = new RelayCommand(() => Clipboard.SetText(LogFilePath));
        OpenLogFolderCommand = new RelayCommand(() => _externalLauncher.Open(AppPaths.LogsDirectory));
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
    ///   The navigation pages, in display order.
    /// </summary>
    public IReadOnlyList<SettingsPage> Pages => AllPages;

    /// <summary>
    ///   The navigation groups and their pages, in display order.
    /// </summary>
    public IReadOnlyList<SettingsGroup> PageGroups => PagesByGroup;

    /// <summary>
    ///   The currently selected navigation page.
    /// </summary>
    public SettingsPage SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

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
    ///   Whether the application starts minimized in the system tray instead of showing the window.
    /// </summary>
    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
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
    ///   Whether available updates are downloaded automatically in the background.
    /// </summary>
    public bool AutoDownloadUpdates
    {
        get => _autoDownloadUpdates;
        set => SetProperty(ref _autoDownloadUpdates, value);
    }

    /// <summary>
    ///   Whether stable update availability is notified.
    /// </summary>
    public bool NotifyStableUpdates
    {
        get => _notifyStableUpdates;
        set => SetProperty(ref _notifyStableUpdates, value);
    }

    /// <summary>
    ///   Whether pre-release versions are checked and notified.
    /// </summary>
    public bool NotifyPrereleaseUpdates
    {
        get => _notifyPrereleaseUpdates;
        set => SetProperty(ref _notifyPrereleaseUpdates, value);
    }

    /// <summary>
    ///   The log level options offered by the selector.
    /// </summary>
    public IReadOnlyList<LogLevelOption> LogLevels => LogLevelOptions;

    /// <summary>
    ///   The selected log level.
    /// </summary>
    public LogLevelOption SelectedLogLevel
    {
        get => _selectedLogLevel;
        set => SetProperty(ref _selectedLogLevel, value);
    }

    /// <summary>
    ///   The number of daily log files to retain, as text.
    /// </summary>
    public string LogRetentionDaysText
    {
        get => _logRetentionDaysText;
        set => SetProperty(ref _logRetentionDaysText, value);
    }

    /// <summary>
    ///   The per-file log size limit in megabytes, as text (0 = unlimited).
    /// </summary>
    public string LogFileSizeLimitText
    {
        get => _logFileSizeLimitText;
        set => SetProperty(ref _logFileSizeLimitText, value);
    }

    /// <summary>
    ///   The current log file path.
    /// </summary>
    public string LogFilePath => LogFileLocator.CurrentLogFilePath(AppPaths.LogsDirectory);

    /// <summary>
    ///   Copies the current log file path to the clipboard.
    /// </summary>
    public RelayCommand CopyLogPathCommand { get; }

    /// <summary>
    ///   Opens the logs folder in the system file browser.
    /// </summary>
    public RelayCommand OpenLogFolderCommand { get; }

    /// <summary>
    ///   The popup screen position options offered by the selector.
    /// </summary>
    public IReadOnlyList<PopupPositionOption> PopupPositions => PopupPositionOptions;

    /// <summary>
    ///   The selected popup screen position.
    /// </summary>
    public PopupPositionOption SelectedPopupPosition
    {
        get => _selectedPopupPosition;
        set => SetProperty(ref _selectedPopupPosition, value);
    }

    /// <summary>
    ///   The application theme options offered by the selector.
    /// </summary>
    public IReadOnlyList<AppThemeOption> AppThemes => AppThemeOptions;

    /// <summary>
    ///   The selected application theme.
    /// </summary>
    public AppThemeOption SelectedAppTheme
    {
        get => _selectedAppTheme;
        set => SetProperty(ref _selectedAppTheme, value);
    }

    /// <summary>
    ///   The accent color presets offered by the selector.
    /// </summary>
    public IReadOnlyList<AccentOption> AccentOptions => AccentPalette.Presets;

    /// <summary>
    ///   The accent color text as <c>#RRGGBB</c>.
    /// </summary>
    public string AccentText
    {
        get => _accentText;
        set
        {
            if (SetProperty(ref _accentText, value))
            {
                OnPropertyChanged(nameof(SelectedAccent));
            }
        }
    }

    /// <summary>
    ///   The preset matching the current accent, or null for a custom value.
    /// </summary>
    public AccentOption? SelectedAccent
    {
        get => AccentOptions.FirstOrDefault(o =>
            string.Equals(AccentPalette.Normalize(o.Hex), AccentPalette.Normalize(_accentText), StringComparison.Ordinal));
        set
        {
            if (value is not null)
            {
                AccentText = value.Hex;
            }
        }
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
            .FirstOrDefault(o => o.Kind == draft.Runtime) ?? RuntimeOptions[0];
        _selectedPathDisplay = PathDisplayOptions
            .FirstOrDefault(o => o.Style == draft.PathDisplayStyle) ?? PathDisplayOptions[0];
        _hotkeyText = HotkeyParser.Format(draft.Hotkey);
        _hotkeyParts = SplitHotkeyParts(_hotkeyText);
        _probeTtlText = draft.ProbeTtlMinutes?.ToString() ?? string.Empty;
        _probeTimeoutText = draft.ProbeTimeoutSeconds?.ToString() ?? string.Empty;
        _checkForUpdatesOnStartup = draft.CheckForUpdatesOnStartup;
        _autoDownloadUpdates = draft.AutoDownloadUpdates;
        _notifyStableUpdates = draft.NotifyStableUpdates;
        _notifyPrereleaseUpdates = draft.NotifyPrereleaseUpdates;
        _selectedLogLevel = LogLevelOptions.FirstOrDefault(o => o.Value == draft.LogLevel) ?? LogLevelOptions[2];
        _logRetentionDaysText = draft.LogRetentionDays.ToString();
        _logFileSizeLimitText = draft.LogFileSizeLimitMb.ToString();
        _selectedPopupPosition = PopupPositionOptions.FirstOrDefault(o => o.Position == draft.PopupScreenPosition)
            ?? PopupPositionOptions[0];
        _selectedAppTheme = AppThemeOptions.FirstOrDefault(o => o.Theme == draft.AppTheme) ?? AppThemeOptions[0];
        _accentText = AccentPalette.Normalize(draft.AccentColor);
        _startWithWindows = draft.StartWithWindows;
        _startMinimized = draft.StartMinimized;
        _updateMessage = string.Empty;
        _validationError = null;
        _selectedPage = AllPages[0];

        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(StartMinimized));
        OnPropertyChanged(nameof(SelectedRuntime));
        OnPropertyChanged(nameof(SelectedPathDisplay));
        OnPropertyChanged(nameof(HotkeyText));
        OnPropertyChanged(nameof(HotkeyParts));
        OnPropertyChanged(nameof(ProbeTtlText));
        OnPropertyChanged(nameof(ProbeTimeoutText));
        OnPropertyChanged(nameof(CheckForUpdatesOnStartup));
        OnPropertyChanged(nameof(AutoDownloadUpdates));
        OnPropertyChanged(nameof(NotifyStableUpdates));
        OnPropertyChanged(nameof(NotifyPrereleaseUpdates));
        OnPropertyChanged(nameof(SelectedLogLevel));
        OnPropertyChanged(nameof(LogRetentionDaysText));
        OnPropertyChanged(nameof(LogFileSizeLimitText));
        OnPropertyChanged(nameof(SelectedPopupPosition));
        OnPropertyChanged(nameof(SelectedAppTheme));
        OnPropertyChanged(nameof(AccentText));
        OnPropertyChanged(nameof(SelectedAccent));
        OnPropertyChanged(nameof(UpdateMessage));
        OnPropertyChanged(nameof(ValidationError));
        OnPropertyChanged(nameof(SelectedPage));
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
            result = await _updateWorkflow.CheckForUpdatesAsync(cancellationToken);
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
            StartMinimized,
            CheckForUpdatesOnStartup,
            AutoDownloadUpdates,
            NotifyStableUpdates,
            NotifyPrereleaseUpdates,
            SelectedLogLevel.Value,
            LogRetentionDaysText,
            LogFileSizeLimitText,
            SelectedPopupPosition.Position,
            SelectedAppTheme.Theme,
            AccentText);

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
