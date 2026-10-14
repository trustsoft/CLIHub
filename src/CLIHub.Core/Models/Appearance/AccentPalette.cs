namespace CLIHub.Core.Models;

/// <summary>
///   A named accent color preset.
/// </summary>
/// <param name="Name"> Display name. </param>
/// <param name="Hex"> The preset color as <c> #RRGGBB </c>. </param>
public sealed record AccentOption(string Name, string Hex);

/// <summary>
///   Application accent color presets and normalization.
/// </summary>
public static class AccentPalette
{
    /// <summary>
    ///   The default accent color (Sky).
    /// </summary>
    public const string DefaultHex = "#4CC2FF";

    /// <summary>
    ///   The preset accent colors offered by the selector.
    /// </summary>
    public static IReadOnlyList<AccentOption> Presets { get; } = new[]
    {
        new AccentOption("Sky", "#4CC2FF"),
        new AccentOption("Windows Blue", "#3B82F6"),
        new AccentOption("Emerald", "#10B981"),
        new AccentOption("Purple", "#8B5CF6"),
        new AccentOption("Amber", "#F59E0B")
    };

    /// <summary>
    ///   Normalizes a color value to <c> #RRGGBB </c> (uppercase). Accepts an optional leading
    ///   <c> # </c> and an optional leading alpha pair. Returns false for invalid input.
    /// </summary>
    /// <param name="value"> The raw color value. </param>
    /// <param name="normalized"> The normalized value when valid. </param>
    /// <returns> True when the value is a valid color. </returns>
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = DefaultHex;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        if (hex.Length != 6 || !hex.All(Uri.IsHexDigit))
        {
            return false;
        }

        normalized = "#" + hex.ToUpperInvariant();
        return true;
    }

    /// <summary>
    ///   Normalizes a color value, falling back to the default for invalid input.
    /// </summary>
    /// <param name="value"> The raw color value. </param>
    /// <returns> The normalized value or the default. </returns>
    public static string Normalize(string? value) => TryNormalize(value, out var normalized) ? normalized : DefaultHex;

    /// <summary>
    ///   Returns the low-alpha selection tint for an accent value.
    /// </summary>
    /// <param name="accentHex"> The normalized <c> #RRGGBB </c> accent. </param>
    /// <returns> The <c> #AARRGGBB </c> selection tint. </returns>
    public static string SelectionTint(string accentHex) => "#1F" + Normalize(accentHex).TrimStart('#');

    /// <summary>
    ///   Returns the fully opaque <c> #AARRGGBB </c> form of an accent value.
    /// </summary>
    /// <param name="accentHex"> The normalized <c> #RRGGBB </c> accent. </param>
    /// <returns> The opaque color. </returns>
    public static string Opaque(string accentHex) => "#FF" + Normalize(accentHex).TrimStart('#');
}
