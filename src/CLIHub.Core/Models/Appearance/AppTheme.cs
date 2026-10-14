namespace CLIHub.Core.Models;

/// <summary>
///   The application-wide visual theme.
/// </summary>
public enum AppTheme
{
    /// <summary>
    ///   The dark theme (default).
    /// </summary>
    Dark,

    /// <summary>
    ///   The light theme.
    /// </summary>
    Light,

    /// <summary>
    ///   Follow the Windows personalization setting.
    /// </summary>
    System
}

/// <summary>
///   Maps <see cref="AppTheme" /> to and from its stored token.
/// </summary>
public static class AppThemes
{
    /// <summary>
    ///   The token for the dark theme.
    /// </summary>
    public const string DarkToken = "dark";

    /// <summary>
    ///   The token for the light theme.
    /// </summary>
    public const string LightToken = "light";

    /// <summary>
    ///   The token for the system-synced theme.
    /// </summary>
    public const string SystemToken = "system";

    /// <summary>
    ///   Parses a stored token. Returns false for null, empty, or unknown values.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <param name="theme"> The parsed theme when recognized. </param>
    /// <returns> True when the token is recognized. </returns>
    public static bool TryParse(string? value, out AppTheme theme)
    {
        theme = AppTheme.Dark;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "dark":
                theme = AppTheme.Dark;
                return true;
            case "light":
                theme = AppTheme.Light;
                return true;
            case "system":
                theme = AppTheme.System;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///   Parses a stored token, falling back to <see cref="AppTheme.Dark" />.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <returns> The parsed or default theme. </returns>
    public static AppTheme Parse(string? value) => TryParse(value, out var theme) ? theme : AppTheme.Dark;

    /// <summary>
    ///   Returns the stored token for a theme.
    /// </summary>
    /// <param name="theme"> The theme. </param>
    /// <returns> The canonical token. </returns>
    public static string ToToken(AppTheme theme) => theme switch
    {
        AppTheme.Light => LightToken,
        AppTheme.System => SystemToken,
        _ => DarkToken
    };
}
