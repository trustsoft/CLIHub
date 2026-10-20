namespace CLIHub.Core.Windows.Tests;

using CLIHub.Core.Windows;

/// <summary>
///   In-memory <see cref="IWindowsRuntimeEnvironment" /> for tests. Paths are normalized with
///   <see cref="Path.GetFullPath(string)" /> and compared case-insensitively, matching Windows.
/// </summary>
public sealed class FakeWindowsRuntimeEnvironment : IWindowsRuntimeEnvironment
{
    private readonly HashSet<string> existingFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///   The Git for Windows registry install root, or <c> null </c> when the registry misses.
    /// </summary>
    public string? InstallPath { get; set; }

    /// <summary>
    ///   The PowerShell Core registry install directory (the directory containing pwsh.exe), or
    ///   <c> null </c> when the registry misses.
    /// </summary>
    public string? PowerShellInstallPath { get; set; }

    /// <summary>
    ///   When true, a registry read throws an <see cref="IOException" />.
    /// </summary>
    public bool ThrowOnRegistryRead { get; set; }

    /// <summary>
    ///   The well-known Git for Windows install roots, in lookup order.
    /// </summary>
    public List<string> WellKnownInstallRoots { get; } = new();

    /// <summary>
    ///   The well-known PowerShell 7 executable path, or <c> null </c> when the well-known root misses.
    /// </summary>
    public string? PowerShellWellKnownExecutablePath { get; set; }

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
    public string? ReadPowerShellCoreInstallPath() =>
        this.ThrowOnRegistryRead ? throw new IOException("registry unavailable") : this.PowerShellInstallPath;

    /// <inheritdoc />
    public IReadOnlyList<string> GetGitWellKnownInstallRoots() => this.WellKnownInstallRoots;

    /// <inheritdoc />
    public string? GetPowerShellWellKnownExecutablePath() => this.PowerShellWellKnownExecutablePath;

    /// <inheritdoc />
    public IReadOnlyList<string> GetPathDirectories() => this.PathDirectories;

    /// <inheritdoc />
    public string GetWindowsDirectory() => this.WindowsDirectory;

    /// <inheritdoc />
    public bool FileExists(string path) => this.existingFiles.Contains(Normalize(path));

    private static string Normalize(string path) => Path.GetFullPath(path);
}
