namespace CLIHub.Core.Infrastructure.FileSystem;

/// <summary>
///   Writes file contents atomically: the write lands in a temp file next to the target and
///   is renamed over the target, so an interrupted write cannot corrupt the previous content.
/// </summary>
internal static class AtomicFileWriter
{
    /// <summary>
    ///   Writes <paramref name="contents"/> to <paramref name="targetPath"/> atomically:
    ///   ensures the target directory exists, writes to a temp file, then renames it over
    ///   the target.
    /// </summary>
    /// <param name="targetPath"> The destination file path. </param>
    /// <param name="contents"> The contents to write. </param>
    internal static void Write(string targetPath, string contents)
    {
        // Atomic write: write to a temp file, then rename,
        // so an interrupted save cannot corrupt the previous state.
        var tempPath = targetPath + ".tmp";
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(tempPath, contents);
        File.Move(tempPath, targetPath, overwrite: true);
    }
}
