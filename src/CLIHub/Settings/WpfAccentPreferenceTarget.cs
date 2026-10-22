namespace CLIHub.Settings;

using System.Windows;

/// <summary>
///   Applies the accent color by writing the application-scoped accent brushes through
///   <see cref="AccentApplier" />.
/// </summary>
public sealed class WpfAccentPreferenceTarget : IAccentPreferenceTarget
{
    /// <inheritdoc />
    public void SetAccent(string accentHex)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        AccentApplier.Apply(application.Resources, accentHex);
    }
}
