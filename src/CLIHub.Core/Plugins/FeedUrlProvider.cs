namespace CLIHub.Core.Plugins;

using CLIHub.Core.Configuration;

/// <summary>
///   Resolves the plugin feed URL from configuration.
/// </summary>
public static class FeedUrlProvider
{
    /// <summary>
    ///   Default official feed URL.
    /// </summary>
    public const string DefaultFeedUrl = "https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json";

    /// <summary>
    ///   Resolves the effective feed URL from preferences.
    /// </summary>
    /// <param name="preferencesStore"> Preferences store. </param>
    /// <returns> The feed URL to use, or null if the feed is disabled. </returns>
    public static string? GetFeedUrl(IPreferencesStore preferencesStore)
    {
        ArgumentNullException.ThrowIfNull(preferencesStore);

        var config = preferencesStore.Load();
        var pluginFeedUrl = config.PluginFeedUrl;

        // Absent or null: default official URL
        if (pluginFeedUrl is null)
        {
            return DefaultFeedUrl;
        }

        // Explicit empty string: disabled
        if (string.IsNullOrWhiteSpace(pluginFeedUrl))
        {
            return null;
        }

        // Non-empty: custom URL
        return pluginFeedUrl;
    }
}
