namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

/// <summary>
///   Raw values collected from the Settings form before validation.
/// </summary>
/// <param name="Runtime"> Selected runtime. </param>
/// <param name="PathDisplayStyle"> Selected path display style. </param>
/// <param name="HotkeyText"> Hotkey text entered by the user. </param>
/// <param name="ProbeTtlText"> Probe TTL text, or empty for the default. </param>
/// <param name="ProbeTimeoutText"> Probe timeout text, or empty for the default. </param>
/// <param name="StartWithWindows"> Whether Windows startup should be enabled. </param>
/// <param name="ShowWindowOnStartup"> Whether the launch window should be shown at startup. </param>
/// <param name="CheckForUpdatesOnStartup"> Whether startup update checks are enabled. </param>
public sealed record SettingsInput(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    string HotkeyText,
    string ProbeTtlText,
    string ProbeTimeoutText,
    bool StartWithWindows,
    bool ShowWindowOnStartup,
    bool CheckForUpdatesOnStartup);

/// <summary>
///   Validated, typed settings ready for persistence and application.
/// </summary>
/// <param name="Runtime"> Runtime used for interactive agent launches. </param>
/// <param name="PathDisplayStyle"> Style used for project path display. </param>
/// <param name="Hotkey"> Parsed global hotkey. </param>
/// <param name="ProbeTtlMinutes"> Probe TTL, or null for the default. </param>
/// <param name="ProbeTimeoutSeconds"> Probe timeout, or null for the default. </param>
/// <param name="StartWithWindows"> Whether Windows startup should be enabled. </param>
/// <param name="ShowWindowOnStartup"> Whether the launch window should be shown at startup. </param>
/// <param name="CheckForUpdatesOnStartup"> Whether startup update checks are enabled. </param>
public sealed record SettingsDraft(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    HotkeyDefinition Hotkey,
    int? ProbeTtlMinutes,
    int? ProbeTimeoutSeconds,
    bool StartWithWindows,
    bool ShowWindowOnStartup,
    bool CheckForUpdatesOnStartup);

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
    private readonly IPreferencesStore _preferencesStore;
    private readonly IStartupService _startupService;
    private readonly IPreferenceApplier _applier;
    private readonly ILogger<SettingsApplicationService> _logger;

    /// <summary>
    ///   Creates the Settings application service.
    /// </summary>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupService"> Windows startup registration state boundary. </param>
    /// <param name="applier"> Runtime system preference boundary. </param>
    /// <param name="logger"> Logger for application and rollback failures. </param>
    public SettingsApplicationService(
        IPreferencesStore preferencesStore,
        IStartupService startupService,
        IPreferenceApplier applier,
        ILogger<SettingsApplicationService> logger)
    {
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _applier = applier ?? throw new ArgumentNullException(nameof(applier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Loads the current persisted preferences and system startup state as a typed draft.
    /// </summary>
    /// <returns> The current Settings draft. </returns>
    public SettingsDraft Load()
    {
        var preferences = _preferencesStore.Load();
        var hotkey = HotkeyParser.TryParse(preferences.Hotkey, out var parsedHotkey) && parsedHotkey is not null
            ? parsedHotkey
            : HotkeyParser.Default;

        return new SettingsDraft(
            preferences.DefaultRuntime,
            PathDisplayStyles.Parse(preferences.PathDisplayStyle),
            hotkey,
            preferences.AgentProbeTtlMinutes,
            preferences.AgentProbeTimeoutSeconds,
            _startupService.IsEnabled(),
            preferences.ShowWindowOnStartup,
            preferences.CheckForUpdatesOnStartup);
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

        draft = new SettingsDraft(
            input.Runtime,
            input.PathDisplayStyle,
            hotkey,
            ttl,
            timeout,
            input.StartWithWindows,
            input.ShowWindowOnStartup,
            input.CheckForUpdatesOnStartup);
        return true;
    }

    /// <summary>
    ///   Applies and persists a validated draft, rolling back system changes on failure.
    /// </summary>
    /// <param name="draft"> Validated Settings draft. </param>
    /// <returns> Application result with a user-facing error when unsuccessful. </returns>
    public SettingsApplicationResult Save(SettingsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var previousPreferences = _preferencesStore.Load();
        var previousStartupState = _startupService.IsEnabled();
        var previousHotkey = ParseHotkey(previousPreferences.Hotkey);

        try
        {
            if (!_applier.ApplyStartWithWindows(draft.StartWithWindows))
            {
                Rollback(previousPreferences, previousStartupState, previousHotkey);
                return SettingsApplicationResult.Failed(
                    "Could not update the Windows startup registration. Changes were not saved.");
            }

            _applier.ApplyRuntime(draft.Runtime);
            _applier.ApplyPathDisplayStyle(draft.PathDisplayStyle);

            if (!_applier.ApplyHotkey(draft.Hotkey))
            {
                Rollback(previousPreferences, previousStartupState, previousHotkey);
                return SettingsApplicationResult.Failed(
                    "Could not register the global hotkey. Changes were not saved.");
            }

            _preferencesStore.Update(preferences =>
            {
                preferences.DefaultRuntime = draft.Runtime;
                preferences.Hotkey = HotkeyParser.Format(draft.Hotkey);
                preferences.AgentProbeTtlMinutes = draft.ProbeTtlMinutes;
                preferences.AgentProbeTimeoutSeconds = draft.ProbeTimeoutSeconds;
                preferences.CheckForUpdatesOnStartup = draft.CheckForUpdatesOnStartup;
                preferences.StartWithWindows = draft.StartWithWindows;
                preferences.ShowWindowOnStartup = draft.ShowWindowOnStartup;
                preferences.PathDisplayStyle = PathDisplayStyles.ToToken(draft.PathDisplayStyle);
            });

            _applier.ApplyStartupUpdateCheck(draft.CheckForUpdatesOnStartup);
            return SettingsApplicationResult.Applied;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Applying Settings failed; rolling back system preferences");
            Rollback(previousPreferences, previousStartupState, previousHotkey);
            return SettingsApplicationResult.Failed($"Could not save settings: {ex.Message}");
        }
    }

    private void Rollback(AppPreferences previous, bool previousStartupState, HotkeyDefinition previousHotkey)
    {
        TryRollback("runtime", () => _applier.ApplyRuntime(previous.DefaultRuntime));
        TryRollback("path display style", () => _applier.ApplyPathDisplayStyle(PathDisplayStyles.Parse(previous.PathDisplayStyle)));
        TryRollback("hotkey", () => _applier.ApplyHotkey(previousHotkey));
        TryRollback("Windows startup", () => _applier.ApplyStartWithWindows(previousStartupState));
    }

    private void TryRollback(string setting, Action rollback)
    {
        try
        {
            rollback();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not roll back Settings value {Setting}", setting);
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
