namespace CLIHub.Core.Windows;

using System.IO;
using System.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Thin seam over the operating system, so Windows runtime resolution is testable without touching
///   the real registry or process-wide PATH.
/// </summary>
internal interface IWindowsRuntimeEnvironment
{
    /// <summary>
    ///   Reads every non-empty Git for Windows <c> InstallPath </c> registry value across the four
    ///   registry views (HKLM 64-bit, HKLM 32-bit, HKCU default, HKCU 32-bit), in that order. A view
    ///   that faults contributes nothing, and values are trimmed. Returns an empty list when no view
    ///   yields a value; per-user layout is best-effort.
    /// </summary>
    IReadOnlyList<string> ReadGitForWindowsInstallPaths();

    /// <summary>
    ///   Reads the PowerShell Core <c> InstallPath </c> registry value, which names the directory that
    ///   contains <c> pwsh.exe </c>. Only the machine-wide hive is read, in the 64-bit view and then the
    ///   32-bit view: the PowerShell 7 MSI is machine-wide and per-user installs surface through PATH.
    ///   Returns <c> null </c> on a miss or on any registry fault.
    /// </summary>
    string? ReadPowerShellCoreInstallPath();

    /// <summary>
    ///   Returns the well-known Git for Windows install roots (Program Files, Program Files (x86),
    ///   and the per-user Programs folder).
    /// </summary>
    IReadOnlyList<string> GetGitWellKnownInstallRoots();

    /// <summary>
    ///   Returns the well-known PowerShell 7 executable path
    ///   (<c> ProgramFiles\PowerShell\7\pwsh.exe </c>), or <c> null </c> when no well-known root is set.
    /// </summary>
    string? GetPowerShellWellKnownExecutablePath();

    /// <summary>
    ///   Returns the process PATH entries with empty entries removed.
    /// </summary>
    /// <remarks>
    ///   Only the Git Bash lookup excludes PATH entries under <see cref="GetWindowsDirectory" />:
    ///   <c> pwsh.exe </c> has no WSL-launcher-shaped namesake in the Windows directory, and Microsoft
    ///   Store installs of PowerShell 7 surface as an app-execution alias in
    ///   <c> %LOCALAPPDATA%\Microsoft\WindowsApps </c> that the PowerShell 7 PATH scan must find.
    /// </remarks>
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
///   Default <see cref="IWindowsRuntimeEnvironment" /> backed by the registry, the environment, and the
///   file system.
/// </summary>
internal sealed class WindowsRuntimeEnvironment : IWindowsRuntimeEnvironment
{
    private const string GitForWindowsKeyPath = @"SOFTWARE\GitForWindows";
    private const string PowerShellCoreKeyPath = @"SOFTWARE\Microsoft\PowerShellCore";
    private const string InstallPathValueName = "InstallPath";

    private static readonly (RegistryHive Hive, RegistryView View)[] GitViews =
    [
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser, RegistryView.Default),
        (RegistryHive.CurrentUser, RegistryView.Registry32)
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> ReadGitForWindowsInstallPaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        // A machine-wide install writes the 64-bit view; a 32-bit install writes the 32-bit view.
        // The per-user layout is best-effort: it is read after the machine-wide keys.
        return CollectInstallPaths(
            GitViews,
            (hive, view) => this.ReadInstallPath(GitForWindowsKeyPath, hive, view));
    }

    /// <inheritdoc />
    public string? ReadPowerShellCoreInstallPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        // The PowerShell 7 MSI is machine-wide, so only HKLM is read: the 64-bit view first, then the
        // 32-bit view. There is no per-user key; per-user installs surface through PATH.
        return this.ReadInstallPath(PowerShellCoreKeyPath, RegistryHive.LocalMachine, RegistryView.Registry64)
            ?? this.ReadInstallPath(PowerShellCoreKeyPath, RegistryHive.LocalMachine, RegistryView.Registry32);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGitWellKnownInstallRoots()
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
    public string? GetPowerShellWellKnownExecutablePath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return string.IsNullOrWhiteSpace(programFiles)
            ? null
            : Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
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

    /// <summary>
    ///   Collects every non-empty, trimmed install path across the registry views in order, so view
    ///   order, non-empty filtering, and trimming stay testable without touching the registry.
    ///   Views are deliberately not de-duplicated: aliased views may yield the same value twice,
    ///   which is harmless (a stale duplicate fails twice; a valid one succeeds on the first hit).
    /// </summary>
    /// <param name="views"> The registry views to read, in preference order. </param>
    /// <param name="readInstallPath"> Reads one view's install path, or null on a miss or fault. </param>
    /// <returns> The non-empty trimmed values, in view order. </returns>
    internal static IReadOnlyList<string> CollectInstallPaths(
        IReadOnlyList<(RegistryHive Hive, RegistryView View)> views,
        Func<RegistryHive, RegistryView, string?> readInstallPath)
    {
        var installPaths = new List<string>(views.Count);
        foreach (var (hive, view) in views)
        {
            var value = readInstallPath(hive, view);
            if (!string.IsNullOrWhiteSpace(value))
            {
                installPaths.Add(value.Trim());
            }
        }

        return installPaths;
    }

