namespace CLIHub.Settings;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Core.Scanning;

/// <summary>
///   Raw values collected from the Settings form before validation.
/// </summary>
/// <param name="Runtime"> Selected runtime. </param>
/// <param name="PathDisplayStyle"> Selected path display style. </param>
/// <param name="HotkeyText"> Hotkey text entered by the user. </param>
/// <param name="ProbeTtlText"> Probe TTL text, or empty for the default. </param>
/// <param name="ProbeTimeoutText"> Probe timeout text, or empty for the default. </param>
/// <param name="StartWithWindows"> Whether Windows startup should be enabled. </param>
/// <param name="StartMinimized"> Whether the application should start minimized to the tray. </param>
/// <param name="CheckForUpdatesOnStartup"> Whether startup update checks are enabled. </param>
/// <param name="AutoDownloadUpdates"> Whether updates are downloaded automatically in the background. </param>
/// <param name="NotifyStableUpdates"> Whether stable update availability is notified. </param>
/// <param name="NotifyPrereleaseUpdates"> Whether pre-release versions are checked and notified. </param>
/// <param name="LogLevel"> Selected log level name. </param>
/// <param name="LogRetentionDaysText"> Retained daily log file count as text. </param>
/// <param name="LogFileSizeLimitText"> Per-file log size limit in megabytes as text (0 = unlimited). </param>
/// <param name="PopupScreenPosition"> Where the launch window is placed when shown. </param>
/// <param name="AppTheme"> The application-wide visual theme. </param>
/// <param name="AccentColor"> The accent color as text. </param>
/// <param name="WatchedDirectories"> Watched root directories for repository scanning. </param>
/// <param name="ScanDepthText"> Repository scan depth as text. </param>
/// <param name="IgnoredPatterns"> Ignored directory-name patterns. </param>
/// <param name="DiscoveryMode"> Which directories count as repositories. </param>
public sealed record SettingsInput(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    string HotkeyText,
    string ProbeTtlText,
    string ProbeTimeoutText,
    bool StartWithWindows,
    bool StartMinimized,
    bool CheckForUpdatesOnStartup,
    bool AutoDownloadUpdates,
    bool NotifyStableUpdates,
    bool NotifyPrereleaseUpdates,
    string LogLevel,
    string LogRetentionDaysText,
    string LogFileSizeLimitText,
    PopupScreenPosition PopupScreenPosition,
    AppTheme AppTheme,
    string AccentColor,
    IReadOnlyList<string>? WatchedDirectories = null,
    string ScanDepthText = "2",
    IReadOnlyList<string>? IgnoredPatterns = null,
    ProjectDiscoveryMode DiscoveryMode = ProjectDiscoveryMode.Both,
    string? DefaultAgentId = null);

