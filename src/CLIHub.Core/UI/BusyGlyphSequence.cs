namespace CLIHub.Core.UI;

/// <summary>
///   Owns the busy-marker glyph sequence shared by UI surfaces: the braille animation frames,
///   the frame duration clamp, and the static glyph shown when animations are disabled.
///   UI-independent, so the WPF control stays thin and the sequence logic is unit-testable.
/// </summary>
public static class BusyGlyphSequence
{
    private static readonly string[] Frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    /// <summary>
    ///   The animation frame at <paramref name="index" />, wrapping around at the end of the
    ///   sequence.
    /// </summary>
    /// <param name="index"> Zero-based frame index; indexes past the last frame wrap. </param>
    /// <returns> The braille frame at the index. </returns>
    public static string GetFrame(int index) => Frames[index % Frames.Length];

    /// <summary>
    ///   Clamps a frame duration in milliseconds to the supported range (40-200) so the
    ///   animation can neither flicker nor appear frozen.
    /// </summary>
    /// <param name="milliseconds"> The requested frame duration in milliseconds. </param>
    /// <returns> The duration clamped to 40-200 milliseconds. </returns>
    public static int ClampSpeed(int milliseconds) => Math.Clamp(milliseconds, 40, 200);

    /// <summary>
    ///   The static full-cell glyph shown instead of the animation when the host has disabled
    ///   client-area animations (reduced motion).
    /// </summary>
    /// <returns> The static full-cell braille glyph. </returns>
    public static string GetReducedMotionGlyph() => "⣿";
}
