namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub.Controls;
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
///   A selectable repository scan depth.
/// </summary>
/// <param name="Value"> The depth in directory levels. </param>
/// <param name="Name"> Display name. </param>
public sealed record ScanDepthOption(int Value, string Name);

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
/// <param name="Description"> One-line explanation shown under the name in the theme picker. </param>
public sealed record AppThemeOption(AppTheme Theme, string Name, string Description);

/// <summary>
///   A selectable accent color: a palette preset or the custom hex entry.
/// </summary>
/// <param name="Name"> Display name. </param>
/// <param name="Hex"> The swatch color, or <c> Transparent </c> for the custom entry. </param>
/// <param name="Hint"> The muted line under the name: the hex value or the custom-field hint. </param>
/// <param name="IsCustom"> True for the custom hex entry. </param>
public sealed record AccentChoice(string Name, string Hex, string Hint, bool IsCustom)
{
    /// <summary>
    ///   The accessible name combining the label with the color value or hint.
    /// </summary>
    public string AccessibleName => $"{this.Name} {this.Hint}";
}

/// <summary>
///   A selectable repository discovery mode.
/// </summary>
/// <param name="Mode"> The discovery mode. </param>
/// <param name="Name"> Display name. </param>
public sealed record DiscoveryModeOption(ProjectDiscoveryMode Mode, string Name);

/// <summary>
///   One watched-directory row: its path, the per-directory scan settings, and the number of
///   repositories found beneath it.
/// </summary>
public sealed class WatchedDirectoryRow : ObservableObject
{
    private ScanDepthOption selectedDepth;
    private DiscoveryModeOption selectedDiscovery;
    private int discoveredCount;

    /// <summary>
    ///   Creates a row.
    /// </summary>
    /// <param name="path"> The normalized watched root path. </param>
    /// <param name="depthOptions"> The allowed scan depths. </param>
    /// <param name="discoveryOptions"> The allowed discovery modes. </param>
    /// <param name="selectedDepth"> The row's scan depth. </param>
    /// <param name="selectedDiscovery"> The row's discovery mode. </param>
    public WatchedDirectoryRow(string path,
                               IReadOnlyList<ScanDepthOption> depthOptions,
                               IReadOnlyList<DiscoveryModeOption> discoveryOptions,
                               ScanDepthOption selectedDepth,
                               DiscoveryModeOption selectedDiscovery)
    {
        this.Path = path;
        this.DepthOptions = depthOptions;
        this.DiscoveryOptions = discoveryOptions;
        this.selectedDepth = selectedDepth;
        this.selectedDiscovery = selectedDiscovery;
    }

    /// <summary>
    ///   The normalized watched root path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///   The scan depths offered by the depth chip.
    /// </summary>
    public IReadOnlyList<ScanDepthOption> DepthOptions { get; }

    /// <summary>
    ///   The discovery modes offered by the discovery chip.
    /// </summary>
    public IReadOnlyList<DiscoveryModeOption> DiscoveryOptions { get; }

    /// <summary>
    ///   The row's scan depth.
    /// </summary>
    public ScanDepthOption SelectedDepth
    {
        get => this.selectedDepth;
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDepth, value))
            {
                this.OnPropertyChanged(nameof(this.DepthChipLabel));
            }
        }
    }

    /// <summary>
    ///   The row's discovery mode.
    /// </summary>
    public DiscoveryModeOption SelectedDiscovery
    {
        get => this.selectedDiscovery;
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDiscovery, value))
            {
                this.OnPropertyChanged(nameof(this.DiscoveryChipLabel));
            }
        }
    }

    /// <summary>
    ///   The text shown on the depth chip.
    /// </summary>
    public string DepthChipLabel => $"Depth {this.selectedDepth.Value}";

    /// <summary>
    ///   The text shown on the discovery chip.
    /// </summary>
    public string DiscoveryChipLabel => this.selectedDiscovery.Name;

    /// <summary>
    ///   The number of discovered repositories beneath this root.
    /// </summary>
    public int DiscoveredCount
    {
        get => this.discoveredCount;
        set
        {
            if (this.SetProperty(ref this.discoveredCount, value))
            {
                this.OnPropertyChanged(nameof(this.DiscoveredSummary));
            }
        }
    }

    /// <summary>
    ///   The repository count in singular/plural wording.
    /// </summary>
    public string DiscoveredSummary => this.discoveredCount == 1 ? "1 repository" : $"{this.discoveredCount} repositories";
}

