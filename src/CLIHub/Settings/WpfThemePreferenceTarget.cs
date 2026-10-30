namespace CLIHub.Settings;

using System.Windows;

using CLIHub.Core.Models;
using CLIHub.Core.Windows;
using CLIHub.Themes;

/// <summary>
///   Applies the theme palette by swapping the tracked palette overlay of the application resources.
/// </summary>
public sealed class WpfThemePreferenceTarget : IThemePreferenceTarget
{
    private readonly PaletteSwitcher switcher;

    /// <summary>
    ///   Creates the target with the palette switcher shared with startup.
    /// </summary>
    /// <param name="switcher"> The switcher owning the palette overlay. </param>
    public WpfThemePreferenceTarget(PaletteSwitcher switcher) => this.switcher = switcher;

    /// <inheritdoc />
    public bool SetTheme(AppTheme theme)
    {
        if (Application.Current is not { } application)
        {
            return false;
        }

        return this.switcher.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight());
    }
}
