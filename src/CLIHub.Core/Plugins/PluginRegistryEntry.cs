namespace CLIHub.Core.Plugins;

/// <summary>
///   The well-known origin values recorded in the registry document.
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
    ///   The plugin's origin is not known: it predates the registry or its content did not
    ///   match any content CLIHub wrote.
    /// </summary>
    public const string Unknown = "unknown";
}

/// <summary>
///   The per-plugin lifecycle state recorded in <c> registry.json </c>: where the plugin came
///   from, the content hashes CLIHub last wrote, the installed version, the enabled flag, and
///   the install timestamp. Lifecycle state is operational plugin metadata, not user
///   preference, so it lives in the registry document rather than the single configuration
///   document.
/// </summary>
public sealed record PluginRegistryEntry
{
    /// <summary>
    ///   Creates an entry with the specified origin and enabled state.
    /// </summary>
    /// <param name="origin"> One of the <see cref="PluginOrigin" /> values. </param>
    /// <param name="enabled">
    ///   Whether the plugin is enabled; defaults to true so a registry entry whose JSON omits
    ///   <c> enabled </c> fails safe (the plugin loads).
    /// </param>
    public PluginRegistryEntry(string origin = PluginOrigin.Unknown, bool enabled = true)
    {
        this.Origin = origin;
        this.Enabled = enabled;
    }

    /// <summary>
    ///   The plugin origin: one of the <see cref="PluginOrigin" /> values. Defaults to
    ///   <see cref="PluginOrigin.Unknown" /> so a partially populated registry entry degrades
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
    ///   Whether the plugin is enabled; a disabled plugin stays on disk but is not loaded by
    ///   the catalog and is not launchable. Absent JSON field defaults to true (fail-safe).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///   The UTC time the plugin was installed by the installer, or null when the plugin was
    ///   not installed through an install flow: built-ins ship with the application and adopted
    ///   or legacy entries have no knowable install time, so their value stays null rather than
    ///   being fabricated.
    /// </summary>
    public DateTimeOffset? InstalledAt { get; set; }
}
