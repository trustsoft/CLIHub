namespace CLIHub.Core.Plugins;

/// <summary>
///   The well-known origin values recorded in <c> plugin-state.json </c>.
/// </summary>
public static class PluginOrigin
{
    /// <summary>
    ///   The plugin was written by this CLIHub build from its embedded built-in descriptors.
    /// </summary>
    public const string Builtin = "builtin";

    /// <summary>
    ///   The plugin was installed from a local <c> .clihub-plugin.zip </c> package.
    /// </summary>
    public const string Installed = "installed";

    /// <summary>
    ///   The plugin's origin is not known: it predates the state file or its content did not
    ///   match any content CLIHub wrote.
    /// </summary>
    public const string Unknown = "unknown";
}

/// <summary>
///   The per-plugin lifecycle state recorded in <c> plugin-state.json </c>: where the plugin
///   came from, the content hashes CLIHub last wrote, the installed version, and the disabled
///   flag. Lifecycle state is operational plugin metadata, not user preference, so it lives in
///   its own state file rather than the single configuration document.
/// </summary>
public sealed record PluginStateEntry
{
    /// <summary>
    ///   Creates an entry with the specified origin and no recorded hashes.
    /// </summary>
    /// <param name="origin"> One of the <see cref="PluginOrigin" /> values. </param>
    public PluginStateEntry(string origin = PluginOrigin.Unknown)
    {
        this.Origin = origin;
    }

    /// <summary>
    ///   The plugin origin: one of the <see cref="PluginOrigin" /> values. Defaults to
    ///   <see cref="PluginOrigin.Unknown" /> so a partially populated state file degrades
    ///   gracefully.
    /// </summary>
    public string Origin { get; set; } = PluginOrigin.Unknown;

    /// <summary>
    ///   The hex-encoded SHA-256 hash of the <c> plugin.json </c> content CLIHub last wrote, or
    ///   null when unknown. Detects content drift and user modification; it is not a signature.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    ///   The hex-encoded SHA-256 hash of the <c> logo.png </c> content CLIHub last wrote, or
    ///   null when there is no logo or the hash is unknown.
    /// </summary>
    public string? LogoHash { get; set; }

    /// <summary>
    ///   The plugin's version from its descriptor <c> version </c> field, or null when the
    ///   descriptor omits it.
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    ///   Whether the plugin is disabled: disabled plugins stay on disk but are not loaded by
    ///   the catalog and are not launchable.
    /// </summary>
    public bool Disabled { get; set; }
}
