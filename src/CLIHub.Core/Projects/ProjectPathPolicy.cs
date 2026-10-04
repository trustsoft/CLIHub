namespace CLIHub.Core.Services;

/// <summary>
///   Normalizes and compares project paths using Windows path semantics.
/// </summary>
internal static class ProjectPathPolicy
{
    /// <summary>
    ///   Returns a normalized full path without trailing directory separators.
    /// </summary>
    /// <param name="path"> The path to normalize. </param>
    /// <returns> The normalized path. </returns>
    internal static string Normalize(string path) =>
        Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>
    ///   Compares two paths using normalized, case-insensitive Windows semantics.
    /// </summary>
    /// <param name="left"> The first path. </param>
    /// <param name="right"> The second path. </param>
    /// <returns> True when the paths identify the same directory. </returns>
    internal static bool AreEqual(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
}