    private string? ReadInstallPath(string keyPath, RegistryHive hive, RegistryView view)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
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
///   Resolves the runtimes that must not be left to operating-system PATH order: the Git Bash runtime
///   to a Git for Windows <c> bash.exe </c>, and the PowerShell 7 runtime to an installed
///   <c> pwsh.exe </c>. <c> cmd.exe </c> and <c> powershell.exe </c> ship with Windows and are launched
///   by bare name by design.
/// </summary>
public sealed class WindowsRuntimeLocator : IRuntimeExecutableLocator
{
    private const string BashExecutableName = "bash.exe";
    private const string PwshExecutableName = "pwsh.exe";

    private readonly IWindowsRuntimeEnvironment environment;
    private readonly ILogger<WindowsRuntimeLocator> logger;

    /// <summary>
    ///   Creates the locator over an operating-system seam.
    /// </summary>
    /// <param name="environment"> The operating-system seam. </param>
    /// <param name="logger"> Logger for resolution diagnostics. </param>
    internal WindowsRuntimeLocator(IWindowsRuntimeEnvironment environment,
                                   ILogger<WindowsRuntimeLocator> logger)
    {
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string? Locate(RuntimeKind kind)
    {
        return kind switch
        {
            RuntimeKind.GitBash => this.LocateGitBash(),
            RuntimeKind.PowerShell7 => this.LocatePowerShell7(),

            // cmd.exe and powershell.exe ship with Windows and are launched by bare name by design.
            _ => null
        };
    }

    private string? LocateGitBash()
    {
        foreach (var installPath in this.ReadGitRegistryInstallPaths())
        {
            var registryCandidate = this.TryGitCandidate(installPath);
            if (registryCandidate is not null)
            {
                this.logger.LogInformation(
                    "Resolved Git Bash to {ExecutablePath} from the Git for Windows registry InstallPath",
                    registryCandidate);
                return registryCandidate;
            }
        }

        foreach (var root in this.environment.GetGitWellKnownInstallRoots())
        {
            var wellKnownCandidate = this.TryGitCandidate(root);
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

    private string? LocatePowerShell7()
    {
        var registryInstallPath = this.ReadPowerShellRegistryInstallPath();
        if (registryInstallPath is not null)
        {
            var registryCandidate = this.TryPowerShellCandidate(registryInstallPath);
            if (registryCandidate is not null)
            {
                this.logger.LogInformation(
                    "Resolved PowerShell 7 to {ExecutablePath} from the PowerShell Core registry InstallPath",
                    registryCandidate);
                return registryCandidate;
            }
        }

        var wellKnownCandidate = this.environment.GetPowerShellWellKnownExecutablePath();
        if (wellKnownCandidate is not null && this.environment.FileExists(wellKnownCandidate))
        {
            var wellKnownResolved = ToAbsolutePath(wellKnownCandidate);
            if (wellKnownResolved is not null)
            {
                this.logger.LogInformation(
                    "Resolved PowerShell 7 to {ExecutablePath} from the well-known install location",
                    wellKnownResolved);
                return wellKnownResolved;
            }
        }

        // No Windows-directory exclusion here: unlike bash.exe, pwsh.exe has no WSL-launcher-shaped
        // namesake in the Windows directory. A PATH hit is also how Microsoft Store installs surface,
        // through the app-execution alias in %LOCALAPPDATA%\Microsoft\WindowsApps.
        foreach (var directory in this.environment.GetPathDirectories())
        {
            string candidate;
            try
            {
                candidate = Path.Combine(directory, PwshExecutableName);
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
                    "Resolved PowerShell 7 to {ExecutablePath} from the PATH entry {Directory}", resolved, directory);
                return resolved;
            }
        }

        this.logger.LogWarning(
            "PowerShell 7 is configured but no pwsh.exe was found. Lookup order: " +
            @"HKLM SOFTWARE\Microsoft\PowerShellCore InstallPath, the well-known install location " +
            @"(ProgramFiles\PowerShell\7), and PATH entries");
        return null;
    }

    private IReadOnlyList<string> ReadGitRegistryInstallPaths()
    {
        try
        {
            var installPaths = this.environment.ReadGitForWindowsInstallPaths();
            if (installPaths.Count == 0)
            {
                this.logger.LogDebug("The Git for Windows registry InstallPath is absent");
                return [];
            }

            return installPaths;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            // Registry faults are misses, never failures.
            this.logger.LogDebug("The Git for Windows registry InstallPath could not be read: {Reason}", ex.Message);
            return [];
        }
    }

    private string? ReadPowerShellRegistryInstallPath()
    {
        try
        {
            var installPath = this.environment.ReadPowerShellCoreInstallPath();
            if (string.IsNullOrWhiteSpace(installPath))
            {
                this.logger.LogDebug("The PowerShell Core registry InstallPath is absent");
                return null;
            }

            return installPath;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            // Registry faults are misses, never failures.
            this.logger.LogDebug("The PowerShell Core registry InstallPath could not be read: {Reason}", ex.Message);
            return null;
        }
    }

    private string? TryGitCandidate(string installRoot)
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

    private string? TryPowerShellCandidate(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return null;
        }

        string candidate;
        try
        {
            // The PowerShell Core InstallPath value is the directory that contains pwsh.exe.
            candidate = Path.Combine(installDirectory, PwshExecutableName);
        }
        catch (ArgumentException)
        {
            this.logger.LogDebug("Rejected install directory {InstallDirectory} because it is not a valid path", installDirectory);
            return null;
        }

        if (!this.environment.FileExists(candidate))
        {
            this.logger.LogDebug(
                "Rejected install directory {InstallDirectory} because {Candidate} does not exist",
                installDirectory, candidate);
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
