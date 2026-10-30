namespace CLIHub.Settings;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Windows;
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
/// <param name="Scanning"> Scanning settings edited on the Project Scanning page. </param>
/// <param name="DefaultAgentId"> Identifier of the agent preselected for launches, or null for none. </param>
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
    ScanningSettingsInput Scanning,
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
/// <param name="Scanning"> Normalized scanning settings ready for persistence. </param>
/// <param name="DefaultAgentId"> Identifier of the agent preselected for launches, or null for none. </param>
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
    ScanningSettingsInput Scanning,
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
///   Scanning settings edited on the Project Scanning page. They apply as soon as they change.
/// </summary>
/// <param name="WatchedDirectories"> Watched root directories for repository scanning. </param>
/// <param name="ScanDepth"> Default repository scan depth. </param>
/// <param name="IgnoredPatterns"> Ignored directory-name patterns. </param>
/// <param name="DiscoveryMode"> Default discovery mode for directories added from now on. </param>
/// <param name="WatchedDirectorySettings"> Per-directory scan settings keyed by watched root path. </param>
public sealed record ScanningSettingsInput(IReadOnlyList<string>? WatchedDirectories,
                                           int ScanDepth,
                                           IReadOnlyList<string>? IgnoredPatterns,
                                           ProjectDiscoveryMode DiscoveryMode,
                                           IReadOnlyDictionary<string, WatchedDirectoryScanSettings>? WatchedDirectorySettings);

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
    public SettingsApplicationService(IPreferencesStore preferencesStore,
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
    ///   One system preference applied during a Settings save, with its rollback action.
    /// </summary>
    /// <param name="Name"> Setting name used in rollback diagnostics. </param>
    /// <param name="Apply"> Applies the new value and reports whether it took effect. </param>
    /// <param name="Rollback"> Restores the previous value. </param>
    /// <param name="RollbackOrder"> Position of this entry in the rollback sequence. </param>
    /// <param name="FailureMessage"> User-facing message when applying returns false. </param>
    private sealed record AppliedPreference(string Name,
                                            Func<bool> Apply,
                                            Action Rollback,
                                            int RollbackOrder,
                                            string FailureMessage);

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
        var scanning = new ScanningSettingsInput(
            preferences.Scanning.WatchedDirectories is { } watched ? watched : Array.Empty<string>(),
            ProjectScanningDefaults.NormalizeScanDepth(preferences.Scanning.ScanDepth),
            preferences.Scanning.IgnoredPatterns is { } ignored ? ignored : ProjectScanningDefaults.IgnoredPatterns,
            preferences.Scanning.DiscoveryMode,
            preferences.Scanning.WatchedDirectorySettings);

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
            scanning,
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
            error = "Cache TTL must be a positive number of minutes, or empty for the default.";
            return false;
        }

        if (!TryParsePositive(input.ProbeTimeoutText, out var timeout))
        {
            error = "Version check timeout must be a positive number of seconds, or empty for the default.";
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

        if (!TryNormalizeScanning(input.Scanning, out var normalizedScanning, out error))
        {
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
            normalizedScanning,
            string.IsNullOrWhiteSpace(input.DefaultAgentId) ? null : input.DefaultAgentId);
        return true;
    }

    /// <summary>
    ///   Applies and persists scanning settings edited on the Project Scanning page and reconfigures the scanner.
    ///   Only scanning preferences are written; the remaining settings keep the Save/Cancel flow.
    /// </summary>
    /// <param name="input"> Scanning values from the page. </param>
    /// <returns> Application result with a user-facing error when unsuccessful. </returns>
    /// <remarks>
    ///   The scanner reload is best-effort: once the change is written, a reload failure is logged
    ///   and the result still reports success, so the stored values are never rolled back.
    /// </remarks>
    public SettingsApplicationResult ApplyScanning(ScanningSettingsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!TryNormalizeScanning(input, out var normalized, out var error))
        {
            return SettingsApplicationResult.Failed(error!);
        }

        try
        {
            this.preferencesStore.Update(preferences => WriteScanning(preferences, normalized));

            // Reconfigure watching and rescan with the settings the page now shows.
            // The scanner reload is best-effort: the write already succeeded, so a reload failure
            // must not report an apply failure or undo the stored values.
            try
            {
                this.projectScanner.Reload();
            }
            catch (Exception reloadEx)
            {
                this.logger.LogWarning(reloadEx, "Settings saved but the project scanner could not reload with the new configuration");
            }

            return SettingsApplicationResult.Applied;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Applying scanning settings failed");
            return SettingsApplicationResult.Failed($"Could not apply scanning settings: {ex.Message}");
        }
    }

    private static string ScanDepthError =>
        $"Repository scan depth must be a whole number between {ProjectScanningDefaults.MinScanDepth} and {ProjectScanningDefaults.MaxScanDepth}.";

    private static string WatchedDirectoryScanDepthError(string root) =>
        $"Repository scan depth for {root} must be between {ProjectScanningDefaults.MinScanDepth} and {ProjectScanningDefaults.MaxScanDepth}.";

    private static bool IsScanDepthInRange(int scanDepth) =>
        scanDepth is >= ProjectScanningDefaults.MinScanDepth and <= ProjectScanningDefaults.MaxScanDepth;

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

    private static IReadOnlyDictionary<string, WatchedDirectoryScanSettings> NormalizeWatchedDirectorySettings(
        IReadOnlyDictionary<string, WatchedDirectoryScanSettings>? settings,
        IReadOnlyList<string> watchedDirectories)
    {
        var result = new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase);

        if (settings is null)
        {
            return result;
        }

        var roots = new HashSet<string>(watchedDirectories, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in settings)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || !roots.Contains(entry.Key))
            {
                continue;
            }

            result[entry.Key] = new WatchedDirectoryScanSettings(
                ProjectScanningDefaults.NormalizeScanDepth(entry.Value.ScanDepth),
                entry.Value.DiscoveryMode);
        }

        return result;
    }

    private static bool TryNormalizeScanning(
        ScanningSettingsInput input,
        out ScanningSettingsInput normalized,
        out string? error)
    {
        normalized = input;
        error = null;

        if (!IsScanDepthInRange(input.ScanDepth))
        {
            error = ScanDepthError;
            return false;
        }

        if (input.WatchedDirectorySettings is not null)
        {
            foreach (var entry in input.WatchedDirectorySettings)
            {
                if (!IsScanDepthInRange(entry.Value.ScanDepth))
                {
                    error = WatchedDirectoryScanDepthError(entry.Key);
                    return false;
                }
            }
        }

        var watchedDirectories = NormalizeList(input.WatchedDirectories);
        var watchedDirectorySettings = NormalizeWatchedDirectorySettings(
            input.WatchedDirectorySettings,
            watchedDirectories);

        normalized = new ScanningSettingsInput(
            watchedDirectories,
            input.ScanDepth,
            NormalizeList(input.IgnoredPatterns),
            input.DiscoveryMode,
            watchedDirectorySettings);
        return true;
    }

    private static void WriteScanning(AppPreferences preferences, ScanningSettingsInput scanning)
    {
        preferences.Scanning.WatchedDirectories = (scanning.WatchedDirectories ?? Array.Empty<string>()).ToList();
        preferences.Scanning.WatchedDirectorySettings = scanning.WatchedDirectorySettings is null
            ? new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, WatchedDirectoryScanSettings>(
                scanning.WatchedDirectorySettings,
                StringComparer.OrdinalIgnoreCase);
        preferences.Scanning.ScanDepth = scanning.ScanDepth;
        preferences.Scanning.IgnoredPatterns = (scanning.IgnoredPatterns ?? Array.Empty<string>()).ToList();
        preferences.Scanning.DiscoveryMode = scanning.DiscoveryMode;
    }

    /// <summary>
    ///   Applies and persists a validated draft, rolling back system changes on failure.
    /// </summary>
    /// <param name="draft"> Validated Settings draft. </param>
    /// <returns> Application result with a user-facing error when unsuccessful. </returns>
    /// <remarks>
    ///   Apply and persistence failures before the write roll back the system preferences.
    ///   The scanner reload after a successful write is best-effort: a reload failure is logged and
    ///   the save still reports success, so the persisted values are kept.
    /// </remarks>
    public SettingsApplicationResult Save(SettingsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var previousPreferences = this.preferencesStore.Load();
        var previousStartupState = this.startupService.IsEnabled();
        var previousHotkey = ParseHotkey(previousPreferences.Hotkey);
        var items = new List<AppliedPreference>
        {
            new("Windows startup",
                () => this.applier.ApplyStartWithWindows(draft.StartWithWindows),
                () => this.applier.ApplyStartWithWindows(previousStartupState),
                6,
                "Could not update the Windows startup registration. Changes were not saved."),
            new("runtime",
                () =>
                {
                    this.applier.ApplyRuntime(draft.Runtime);
                    return true;
                },
                () => this.applier.ApplyRuntime(previousPreferences.DefaultRuntime),
                1,
                string.Empty),
            new("path display style",
                () =>
                {
                    this.applier.ApplyPathDisplayStyle(draft.PathDisplayStyle);
                    return true;
                },
                () => this.applier.ApplyPathDisplayStyle(PathDisplayStyles.Parse(previousPreferences.PathDisplayStyle)),
                2,
                string.Empty),
            new("accent",
                () =>
                {
                    this.applier.ApplyAccent(draft.AccentColor);
                    return true;
                },
                () => this.applier.ApplyAccent(previousPreferences.AccentColor),
                3,
                string.Empty),
            new("theme",
                () =>
                {
                    this.applier.ApplyTheme(draft.AppTheme);
                    return true;
                },
                () => this.applier.ApplyTheme(previousPreferences.AppTheme),
                4,
                string.Empty),
            new("hotkey",
                () => this.applier.ApplyHotkey(draft.Hotkey),
                () => this.applier.ApplyHotkey(previousHotkey),
                5,
                "Could not register the global hotkey. Changes were not saved.")
        };

        try
        {
            foreach (var item in items)
            {
                if (!item.Apply())
                {
                    this.Rollback(items);
                    return SettingsApplicationResult.Failed(item.FailureMessage);
                }
            }

            this.preferencesStore.Update(preferences => WritePreferences(preferences, draft));

            this.applier.ApplyStartupUpdateCheck(draft.CheckForUpdatesOnStartup);

            // Reconfigure watching and rescan with the newly saved scanning settings.
            // The scanner reload is best-effort: the configuration is already persisted, so a
            // reload failure must not roll back the system preferences applied above.
            try
            {
                this.projectScanner.Reload();
            }
            catch (Exception reloadEx)
            {
                this.logger.LogWarning(reloadEx, "Settings saved but the project scanner could not reload with the new configuration");
            }

            return SettingsApplicationResult.Applied;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Applying Settings failed; rolling back system preferences");
            this.Rollback(items);
            return SettingsApplicationResult.Failed($"Could not save settings: {ex.Message}");
        }
    }

    private static void WritePreferences(AppPreferences preferences, SettingsDraft draft)
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
        WriteScanning(preferences, draft.Scanning);
        preferences.DefaultAgentId = string.IsNullOrWhiteSpace(draft.DefaultAgentId) ? null : draft.DefaultAgentId;
    }

    private void Rollback(IReadOnlyList<AppliedPreference> items)
    {
        foreach (var item in items.OrderBy(x => x.RollbackOrder))
        {
            this.TryRollback(item.Name, item.Rollback);
        }
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
