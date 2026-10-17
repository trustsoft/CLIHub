namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub.Themes;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;
using CLIHub.Core.Scanning;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Updates;
using CLIHub.Settings;

/// <summary>
///   A selectable default runtime with a friendly label and an icon.
/// </summary>
/// <param name="Kind"> The runtime kind. </param>
/// <param name="Name"> Display name shown in the combo box and diagnostics. </param>
/// <param name="Icon"> Pack URI of the icon rendered beside the name. </param>
public sealed record RuntimeOption(RuntimeKind Kind, string Name, string Icon);

/// <summary>
///   A selectable path display style with a friendly label.
/// </summary>
/// <param name="Style"> The path display style. </param>
/// <param name="Name"> Display name shown in the combo box and tooltips. </param>
public sealed record PathDisplayOption(PathDisplayStyle Style, string Name);

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
///   A selectable repository discovery mode.
/// </summary>
/// <param name="Mode"> The discovery mode. </param>
/// <param name="Name"> Display name. </param>
public sealed record DiscoveryModeOption(ProjectDiscoveryMode Mode, string Name);

/// <summary>
///   A Settings navigation page: a stable key, a display title, the group it belongs to, and
///   whether its settings are implemented yet.
/// </summary>
/// <param name="Key"> Stable page key used to route the page view. </param>
/// <param name="Title"> Display title shown in the sidebar and page header. </param>
/// <param name="Group"> The navigation group the page belongs to. </param>
/// <param name="IsImplemented"> Whether the page has implemented settings. </param>
/// <param name="Icon"> The Segoe MDL2 glyph shown before the page title in the sidebar. </param>
public sealed record SettingsPage(string Key, string Title, string Group, bool IsImplemented, string Icon);

/// <summary>
///   A Settings navigation group: an uppercase label and its pages.
/// </summary>
/// <param name="Title"> The group label. </param>
/// <param name="Pages"> The pages in the group, in display order. </param>
public sealed record SettingsGroup(string Title, IReadOnlyList<SettingsPage> Pages);

