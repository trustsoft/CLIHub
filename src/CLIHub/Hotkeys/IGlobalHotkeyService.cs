namespace CLIHub.Hotkeys;

using CLIHub.Core.Hotkeys;

/// <summary>
///   Registers global hotkey definitions against the application window.
/// </summary>
public interface IGlobalHotkeyService
{
    /// <summary>
    ///   Registers a hotkey definition and returns whether Windows accepted it.
    /// </summary>
    /// <param name="definition"> The hotkey definition to register. </param>
    /// <returns> True when the hotkey was registered; otherwise false. </returns>
    bool Register(HotkeyDefinition definition);

    /// <summary>
    ///   Replaces the current hotkey and restores the previous combination when registration fails.
    /// </summary>
    /// <param name="definition"> The replacement hotkey definition. </param>
    /// <returns> True when the replacement was registered; otherwise false. </returns>
    bool ReRegister(HotkeyDefinition definition);
}
