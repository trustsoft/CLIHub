namespace CLIHub.Settings;

/// <summary>
///   Applies the application accent color to the running UI.
/// </summary>
public interface IAccentPreferenceTarget
{
    /// <summary>
    ///   Applies the accent color.
    /// </summary>
    /// <param name="accentHex"> The accent as <c>#RRGGBB</c>. </param>
    void SetAccent(string accentHex);
}
