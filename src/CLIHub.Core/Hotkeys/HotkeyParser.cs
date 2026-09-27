namespace CLIHub.Core.Hotkeys;

/// <summary>
/// Parses human-readable hotkey strings such as <c>Ctrl+Shift+A</c> into a
/// <see cref="HotkeyDefinition"/>. Pure logic; no Win32 or WPF dependency.
/// </summary>
public static class HotkeyParser
{
    /// <summary>
    /// The default hotkey: Ctrl+Shift+A (virtual key 0x41).
    /// </summary>
    public static HotkeyDefinition Default { get; } =
        new(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x41);

    /// <summary>
    /// Attempts to parse a hotkey string. Requires at least one modifier and exactly
    /// one recognized key. Returns false for null, empty, modifier-less, or unknown input.
    /// </summary>
    public static bool TryParse(string? value, out HotkeyDefinition? definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var tokens = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
            return false;

        var modifiers = HotkeyModifiers.None;
        string? keyToken = null;

        foreach (var token in tokens)
        {
            switch (token.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "win":
                case "windows":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    if (keyToken != null)
                        return false; // more than one key token
                    keyToken = token;
                    break;
            }
        }

        if (modifiers == HotkeyModifiers.None || keyToken == null)
            return false;

        if (!TryGetVirtualKey(keyToken, out var virtualKey))
            return false;

        definition = new HotkeyDefinition(modifiers, virtualKey);
        return true;
    }

    /// <summary>
    /// Returns the parsed definition, or <paramref name="fallback"/> when the value is invalid.
    /// </summary>
    public static HotkeyDefinition ParseOrDefault(string? value, HotkeyDefinition fallback) =>
        TryParse(value, out var definition) && definition != null ? definition : fallback;

    private static bool TryGetVirtualKey(string token, out int virtualKey)
    {
        virtualKey = 0;

        if (token.Length == 1)
        {
            var c = char.ToUpperInvariant(token[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                virtualKey = c;
                return true;
            }
            return false;
        }

        if ((token[0] == 'F' || token[0] == 'f') &&
            int.TryParse(token.AsSpan(1), out var number) &&
            number >= 1 && number <= 24)
        {
            virtualKey = 0x70 + (number - 1); // VK_F1 == 0x70
            return true;
        }

        return false;
    }
}
