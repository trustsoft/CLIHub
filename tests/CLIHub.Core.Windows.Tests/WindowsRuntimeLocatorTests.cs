namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Windows;

public class WindowsRuntimeLocatorTests
{
    [Fact]
    public void Locate_GitBash_RegistryInstallPathWins()
    {
        var environment = new FakeWindowsRuntimeEnvironment
        {
            InstallPaths = { @"C:\Program Files\Git" }
        };
        environment.AddExistingFile(@"C:\Program Files\Git\bin\bash.exe");
        environment.WellKnownInstallRoots.Add(@"D:\Tools\Git");
        environment.AddExistingFile(@"D:\Tools\Git\bin\bash.exe");
        environment.PathDirectories.Add(@"E:\Portable\Git\bin");
        environment.AddExistingFile(@"E:\Portable\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"c:\program files\git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_FirstRegistryCandidateStale_SecondCandidateWins()
    {
        var environment = new FakeWindowsRuntimeEnvironment();
        environment.InstallPaths.Add(@"C:\Stale\Git");
        environment.InstallPaths.Add(@"D:\Real\Git");
        environment.AddExistingFile(@"D:\Real\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"D:\Real\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_AllRegistryCandidatesStale_FallsThroughToWellKnownRoot()
    {
        var environment = new FakeWindowsRuntimeEnvironment();
        environment.InstallPaths.Add(@"C:\StaleOne\Git");
        environment.InstallPaths.Add(@"C:\StaleTwo\Git");
        environment.WellKnownInstallRoots.Add(@"D:\Tools\Git");
        environment.AddExistingFile(@"D:\Tools\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"D:\Tools\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Git")]
    [InlineData(@"C:\Program Files (x86)\Git")]
    [InlineData(@"C:\Users\me\AppData\Local\Programs\Git")]
    public void Locate_GitBash_FallsBackToWellKnownRootWhenRegistryMisses(string installRoot)
    {
        var bashPath = Path.Combine(installRoot, "bin", "bash.exe");
        var environment = new FakeWindowsRuntimeEnvironment();
        environment.WellKnownInstallRoots.Add(installRoot);
        environment.AddExistingFile(bashPath);

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(bashPath, resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed()
    {
        var environment = new FakeWindowsRuntimeEnvironment();
        environment.PathDirectories.Add(@"C:\Tools\Git\bin");
        environment.AddExistingFile(@"C:\Tools\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"C:\Tools\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_WslLauncherOnPath_ReturnsNull()
    {
        var environment = new FakeWindowsRuntimeEnvironment { WindowsDirectory = @"C:\Windows" };
        environment.PathDirectories.Add(@"C:\Windows\System32");
        environment.AddExistingFile(@"C:\Windows\System32\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Null(resolved);
    }

    [Fact]
    public void Locate_GitBash_NoCandidate_ReturnsNull()
    {
        var environment = new FakeWindowsRuntimeEnvironment();
        environment.WellKnownInstallRoots.Add(@"C:\Program Files\Git");
        environment.PathDirectories.Add(@"C:\Tools");
        environment.PathDirectories.Add(@"C:\Windows\System32");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Null(resolved);
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.CommandPrompt)]
    public void Locate_UncoveredKind_ReturnsNull(RuntimeKind kind)
    {
        var environment = new FakeWindowsRuntimeEnvironment { InstallPaths = { @"C:\Program Files\Git" } };
        environment.AddExistingFile(@"C:\Program Files\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(kind);

        Assert.Null(resolved);
    }

    [Fact]
    public void Locate_GitBash_RegistryThrows_FallsThroughToPath()
    {
        var environment = new FakeWindowsRuntimeEnvironment { ThrowOnRegistryRead = true };
        environment.PathDirectories.Add(@"C:\Tools\Git\bin");
        environment.AddExistingFile(@"C:\Tools\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"C:\Tools\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_PowerShell7_RegistryInstallPathWins()
    {
        var environment = new FakeWindowsRuntimeEnvironment
        {
            PowerShellInstallPath = @"C:\Program Files\PowerShell\7"
        };
        environment.AddExistingFile(@"C:\Program Files\PowerShell\7\pwsh.exe");
        environment.PowerShellWellKnownExecutablePath = @"D:\Alt\PowerShell\7\pwsh.exe";
        environment.AddExistingFile(@"D:\Alt\PowerShell\7\pwsh.exe");
        environment.PathDirectories.Add(@"E:\Portable\PowerShell");
        environment.AddExistingFile(@"E:\Portable\PowerShell\pwsh.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Equal(@"c:\program files\powershell\7\pwsh.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_PowerShell7_FallsBackToWellKnownPathWhenRegistryMisses()
    {
        var environment = new FakeWindowsRuntimeEnvironment
        {
            PowerShellWellKnownExecutablePath = @"C:\Program Files\PowerShell\7\pwsh.exe"
        };
        environment.AddExistingFile(@"C:\Program Files\PowerShell\7\pwsh.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Equal(@"C:\Program Files\PowerShell\7\pwsh.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_PowerShell7_PathEntryIsUsed()
    {
        var environment = new FakeWindowsRuntimeEnvironment
        {
            PowerShellWellKnownExecutablePath = @"C:\Program Files\PowerShell\7\pwsh.exe"
        };
        environment.PathDirectories.Add(@"C:\Tools\PowerShell");
        environment.AddExistingFile(@"C:\Tools\PowerShell\pwsh.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Equal(@"C:\Tools\PowerShell\pwsh.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_PowerShell7_PathEntryInsideWindowsDirectoryIsUsed()
    {
        // Unlike bash.exe, pwsh.exe has no WSL-launcher-shaped namesake in the Windows directory, so
        // the PowerShell 7 PATH scan deliberately has no Windows-directory exclusion.
        var environment = new FakeWindowsRuntimeEnvironment { WindowsDirectory = @"C:\Windows" };
        environment.PathDirectories.Add(@"C:\Windows\System32");
        environment.AddExistingFile(@"C:\Windows\System32\pwsh.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Equal(@"C:\Windows\System32\pwsh.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_PowerShell7_NoCandidate_ReturnsNull()
    {
        var environment = new FakeWindowsRuntimeEnvironment
        {
            PowerShellWellKnownExecutablePath = @"C:\Program Files\PowerShell\7\pwsh.exe"
        };
        environment.PathDirectories.Add(@"C:\Tools");
        environment.PathDirectories.Add(@"C:\Windows\System32");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Null(resolved);
    }

    [Fact]
    public void Locate_PowerShell7_RegistryThrows_FallsThroughToPath()
    {
        var environment = new FakeWindowsRuntimeEnvironment { ThrowOnRegistryRead = true };
        environment.PathDirectories.Add(@"C:\Tools\PowerShell");
        environment.AddExistingFile(@"C:\Tools\PowerShell\pwsh.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.PowerShell7);

        Assert.Equal(@"C:\Tools\PowerShell\pwsh.exe", resolved, ignoreCase: true);
    }

    private static WindowsRuntimeLocator CreateLocator(IWindowsRuntimeEnvironment environment) =>
        new(environment, NullLogger<WindowsRuntimeLocator>.Instance);
}
