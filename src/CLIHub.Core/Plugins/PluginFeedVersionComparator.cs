namespace CLIHub.Core.Plugins;

/// <summary>
///   Plugin update status from version comparison.
/// </summary>
public enum PluginUpdateStatus
{
    /// <summary>
    ///   Plugin is not installed.
    /// </summary>
    Available,

    /// <summary>
    ///   Installed version matches feed version (up to date).
    /// </summary>
    Current,

    /// <summary>
    ///   Feed version is newer than installed version (upgrade available).
    /// </summary>
    UpdateAvailable,

    /// <summary>
    ///   Feed version differs from installed but is not provably newer (downgrade or non-comparable).
    /// </summary>
    DifferentVersionAvailable
}

/// <summary>
///   Compares installed plugin version with feed version using numeric comparison with fallback to string equality.
/// </summary>
public static class PluginFeedVersionComparator
{
    /// <summary>
    ///   Compares installed and feed versions to determine update status.
    /// </summary>
    /// <param name="installedVersion"> Currently installed version, or null if not installed. </param>
    /// <param name="feedVersion"> Version available in the feed. </param>
    /// <returns> Update status. </returns>
    public static PluginUpdateStatus Compare(string? installedVersion, string feedVersion)
    {
        if (installedVersion is null)
        {
            return PluginUpdateStatus.Available;
        }

        // Attempt numeric major.minor.patch comparison
        var installedParsed = TryParseVersion(installedVersion);
        var feedParsed = TryParseVersion(feedVersion);

        if (installedParsed.HasValue && feedParsed.HasValue)
        {
            var cmp = CompareVersions(installedParsed.Value, feedParsed.Value);
            if (cmp > 0)
            {
                return PluginUpdateStatus.DifferentVersionAvailable; // Installed > feed (downgrade)
            }
            else if (cmp == 0)
            {
                return PluginUpdateStatus.Current;
            }
            else
            {
                return PluginUpdateStatus.UpdateAvailable; // Feed > installed (upgrade)
            }
        }

        // Fall back to string equality for non-numeric versions
        return string.Equals(installedVersion, feedVersion, StringComparison.Ordinal)
            ? PluginUpdateStatus.Current
            : PluginUpdateStatus.DifferentVersionAvailable;
    }

    internal static (int Major, int Minor, int Patch)? TryParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length < 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return null;
        }

        return (major, minor, patch);
    }

    internal static int CompareVersions((int Major, int Minor, int Patch) a, (int Major, int Minor, int Patch) b)
    {
        if (a.Major != b.Major)
        {
            return a.Major.CompareTo(b.Major);
        }

        if (a.Minor != b.Minor)
        {
            return a.Minor.CompareTo(b.Minor);
        }

        return a.Patch.CompareTo(b.Patch);
    }
}
