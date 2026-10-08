namespace CLIHub;

/// <summary>
///   Opens an external resource such as a folder using the operating system.
/// </summary>
public interface IExternalLauncher
{
    /// <summary>
    ///   Opens the specified path using the operating system's default handler.
    /// </summary>
    /// <param name="path"> The file or directory path to open. </param>
    void Open(string path);
}
