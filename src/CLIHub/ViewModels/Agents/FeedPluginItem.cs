namespace CLIHub.ViewModels;

using CLIHub.Core.Plugins;

/// <summary>
///   A plugin entry from the remote feed, displayed in the Browse Plugins dialog. Items are
///   immutable except for the post-install status refresh, which updates the installed status
///   in place after a successful install from the feed.
/// </summary>
public sealed class FeedPluginItem : ObservableObject
{
    private PluginUpdateStatus status;
    private string? installedVersion;
    private bool isDowngrade;

    /// <summary>
    ///   Creates a feed plugin item.
    /// </summary>
    /// <param name="id"> Plugin ID. </param>
    /// <param name="name"> Plugin name. </param>
    /// <param name="description"> Plugin description. </param>
    /// <param name="version"> Feed version. </param>
    /// <param name="publisher"> Publisher. </param>
    /// <param name="homepage"> Homepage URL. </param>
    /// <param name="packageUrl"> Package download URL, or null for a descriptor-only entry. </param>
    /// <param name="sha256"> Package checksum, or null for a descriptor-only entry. </param>
    /// <param name="minAppVersion"> Minimum required CLIHub version (v1 entries), or null for v2. </param>
    /// <param name="compatibility"> Compatibility range the current app version must satisfy. </param>
    /// <param name="descriptorJson"> Inline descriptor JSON (v2 entries), or null. </param>
    /// <param name="logoUrl"> Optional logo image URL (v2 entries), or null. </param>
    /// <param name="releaseNotesUrl"> Optional release notes URL. </param>
    /// <param name="status"> Plugin status (Available/Current/UpdateAvailable/DifferentVersionAvailable). </param>
    /// <param name="installedVersion"> Currently installed version, or null. </param>
    /// <param name="canInstall"> Whether the plugin can be installed (compatibility satisfied). </param>
    /// <param name="isDowngrade"> Whether installing would be a downgrade. </param>
    public FeedPluginItem(
        string id,
        string name,
        string description,
        string version,
        string publisher,
        string homepage,
        string? packageUrl,
        string? sha256,
        string? minAppVersion,
        string compatibility,
        string? descriptorJson,
        string? logoUrl,
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
        this.Compatibility = compatibility;
        this.DescriptorJson = descriptorJson;
        this.LogoUrl = logoUrl;
        this.ReleaseNotesUrl = releaseNotesUrl;
        this.status = status;
        this.installedVersion = installedVersion;
        this.CanInstall = canInstall;
        this.isDowngrade = isDowngrade;
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
    ///   Package download URL (HTTPS), or null for a descriptor-only entry.
    /// </summary>
    public string? PackageUrl { get; }

    /// <summary>
    ///   SHA-256 checksum of the package, or null for a descriptor-only entry.
    /// </summary>
    public string? Sha256 { get; }

    /// <summary>
    ///   Minimum required CLIHub version (v1 entries), or null for v2 entries.
    /// </summary>
    public string? MinAppVersion { get; }

    /// <summary>
    ///   Compatibility range the current app version must satisfy: <c> "&gt;=" + minAppVersion </c>
    ///   for v1 entries, the raw range text for v2 entries.
    /// </summary>
    public string Compatibility { get; }

    /// <summary>
    ///   Verbatim inline descriptor JSON for a v2 descriptor-only entry, or null.
    /// </summary>
    public string? DescriptorJson { get; }

    /// <summary>
    ///   Optional logo image URL for a v2 entry, or null.
    /// </summary>
    public string? LogoUrl { get; }

    /// <summary>
    ///   Whether this item installs from the inline descriptor instead of a downloaded package:
    ///   true when the entry carries a descriptor and no package URL.
    /// </summary>
    public bool UsesDescriptorInstall => this.DescriptorJson is not null && this.PackageUrl is null;

    /// <summary>
    ///   Optional release notes URL.
    /// </summary>
    public string? ReleaseNotesUrl { get; }

    /// <summary>
    ///   Plugin status: Available, Current, UpdateAvailable, or DifferentVersionAvailable. Mutable
    ///   only for the post-install status refresh.
    /// </summary>
    public PluginUpdateStatus Status
    {
        get => this.status;
        private set
        {
            if (this.SetProperty(ref this.status, value))
            {
                this.OnPropertyChanged(nameof(this.StatusText));
                this.OnPropertyChanged(nameof(this.VersionText));
                this.OnPropertyChanged(nameof(this.ActionText));
                this.OnPropertyChanged(nameof(this.ShowActionButton));
            }
        }
    }

    /// <summary>
    ///   Currently installed version, or null if not installed. Mutable only for the post-install
    ///   status refresh.
    /// </summary>
    public string? InstalledVersion
    {
        get => this.installedVersion;
        private set
        {
            if (this.SetProperty(ref this.installedVersion, value))
            {
                this.OnPropertyChanged(nameof(this.VersionText));
            }
        }
    }

    /// <summary>
    ///   Whether the plugin can be installed (compatibility range satisfied).
    /// </summary>
    public bool CanInstall { get; }

    /// <summary>
    ///   Whether installing this version would be a downgrade. Mutable only for the post-install
    ///   status refresh.
    /// </summary>
    public bool IsDowngrade
    {
        get => this.isDowngrade;
        private set
        {
            if (this.SetProperty(ref this.isDowngrade, value))
            {
                this.OnPropertyChanged(nameof(this.WarningMessage));
                this.OnPropertyChanged(nameof(this.HasWarning));
            }
        }
    }

    /// <summary>
    ///   Marks the item as installed from the feed, refreshing the installed version, status, and
    ///   downgrade warning in place so the card re-renders without rebuilding the collection. The
    ///   status is recomputed through <see cref="PluginFeedVersionComparator.Compare" /> so a
    ///   descriptor version that drifted from the feed entry is displayed truthfully; a null
    ///   descriptor version maps to <see cref="PluginUpdateStatus.UpdateAvailable" />, matching
    ///   what the next feed load would compute for an installed entry without a recorded version.
    /// </summary>
    /// <param name="installedVersion"> Version declared by the installed package's descriptor, or null. </param>
    public void MarkInstalled(string? installedVersion)
    {
        this.InstalledVersion = installedVersion;
        this.Status = installedVersion is null
            ? PluginUpdateStatus.UpdateAvailable
            : PluginFeedVersionComparator.Compare(installedVersion, this.Version);
        this.IsDowngrade = PluginFeedVersionComparator.IsDowngrade(installedVersion, this.Version);
    }

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
    ///   Reason why installation is blocked, or null if it can be installed. v1 entries keep the
    ///   verbatim minAppVersion message; v2 entries show the raw compatibility range.
    /// </summary>
    public string? BlockedReason => !this.CanInstall
        ? this.MinAppVersion is not null
            ? $"Requires CLIHub {this.MinAppVersion} or newer. Update CLIHub to install this plugin."
            : $"Requires CLIHub {this.Compatibility}. Update CLIHub to install this plugin."
        : null;
}