/// <summary>
///   A Settings navigation page: a stable key, a display title, the group it belongs to, and
///   whether its settings are implemented yet.
/// </summary>
/// <param name="Key"> Stable page key used to route the page view. </param>
/// <param name="Title"> Display title shown in the sidebar and page header. </param>
/// <param name="Description"> One-line description shown under the page title in the page header. </param>
/// <param name="Group"> The navigation group the page belongs to. </param>
/// <param name="IsImplemented"> Whether the page has implemented settings. </param>
/// <param name="Icon"> The Segoe MDL2 glyph shown before the page title in the sidebar. </param>
public sealed record SettingsPage(
    string Key,
    string Title,
    string Description,
    string Group,
    bool IsImplemented,
    string Icon);

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
        new(
            "general",
            "General & Startup",
            "Startup behavior and how the window opens.",
            "General",
            true,
            IconGlyphs.Settings),
        new(
            "hotkeys",
            "Hotkeys & Quick Launcher",
            "The global hotkey and where the launcher appears.",
            "General",
            true,
            IconGlyphs.Keyboard),
        new(
            "projects-registry",
            "Projects",
            "Registered projects, their provenance, and manually added entries.",
            "Engines & Repos",
            true,
            IconGlyphs.FolderAdd),
        new(
            "projects",
            "Project Discovery",
            "Watched directories, discovery rules, and the background watcher.",
            "Engines & Repos",
            true,
            IconGlyphs.FolderOpen),
        new(
            "agents",
            "CLI Agents",
            "Installed CLI agents, the default agent, and probe settings.",
            "Engines & Repos",
            true,
            IconGlyphs.Robot),
        new(
            "terminal",
            "Terminal Profiles",
            "The shell used to open interactive agent commands.",
            "Engines & Repos",
            true,
            IconGlyphs.Terminal),
        new(
            "appearance",
            "Appearance",
            "Theme, accent color, and how project paths are shown.",
            "System",
            true,
            IconGlyphs.Palette),
        new(
            "telemetry",
            "Logs",
            "Log level, retention, and the log file.",
            "System",
            true,
            IconGlyphs.Diagnostic),
        new(
            "updates",
            "Updates",
            "Check for new CLIHub builds, configure update preferences, and view release notes.",
            "System",
            true,
            IconGlyphs.Update)
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
        new(PopupScreenPosition.Center, "Active monitor — centered"),
        new(PopupScreenPosition.TopCenter, "Active monitor — top-center"),
        new(PopupScreenPosition.CursorRelative, "Cursor-relative"),
        new(PopupScreenPosition.PrimaryScreen, "Primary monitor — centered")
    };

    private static readonly AppThemeOption[] AppThemeOptions =
    {
        new(AppTheme.Dark, "Dark Theme", "Windows 11 Fluent Dark (Recommended)"),
        new(AppTheme.Light, "Light Theme", "High contrast daylight profile"),
        new(AppTheme.System, "System Sync", "Follow Windows personalization state")
    };

    private static readonly AccentChoice CustomAccentChoice =
        new("Custom...", "Transparent", "RGB / Hex", true);

    private static readonly AccentChoice[] AccentChoices = AccentPalette.Presets
        .Select(preset => new AccentChoice(preset.Name, preset.Hex, preset.Hex, false))
        .Append(CustomAccentChoice)
        .ToArray();

    private static readonly DiscoveryModeOption[] DiscoveryModeOptions =
    {
        new(ProjectDiscoveryMode.Git, "Git only"),
        new(ProjectDiscoveryMode.Agents, "Agents only"),
        new(ProjectDiscoveryMode.Both, "Git and agents")
    };

    private static readonly ScanDepthOption[] ScanDepthChoices = Enumerable
        .Range(
            ProjectScanningDefaults.MinScanDepth,
            ProjectScanningDefaults.MaxScanDepth - ProjectScanningDefaults.MinScanDepth + 1)
        .Select(depth => new ScanDepthOption(
            depth,
            depth == ProjectScanningDefaults.DefaultScanDepth
                ? $"{depth} levels (default)"
                : depth == 1 ? "1 level" : $"{depth} levels"))
        .ToArray();

    private static readonly SettingsGroup[] PagesByGroup = AllPages
        .GroupBy(page => page.Group)
        .Select(group => new SettingsGroup(group.Key, group.ToArray()))
        .ToArray();

    private readonly SettingsApplicationService settingsApplication;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly IReleaseNotesLauncher releaseNotesLauncher;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly IProjectDialogService dialogs;
    private readonly IProjectScanner projectScanner;
    private readonly AccentSelectionMemory accentMemory;
    private readonly ILogger<SettingsViewModel> logger;
    private readonly List<WatchedDirectoryRow> observedWatchedDirectoryRows = new();

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
    private string updateStatusTitle = string.Empty;
    private string updateStatusDetail = string.Empty;
    private string updateStatusBadge = string.Empty;
    private string updateStatusGlyph = IconGlyphs.Info;
    private SettingBadgeTone updateStatusBadgeTone = SettingBadgeTone.Neutral;
    private string? validationError;
    private SettingsPage selectedPage = AllPages[0];
    private readonly IExternalLauncher externalLauncher;
    private LogLevelOption selectedLogLevel = LogLevelOptions[2];
    private string logRetentionDaysText = "7";
    private string logFileSizeLimitText = "0";
    private PopupPositionOption selectedPopupPosition = PopupPositionOptions[0];
    private AppThemeOption selectedAppTheme = AppThemeOptions[0];
    private string accentText = AccentPalette.DefaultHex;
    private bool customAccentSelected;
    private string scanDepthText = ProjectScanningDefaults.DefaultScanDepth.ToString();
    private DiscoveryModeOption selectedDiscoveryMode = DiscoveryModeOptions[2];
    private string newIgnoredPatternText = string.Empty;
    private string newWatchedDirectoryText = string.Empty;
    private string? watchedDirectoryError;
    private int discoveredProjectCount;
    private int manualProjectCount;
    private int scannedDirectoryCount;
    private int agentsInstalledCount;
    private int agentsSupportedCount;
    private string watcherStateLabel = "Off";
    private string watcherDetailText = "No watched roots configured";
    private bool disposed;

    /// <summary>
    ///   Creates the view model with its services.
    /// </summary>
    /// <param name="settingsApplication"> Typed Settings draft and application service. </param>
    /// <param name="versionProvider"> Provides the current application version. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="releaseNotesLauncher"> Opens the application release notes. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked update checks. </param>
    /// <param name="externalLauncher"> Operating-system path launcher for opening the log folder. </param>
    /// <param name="logger"> Logger for unexpected settings update-check failures. </param>
    /// <param name="dialogs"> Folder picker used to add watched directories. </param>
    /// <param name="projectScanner"> Scanner whose status is shown on the Project Discovery page. </param>
    /// <param name="agentsState"> Agent-management section shown on the CLI Agents page. </param>
    /// <param name="projectRegistry"> Project-registry section shown on the Projects page. </param>
    /// <param name="accentMemory"> Process-wide memory of the last custom accent. </param>
    public SettingsViewModel(SettingsApplicationService settingsApplication,
                             IUpdateVersionProvider versionProvider,
                             IUpdateWorkflow updateWorkflow,
                             IReleaseNotesLauncher releaseNotesLauncher,
                             IApplicationOperationLifetime operationLifetime,
                             IExternalLauncher externalLauncher,
                             IProjectDialogService dialogs,
                             IProjectScanner projectScanner,
                             IAgentManagementSection agentsState,
                             IProjectRegistrySection projectRegistry,
                             AccentSelectionMemory accentMemory,
                             ILogger<SettingsViewModel> logger)
    {
        this.settingsApplication = settingsApplication;
        this.versionProvider = versionProvider;
        this.updateWorkflow = updateWorkflow;
        this.releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        this.operationLifetime = operationLifetime;
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.AgentsState = agentsState ?? throw new ArgumentNullException(nameof(agentsState));
        this.ProjectRegistry = projectRegistry ?? throw new ArgumentNullException(nameof(projectRegistry));
        this.accentMemory = accentMemory ?? throw new ArgumentNullException(nameof(accentMemory));
        this.logger = logger;

        this.Version = this.versionProvider.GetCurrentVersion();
        this.ResetUpdateStatus();

        this.SaveCommand = new RelayCommand(this.Save);
        this.CancelCommand = new RelayCommand(this.Cancel);
        this.CheckForUpdatesCommand = new RelayCommand(() =>
            _ = this.operationLifetime.RunAsync("Settings update check", this.RunUpdateCheckAsync));
        this.ShowReleaseNotesCommand = new RelayCommand(this.releaseNotesLauncher.ShowReleaseNotes);
        this.CopyLogPathCommand = new RelayCommand(() => Clipboard.SetText(this.LogFilePath));
        this.OpenLogFolderCommand = new RelayCommand(() => this.externalLauncher.Open(AppPaths.LogsDirectory));
        this.BrowseWatchedDirectoryCommand = new RelayCommand(this.BrowseWatchedDirectory);
        this.AddWatchedDirectoryFromPickerCommand = new RelayCommand(this.AddWatchedDirectoryFromPicker);
        this.AddWatchedDirectoryCommand = new RelayCommand(this.AddWatchedDirectory);
        this.RemoveWatchedDirectoryCommand = new RelayCommand<string>(this.RemoveWatchedDirectory);
        this.ScanWatchedDirectoryCommand = new RelayCommand<string>(this.ScanWatchedDirectory);
        this.AddIgnoredPatternCommand = new RelayCommand(this.AddIgnoredPattern);
        this.RemoveIgnoredPatternCommand = new RelayCommand<string>(this.RemoveIgnoredPattern);
        this.ScanNowCommand = new RelayCommand(() => this.projectScanner.Reload());

        this.WatchedDirectories.CollectionChanged += this.OnWatchedDirectoriesChanged;
        this.projectScanner.StatusChanged += this.OnScannerStatusChanged;
        this.ApplyScannerStatus(this.projectScanner.Status);
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
    ///   The agent-management section shown on the CLI Agents page.
    /// </summary>
    public IAgentManagementSection AgentsState { get; }

    /// <summary>
    ///   The project-registry section shown on the Projects page.
    /// </summary>
    public IProjectRegistrySection ProjectRegistry { get; }

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
        set
        {
            if (this.SetProperty(ref this.probeTtlText, value))
            {
                this.RaiseProbePolicyChanges();
            }
        }
    }

    /// <summary>
    ///   The agent probe timeout as text; empty means the built-in default.
    /// </summary>
    public string ProbeTimeoutText
    {
        get => this.probeTimeoutText;
        set
        {
            if (this.SetProperty(ref this.probeTimeoutText, value))
            {
                this.RaiseProbePolicyChanges();
            }
        }
    }

    /// <summary>
    ///   The effective probe policy shown in the page header, e.g. "15 min · 10 s".
    /// </summary>
    public string ProbePolicySummary =>
        $"{this.ResolveProbeTtlMinutes()} min · {this.ResolveProbeTimeoutSeconds()} s";

    /// <summary>
    ///   Whether the probe card reflects built-in defaults or custom values.
    /// </summary>
    public string ProbePolicyDetail => this.HasCustomProbePolicy
        ? "Custom probe settings"
        : "Built-in defaults";

    private bool HasCustomProbePolicy =>
        !string.IsNullOrWhiteSpace(this.probeTtlText) || !string.IsNullOrWhiteSpace(this.probeTimeoutText);

    private int ResolveProbeTtlMinutes() =>
        int.TryParse(this.probeTtlText.Trim(), out var minutes) && minutes > 0
            ? minutes
            : (int)AgentVersionService.DefaultTtl.TotalMinutes;

    private int ResolveProbeTimeoutSeconds() =>
        int.TryParse(this.probeTimeoutText.Trim(), out var seconds) && seconds > 0
            ? seconds
            : (int)AgentVersionService.DefaultProbeTimeout.TotalSeconds;

    private void RaiseProbePolicyChanges()
    {
        this.OnPropertyChanged(nameof(this.ProbePolicySummary));
        this.OnPropertyChanged(nameof(this.ProbePolicyDetail));
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
    ///   The accent color choices offered by the selector: the presets and the custom entry.
    /// </summary>
    public IReadOnlyList<AccentChoice> AccentOptions => AccentChoices;

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
                var preset = this.FindAccentPreset(value);
                this.customAccentSelected = preset is null;
                if (preset is null && AccentPalette.TryNormalize(value, out var normalized))
                {
                    this.accentMemory.LastCustomAccent = normalized;
                }

                this.OnPropertyChanged(nameof(this.SelectedAccent));
                this.OnPropertyChanged(nameof(this.ActiveAccentLabel));
            }
        }
    }

    /// <summary>
    ///   The selected accent choice: the matching preset, or the custom entry for any other value.
    /// </summary>
    public AccentChoice SelectedAccent
    {
        get => this.customAccentSelected
            ? CustomAccentChoice
            : this.FindAccentPreset(this.accentText) ?? CustomAccentChoice;
        set
        {
            if (value is null)
            {
                return;
            }

            if (value.IsCustom)
            {
                if (this.accentMemory.LastCustomAccent is { } customAccent)
                {
                    this.AccentText = customAccent;
                    return;
                }

                if (this.customAccentSelected)
                {
                    return;
                }

                this.customAccentSelected = true;
                this.OnPropertyChanged(nameof(this.SelectedAccent));
                return;
            }

            this.AccentText = value.Hex;
        }
    }

    /// <summary>
    ///   The accent shown next to the selector title, e.g. "Active: Blue".
    /// </summary>
    public string ActiveAccentLabel
    {
        get
        {
            var preset = this.FindAccentPreset(this.accentText);
            return preset is null ? "Active: Custom" : $"Active: {preset.Name}";
        }
    }

    /// <summary>
    ///   Watched root directories edited on the Project Discovery page.
    /// </summary>
    public ObservableCollection<WatchedDirectoryRow> WatchedDirectories { get; } = new();

    /// <summary>
    ///   The absolute directory path pending registration.
    /// </summary>
    public string NewWatchedDirectoryText
    {
        get => this.newWatchedDirectoryText;
        set
        {
            if (this.SetProperty(ref this.newWatchedDirectoryText, value))
            {
                this.WatchedDirectoryError = null;
            }
        }
    }

    /// <summary>
    ///   The validation error shown under the watched-directory entry, or null.
    /// </summary>
    public string? WatchedDirectoryError
    {
        get => this.watchedDirectoryError;
        private set
        {
            if (this.SetProperty(ref this.watchedDirectoryError, value))
            {
                this.OnPropertyChanged(nameof(this.HasWatchedDirectoryError));
            }
        }
    }

    /// <summary>
    ///   True when <see cref="WatchedDirectoryError" /> has a message.
    /// </summary>
    public bool HasWatchedDirectoryError => !string.IsNullOrEmpty(this.watchedDirectoryError);

    /// <summary>
    ///   The watched-root count shown next to the section header.
    /// </summary>
    public string WatchedRootsSummary => this.WatchedDirectories.Count switch
    {
        0 => "No roots monitored",
        1 => "1 root monitored",
        var count => $"{count} roots monitored"
    };

    /// <summary>
    ///   The ignored directory-name patterns edited on the Project Discovery page.
    /// </summary>
    public ObservableCollection<string> IgnoredPatterns { get; } = new();

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
        set
        {
            if (this.SetProperty(ref this.scanDepthText, value))
            {
                this.OnPropertyChanged(nameof(this.SelectedScanDepth));
            }
        }
    }

    /// <summary>
    ///   The scan depth options offered by the selector.
    /// </summary>
    public IReadOnlyList<ScanDepthOption> ScanDepthOptions => ScanDepthChoices;

    /// <summary>
    ///   The selected scan depth option.
    /// </summary>
    public ScanDepthOption SelectedScanDepth
    {
        get => ScanDepthChoices.FirstOrDefault(option =>
                   string.Equals(option.Value.ToString(), this.scanDepthText, StringComparison.Ordinal))
               ?? ScanDepthChoices.First(option => option.Value == ProjectScanningDefaults.DefaultScanDepth);
        set
        {
            if (value is not null && !string.Equals(this.scanDepthText, value.Value.ToString(), StringComparison.Ordinal))
            {
                this.ScanDepthText = value.Value.ToString();
                this.ApplyScanningSettings();
            }
        }
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
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDiscoveryMode, value))
            {
                this.ApplyScanningSettings();
            }
        }
    }

    /// <summary>
    ///   The number of discovered projects registered from the last scan.
    /// </summary>
    public int DiscoveredProjectCount
    {
        get => this.discoveredProjectCount;
        private set
        {
            if (this.SetProperty(ref this.discoveredProjectCount, value))
            {
                this.OnProjectTotalsChanged();
            }
        }
    }

    /// <summary>
    ///   The number of projects that were added manually rather than discovered.
    /// </summary>
    public int ManualProjectCount
    {
        get => this.manualProjectCount;
        private set
        {
            if (this.SetProperty(ref this.manualProjectCount, value))
            {
                this.OnProjectTotalsChanged();
            }
        }
    }

    /// <summary>
    ///   The total number of registered projects: discovered plus manually added.
    /// </summary>
    public int TotalProjectCount => this.DiscoveredProjectCount + this.ManualProjectCount;

    /// <summary>
    ///   The provenance split shown under the total project count, e.g. "31 discovered · 4 manual".
    /// </summary>
    public string ProjectMixSummary =>
        $"{this.DiscoveredProjectCount} discovered · {this.ManualProjectCount} manual";

    /// <summary>
    ///   The number of folders the last scan examined across the watched roots.
    /// </summary>
    public int ScannedDirectoryCount
    {
        get => this.scannedDirectoryCount;
        private set => this.SetProperty(ref this.scannedDirectoryCount, value);
    }

    private string ScannedCoverageSuffix => this.ScannedDirectoryCount switch
    {
        0 => string.Empty,
        1 => " · 1 folder",
        var count => $" · {count} folders"
    };

    private void OnProjectTotalsChanged()
    {
        this.OnPropertyChanged(nameof(this.TotalProjectCount));
        this.OnPropertyChanged(nameof(this.ProjectMixSummary));
    }

    /// <summary>
    ///   The number of agents installed on this machine.
    /// </summary>
    public int AgentsInstalledCount
    {
        get => this.agentsInstalledCount;
        private set => this.SetProperty(ref this.agentsInstalledCount, value);
    }

    /// <summary>
    ///   The number of supported agent plugins.
    /// </summary>
    public int AgentsSupportedCount
    {
        get => this.agentsSupportedCount;
        private set
        {
            if (this.SetProperty(ref this.agentsSupportedCount, value))
            {
                this.OnPropertyChanged(nameof(this.AgentsSupportSummary));
            }
        }
    }

    /// <summary>
    ///   The supported-agent count shown under the installed count.
    /// </summary>
    public string AgentsSupportSummary => $"{this.AgentsSupportedCount} supported";

    /// <summary>
    ///   The short watcher state shown in the summary card: Off, Idle, Scanning, Watching, or Error.
    /// </summary>
    public string WatcherStateLabel
    {
        get => this.watcherStateLabel;
        private set => this.SetProperty(ref this.watcherStateLabel, value);
    }

    /// <summary>
    ///   The watcher detail shown under the state, e.g. the last scan time.
    /// </summary>
    public string WatcherDetailText
    {
        get => this.watcherDetailText;
        private set => this.SetProperty(ref this.watcherDetailText, value);
    }

    /// <summary>
    ///   Opens the folder picker and fills the watched-directory entry.
    /// </summary>
    public RelayCommand BrowseWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Opens the folder picker and registers the selected folder as a watched root.
    /// </summary>
    public RelayCommand AddWatchedDirectoryFromPickerCommand { get; }

    /// <summary>
    ///   Registers the entered absolute folder path as a watched root.
    /// </summary>
    public RelayCommand AddWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Removes the watched root passed as the command parameter.
    /// </summary>
    public RelayCommand<string> RemoveWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Rescans only the watched root passed as the command parameter.
    /// </summary>
    public RelayCommand<string> ScanWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Adds the entered ignored pattern.
    /// </summary>
    public RelayCommand AddIgnoredPatternCommand { get; }

    /// <summary>
    ///   Removes the ignored pattern passed as the command parameter.
    /// </summary>
    public RelayCommand<string> RemoveIgnoredPatternCommand { get; }

    /// <summary>
    ///   Re-scans the saved watched roots.
    /// </summary>
    public RelayCommand ScanNowCommand { get; }

    /// <summary>
    ///   The status line of the update check.
    /// </summary>
    public string UpdateMessage
    {
        get => this.updateMessage;
        private set => this.SetProperty(ref this.updateMessage, value);
    }

    /// <summary>
    ///   The heading of the update status card.
    /// </summary>
    public string UpdateStatusTitle
    {
        get => this.updateStatusTitle;
        private set => this.SetProperty(ref this.updateStatusTitle, value);
    }

    /// <summary>
    ///   The version and last-check detail shown under the update status card heading.
    /// </summary>
    public string UpdateStatusDetail
    {
        get => this.updateStatusDetail;
        private set => this.SetProperty(ref this.updateStatusDetail, value);
    }

    /// <summary>
    ///   The status badge text; empty hides the badge.
    /// </summary>
    public string UpdateStatusBadge
    {
        get => this.updateStatusBadge;
        private set => this.SetProperty(ref this.updateStatusBadge, value);
    }

    /// <summary>
    ///   The tone of the update status badge.
    /// </summary>
    public SettingBadgeTone UpdateStatusBadgeTone
    {
        get => this.updateStatusBadgeTone;
        private set => this.SetProperty(ref this.updateStatusBadgeTone, value);
    }

    /// <summary>
    ///   The glyph shown in the update status card's leading icon block.
    /// </summary>
    public string UpdateStatusGlyph
    {
        get => this.updateStatusGlyph;
        private set => this.SetProperty(ref this.updateStatusGlyph, value);
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

    /// <summary>
    ///   Opens the application release notes.
    /// </summary>
    public RelayCommand ShowReleaseNotesCommand { get; }

    private Task RunUpdateCheckAsync(CancellationToken cancellationToken) =>
        AsyncOperationRunner.RunAsync(
            "Settings update check",
            () => this.CheckForUpdatesAsync(cancellationToken),
            this.logger,
            message =>
            {
                this.UpdateMessage = message;
                this.SetUpdateStatus("Update check failed", message, "Failed", SettingBadgeTone.Neutral);
            },
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
        this.customAccentSelected = this.FindAccentPreset(this.accentText) is null;
        if (this.customAccentSelected && this.accentMemory.LastCustomAccent is null)
        {
            this.accentMemory.LastCustomAccent = this.accentText;
        }
        this.startWithWindows = draft.StartWithWindows;
        this.startMinimized = draft.StartMinimized;

        this.WatchedDirectories.Clear();
        var defaultScanDepth = ProjectScanningDefaults.NormalizeScanDepth(draft.ScanDepth);
        foreach (var directory in draft.WatchedDirectories ?? Array.Empty<string>())
        {
            var settings = draft.WatchedDirectorySettings is not null
                && draft.WatchedDirectorySettings.TryGetValue(directory, out var stored)
                    ? stored
                    : null;

            this.WatchedDirectories.Add(this.CreateWatchedDirectoryRow(
                directory,
                settings?.ScanDepth ?? defaultScanDepth,
                settings?.DiscoveryMode ?? draft.DiscoveryMode));
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
        this.newWatchedDirectoryText = string.Empty;
        this.watchedDirectoryError = null;
        this.ApplyScannerStatus(this.projectScanner.Status);

        this.AgentsState.Load(draft.DefaultAgentId);
        this.RefreshAgentSummary();
        this.ProjectRegistry.Load();

        this.ResetUpdateStatus();
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
        this.OnPropertyChanged(nameof(this.ProbePolicySummary));
        this.OnPropertyChanged(nameof(this.ProbePolicyDetail));
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
        this.OnPropertyChanged(nameof(this.ActiveAccentLabel));
        this.OnPropertyChanged(nameof(this.ScanDepthText));
        this.OnPropertyChanged(nameof(this.SelectedScanDepth));
        this.OnPropertyChanged(nameof(this.SelectedDiscoveryMode));
        this.OnPropertyChanged(nameof(this.NewIgnoredPatternText));
        this.OnPropertyChanged(nameof(this.NewWatchedDirectoryText));
        this.OnPropertyChanged(nameof(this.WatchedDirectoryError));
        this.OnPropertyChanged(nameof(this.HasWatchedDirectoryError));
        this.OnPropertyChanged(nameof(this.WatchedRootsSummary));
        this.OnPropertyChanged(nameof(this.DiscoveredProjectCount));
        this.OnPropertyChanged(nameof(this.ManualProjectCount));
        this.OnPropertyChanged(nameof(this.TotalProjectCount));
        this.OnPropertyChanged(nameof(this.ProjectMixSummary));
        this.OnPropertyChanged(nameof(this.AgentsInstalledCount));
        this.OnPropertyChanged(nameof(this.AgentsSupportedCount));
        this.OnPropertyChanged(nameof(this.AgentsSupportSummary));
        this.OnPropertyChanged(nameof(this.WatcherStateLabel));
        this.OnPropertyChanged(nameof(this.WatcherDetailText));
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

    /// <summary>
    ///   Finds the preset matching a color value, ignoring the custom entry. Invalid or partial
    ///   values match nothing, so the selector shows the custom entry while the user types.
    /// </summary>
    private AccentChoice? FindAccentPreset(string value)
    {
        if (!AccentPalette.TryNormalize(value, out var normalized))
        {
            return null;
        }

        return AccentChoices.FirstOrDefault(choice =>
            !choice.IsCustom &&
            string.Equals(AccentPalette.Normalize(choice.Hex), normalized, StringComparison.Ordinal));
    }

    private void BrowseWatchedDirectory()
    {
        var folder = this.dialogs.SelectFolder("Add Watched Directory");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            this.NewWatchedDirectoryText = folder;
        }
    }

    private void AddWatchedDirectoryFromPicker()
    {
        var folder = this.dialogs.SelectFolder("Add Watched Directory");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        if (this.TryAddWatchedDirectory(folder))
        {
            this.WatchedDirectoryError = null;
            this.ValidationError = null;
        }
    }

    private void AddWatchedDirectory()
    {
        if (this.TryAddWatchedDirectory(this.NewWatchedDirectoryText))
        {
            this.NewWatchedDirectoryText = string.Empty;
            this.ValidationError = null;
        }
    }

    private bool TryAddWatchedDirectory(string value)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            this.WatchedDirectoryError = "Enter a folder path or browse for one.";
            return false;
        }

        if (!Path.IsPathFullyQualified(text) || !Directory.Exists(text))
        {
            this.WatchedDirectoryError = "Enter an existing absolute folder path.";
            return false;
        }

        string path;
        try
        {
            path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            this.WatchedDirectoryError = "Enter a valid absolute folder path.";
            return false;
        }

        if (!this.WatchedDirectories.Any(existing => string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            var row = this.CreateWatchedDirectoryRow(
                path,
                this.SelectedScanDepth.Value,
                this.SelectedDiscoveryMode.Mode);

            this.WatchedDirectories.Add(row);

            if (this.ApplyScanningSettings())
            {
                return true;
            }

            // A failed apply must not leave an unpersisted root in the live list.
            this.WatchedDirectories.Remove(row);
        }

        return true;
    }

    private WatchedDirectoryRow CreateWatchedDirectoryRow(string path, int scanDepth, ProjectDiscoveryMode discoveryMode)
    {
        var normalizedDepth = ProjectScanningDefaults.NormalizeScanDepth(scanDepth);
        var depth = ScanDepthChoices.First(option => option.Value == normalizedDepth);
        var discovery = DiscoveryModeOptions.FirstOrDefault(option => option.Mode == discoveryMode) ?? DiscoveryModeOptions[2];

        return new WatchedDirectoryRow(path, ScanDepthChoices, DiscoveryModeOptions, depth, discovery);
    }

    private void RemoveWatchedDirectory(string directory)
    {
        var row = this.WatchedDirectories.FirstOrDefault(
            candidate => string.Equals(candidate.Path, directory, StringComparison.OrdinalIgnoreCase));

        if (row is null)
        {
            return;
        }

        var index = this.WatchedDirectories.IndexOf(row);
        this.WatchedDirectories.RemoveAt(index);

        if (!this.ApplyScanningSettings())
        {
            this.WatchedDirectories.Insert(index, row);
        }
    }

    private void ScanWatchedDirectory(string directory) => this.projectScanner.Rescan(directory);

    private void OnWatchedDirectoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.SyncObservedWatchedDirectoryRows(e);
        this.OnPropertyChanged(nameof(this.WatchedRootsSummary));
        this.RefreshWatchedDirectoryCounts();
    }

    private void SyncObservedWatchedDirectoryRows(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var row in this.observedWatchedDirectoryRows)
            {
                row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
            }

            this.observedWatchedDirectoryRows.Clear();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (WatchedDirectoryRow row in e.OldItems)
            {
                row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
                this.observedWatchedDirectoryRows.Remove(row);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (WatchedDirectoryRow row in e.NewItems)
            {
                row.PropertyChanged += this.OnWatchedDirectoryRowChanged;
                this.observedWatchedDirectoryRows.Add(row);
            }
        }
    }

    private void OnWatchedDirectoryRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WatchedDirectoryRow.SelectedDepth) or nameof(WatchedDirectoryRow.SelectedDiscovery))
        {
            this.ApplyScanningSettings();
        }
    }

    /// <summary>
    ///   Applies the scanning settings currently shown on the page and rescans in the background.
    ///   The remaining settings still go through Save/Cancel.
    /// </summary>
    /// <returns> True when the settings were persisted and the scanner was reconfigured. </returns>
    private bool ApplyScanningSettings()
    {
        if (this.disposed)
        {
            return true;
        }

        var input = new ScanningSettingsInput(
            this.WatchedDirectories.Select(row => row.Path).ToList(),
            this.SelectedScanDepth.Value,
            this.IgnoredPatterns.ToList(),
            this.SelectedDiscoveryMode.Mode,
            this.WatchedDirectories.ToDictionary(
                row => row.Path,
                row => new WatchedDirectoryScanSettings(row.SelectedDepth.Value, row.SelectedDiscovery.Mode),
                StringComparer.OrdinalIgnoreCase));

        var result = this.settingsApplication.ApplyScanning(input);
        if (result.Success)
        {
            return true;
        }

        this.ValidationError = result.Error;
        return false;
    }

    private void RefreshWatchedDirectoryCounts()
    {
        foreach (var row in this.WatchedDirectories)
        {
            row.DiscoveredCount = this.projectScanner.CountDiscoveredProjects(row.Path);
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

            if (!this.ApplyScanningSettings())
            {
                this.IgnoredPatterns.Remove(pattern);
            }
        }

        this.NewIgnoredPatternText = string.Empty;
    }

    private void RemoveIgnoredPattern(string pattern)
    {
        var index = this.IgnoredPatterns.IndexOf(pattern);
        if (index < 0)
        {
            return;
        }

        this.IgnoredPatterns.RemoveAt(index);

        if (!this.ApplyScanningSettings())
        {
            this.IgnoredPatterns.Insert(index, pattern);
        }
    }

    private void OnScannerStatusChanged(object? sender, ProjectScanStatus status)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => this.ApplyScannerStatus(status));
            return;
        }

        this.ApplyScannerStatus(status);
    }

    private void ApplyScannerStatus(ProjectScanStatus? status)
    {
        var current = status ?? ProjectScanStatus.NotConfigured;
        this.DiscoveredProjectCount = current.DiscoveredCount;
        this.ManualProjectCount = this.projectScanner.CountManualProjects();
        this.ScannedDirectoryCount = current.ScannedDirectoryCount;
        this.WatcherStateLabel = current.State switch
        {
            ProjectScanState.NotConfigured => "Off",
            ProjectScanState.Scanning => "Scanning",
            ProjectScanState.Error => "Error",
            _ => current.IsWatching ? "Watching" : "Idle"
        };
        this.WatcherDetailText = current.State switch
        {
            ProjectScanState.NotConfigured => "No watched roots configured",
            ProjectScanState.Scanning => "Scan in progress",
            ProjectScanState.Error => current.Error ?? "Scan failed",
            _ => current.LastScanUtc is { } last
                ? $"Last scan {last.ToLocalTime():HH:mm:ss}{this.ScannedCoverageSuffix}"
                : "Not scanned yet"
        };

        this.RefreshWatchedDirectoryCounts();
    }

    private void RefreshAgentSummary()
    {
        var cards = this.AgentsState.Cards;
        this.AgentsInstalledCount = cards?.Count(card => card.IsInstalled) ?? 0;
        this.AgentsSupportedCount = cards?.Count ?? 0;
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

        foreach (var row in this.observedWatchedDirectoryRows)
        {
            row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
        }

        this.observedWatchedDirectoryRows.Clear();
        this.AgentsState.Dispose();
        this.ProjectRegistry.Dispose();
    }

    /// <summary>
    ///   Checks for updates and reports the outcome in <see cref="UpdateMessage" />.
    /// </summary>
    /// <returns> A task that completes when the check finishes. </returns>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        this.SetUpdateStatus(
            "Checking for updates",
            "Looking for a newer CLIHub version.",
            "Checking",
            SettingBadgeTone.Accent);
        this.UpdateMessage = "Checking for updates...";

        UpdateCheckResult result;
        try
        {
            result = await this.updateWorkflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.SetUpdateStatus(
                "Update check cancelled",
                "The check did not complete.",
                "Cancelled",
                SettingBadgeTone.Neutral);
            this.UpdateMessage = "Update check cancelled.";
            return;
        }

        this.ApplyUpdateResult(result);
    }

    private void ResetUpdateStatus()
    {
        if (this.updateWorkflow.LastKnownAvailableVersion is { Length: > 0 } available)
        {
            this.SetUpdateStatus(
                "Update available",
                $"Version {this.Version} → {available}",
                "Update available",
                SettingBadgeTone.Accent);
            this.UpdateMessage = $"Update available: {available} (current v{this.Version})";
            return;
        }

        this.SetUpdateStatus(
            "Check for updates",
            $"Current version: {this.Version}",
            "Not checked",
            SettingBadgeTone.Neutral);
        this.UpdateMessage = string.Empty;
    }

    private void ApplyUpdateResult(UpdateCheckResult result)
    {
        var lastChecked = DateTimeOffset.Now.ToString("t");

        this.UpdateMessage = result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };

        switch (result.Status)
        {
            case UpdateStatus.UpdateAvailable:
                this.SetUpdateStatus(
                    "Update available",
                    $"Version {result.CurrentVersion} → {result.AvailableVersion} · Last checked {lastChecked}",
                    "Update available",
                    SettingBadgeTone.Accent);
                break;
            case UpdateStatus.UpToDate:
                this.SetUpdateStatus(
                    "CLIHub is up to date",
                    $"Version {result.CurrentVersion} · Last checked {lastChecked}",
                    "Up to date",
                    SettingBadgeTone.Success);
                break;
            case UpdateStatus.NotInstalled:
                this.SetUpdateStatus(
                    "Updates unavailable",
                    "Updates apply to installed builds only.",
                    "Not installed",
                    SettingBadgeTone.Neutral);
                break;
            default:
                this.SetUpdateStatus(
                    "Update check failed",
                    "Could not check for updates. Try again.",
                    "Failed",
                    SettingBadgeTone.Neutral);
                break;
        }
    }

    private void SetUpdateStatus(string title, string detail, string badge, SettingBadgeTone tone)
    {
        this.UpdateStatusTitle = title;
        this.UpdateStatusDetail = detail;
        this.UpdateStatusBadge = badge;
        this.UpdateStatusBadgeTone = tone;
        this.UpdateStatusGlyph = tone switch
        {
            SettingBadgeTone.Success => IconGlyphs.Checkmark,
            SettingBadgeTone.Accent => IconGlyphs.Refresh,
            _ => IconGlyphs.Info
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
            this.WatchedDirectories.Select(row => row.Path).ToList(),
            this.ScanDepthText,
            this.IgnoredPatterns.ToList(),
            this.SelectedDiscoveryMode.Mode,
            this.AgentsState.CurrentDefaultAgentId,
            this.WatchedDirectories.ToDictionary(
                row => row.Path,
                row => new WatchedDirectoryScanSettings(row.SelectedDepth.Value, row.SelectedDiscovery.Mode),
                StringComparer.OrdinalIgnoreCase));

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
