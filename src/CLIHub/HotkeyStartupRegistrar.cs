namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;

/// <summary>
///   Coordinates startup hotkey parsing, fallback selection, and global registration.
/// </summary>
public sealed class HotkeyStartupRegistrar : IHotkeyStartupRegistrar
{
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly ILogger<HotkeyStartupRegistrar> _logger;

    /// <summary>
    ///   Creates the startup registrar over the global hotkey boundary.
    /// </summary>
    /// <param name="hotkeyService"> Global hotkey registration service. </param>
    /// <param name="logger"> Logger for invalid configured hotkeys. </param>
    public HotkeyStartupRegistrar(
        IGlobalHotkeyService hotkeyService,
        ILogger<HotkeyStartupRegistrar> logger)
    {
        _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogWarning(
                "Invalid hotkey '{Hotkey}' in config; using default Ctrl+Shift+A",
                preferences.Hotkey);
            definition = HotkeyParser.Default;
        }

        _hotkeyService.Register(definition);
    }
}
