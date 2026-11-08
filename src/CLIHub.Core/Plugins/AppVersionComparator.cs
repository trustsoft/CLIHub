namespace CLIHub.Core.Plugins;

/// <summary>
///   Compares current application version with minimum required plugin version using numeric major.minor.patch
///   comparison.
/// </summary>
public static class AppVersionComparator
{
    /// <summary>
    ///   Checks whether the current application version satisfies the minimum required version.
    /// </summary>
    /// <param name="currentVersion"> Current CLIHub version (e.g., "1.2.3"). </param>
    /// <param name="minAppVersion"> Minimum required version (e.g., "1.1.0"). </param>
    /// <returns> True if current version satisfies the requirement, false otherwise. </returns>
    public static bool IsSatisfied(string currentVersion, string minAppVersion)
    {
        var current = ParseVersion(currentVersion);
        var required = ParseVersion(minAppVersion);

        if (current.Major != required.Major)
        {
            return current.Major > required.Major;
        }

        if (current.Minor != required.Minor)
        {
            return current.Minor > required.Minor;
        }

        return current.Patch >= required.Patch;
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        return (major, minor, patch);
    }
}
