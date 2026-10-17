namespace CLIHub.Startup;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;

/// <summary>
///   Coordinates startup hotkey parsing, fallback selection, and global registration.
/// </summary>
public sealed class HotkeyStartupRegistrar : IHotkeyStartupRegistrar
{
    private readonly IGlobalHotkeyService hotkeyService;
    private readonly ILogger<HotkeyStartupRegistrar> logger;

    /// <summary>
    ///   Creates the startup registrar over the global hotkey boundary.
    /// </summary>
    /// <param name="hotkeyService"> Global hotkey registration service. </param>
    /// <param name="logger"> Logger for invalid configured hotkeys. </param>
    public HotkeyStartupRegistrar(IGlobalHotkeyService hotkeyService,
                                  ILogger<HotkeyStartupRegistrar> logger)
    {
        this.hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Register(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        HotkeyDefinition definition;
        if (HotkeyParser.TryParse(preferences.Hotkey, out var parsed) && parsed != null)
        {
            definition = parsed;
        }
        else
        {
            this.logger.LogWarning(
                "Invalid hotkey '{Hotkey}' in config; using default {DefaultHotkey}",
                preferences.Hotkey,
                HotkeyParser.Format(HotkeyParser.Default));
            definition = HotkeyParser.Default;
        }

        this.hotkeyService.Register(definition);
    }
}
