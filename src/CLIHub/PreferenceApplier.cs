namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;
using CLIHub.ViewModels;

/// <summary>
///   Default <see cref="IPreferenceApplier"/>; forwards changes to the launcher and hotkey service.
/// </summary>
public sealed class PreferenceApplier : IPreferenceApplier
{
    private readonly IProcessLauncher _processLauncher;
    private readonly GlobalHotkeyService _hotkey;
    private readonly IStartupService _startupService;
    private readonly LaunchWindowViewModel _launchWindow;
    private readonly ILogger<PreferenceApplier> _logger;

    /// <summary>
    ///   Creates the applier with the services it forwards preference changes to.
    /// </summary>
    /// <param name="processLauncher"> Process launcher receiving the runtime change. </param>
    /// <param name="hotkey"> Hotkey service receiving the hotkey change. </param>
    /// <param name="startupService"> Startup service receiving the start-with-Windows change. </param>
    /// <param name="launchWindow"> Launch window receiving display changes. </param>
    /// <param name="logger"> Logger. </param>
    public PreferenceApplier(
        IProcessLauncher processLauncher,
        GlobalHotkeyService hotkey,
        IStartupService startupService,
        LaunchWindowViewModel launchWindow,
        ILogger<PreferenceApplier> logger)
    {
        _processLauncher = processLauncher;
        _hotkey = hotkey;
        _startupService = startupService;
        _launchWindow = launchWindow;
        _logger = logger;
    }

    /// <inheritdoc />
    public void ApplyRuntime(RuntimeKind runtime)
    {
        _processLauncher.SetRuntime(runtime);
        _logger.LogInformation("Default runtime set to {Runtime}", runtime);
    }

    /// <inheritdoc />
    public void ApplyPathDisplayStyle(PathDisplayStyle style)
    {
        _launchWindow.ApplyPathDisplayStyle(style);
        _logger.LogInformation("Path display style set to {Style}", style);
    }

    /// <inheritdoc />
    public bool ApplyHotkey(HotkeyDefinition definition)
    {
        var registered = _hotkey.ReRegister(definition);

        if (registered)
        {
            _logger.LogInformation("Global hotkey changed to {Hotkey}", HotkeyParser.Format(definition));
        }
        else
        {
            _logger.LogWarning(
                "Could not register new hotkey {Hotkey}; the previous combination was kept",
                HotkeyParser.Format(definition));
        }

        return registered;
    }

    /// <inheritdoc />
    public void ApplyStartupUpdateCheck(bool enabled) =>
        _logger.LogInformation("Startup update check set to {Enabled} (applies on next start)", enabled);

    /// <inheritdoc />
    public bool ApplyStartWithWindows(bool enabled)
    {
        var updated = _startupService.SetEnabled(enabled);

        if (updated)
        {
            _logger.LogInformation("Start with Windows set to {Enabled}", enabled);
        }
        else
        {
            _logger.LogWarning("Could not update the start-with-Windows registration");
        }

        return updated;
    }
}
