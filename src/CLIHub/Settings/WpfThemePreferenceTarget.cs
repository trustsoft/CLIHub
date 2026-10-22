namespace CLIHub.Settings;

using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Windows;
using CLIHub.Themes;

/// <summary>
///   Applies the theme palette by swapping the tracked palette overlay of the application resources.
/// </summary>
public sealed class WpfThemePreferenceTarget : IThemePreferenceTarget
{
    private readonly PaletteSwitcher switcher;
    private readonly ILogger<WpfThemePreferenceTarget> logger;

    /// <summary>
    ///   Creates the target with the palette switcher shared with startup and the logger used for
    ///   the applied or failed palette switch.
    /// </summary>
    /// <param name="switcher"> The switcher owning the palette overlay. </param>
    /// <param name="logger"> Logger. </param>
    public WpfThemePreferenceTarget(PaletteSwitcher switcher, ILogger<WpfThemePreferenceTarget> logger)
    {
        this.switcher = switcher;
        this.logger = logger;
    }

    /// <inheritdoc />
    public void SetTheme(AppTheme theme)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        if (this.switcher.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight()))
        {
            this.logger.LogInformation("Theme set to {Theme}", theme);
        }
        else
        {
            this.logger.LogWarning(
                "Could not apply theme {Theme}; the previous palette was kept",
                theme);
        }
    }
}
