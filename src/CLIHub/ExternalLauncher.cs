namespace CLIHub;

using System.Diagnostics;

/// <summary>
///   Opens paths through the Windows shell.
/// </summary>
public sealed class ExternalLauncher : IExternalLauncher
{
    /// <inheritdoc />
    public void Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