/// <summary>
///   Validated, typed settings ready for persistence and application.
/// </summary>
/// <param name="Runtime"> Runtime used for interactive agent launches. </param>
/// <param name="PathDisplayStyle"> Style used for project path display. </param>
/// <param name="Hotkey"> Parsed global hotkey. </param>
/// <param name="ProbeTtlMinutes"> Probe TTL, or null for the default. </param>
/// <param name="ProbeTimeoutSeconds"> Probe timeout, or null for the default. </param>
/// <param name="StartWithWindows"> Whether Windows startup should be enabled. </param>
/// <param name="StartMinimized"> Whether the application should start minimized to the tray. </param>
/// <param name="CheckForUpdatesOnStartup"> Whether startup update checks are enabled. </param>
/// <param name="AutoDownloadUpdates"> Whether updates are downloaded automatically in the background. </param>
/// <param name="NotifyStableUpdates"> Whether stable update availability is notified. </param>
/// <param name="NotifyPrereleaseUpdates"> Whether pre-release versions are checked and notified. </param>
/// <param name="LogLevel"> Selected log level name. </param>
/// <param name="LogRetentionDays"> Number of daily log files to retain. </param>
/// <param name="LogFileSizeLimitMb"> Per-file log size limit in megabytes (0 = unlimited). </param>
/// <param name="PopupScreenPosition"> Where the launch window is placed when shown. </param>
/// <param name="AppTheme"> The application-wide visual theme. </param>
/// <param name="AccentColor"> The normalized accent color (#RRGGBB). </param>
/// <param name="WatchedDirectories"> Watched root directories for repository scanning. </param>
/// <param name="ScanDepth"> Repository scan depth, within the allowed range. </param>
/// <param name="IgnoredPatterns"> Ignored directory-name patterns (normalized). </param>
/// <param name="DiscoveryMode"> Which directories count as repositories. </param>
public sealed record SettingsDraft(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    HotkeyDefinition Hotkey,
    int? ProbeTtlMinutes,
    int? ProbeTimeoutSeconds,
    bool StartWithWindows,
    bool StartMinimized,
    bool CheckForUpdatesOnStartup,
    bool AutoDownloadUpdates,
    bool NotifyStableUpdates,
    bool NotifyPrereleaseUpdates,
    string LogLevel,
    int LogRetentionDays,
    int LogFileSizeLimitMb,
    PopupScreenPosition PopupScreenPosition,
    AppTheme AppTheme,
    string AccentColor,
    IReadOnlyList<string>? WatchedDirectories = null,
    int ScanDepth = ProjectScanningDefaults.DefaultScanDepth,
    IReadOnlyList<string>? IgnoredPatterns = null,
    ProjectDiscoveryMode DiscoveryMode = ProjectDiscoveryMode.Both,
    string? DefaultAgentId = null);

/// <summary>
///   Outcome of applying a settings draft.
/// </summary>
/// <param name="Success"> Whether the draft was applied and persisted. </param>
/// <param name="Error"> User-facing error when application failed. </param>
public sealed record SettingsApplicationResult(bool Success, string? Error)
{
    /// <summary>
    ///   Creates a successful application result.
    /// </summary>
    public static SettingsApplicationResult Applied { get; } = new(true, null);

    /// <summary>
    ///   Creates a failed application result.
    /// </summary>
    /// <param name="error"> User-facing error. </param>
    public static SettingsApplicationResult Failed(string error) => new(false, error);
}

/// <summary>
///   Loads, validates, applies, persists, and rolls back Settings drafts.
/// </summary>
public sealed class SettingsApplicationService
{
    private readonly IPreferencesStore preferencesStore;
    private readonly IStartupService startupService;
    private readonly IPreferenceApplier applier;
    private readonly IProjectScanner projectScanner;
    private readonly ILogger<SettingsApplicationService> logger;

    /// <summary>
    ///   Creates the Settings application service.
    /// </summary>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupService"> Windows startup registration state boundary. </param>
    /// <param name="applier"> Runtime system preference boundary. </param>
    /// <param name="projectScanner"> Repository scanner reconfigured after scanning settings change. </param>
    /// <param name="logger"> Logger for application and rollback failures. </param>
    public SettingsApplicationService(
        IPreferencesStore preferencesStore,
        IStartupService startupService,
        IPreferenceApplier applier,
        IProjectScanner projectScanner,
        ILogger<SettingsApplicationService> logger)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Loads the current persisted preferences and system startup state as a typed draft.
    /// </summary>
    /// <returns> The current Settings draft. </returns>
    public SettingsDraft Load()
    {
        var preferences = this.preferencesStore.Load();
        var hotkey = HotkeyParser.TryParse(preferences.Hotkey, out var parsedHotkey) && parsedHotkey is not null
            ? parsedHotkey
            : HotkeyParser.Default;

        return new SettingsDraft(
            preferences.DefaultRuntime,
            PathDisplayStyles.Parse(preferences.PathDisplayStyle),
            hotkey,
            preferences.AgentProbeTtlMinutes,
            preferences.AgentProbeTimeoutSeconds,
            this.startupService.IsEnabled(),
            preferences.StartMinimized,
            preferences.CheckForUpdatesOnStartup,
            preferences.AutoDownloadUpdates,
            preferences.NotifyStableUpdates,
            preferences.NotifyPrereleaseUpdates,
            preferences.LogLevel,
            preferences.LogRetentionDays,
            preferences.LogFileSizeLimitMb,
            preferences.PopupScreenPosition,
            preferences.AppTheme,
            preferences.AccentColor,
            preferences.WatchedDirectories is { } watched ? watched : Array.Empty<string>(),
            ProjectScanningDefaults.NormalizeScanDepth(preferences.ScanDepth),
            preferences.IgnoredPatterns is { } ignored ? ignored : ProjectScanningDefaults.IgnoredPatterns,
            preferences.DiscoveryMode,
            preferences.DefaultAgentId);
    }