/// <summary>
///   View model for the Settings window: loads preferences,
///   validates input, and applies changes on save.
/// </summary>
public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    private static readonly RuntimeOption[] RuntimeOptions =
    {
        new(RuntimeKind.PowerShell, "PowerShell", "pack://application:,,,/Assets/powershell.png"),
        new(RuntimeKind.PowerShell7, "PowerShell 7", "pack://application:,,,/Assets/powershell7.png"),
        new(RuntimeKind.CommandPrompt, "Command Prompt", "pack://application:,,,/Assets/cmd.png"),
        new(RuntimeKind.GitBash, "Git Bash", "pack://application:,,,/Assets/gitbash.png")
    };

    private static readonly PathDisplayOption[] PathDisplayOptions =
    {
        new(PathDisplayStyle.LeftTrim, "Left trim — keep the end of the path"),
        new(PathDisplayStyle.MiddleEllipsis, "Middle ellipsis — keep both ends")
    };

    private static readonly SettingsPage[] AllPages =
    {
        new("general", "General & Startup", "General", true, IconGlyphs.Settings),
        new("hotkeys", "Hotkeys & Quick Launcher", "General", true, IconGlyphs.Keyboard),
        new("projects", "Projects & Paths", "Engines & Repos", true, IconGlyphs.FolderOpen),
        new("agents", "CLI Agents", "Engines & Repos", true, IconGlyphs.Robot),
        new("terminal", "Terminal Profiles", "Engines & Repos", true, IconGlyphs.Terminal),
        new("appearance", "Appearance", "System", true, IconGlyphs.Palette),
        new("telemetry", "Logs", "System", true, IconGlyphs.Diagnostic),
        new("updates", "Updates", "System", true, IconGlyphs.Update)
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

    private static readonly DiscoveryModeOption[] DiscoveryModeOptions =
    {
        new(ProjectDiscoveryMode.Git, "Git repositories"),
        new(ProjectDiscoveryMode.Agents, "Agent folders"),
        new(ProjectDiscoveryMode.Both, "Both")
    };

    private static readonly SettingsGroup[] PagesByGroup = AllPages
        .GroupBy(page => page.Group)
        .Select(group => new SettingsGroup(group.Key, group.ToArray()))
        .ToArray();

    private readonly SettingsApplicationService settingsApplication;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly IProjectDialogService dialogs;
    private readonly IProjectScanner projectScanner;
    private readonly ILogger<SettingsViewModel> logger;

    private bool startWithWindows;
    private bool startMinimized = true;
    private RuntimeOption selectedRuntime = RuntimeOptions[0];
    private PathDisplayOption selectedPathDisplay = PathDisplayOptions[0];
    private string hotkeyText = string.Empty;
    private IReadOnlyList<string> hotkeyParts = [];
    private string probeTtlText = string.Empty;
    private string probeTimeoutText = string.Empty;
    private bool checkForUpdatesOnStartup = true;
    private bool autoDownloadUpdates = true;
    private bool notifyStableUpdates = true;
    private bool notifyPrereleaseUpdates;
    private string updateMessage = string.Empty;
    private string? validationError;
    private SettingsPage selectedPage = AllPages[0];
    private readonly IExternalLauncher externalLauncher;
    private LogLevelOption selectedLogLevel = LogLevelOptions[2];
    private string logRetentionDaysText = "7";
    private string logFileSizeLimitText = "0";
    private PopupPositionOption selectedPopupPosition = PopupPositionOptions[0];
    private AppThemeOption selectedAppTheme = AppThemeOptions[0];
    private string accentText = AccentPalette.DefaultHex;
    private string scanDepthText = ProjectScanningDefaults.DefaultScanDepth.ToString();
    private DiscoveryModeOption selectedDiscoveryMode = DiscoveryModeOptions[2];
    private string newIgnoredPatternText = string.Empty;
    private string? selectedWatchedDirectory;
    private string? selectedIgnoredPattern;
    private string watcherStatusText = "No watched directories configured.";
    private bool disposed;

    /// <summary>
    ///   Creates the view model with its services.
    /// </summary>
    /// <param name="settingsApplication"> Typed Settings draft and application service. </param>
    /// <param name="versionProvider"> Provides the current application version. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked update checks. </param>
    /// <param name="externalLauncher"> Operating-system path launcher for opening the log folder. </param>
    /// <param name="logger"> Logger for unexpected settings update-check failures. </param>
    /// <param name="dialogs"> Folder picker used to add watched directories. </param>
    /// <param name="projectScanner"> Scanner whose status is shown on the Projects &amp; Paths page. </param>
    /// <param name="agentsState"> Agent-management section shown on the CLI Agents page. </param>
    public SettingsViewModel(SettingsApplicationService settingsApplication,
                             IUpdateVersionProvider versionProvider,
                             IUpdateWorkflow updateWorkflow,
                             IApplicationOperationLifetime operationLifetime,
                             IExternalLauncher externalLauncher,
                             IProjectDialogService dialogs,
                             IProjectScanner projectScanner,
                             IAgentManagementSection agentsState,
                             ILogger<SettingsViewModel> logger)
    {
        this.settingsApplication = settingsApplication;
        this.versionProvider = versionProvider;
        this.updateWorkflow = updateWorkflow;
        this.operationLifetime = operationLifetime;
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.AgentsState = agentsState ?? throw new ArgumentNullException(nameof(agentsState));
        this.logger = logger;

        this.Version = this.versionProvider.GetCurrentVersion();

        this.SaveCommand = new RelayCommand(this.Save);
        this.CancelCommand = new RelayCommand(this.Cancel);
        this.CheckForUpdatesCommand = new RelayCommand(() =>
            _ = this.operationLifetime.RunAsync("Settings update check", this.RunUpdateCheckAsync));
        this.CopyLogPathCommand = new RelayCommand(() => Clipboard.SetText(this.LogFilePath));
        this.OpenLogFolderCommand = new RelayCommand(() => this.externalLauncher.Open(AppPaths.LogsDirectory));
        this.AddWatchedDirectoryCommand = new RelayCommand(this.AddWatchedDirectory);
        this.RemoveWatchedDirectoryCommand = new RelayCommand(this.RemoveWatchedDirectory);
        this.AddIgnoredPatternCommand = new RelayCommand(this.AddIgnoredPattern);
        this.RemoveIgnoredPatternCommand = new RelayCommand(this.RemoveIgnoredPattern);

        this.projectScanner.StatusChanged += this.OnScannerStatusChanged;
        this.watcherStatusText = FormatScannerStatus(this.projectScanner.Status);
    }

    private const string NoWatchedDirectoriesStatus = "No watched directories configured.";

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
    ///   The agent-management section shown on the CLI Agents page.
    /// </summary>
    public IAgentManagementSection AgentsState { get; }

    /// <summary>
    ///   The currently selected navigation page.
    /// </summary>
    public SettingsPage SelectedPage
    {
        get => this.selectedPage;
        set => this.SetProperty(ref this.selectedPage, value);
    }

    /// <summary>
    ///   The selected path display style.
    /// </summary>
    public PathDisplayOption SelectedPathDisplay
    {
        get => this.selectedPathDisplay;
        set => this.SetProperty(ref this.selectedPathDisplay, value);
    }

    /// <summary>
    ///   Whether the application starts with Windows.
    /// </summary>
    public bool StartWithWindows
    {
        get => this.startWithWindows;
        set => this.SetProperty(ref this.startWithWindows, value);
    }

    /// <summary>
    ///   Whether the application starts minimized in the system tray instead of showing the window.
    /// </summary>
    public bool StartMinimized
    {
        get => this.startMinimized;
        set => this.SetProperty(ref this.startMinimized, value);
    }

    /// <summary>
    ///   The selected default runtime.
    /// </summary>
    public RuntimeOption SelectedRuntime
    {
        get => this.selectedRuntime;
        set => this.SetProperty(ref this.selectedRuntime, value);
    }

    /// <summary>
    ///   The global hotkey in its canonical textual form.
    /// </summary>
    public string HotkeyText
    {
        get => this.hotkeyText;
        set
        {
            if (this.SetProperty(ref this.hotkeyText, value))
            {
                this.HotkeyParts = SplitHotkeyParts(value);
            }
        }
    }

    /// <summary>
    ///   The captured combination split into key-chip tokens, for the segmented hotkey field.
    /// </summary>
    public IReadOnlyList<string> HotkeyParts
    {
        get => this.hotkeyParts;
        private set => this.SetProperty(ref this.hotkeyParts, value);
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
        get => this.probeTtlText;
        set => this.SetProperty(ref this.probeTtlText, value);
    }

    /// <summary>
    ///   The agent probe timeout as text; empty means the built-in default.
    /// </summary>
    public string ProbeTimeoutText
    {
        get => this.probeTimeoutText;
        set => this.SetProperty(ref this.probeTimeoutText, value);
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
        get => this.checkForUpdatesOnStartup;
        set => this.SetProperty(ref this.checkForUpdatesOnStartup, value);
    }

    /// <summary>
    ///   Whether available updates are downloaded automatically in the background.
    /// </summary>
    public bool AutoDownloadUpdates
    {
        get => this.autoDownloadUpdates;
        set => this.SetProperty(ref this.autoDownloadUpdates, value);
    }

    /// <summary>
    ///   Whether stable update availability is notified.
    /// </summary>
    public bool NotifyStableUpdates
    {
        get => this.notifyStableUpdates;
        set => this.SetProperty(ref this.notifyStableUpdates, value);
    }

    /// <summary>
    ///   Whether pre-release versions are checked and notified.
    /// </summary>
    public bool NotifyPrereleaseUpdates
    {
        get => this.notifyPrereleaseUpdates;
        set => this.SetProperty(ref this.notifyPrereleaseUpdates, value);
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
        get => this.selectedLogLevel;
        set => this.SetProperty(ref this.selectedLogLevel, value);
    }

    /// <summary>
    ///   The number of daily log files to retain, as text.
    /// </summary>
    public string LogRetentionDaysText
    {
        get => this.logRetentionDaysText;
        set => this.SetProperty(ref this.logRetentionDaysText, value);
    }

    /// <summary>
    ///   The per-file log size limit in megabytes, as text (0 = unlimited).
    /// </summary>
    public string LogFileSizeLimitText
    {
        get => this.logFileSizeLimitText;
        set => this.SetProperty(ref this.logFileSizeLimitText, value);
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
        get => this.selectedPopupPosition;
        set => this.SetProperty(ref this.selectedPopupPosition, value);
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
        get => this.selectedAppTheme;
        set => this.SetProperty(ref this.selectedAppTheme, value);
    }

    /// <summary>
    ///   The accent color presets offered by the selector.
    /// </summary>
    public IReadOnlyList<AccentOption> AccentOptions => AccentPalette.Presets;

    /// <summary>
    ///   The accent color text as <c> #RRGGBB </c>.
    /// </summary>
    public string AccentText
    {
        get => this.accentText;
        set
        {
            if (this.SetProperty(ref this.accentText, value))
            {
                this.OnPropertyChanged(nameof(this.SelectedAccent));
            }
        }
    }

    /// <summary>
    ///   The preset matching the current accent, or null for a custom value.
    /// </summary>
    public AccentOption? SelectedAccent
    {
        get => this.AccentOptions.FirstOrDefault(o =>
            string.Equals(AccentPalette.Normalize(o.Hex), AccentPalette.Normalize(this.accentText), StringComparison.Ordinal));
        set
        {
            if (value is not null)
            {
                this.AccentText = value.Hex;
            }
        }
    }

    /// <summary>
    ///   Watched root directories edited on the Projects &amp; Paths page.
    /// </summary>
    public ObservableCollection<string> WatchedDirectories { get; } = new();

    /// <summary>
    ///   The watched directory selected for removal.
    /// </summary>
    public string? SelectedWatchedDirectory
    {
        get => this.selectedWatchedDirectory;
        set => this.SetProperty(ref this.selectedWatchedDirectory, value);
    }

    /// <summary>
    ///   The ignored directory-name patterns edited on the Projects &amp; Paths page.
    /// </summary>
    public ObservableCollection<string> IgnoredPatterns { get; } = new();

    /// <summary>
    ///   The ignored pattern selected for removal.
    /// </summary>
    public string? SelectedIgnoredPattern
    {
        get => this.selectedIgnoredPattern;
        set => this.SetProperty(ref this.selectedIgnoredPattern, value);
    }

    /// <summary>
    ///   The ignored pattern pending to be added.
    /// </summary>
    public string NewIgnoredPatternText
    {
        get => this.newIgnoredPatternText;
        set => this.SetProperty(ref this.newIgnoredPatternText, value);
    }

    /// <summary>
    ///   The repository scan depth as text.
    /// </summary>
    public string ScanDepthText
    {
        get => this.scanDepthText;
        set => this.SetProperty(ref this.scanDepthText, value);
    }

    /// <summary>
    ///   The repository discovery mode options offered by the selector.
    /// </summary>
    public IReadOnlyList<DiscoveryModeOption> DiscoveryModes => DiscoveryModeOptions;

    /// <summary>
    ///   The selected repository discovery mode.
    /// </summary>
    public DiscoveryModeOption SelectedDiscoveryMode
    {
        get => this.selectedDiscoveryMode;
        set => this.SetProperty(ref this.selectedDiscoveryMode, value);
    }

    /// <summary>
    ///   A read-only description of the background repository scanner status.
    /// </summary>
    public string WatcherStatusText
    {
        get => this.watcherStatusText;
        private set => this.SetProperty(ref this.watcherStatusText, value);
    }

    /// <summary>
    ///   Adds the folder selected by the user to the watched-directories list.
    /// </summary>
    public RelayCommand AddWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Removes the selected watched directory.
    /// </summary>
    public RelayCommand RemoveWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Adds the entered ignored pattern.
    /// </summary>
    public RelayCommand AddIgnoredPatternCommand { get; }

    /// <summary>
    ///   Removes the selected ignored pattern.
    /// </summary>
    public RelayCommand RemoveIgnoredPatternCommand { get; }

    /// <summary>
    ///   The status line of the update check.
    /// </summary>
    public string UpdateMessage
    {
        get => this.updateMessage;
        private set => this.SetProperty(ref this.updateMessage, value);
    }

    /// <summary>
    ///   The current validation error, or null when the form is valid.
    /// </summary>
    public string? ValidationError
    {
        get => this.validationError;
        private set => this.SetProperty(ref this.validationError, value);
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
    ///   Runs an update check and reports the outcome in <see cref="UpdateMessage" />.
    /// </summary>
    public RelayCommand CheckForUpdatesCommand { get; }

    private Task RunUpdateCheckAsync(CancellationToken cancellationToken) =>
        AsyncOperationRunner.RunAsync(
            "Settings update check",
            () => this.CheckForUpdatesAsync(cancellationToken),
            this.logger,
            message => this.UpdateMessage = message,
            cancellationToken);

    /// <summary>
    ///   Reloads all fields from the stored configuration.
    /// </summary>
    public void Load()
    {
        var draft = this.settingsApplication.Load();

        this.selectedRuntime = RuntimeOptions
            .FirstOrDefault(o => o.Kind == draft.Runtime) ?? RuntimeOptions[0];
        this.selectedPathDisplay = PathDisplayOptions
            .FirstOrDefault(o => o.Style == draft.PathDisplayStyle) ?? PathDisplayOptions[0];
        this.hotkeyText = HotkeyParser.Format(draft.Hotkey);
        this.hotkeyParts = SplitHotkeyParts(this.hotkeyText);
        this.probeTtlText = draft.ProbeTtlMinutes?.ToString() ?? string.Empty;
        this.probeTimeoutText = draft.ProbeTimeoutSeconds?.ToString() ?? string.Empty;
        this.checkForUpdatesOnStartup = draft.CheckForUpdatesOnStartup;
        this.autoDownloadUpdates = draft.AutoDownloadUpdates;
        this.notifyStableUpdates = draft.NotifyStableUpdates;
        this.notifyPrereleaseUpdates = draft.NotifyPrereleaseUpdates;
        this.selectedLogLevel = LogLevelOptions.FirstOrDefault(o => o.Value == draft.LogLevel) ?? LogLevelOptions[2];
        this.logRetentionDaysText = draft.LogRetentionDays.ToString();
        this.logFileSizeLimitText = draft.LogFileSizeLimitMb.ToString();
        this.selectedPopupPosition = PopupPositionOptions.FirstOrDefault(o => o.Position == draft.PopupScreenPosition)
            ?? PopupPositionOptions[0];
        this.selectedAppTheme = AppThemeOptions.FirstOrDefault(o => o.Theme == draft.AppTheme) ?? AppThemeOptions[0];
        this.accentText = AccentPalette.Normalize(draft.AccentColor);
        this.startWithWindows = draft.StartWithWindows;
        this.startMinimized = draft.StartMinimized;

        this.WatchedDirectories.Clear();
        foreach (var directory in draft.WatchedDirectories ?? Array.Empty<string>())
        {
            this.WatchedDirectories.Add(directory);
        }

        this.IgnoredPatterns.Clear();
        foreach (var pattern in draft.IgnoredPatterns ?? Array.Empty<string>())
        {
            this.IgnoredPatterns.Add(pattern);
        }

        this.scanDepthText = draft.ScanDepth.ToString();
        this.selectedDiscoveryMode = DiscoveryModeOptions.FirstOrDefault(o => o.Mode == draft.DiscoveryMode)
            ?? DiscoveryModeOptions[2];
        this.newIgnoredPatternText = string.Empty;
        this.selectedWatchedDirectory = null;
        this.selectedIgnoredPattern = null;
        this.watcherStatusText = FormatScannerStatus(this.projectScanner.Status);

        this.AgentsState.Load(draft.DefaultAgentId);

        this.updateMessage = string.Empty;
        this.validationError = null;
        this.selectedPage = AllPages[0];

        this.OnPropertyChanged(nameof(this.StartWithWindows));
        this.OnPropertyChanged(nameof(this.StartMinimized));
        this.OnPropertyChanged(nameof(this.SelectedRuntime));
        this.OnPropertyChanged(nameof(this.SelectedPathDisplay));
        this.OnPropertyChanged(nameof(this.HotkeyText));
        this.OnPropertyChanged(nameof(this.HotkeyParts));
        this.OnPropertyChanged(nameof(this.ProbeTtlText));
        this.OnPropertyChanged(nameof(this.ProbeTimeoutText));
        this.OnPropertyChanged(nameof(this.CheckForUpdatesOnStartup));
        this.OnPropertyChanged(nameof(this.AutoDownloadUpdates));
        this.OnPropertyChanged(nameof(this.NotifyStableUpdates));
        this.OnPropertyChanged(nameof(this.NotifyPrereleaseUpdates));
        this.OnPropertyChanged(nameof(this.SelectedLogLevel));
        this.OnPropertyChanged(nameof(this.LogRetentionDaysText));
        this.OnPropertyChanged(nameof(this.LogFileSizeLimitText));
        this.OnPropertyChanged(nameof(this.SelectedPopupPosition));
        this.OnPropertyChanged(nameof(this.SelectedAppTheme));
        this.OnPropertyChanged(nameof(this.AccentText));
        this.OnPropertyChanged(nameof(this.SelectedAccent));
        this.OnPropertyChanged(nameof(this.ScanDepthText));
        this.OnPropertyChanged(nameof(this.SelectedDiscoveryMode));
        this.OnPropertyChanged(nameof(this.NewIgnoredPatternText));
        this.OnPropertyChanged(nameof(this.SelectedWatchedDirectory));
        this.OnPropertyChanged(nameof(this.SelectedIgnoredPattern));
        this.OnPropertyChanged(nameof(this.WatcherStatusText));
        this.OnPropertyChanged(nameof(this.UpdateMessage));
        this.OnPropertyChanged(nameof(this.ValidationError));
        this.OnPropertyChanged(nameof(this.SelectedPage));
    }

    /// <summary>
    ///   Sets the hotkey field from a captured combination; clears any error.
    /// </summary>
    public void SetCapturedHotkey(HotkeyDefinition definition)
    {
        this.HotkeyText = HotkeyParser.Format(definition);
        this.ValidationError = null;
    }

    /// <summary>
    ///   Reports an invalid hotkey capture without changing the current value.
    /// </summary>
    public void SetHotkeyError(string message) => this.ValidationError = message;

    /// <summary>
    ///   Closes the window without saving.
    /// </summary>
    public void Cancel() => this.RequestClose?.Invoke(this, EventArgs.Empty);

    private void AddWatchedDirectory()
    {
        var folder = this.dialogs.SelectFolder("Add Watched Directory");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        if (!this.WatchedDirectories.Any(existing => string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase)))
        {
            this.WatchedDirectories.Add(folder);
        }

        this.ValidationError = null;
    }

    private void RemoveWatchedDirectory()
    {
        if (this.SelectedWatchedDirectory is { } selected)
        {
            this.WatchedDirectories.Remove(selected);
        }
    }

    private void AddIgnoredPattern()
    {
        var pattern = this.NewIgnoredPatternText.Trim();
        if (pattern.Length == 0)
        {
            return;
        }

        if (!this.IgnoredPatterns.Any(existing => string.Equals(existing, pattern, StringComparison.OrdinalIgnoreCase)))
        {
            this.IgnoredPatterns.Add(pattern);
        }

        this.NewIgnoredPatternText = string.Empty;
    }

    private void RemoveIgnoredPattern()
    {
        if (this.SelectedIgnoredPattern is { } selected)
        {
            this.IgnoredPatterns.Remove(selected);
        }
    }

    private void OnScannerStatusChanged(object? sender, ProjectScanStatus status)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => this.WatcherStatusText = FormatScannerStatus(status));
            return;
        }

        this.WatcherStatusText = FormatScannerStatus(status);
    }

    private static string FormatScannerStatus(ProjectScanStatus? status)
    {
        if (status is null)
        {
            return NoWatchedDirectoriesStatus;
        }

        return status.State switch
        {
            ProjectScanState.NotConfigured => NoWatchedDirectoriesStatus,
            ProjectScanState.Scanning => "Scanning…",
            ProjectScanState.Error => $"Scan failed: {status.Error}",
            _ => FormatIdleStatus(status)
        };
    }

    private static string FormatIdleStatus(ProjectScanStatus status)
    {
        var when = status.LastScanUtc is { } last ? last.ToLocalTime().ToString("HH:mm:ss") : "never";
        var noun = status.DiscoveredCount == 1 ? "project" : "projects";
        return $"Watching · last scan {when} · {status.DiscoveredCount} discovered {noun}";
    }

    /// <summary>
    ///   Detaches the scanner status subscription.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projectScanner.StatusChanged -= this.OnScannerStatusChanged;
        this.AgentsState.Dispose();
    }

    /// <summary>
    ///   Checks for updates and reports the outcome in <see cref="UpdateMessage" />.
    /// </summary>
    /// <returns> A task that completes when the check finishes. </returns>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        this.UpdateMessage = "Checking for updates...";

        UpdateCheckResult result;
        try
        {
            result = await this.updateWorkflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.UpdateMessage = "Update check cancelled.";
            return;
        }

        this.UpdateMessage = result.Status switch
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
            this.SelectedRuntime.Kind,
            this.SelectedPathDisplay.Style,
            this.HotkeyText,
            this.ProbeTtlText,
            this.ProbeTimeoutText,
            this.StartWithWindows,
            this.StartMinimized,
            this.CheckForUpdatesOnStartup,
            this.AutoDownloadUpdates,
            this.NotifyStableUpdates,
            this.NotifyPrereleaseUpdates,
            this.SelectedLogLevel.Value,
            this.LogRetentionDaysText,
            this.LogFileSizeLimitText,
            this.SelectedPopupPosition.Position,
            this.SelectedAppTheme.Theme,
            this.AccentText,
            this.WatchedDirectories.ToList(),
            this.ScanDepthText,
            this.IgnoredPatterns.ToList(),
            this.SelectedDiscoveryMode.Mode,
            this.AgentsState.CurrentDefaultAgentId);

        if (!this.settingsApplication.TryCreateDraft(input, out var draft, out var validationError))
        {
            this.ValidationError = validationError;
            return;
        }

        var result = this.settingsApplication.Save(draft!);
        if (!result.Success)
        {
            this.ValidationError = result.Error;
            return;
        }

        this.RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
