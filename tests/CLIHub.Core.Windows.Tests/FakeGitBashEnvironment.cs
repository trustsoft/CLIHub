namespace CLIHub.Core.Windows.Tests;

using CLIHub.Core.Windows;

/// <summary>
///   In-memory <see cref="IGitBashEnvironment" /> for tests. Paths are normalized with
///   <see cref="Path.GetFullPath(string)" /> and compared case-insensitively, matching Windows.
/// </summary>
public sealed class FakeGitBashEnvironment : IGitBashEnvironment
{
    private readonly HashSet<string> existingFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///   The Git for Windows registry install root, or <c> null </c> when the registry misses.
    /// </summary>
    public string? InstallPath { get; set; }

    /// <summary>
    ///   When true, the registry read throws an <see cref="IOException" />.
    /// </summary>
    public bool ThrowOnRegistryRead { get; set; }

    /// <summary>
    ///   The well-known install roots, in lookup order.
    /// </summary>
    public List<string> WellKnownInstallRoots { get; } = new();

    /// <summary>
    ///   The PATH entries, in lookup order.
    /// </summary>
    public List<string> PathDirectories { get; } = new();

    /// <summary>
    ///   The Windows directory reported to the locator.
    /// </summary>
    public string WindowsDirectory { get; set; } = @"C:\Windows";

    /// <summary>
    ///   Registers a file as existing.
    /// </summary>
    /// <param name="path"> The file path. </param>
    public void AddExistingFile(string path) => this.existingFiles.Add(Normalize(path));

    /// <inheritdoc />
    public string? ReadGitForWindowsInstallPath() =>
        this.ThrowOnRegistryRead ? throw new IOException("registry unavailable") : this.InstallPath;

    /// <inheritdoc />
    public IReadOnlyList<string> GetWellKnownInstallRoots() => this.WellKnownInstallRoots;

    /// <inheritdoc />
    public IReadOnlyList<string> GetPathDirectories() => this.PathDirectories;

    /// <inheritdoc />
    public string GetWindowsDirectory() => this.WindowsDirectory;

    /// <inheritdoc />
    public bool FileExists(string path) => this.existingFiles.Contains(Normalize(path));

    private static string Normalize(string path) => Path.GetFullPath(path);
}
