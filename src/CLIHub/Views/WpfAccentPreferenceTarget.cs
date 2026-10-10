namespace CLIHub.Views;

using System.Windows;
using System.Windows.Media;

using CLIHub.Core.Models;

/// <summary>
///   Applies the accent color by overriding the application-level accent brushes.
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

        application.Resources["AccentBrush"] = CreateBrush(AccentPalette.Opaque(accentHex));
        application.Resources["AccentSelectionBrush"] = CreateBrush(AccentPalette.SelectionTint(accentHex));
    }

    private static SolidColorBrush CreateBrush(string argbHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(argbHex));
        brush.Freeze();
        return brush;
    }
}
