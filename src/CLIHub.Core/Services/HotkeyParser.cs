namespace CLIHub.Core.Services;

public static class HotkeyParser
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    public static bool TryParse(string hotkey, out uint modifiers, out uint virtualKey, out string? error)
    {
        modifiers = 0;
        virtualKey = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(hotkey))
        {
            error = "Комбинация hotkey не задана.";
            return false;
        }

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                    modifiers |= ModWin;
                    break;
                default:
                    if (!TryParseKey(part, out virtualKey))
                    {
                        error = $"Неизвестная клавиша '{part}' в комбинации hotkey.";
                        return false;
                    }

                    break;
            }
        }

        if (virtualKey == 0)
        {
            error = $"В комбинации '{hotkey}' не задана основная клавиша.";
            return false;
        }

        modifiers |= ModNoRepeat;
        return true;
    }

    private static bool TryParseKey(string key, out uint virtualKey)
    {
        virtualKey = 0;

        if (key.Length == 1)
        {
            var character = char.ToUpperInvariant(key[0]);
            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = character;
                return true;
            }
        }

        if (key.Length >= 2 &&
            char.ToUpperInvariant(key[0]) == 'F' &&
            int.TryParse(key.AsSpan(1), out var functionKey) &&
            functionKey is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + functionKey - 1);
            return true;
        }

        virtualKey = key.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "enter" or "return" => 0x0D,
            "tab" => 0x09,
            "escape" or "esc" => 0x1B,
            "backspace" => 0x08,
            "delete" or "del" => 0x2E,
            "insert" or "ins" => 0x2D,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" => 0x21,
            "pagedown" => 0x22,
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            _ => 0
        };

        return virtualKey != 0;
    }
}
