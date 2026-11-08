namespace CLIHub.ViewModels;

using CLIHub.Core.Plugins;

/// <summary>
///   A plugin entry from the remote feed, displayed in the Browse Plugins dialog.
/// </summary>
public sealed class FeedPluginItem : ObservableObject
{
    /// <summary>
    ///   Creates a feed plugin item.
    /// </summary>
    /// <param name="id"> Plugin ID. </param>
    /// <param name="name"> Plugin name. </param>
    /// <param name="description"> Plugin description. </param>
    /// <param name="version"> Feed version. </param>
    /// <param name="publisher"> Publisher. </param>
    /// <param name="homepage"> Homepage URL. </param>
    /// <param name="packageUrl"> Package download URL. </param>
    /// <param name="sha256"> Package checksum. </param>
    /// <param name="minAppVersion"> Minimum required CLIHub version. </param>
    /// <param name="releaseNotesUrl"> Optional release notes URL. </param>
    /// <param name="status"> Plugin status (Available/Current/UpdateAvailable/DifferentVersionAvailable). </param>
    /// <param name="installedVersion"> Currently installed version, or null. </param>
    /// <param name="canInstall"> Whether the plugin can be installed (minAppVersion satisfied). </param>
    /// <param name="isDowngrade"> Whether installing would be a downgrade. </param>
    public FeedPluginItem(
        string id,
        string name,
        string description,
        string version,
        string publisher,
        string homepage,
        string packageUrl,
        string sha256,
        string minAppVersion,
        string? releaseNotesUrl,
        PluginUpdateStatus status,
        string? installedVersion,
        bool canInstall,
        bool isDowngrade)
    {
        this.Id = id;
        this.Name = name;
        this.Description = description;
        this.Version = version;
        this.Publisher = publisher;
        this.Homepage = homepage;
        this.PackageUrl = packageUrl;
        this.Sha256 = sha256;
        this.MinAppVersion = minAppVersion;
        this.ReleaseNotesUrl = releaseNotesUrl;
        this.Status = status;
        this.InstalledVersion = installedVersion;
        this.CanInstall = canInstall;
        this.IsDowngrade = isDowngrade;
    }

    /// <summary>
    ///   Plugin ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///   Plugin name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Plugin description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///   Version available in the feed.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///   Publisher.
    /// </summary>
    public string Publisher { get; }

    /// <summary>
    ///   Homepage URL.
    /// </summary>
    public string Homepage { get; }

    /// <summary>
    ///   Package download URL (HTTPS).
    /// </summary>
    public string PackageUrl { get; }

    /// <summary>
    ///   SHA-256 checksum of the package.
    /// </summary>
    public string Sha256 { get; }

    /// <summary>
    ///   Minimum required CLIHub version.
    /// </summary>
    public string MinAppVersion { get; }

    /// <summary>
    ///   Optional release notes URL.
    /// </summary>
    public string? ReleaseNotesUrl { get; }

    /// <summary>
    ///   Plugin status: Available, Current, UpdateAvailable, or DifferentVersionAvailable.
    /// </summary>
    public PluginUpdateStatus Status { get; }

    /// <summary>
    ///   Currently installed version, or null if not installed.
    /// </summary>
    public string? InstalledVersion { get; }

    /// <summary>
    ///   Whether the plugin can be installed (minAppVersion check passed).
    /// </summary>
    public bool CanInstall { get; }

    /// <summary>
    ///   Whether installing this version would be a downgrade.
    /// </summary>
    public bool IsDowngrade { get; }

    /// <summary>
    ///   Status display text.
    /// </summary>
    public string StatusText => this.Status switch
    {
        PluginUpdateStatus.Available => "Available",
        PluginUpdateStatus.Current => "Installed",
        PluginUpdateStatus.UpdateAvailable => "Update available",
        PluginUpdateStatus.DifferentVersionAvailable => "Different version",
        _ => "Unknown"
    };

    /// <summary>
    ///   Version display text: shows installed and feed versions for updates/downgrades.
    /// </summary>
    public string VersionText => this.Status switch
    {
        PluginUpdateStatus.UpdateAvailable when this.InstalledVersion is { } installed => $"{installed} → {this.Version}",
        PluginUpdateStatus.DifferentVersionAvailable when this.InstalledVersion is { } installed => $"{installed} / {this.Version}",
        _ => this.Version
    };

    /// <summary>
    ///   Action button text: "Install" for Available/DifferentVersionAvailable, "Update" for UpdateAvailable, hidden
    ///   for Current.
    /// </summary>
    public string ActionText => this.Status switch
                                {
                                    PluginUpdateStatus.UpdateAvailable => "Update",
                                    PluginUpdateStatus.Available or PluginUpdateStatus.DifferentVersionAvailable => "Install",
                                    _ => string.Empty
                                };

    /// <summary>
    ///   Whether the action button should be visible.
    /// </summary>
    public bool ShowActionButton => this.Status != PluginUpdateStatus.Current;

    /// <summary>
    ///   Warning message for downgrades.
    /// </summary>
    public string? WarningMessage => this.IsDowngrade ? "This is a different version (may be older)" : null;

    /// <summary>
    ///   Whether the homepage URL is present and should be displayed.
    /// </summary>
    public bool HasHomepage => !string.IsNullOrWhiteSpace(this.Homepage);

    /// <summary>
    ///   Whether a warning message should be shown.
    /// </summary>
    public bool HasWarning => this.WarningMessage is not null;

    /// <summary>
    ///   Reason why installation is blocked, or null if it can be installed.
    /// </summary>
    public string? BlockedReason => !this.CanInstall
        ? $"Requires CLIHub {this.MinAppVersion} or newer. Update CLIHub to install this plugin."
        : null;
}
