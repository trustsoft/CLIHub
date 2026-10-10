namespace CLIHub.Core.Updates;

/// <summary>
///   Classifies an available update version as stable or pre-release.
/// </summary>
public static class VersionClassification
{
    /// <summary>
    ///   Returns true when the semantic version carries a pre-release identifier
    ///   (for example <c>1.2.0-beta.1</c>).
    /// </summary>
    /// <param name="version"> The version text, possibly with build metadata. </param>
    /// <returns> True when the version is a pre-release. </returns>
    public static bool IsPrerelease(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var plus = version.IndexOf('+', StringComparison.Ordinal);
        var core = plus >= 0 ? version[..plus] : version;
        return core.Contains('-');
    }
}
