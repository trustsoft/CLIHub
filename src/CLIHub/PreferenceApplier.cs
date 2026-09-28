namespace CLIHub;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;
using Microsoft.Extensions.Logging;

/// <summary>
/// Default <see cref="IPreferenceApplier"/>; forwards changes to the launcher and hotkey service.
/// </summary>
public sealed class PreferenceApplier : IPreferenceApplier
{
    private readonly IProcessLauncher _processLauncher;
    private readonly GlobalHotkeyService _hotkey;
    private readonly IStartupService _startupService;
    private readonly ILogger<PreferenceApplier> _logger;

    public PreferenceApplier(
        IProcessLauncher processLauncher,
        GlobalHotkeyService hotkey,
        IStartupService startupService,
        ILogger<PreferenceApplier> logger)
    {
        _processLauncher = processLauncher;
        _hotkey = hotkey;
        _startupService = startupService;
        _logger = logger;
    }

    public void ApplyRuntime(RuntimeKind runtime)
    {
        _processLauncher.SetRuntime(runtime);
        _logger.LogInformation("Default runtime set to {Runtime}", runtime);
    }

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

    public void ApplyStartupUpdateCheck(bool enabled) =>
        _logger.LogInformation("Startup update check set to {Enabled} (applies on next start)", enabled);

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
