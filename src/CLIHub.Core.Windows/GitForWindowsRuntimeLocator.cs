namespace CLIHub.Core.Windows;

using System.IO;
using System.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Thin seam over the operating system, so Git for Windows resolution is testable without touching
///   the real registry or process-wide PATH.
/// </summary>
internal interface IGitBashEnvironment
{
    /// <summary>
    ///   Reads the Git for Windows <c> InstallPath </c> registry value. Returns <c> null </c> on a miss or
    ///   on any registry fault; per-user layout is best-effort.
    /// </summary>
    string? ReadGitForWindowsInstallPath();

    /// <summary>
    ///   Returns the well-known Git for Windows install roots (Program Files, Program Files (x86),
    ///   and the per-user Programs folder).
    /// </summary>
    IReadOnlyList<string> GetWellKnownInstallRoots();

    /// <summary>
    ///   Returns the process PATH entries with empty entries removed.
    /// </summary>
    IReadOnlyList<string> GetPathDirectories();

    /// <summary>
    ///   Returns the Windows directory without a trailing separator.
    /// </summary>
    string GetWindowsDirectory();

    /// <summary>
    ///   Returns whether a file exists at the specified path.
    /// </summary>
    bool FileExists(string path);
}

/// <summary>
///   Default <see cref="IGitBashEnvironment" /> backed by the registry, the environment, and the file system.
/// </summary>
internal sealed class WindowsGitBashEnvironment : IGitBashEnvironment
{
    private const string GitForWindowsKeyPath = @"SOFTWARE\GitForWindows";
    private const string InstallPathValueName = "InstallPath";

