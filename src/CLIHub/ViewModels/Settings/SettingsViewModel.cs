namespace CLIHub.ViewModels;

using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub.Controls;
using CLIHub.Themes;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Scanning;
using CLIHub.Core.Updates;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Updates;
using CLIHub.Settings;

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

    private static readonly AppThemeOption[] AppThemeOptions = ThemeDescriptors.All
        .Select(descriptor => new AppThemeOption(
            descriptor.Theme,
            descriptor.Name,
            descriptor.Description,
            descriptor.CardBrush,
            descriptor.BarBrush,
            descriptor.Theme != AppTheme.System))
        .ToArray();

    private readonly SettingsApplicationService settingsApplication;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly IReleaseNotesLauncher releaseNotesLauncher;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly IRuntimeExecutableLocator runtimeExecutables;
    private readonly ILogger<SettingsViewModel> logger;
    private readonly AccentSelection accent;
    private readonly WatchedDirectoryEditor scanning;

    private RuntimeOption[] runtimes = RuntimeOptions;
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
    private SettingsUpdateCard updateCard = new("Check for updates", string.Empty, "Not checked", SettingBadgeTone.Neutral);
    private string? validationError;
    private SettingsPage selectedPage = SettingsPageCatalog.AllPages[0];
    private readonly IExternalLauncher externalLauncher;
    private LogLevelOption selectedLogLevel = LogLevelOptions[2];
    private string logRetentionDaysText = "7";
    private string logFileSizeLimitText = "0";
    private PopupPositionOption selectedPopupPosition = PopupPositionOptions[0];
    private AppThemeOption selectedAppTheme = AppThemeOptions[0];
    private int agentsInstalledCount;
    private int agentsSupportedCount;
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
    /// <param name="projectScanner"> Scanner whose status is shown on the Project Scanning page. </param>
    /// <param name="agentsState"> Agent-management section shown on the CLI Agents page. </param>
    /// <param name="projectRegistry"> Project-registry section shown on the Projects page. </param>
    /// <param name="accentMemory"> Process-wide memory of the last custom accent. </param>
    /// <param name="runtimeExecutables"> Resolves a runtime to its executable so the page can mark availability. </param>
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
                             IRuntimeExecutableLocator runtimeExecutables,
                             ILogger<SettingsViewModel> logger)
    {
        this.settingsApplication = settingsApplication;
        this.versionProvider = versionProvider;
        this.updateWorkflow = updateWorkflow;
        this.releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        this.operationLifetime = operationLifetime;
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        this.AgentsState = agentsState ?? throw new ArgumentNullException(nameof(agentsState));
        this.ProjectRegistry = projectRegistry ?? throw new ArgumentNullException(nameof(projectRegistry));
        this.runtimeExecutables = runtimeExecutables ?? throw new ArgumentNullException(nameof(runtimeExecutables));
        this.logger = logger;
        this.accent = new AccentSelection(accentMemory);
        this.scanning = new WatchedDirectoryEditor(
            this.settingsApplication,
            projectScanner,
            dialogs,
            error => this.ValidationError = error);

        this.Version = this.versionProvider.GetCurrentVersion();
        this.ResetUpdateStatus();

        this.SaveCommand = new RelayCommand(this.Save);
        this.CancelCommand = new RelayCommand(this.Cancel);
        this.CheckForUpdatesCommand = new RelayCommand(() =>
            _ = this.operationLifetime.RunAsync("Settings update check", this.RunUpdateCheckAsync));
        this.ShowReleaseNotesCommand = new RelayCommand(this.releaseNotesLauncher.ShowReleaseNotes);
        this.CopyLogPathCommand = new RelayCommand(() => Clipboard.SetText(this.LogFilePath));
        this.OpenLogFolderCommand = new RelayCommand(() => this.externalLauncher.Open(AppPaths.LogsDirectory));
    }

    /// <summary>
    ///   Raised when the window should close (on Save or Cancel).
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///   The runtime options offered by the segmented control.
    /// </summary>
    public IReadOnlyList<RuntimeOption> Runtimes => this.runtimes;

    /// <summary>
    ///   The path display options offered by the segmented control.
    /// </summary>
    public IReadOnlyList<PathDisplayOption> PathDisplays => PathDisplayOptions;

    /// <summary>
    ///   The navigation pages, in display order.
    /// </summary>
    public IReadOnlyList<SettingsPage> Pages => SettingsPageCatalog.AllPages;

    /// <summary>
    ///   The navigation groups and their pages, in display order.
    /// </summary>
    public IReadOnlyList<SettingsGroup> PageGroups => SettingsPageCatalog.PageGroups;

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
        set
        {
            if (this.SetProperty(ref this.selectedPage, value))
            {
                this.OnPropertyChanged(nameof(this.IsPlaceholderPageVisible));
            }
        }
    }

    /// <summary>
    ///   Whether the placeholder page is shown because the selected page has no implemented
    ///   settings yet.
    /// </summary>
    public bool IsPlaceholderPageVisible => !this.SelectedPage.IsImplemented;

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
        set
        {
            // WPF can push null through the TwoWay SelectedItem binding when the availability
            // rebuild replaces the selected instance. Keep the last non-null selection so the
            // stored runtime and Save() stay valid.
            if (value is null)
            {
                return;
            }

            this.SetProperty(ref this.selectedRuntime, value);
        }
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
        ? "Custom detection settings"
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
    ///   The accent color selection section shown on the Appearance page.
    /// </summary>
    public AccentSelection Accent => this.accent;

    /// <summary>
    ///   The watched directory and scanning section shown on the Project Scanning page.
    /// </summary>
    public WatchedDirectoryEditor Scanning => this.scanning;

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
    ///   The status line of the update check.
    /// </summary>
    public string UpdateMessage
    {
        get => this.updateMessage;
        private set => this.SetProperty(ref this.updateMessage, value);
    }

    /// <summary>
    ///   The update status card shown in the page header.
    /// </summary>
    public SettingsUpdateCard UpdateCard
    {
        get => this.updateCard;
        private set => this.SetProperty(ref this.updateCard, value);
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
                this.UpdateCard = new SettingsUpdateCard(
                    "Update check failed",
                    message,
                    "Failed",
                    SettingBadgeTone.Neutral);
            },
            cancellationToken);

    /// <summary>
    ///   Reloads all fields from the stored configuration.
    /// </summary>
    public void Load()
    {
        var draft = this.settingsApplication.Load();

        // Availability is re-evaluated on every open so a reinstall is reflected without a restart.
        // Git Bash and PowerShell 7 are resolved from their installations; the Windows-shipped
        // cmd.exe and powershell.exe are always available.
        this.runtimes = RuntimeOptions
            .Select(option => option with
            {
                IsInstalled = option.Kind switch
                {
                    RuntimeKind.GitBash or RuntimeKind.PowerShell7 =>
                        this.runtimeExecutables.Locate(option.Kind) is not null,
                    _ => true
                }
            })
            .ToArray();

        this.selectedRuntime = this.runtimes
            .FirstOrDefault(o => o.Kind == draft.Runtime) ?? this.runtimes[0];
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
        this.accent.Load(draft.AccentColor);
        this.startWithWindows = draft.StartWithWindows;
        this.startMinimized = draft.StartMinimized;

        this.scanning.Load(draft);

        this.AgentsState.Load(draft.DefaultAgentId);
        this.RefreshAgentSummary();
        this.ProjectRegistry.Load();

        this.ResetUpdateStatus();
        this.validationError = null;
        this.selectedPage = SettingsPageCatalog.AllPages[0];

        this.OnPropertyChanged(nameof(this.StartWithWindows));
        this.OnPropertyChanged(nameof(this.StartMinimized));
        this.OnPropertyChanged(nameof(this.Runtimes));
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
        this.OnPropertyChanged(nameof(this.AgentsInstalledCount));
        this.OnPropertyChanged(nameof(this.AgentsSupportedCount));
        this.OnPropertyChanged(nameof(this.AgentsSupportSummary));
        this.OnPropertyChanged(nameof(this.UpdateMessage));
        this.OnPropertyChanged(nameof(this.ValidationError));
        this.OnPropertyChanged(nameof(this.SelectedPage));
        this.OnPropertyChanged(nameof(this.IsPlaceholderPageVisible));
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

    private void RefreshAgentSummary()
    {
        var cards = this.AgentsState.Cards;
        this.AgentsInstalledCount = cards?.Count(card => card.IsInstalled) ?? 0;
        this.AgentsSupportedCount = cards?.Count ?? 0;
    }

    /// <summary>
    ///   Detaches the scanning section and the section subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.Scanning.Dispose();
        this.AgentsState.Dispose();
        this.ProjectRegistry.Dispose();
    }

    /// <summary>
    ///   Checks for updates and reports the outcome in <see cref="UpdateMessage" />.
    /// </summary>
    /// <returns> A task that completes when the check finishes. </returns>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        this.UpdateCard = new SettingsUpdateCard(
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
            this.UpdateCard = new SettingsUpdateCard(
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
            this.UpdateCard = new SettingsUpdateCard(
                "Update available",
                $"Version {this.Version} → {available}",
                "Update available",
                SettingBadgeTone.Accent);
            this.UpdateMessage = $"Update available: {available} (current v{this.Version})";
            return;
        }

        this.UpdateCard = new SettingsUpdateCard(
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
                this.UpdateCard = new SettingsUpdateCard(
                    "Update available",
                    $"Version {result.CurrentVersion} → {result.AvailableVersion} · Last checked {lastChecked}",
                    "Update available",
                    SettingBadgeTone.Accent);
                break;
            case UpdateStatus.UpToDate:
                this.UpdateCard = new SettingsUpdateCard(
                    "CLIHub is up to date",
                    $"Version {result.CurrentVersion} · Last checked {lastChecked}",
                    "Up to date",
                    SettingBadgeTone.Success);
                break;
            case UpdateStatus.NotInstalled:
                this.UpdateCard = new SettingsUpdateCard(
                    "Updates unavailable",
                    "Updates apply to installed builds only.",
                    "Not installed",
                    SettingBadgeTone.Neutral);
                break;
            default:
                this.UpdateCard = new SettingsUpdateCard(
                    "Update check failed",
                    "Could not check for updates. Try again.",
                    "Failed",
                    SettingBadgeTone.Neutral);
                break;
        }
    }

    private void Save()
    {
        var scanning = this.Scanning.CreateInput();
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
            this.Accent.AccentText,
            scanning,
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
