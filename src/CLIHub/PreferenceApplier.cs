namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;

/// <summary>
///   Default <see cref="IPreferenceApplier"/>; forwards changes to the launcher and hotkey service.
/// </summary>
public sealed class PreferenceApplier : IPreferenceApplier
{
    private readonly IRuntimePreferenceTarget runtime;
    private readonly IGlobalHotkeyService hotkey;
    private readonly IStartupService startupService;
    private readonly IPathDisplayStyleTarget displayStyle;
    private readonly IAccentPreferenceTarget accent;
    private readonly ILogger<PreferenceApplier> logger;

    /// <summary>
    ///   Creates the applier with the services it forwards preference changes to.
    /// </summary>
    /// <param name="runtime"> Runtime target receiving the runtime change. </param>
    /// <param name="hotkey"> Hotkey target receiving the hotkey change. </param>
    /// <param name="startupService"> Startup service receiving the start-with-Windows change. </param>
    /// <param name="displayStyle"> Display-style target receiving path changes. </param>
    /// <param name="accent"> Accent target receiving the accent change. </param>
    /// <param name="logger"> Logger. </param>
    public PreferenceApplier(
        IRuntimePreferenceTarget runtime,
        IGlobalHotkeyService hotkey,
        IStartupService startupService,
        IPathDisplayStyleTarget displayStyle,
        IAccentPreferenceTarget accent,
        ILogger<PreferenceApplier> logger)
    {
        this.runtime = runtime;
        this.hotkey = hotkey;
        this.startupService = startupService;
        this.displayStyle = displayStyle;
        this.accent = accent;
        this.logger = logger;
    }

    /// <inheritdoc />
    public void ApplyRuntime(RuntimeKind runtime)
    {
        this.runtime.SetRuntime(runtime);
        this.logger.LogInformation("Default runtime set to {Runtime}", runtime);
    }

    /// <inheritdoc />
    public void ApplyPathDisplayStyle(PathDisplayStyle style)
    {
        this.displayStyle.ApplyPathDisplayStyle(style);
        this.logger.LogInformation("Path display style set to {Style}", style);
    }

    /// <inheritdoc />
    public bool ApplyHotkey(HotkeyDefinition definition)
    {
        var registered = this.hotkey.ReRegister(definition);

        if (registered)
        {
            this.logger.LogInformation("Global hotkey changed to {Hotkey}", HotkeyParser.Format(definition));
        }
        else
        {
            this.logger.LogWarning(
                "Could not register new hotkey {Hotkey}; the previous combination was kept",
                HotkeyParser.Format(definition));
        }

        return registered;
    }

    /// <inheritdoc />
    public void ApplyStartupUpdateCheck(bool enabled) =>
        this.logger.LogInformation("Startup update check set to {Enabled} (applies on next start)", enabled);

    /// <inheritdoc />
    public bool ApplyStartWithWindows(bool enabled)
    {
        var updated = this.startupService.SetEnabled(enabled);

        if (updated)
        {
            this.logger.LogInformation("Start with Windows set to {Enabled}", enabled);
        }
        else
        {
            this.logger.LogWarning("Could not update the start-with-Windows registration");
        }

        return updated;
    }

    /// <inheritdoc />
    public void ApplyAccent(string accentHex)
    {
        this.accent.SetAccent(accentHex);
        this.logger.LogInformation("Accent color set to {Accent}", accentHex);
    }
}
