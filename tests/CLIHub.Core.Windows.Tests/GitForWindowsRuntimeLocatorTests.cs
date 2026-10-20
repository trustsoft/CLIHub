namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Windows;

public class GitForWindowsRuntimeLocatorTests
{
    [Fact]
    public void Locate_GitBash_RegistryInstallPathWins()
    {
        var environment = new FakeGitBashEnvironment
        {
            InstallPath = @"C:\Program Files\Git"
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

    [Theory]
    [InlineData(@"C:\Program Files\Git")]
    [InlineData(@"C:\Program Files (x86)\Git")]
    [InlineData(@"C:\Users\me\AppData\Local\Programs\Git")]
    public void Locate_GitBash_FallsBackToWellKnownRootWhenRegistryMisses(string installRoot)
    {
        var bashPath = Path.Combine(installRoot, "bin", "bash.exe");
        var environment = new FakeGitBashEnvironment();
        environment.WellKnownInstallRoots.Add(installRoot);
        environment.AddExistingFile(bashPath);

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(bashPath, resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed()
    {
        var environment = new FakeGitBashEnvironment();
        environment.PathDirectories.Add(@"C:\Tools\Git\bin");
        environment.AddExistingFile(@"C:\Tools\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"C:\Tools\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    [Fact]
    public void Locate_GitBash_WslLauncherOnPath_ReturnsNull()
    {
        var environment = new FakeGitBashEnvironment { WindowsDirectory = @"C:\Windows" };
        environment.PathDirectories.Add(@"C:\Windows\System32");
        environment.AddExistingFile(@"C:\Windows\System32\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Null(resolved);
    }

    [Fact]
    public void Locate_GitBash_NoCandidate_ReturnsNull()
    {
        var environment = new FakeGitBashEnvironment();
        environment.WellKnownInstallRoots.Add(@"C:\Program Files\Git");
        environment.PathDirectories.Add(@"C:\Tools");
        environment.PathDirectories.Add(@"C:\Windows\System32");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Null(resolved);
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.PowerShell7)]
    [InlineData(RuntimeKind.CommandPrompt)]
    public void Locate_NonGitBashKind_ReturnsNull(RuntimeKind kind)
    {
        var environment = new FakeGitBashEnvironment { InstallPath = @"C:\Program Files\Git" };
        environment.AddExistingFile(@"C:\Program Files\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(kind);

        Assert.Null(resolved);
    }

    [Fact]
    public void Locate_GitBash_RegistryThrows_FallsThroughToPath()
    {
        var environment = new FakeGitBashEnvironment { ThrowOnRegistryRead = true };
        environment.PathDirectories.Add(@"C:\Tools\Git\bin");
        environment.AddExistingFile(@"C:\Tools\Git\bin\bash.exe");

        var locator = CreateLocator(environment);

        var resolved = locator.Locate(RuntimeKind.GitBash);

        Assert.Equal(@"C:\Tools\Git\bin\bash.exe", resolved, ignoreCase: true);
    }

    private static GitForWindowsRuntimeLocator CreateLocator(IGitBashEnvironment environment) =>
        new(environment, NullLogger<GitForWindowsRuntimeLocator>.Instance);
}