    /// <inheritdoc />
    public string? ReadGitForWindowsInstallPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        // A machine-wide install writes the 64-bit view; a 32-bit install writes the 32-bit view.
        // The per-user layout is best-effort: it is read after the machine-wide keys.
        return this.ReadInstallPath(RegistryHive.LocalMachine, RegistryView.Registry64)
            ?? this.ReadInstallPath(RegistryHive.LocalMachine, RegistryView.Registry32)
            ?? this.ReadInstallPath(RegistryHive.CurrentUser, RegistryView.Default)
            ?? this.ReadInstallPath(RegistryHive.CurrentUser, RegistryView.Registry32);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetWellKnownInstallRoots()
    {
        var roots = new List<string>(3);
        AddIfPresent(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddIfPresent(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            roots.Add(Path.Combine(localAppData, "Programs", "Git"));
        }

        return roots;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPathDirectories()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return [];
        }

        return path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => directory.Trim())
            .Where(directory => directory.Length > 0)
            .ToArray();
    }

    /// <inheritdoc />
    public string GetWindowsDirectory()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(windowsDirectory))
        {
            windowsDirectory = Environment.GetEnvironmentVariable("SystemRoot") ?? string.Empty;
        }

        return windowsDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    private static void AddIfPresent(List<string> roots, string? root)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            roots.Add(root);
        }
    }

    private string? ReadInstallPath(RegistryHive hive, RegistryView view)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(GitForWindowsKeyPath, writable: false);
            var value = key?.GetValue(InstallPathValueName) as string;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (IOException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>
///   Resolves the Git Bash runtime to a Git for Windows <c> bash.exe </c>, never to an arbitrary
///   <c> bash.exe </c> resolved from the operating system path.
/// </summary>
public sealed class GitForWindowsRuntimeLocator : IRuntimeExecutableLocator
{
    private const string BashExecutableName = "bash.exe";

    private readonly IGitBashEnvironment environment;
    private readonly ILogger<GitForWindowsRuntimeLocator> logger;

    /// <summary>
    ///   Creates the locator over an operating-system seam.
    /// </summary>
    /// <param name="environment"> The operating-system seam. </param>
    /// <param name="logger"> Logger for resolution diagnostics. </param>
    internal GitForWindowsRuntimeLocator(IGitBashEnvironment environment,
                                         ILogger<GitForWindowsRuntimeLocator> logger)
    {
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string? Locate(RuntimeKind kind)
    {
        if (kind != RuntimeKind.GitBash)
        {
            return null;
        }

        var registryInstallPath = this.ReadRegistryInstallPath();
        if (registryInstallPath is not null)
        {
            var registryCandidate = this.TryCandidate(registryInstallPath);
            if (registryCandidate is not null)
            {
                this.logger.LogInformation(
                    "Resolved Git Bash to {ExecutablePath} from the Git for Windows registry InstallPath",
                    registryCandidate);
                return registryCandidate;
            }
        }

        foreach (var root in this.environment.GetWellKnownInstallRoots())
        {
            var wellKnownCandidate = this.TryCandidate(root);
            if (wellKnownCandidate is not null)
            {
                this.logger.LogInformation(
                    "Resolved Git Bash to {ExecutablePath} from the well-known install location {InstallRoot}",
                    wellKnownCandidate, root);
                return wellKnownCandidate;
            }
        }

        var windowsDirectory = this.environment.GetWindowsDirectory();
        foreach (var directory in this.environment.GetPathDirectories())
        {
            if (IsUnderDirectory(directory, windowsDirectory))
            {
                this.logger.LogDebug(
                    "Rejected PATH entry {Directory} because it is under the Windows directory {WindowsDirectory}",
                    directory, windowsDirectory);
                continue;
            }

            string candidate;
            try
            {
                candidate = Path.Combine(directory, BashExecutableName);
            }
            catch (ArgumentException)
            {
                this.logger.LogDebug("Rejected PATH entry {Directory} because it is not a valid path", directory);
                continue;
            }

            if (!this.environment.FileExists(candidate))
            {
                this.logger.LogDebug("Rejected PATH candidate {Candidate} because the file does not exist", candidate);
                continue;
            }

            var resolved = ToAbsolutePath(candidate);
            if (resolved is not null)
            {
                this.logger.LogInformation(
                    "Resolved Git Bash to {ExecutablePath} from the PATH entry {Directory}", resolved, directory);
                return resolved;
            }
        }

        this.logger.LogWarning(
            "Git Bash is configured but no Git for Windows bash.exe was found. Lookup order: " +
            @"HKLM/HKCU SOFTWARE\GitForWindows InstallPath, well-known install locations " +
            @"(ProgramFiles, ProgramFilesX86, LocalAppData\Programs\Git), and PATH entries outside the Windows directory");
        return null;
    }

    private string? ReadRegistryInstallPath()
    {
        try
        {
            var installPath = this.environment.ReadGitForWindowsInstallPath();
            if (string.IsNullOrWhiteSpace(installPath))
            {
                this.logger.LogDebug("The Git for Windows registry InstallPath is absent");
                return null;
            }

            return installPath;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            // Registry faults are misses, never failures.
            this.logger.LogDebug("The Git for Windows registry InstallPath could not be read: {Reason}", ex.Message);
            return null;
        }
    }

    private string? TryCandidate(string installRoot)
    {
        if (string.IsNullOrWhiteSpace(installRoot))
        {
            return null;
        }

        string candidate;
        try
        {
            candidate = Path.Combine(installRoot, "bin", BashExecutableName);
        }
        catch (ArgumentException)
        {
            this.logger.LogDebug("Rejected install root {InstallRoot} because it is not a valid path", installRoot);
            return null;
        }

        if (!this.environment.FileExists(candidate))
        {
            this.logger.LogDebug("Rejected install root {InstallRoot} because {Candidate} does not exist", installRoot, candidate);
            return null;
        }

        return ToAbsolutePath(candidate);
    }

    private static string? ToAbsolutePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        string fullPath;
        string fullDirectory;
        try
        {
            fullPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fullDirectory = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (fullPath.Equals(fullDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fullPath.StartsWith(
            fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
