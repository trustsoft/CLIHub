namespace CLIHub.Core.Hotkeys;

/// <summary>
///   Parses human-readable hotkey strings such as <c>Ctrl+Shift+A</c> into a
///   <see cref="HotkeyDefinition"/>. Pure logic; no Win32 or WPF dependency.
/// </summary>
public static class HotkeyParser
{
    /// <summary>
    ///   The default hotkey: Ctrl+Shift+A (virtual key 0x41).
    /// </summary>
    public static HotkeyDefinition Default { get; } =
        new(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x41);

    private static readonly (string Name, int VirtualKey, string[] Aliases)[] NamedKeys =
    [
        ("Space", 0x20, []),
        ("Enter", 0x0D, ["Return"]),
        ("Tab", 0x09, []),
        ("Escape", 0x1B, ["Esc"]),
        ("Backspace", 0x08, ["Back"]),
        ("Delete", 0x2E, ["Del"]),
        ("Insert", 0x2D, ["Ins"]),
        ("Home", 0x24, []),
        ("End", 0x23, []),
        ("PageUp", 0x21, ["PgUp"]),
        ("PageDown", 0x22, ["PgDn"]),
        ("Up", 0x26, []),
        ("Down", 0x28, []),
        ("Left", 0x25, []),
        ("Right", 0x27, []),
    ];

    private static readonly IReadOnlyDictionary<string, int> NamedKeyLookup = BuildNameLookup();
    private static readonly IReadOnlyDictionary<int, string> VirtualKeyLookup = BuildVirtualKeyLookup();

    /// <summary>
    ///   Attempts to parse a hotkey string. Requires at least one modifier and exactly
    ///   one recognized key. Returns false for null, empty, modifier-less, or unknown input.
    /// </summary>
    public static bool TryParse(string? value, out HotkeyDefinition? definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

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
                    {
                        return false; // more than one key token
                    }

                    keyToken = token;
                    break;
            }
        }

        if (modifiers == HotkeyModifiers.None || keyToken == null)
        {
            return false;
        }

        if (!TryGetVirtualKey(keyToken, out var virtualKey))
        {
            return false;
        }

        definition = new HotkeyDefinition(modifiers, virtualKey);
        return true;
    }

    /// <summary>
    ///   Returns the parsed definition, or <paramref name="fallback"/> when the value is invalid.
    /// </summary>
    public static HotkeyDefinition ParseOrDefault(string? value, HotkeyDefinition fallback) =>
        TryParse(value, out var definition) && definition != null ? definition : fallback;

    /// <summary>
    ///   Formats a definition back into the canonical string form (for example <c>Ctrl+Shift+A</c>).
    /// </summary>
    public static string Format(HotkeyDefinition definition)
    {
        var parts = new List<string>();

        if ((definition.Modifiers & HotkeyModifiers.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((definition.Modifiers & HotkeyModifiers.Shift) != 0)
        {
            parts.Add("Shift");
        }

        if ((definition.Modifiers & HotkeyModifiers.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((definition.Modifiers & HotkeyModifiers.Win) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(VirtualKeyName(definition.VirtualKey));
        return string.Join("+", parts);
    }

    private static IReadOnlyDictionary<string, int> BuildNameLookup()
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, virtualKey, aliases) in NamedKeys)
        {
            lookup[name] = virtualKey;

            foreach (var alias in aliases)
            {
                lookup[alias] = virtualKey;
            }
        }

        return lookup;
    }

    private static IReadOnlyDictionary<int, string> BuildVirtualKeyLookup()
    {
        var lookup = new Dictionary<int, string>();

        foreach (var (name, virtualKey, _) in NamedKeys)
        {
            lookup.TryAdd(virtualKey, name);
        }

        return lookup;
    }

    private static string VirtualKeyName(int virtualKey)
    {
        if (VirtualKeyLookup.TryGetValue(virtualKey, out var name))
        {
            return name;
        }

        if ((virtualKey >= 0x41 && virtualKey <= 0x5A) || (virtualKey >= 0x30 && virtualKey <= 0x39))
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey >= 0x70 && virtualKey <= 0x87)
        {
            return $"F{virtualKey - 0x70 + 1}";
        }

        return "0x" + virtualKey.ToString("X2");
    }

    private static bool TryGetVirtualKey(string token, out int virtualKey)
    {
        virtualKey = 0;

        if (NamedKeyLookup.TryGetValue(token, out virtualKey))
        {
            return true;
        }

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
