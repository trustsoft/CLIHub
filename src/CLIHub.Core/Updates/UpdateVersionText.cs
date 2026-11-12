namespace CLIHub.Core.Updates;

/// <summary>
///   Shared formatting for update version display text.
/// </summary>
internal static class UpdateVersionText
{
    /// <summary>
    ///   Strips build metadata (for example, the "+commit" suffix)
    ///   so the version displays as a plain semantic version.
    /// </summary>
    /// <param name="version"> The version string to strip. </param>
    /// <returns> The version without build metadata. </returns>
    internal static string StripBuildMetadata(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