    /// <summary>
    ///   Validates raw form values and creates a typed draft without side effects.
    /// </summary>
    /// <param name="input"> Raw Settings form values. </param>
    /// <param name="draft"> Validated draft when validation succeeds. </param>
    /// <param name="error"> Validation error when validation fails. </param>
    /// <returns> True when a typed draft was created. </returns>
    public bool TryCreateDraft(SettingsInput input, out SettingsDraft? draft, out string? error)
    {
        ArgumentNullException.ThrowIfNull(input);
        draft = null;
        error = null;

        if (!HotkeyParser.TryParse(input.HotkeyText, out var hotkey) || hotkey is null)
        {
            error = "Hotkey needs a modifier (Ctrl, Shift, Alt, Win) and a key.";
            return false;
        }

        if (!TryParsePositive(input.ProbeTtlText, out var ttl))
        {
            error = "Probe TTL must be a positive number of minutes, or empty for the default.";
            return false;
        }

        if (!TryParsePositive(input.ProbeTimeoutText, out var timeout))
        {
            error = "Probe timeout must be a positive number of seconds, or empty for the default.";
            return false;
        }

        if (!int.TryParse(input.LogRetentionDaysText?.Trim(), out var retentionDays) || retentionDays <= 0)
        {
            error = "Log retention must be a positive number of days.";
            return false;
        }

        if (!int.TryParse(input.LogFileSizeLimitText?.Trim(), out var sizeLimitMb) || sizeLimitMb < 0)
        {
            error = "Log file size limit must be zero or a positive number of megabytes.";
            return false;
        }

        if (!AccentPalette.TryNormalize(input.AccentColor, out var accentColor))
        {
            error = "Accent color must be a hex value like #RRGGBB.";
            return false;
        }

        if (!int.TryParse(input.ScanDepthText?.Trim(), out var scanDepth)
            || scanDepth < ProjectScanningDefaults.MinScanDepth
            || scanDepth > ProjectScanningDefaults.MaxScanDepth)
        {
            error = $"Repository scan depth must be a whole number between {ProjectScanningDefaults.MinScanDepth} and {ProjectScanningDefaults.MaxScanDepth}.";
            return false;
        }

        draft = new SettingsDraft(
            input.Runtime,
            input.PathDisplayStyle,
            hotkey,
            ttl,
            timeout,
            input.StartWithWindows,
            input.StartMinimized,
            input.CheckForUpdatesOnStartup,
            input.AutoDownloadUpdates,
            input.NotifyStableUpdates,
            input.NotifyPrereleaseUpdates,
            input.LogLevel,
            retentionDays,
            sizeLimitMb,
            input.PopupScreenPosition,
            input.AppTheme,
            accentColor,
            NormalizeList(input.WatchedDirectories),
            scanDepth,
            NormalizeList(input.IgnoredPatterns),
            input.DiscoveryMode,
            string.IsNullOrWhiteSpace(input.DefaultAgentId) ? null : input.DefaultAgentId);
        return true;
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    /// <summary>
    ///   Applies and persists a validated draft, rolling back system changes on failure.
    /// </summary>
    /// <param name="draft"> Validated Settings draft. </param>
    /// <returns> Application result with a user-facing error when unsuccessful. </returns>
    public SettingsApplicationResult Save(SettingsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var previousPreferences = this.preferencesStore.Load();
        var previousStartupState = this.startupService.IsEnabled();
        var previousHotkey = ParseHotkey(previousPreferences.Hotkey);

        try
        {
            if (!this.applier.ApplyStartWithWindows(draft.StartWithWindows))
            {
                this.Rollback(previousPreferences, previousStartupState, previousHotkey);
                return SettingsApplicationResult.Failed(
                    "Could not update the Windows startup registration. Changes were not saved.");
            }

            this.applier.ApplyRuntime(draft.Runtime);
            this.applier.ApplyPathDisplayStyle(draft.PathDisplayStyle);
            this.applier.ApplyAccent(draft.AccentColor);

            if (!this.applier.ApplyHotkey(draft.Hotkey))
            {
                this.Rollback(previousPreferences, previousStartupState, previousHotkey);
                return SettingsApplicationResult.Failed(
                    "Could not register the global hotkey. Changes were not saved.");
            }

            this.preferencesStore.Update(preferences =>
            {
                preferences.DefaultRuntime = draft.Runtime;
                preferences.Hotkey = HotkeyParser.Format(draft.Hotkey);
                preferences.AgentProbeTtlMinutes = draft.ProbeTtlMinutes;
                preferences.AgentProbeTimeoutSeconds = draft.ProbeTimeoutSeconds;
                preferences.CheckForUpdatesOnStartup = draft.CheckForUpdatesOnStartup;
                preferences.AutoDownloadUpdates = draft.AutoDownloadUpdates;
                preferences.NotifyStableUpdates = draft.NotifyStableUpdates;
                preferences.NotifyPrereleaseUpdates = draft.NotifyPrereleaseUpdates;
                preferences.LogLevel = draft.LogLevel;
                preferences.LogRetentionDays = draft.LogRetentionDays;
                preferences.LogFileSizeLimitMb = draft.LogFileSizeLimitMb;
                preferences.PopupScreenPosition = draft.PopupScreenPosition;
                preferences.AppTheme = draft.AppTheme;
                preferences.AccentColor = draft.AccentColor;
                preferences.StartWithWindows = draft.StartWithWindows;
                preferences.StartMinimized = draft.StartMinimized;
                preferences.ShowWindowOnStartup = !draft.StartMinimized;
                preferences.PathDisplayStyle = PathDisplayStyles.ToToken(draft.PathDisplayStyle);
                preferences.WatchedDirectories = (draft.WatchedDirectories ?? Array.Empty<string>()).ToList();
                preferences.ScanDepth = draft.ScanDepth;
                preferences.IgnoredPatterns = (draft.IgnoredPatterns ?? Array.Empty<string>()).ToList();
                preferences.DiscoveryMode = draft.DiscoveryMode;
                preferences.DefaultAgentId = string.IsNullOrWhiteSpace(draft.DefaultAgentId) ? null : draft.DefaultAgentId;
            });

            this.applier.ApplyStartupUpdateCheck(draft.CheckForUpdatesOnStartup);

            // Reconfigure watching and rescan with the newly saved scanning settings.
            this.projectScanner.Reload();
            return SettingsApplicationResult.Applied;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Applying Settings failed; rolling back system preferences");
            this.Rollback(previousPreferences, previousStartupState, previousHotkey);
            return SettingsApplicationResult.Failed($"Could not save settings: {ex.Message}");
        }
    }

    private void Rollback(AppPreferences previous, bool previousStartupState, HotkeyDefinition previousHotkey)
    {
        this.TryRollback("runtime", () => this.applier.ApplyRuntime(previous.DefaultRuntime));
        this.TryRollback("path display style", () => this.applier.ApplyPathDisplayStyle(PathDisplayStyles.Parse(previous.PathDisplayStyle)));
        this.TryRollback("accent", () => this.applier.ApplyAccent(previous.AccentColor));
        this.TryRollback("hotkey", () => this.applier.ApplyHotkey(previousHotkey));
        this.TryRollback("Windows startup", () => this.applier.ApplyStartWithWindows(previousStartupState));
    }

    private void TryRollback(string setting, Action rollback)
    {
        try
        {
            rollback();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not roll back Settings value {Setting}", setting);
        }
    }

    private static HotkeyDefinition ParseHotkey(string? value) =>
        HotkeyParser.TryParse(value, out var hotkey) && hotkey is not null
            ? hotkey
            : HotkeyParser.Default;

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
