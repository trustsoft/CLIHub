namespace CLIHub.Core.Infrastructure.Windows;

using Microsoft.Win32;

/// <summary>
///   Reads the Windows personalization theme for the "System" theme mode.
/// </summary>
public static class SystemTheme
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    /// <summary>
    ///   Returns true when Windows apps are set to the light theme. Any failure yields false (dark).
    /// </summary>
    /// <returns> True when the light theme is active. </returns>
    public static bool IsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            return key?.GetValue(AppsUseLightThemeValue) is int value && value != 0;
        }
        catch
        {
            return false;
        }
    }
}
