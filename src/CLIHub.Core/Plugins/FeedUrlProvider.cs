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
    public const string DefaultFeedUrl = "https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json";

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

    /// <summary>
    ///   Whether the URL is the default official feed — the only feed whose index is signature
    ///   verified. Compared ordinally, because the resolved URL is used verbatim for the request.
    /// </summary>
    /// <param name="url"> The resolved feed URL. </param>
    /// <returns> True when the URL is exactly <see cref="DefaultFeedUrl" />. </returns>
    public static bool IsDefaultFeedUrl(string? url) =>
        string.Equals(url, DefaultFeedUrl, StringComparison.Ordinal);
}
