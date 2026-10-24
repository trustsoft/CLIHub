namespace CLIHub.Core.Windows.Tests;

using Microsoft.Win32;

using CLIHub.Core.Windows;

/// <summary>
///   Tests for the hermetic Git registry install-path collector and the guarded real-registry
///   plural read.
/// </summary>
public class WindowsRuntimeEnvironmentTests
{
    [Fact]
    public void CollectInstallPaths_FakeReaderKeyedByView_ReturnsValuesInViewOrder()
    {
        var views = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Default),
            (RegistryHive.CurrentUser, RegistryView.Registry32)
        };

        var collected = WindowsRuntimeEnvironment.CollectInstallPaths(
            views,
            (hive, view) => $"{hive}-{view}-path");

        Assert.Equal(
            new[]
            {
                "LocalMachine-Registry64-path",
                "LocalMachine-Registry32-path",
                "CurrentUser-Default-path",
                "CurrentUser-Registry32-path"
            },
            collected);
    }

    [Fact]
    public void CollectInstallPaths_NullOrWhitespaceValues_SkipsThem()
    {
        var views = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Default),
            (RegistryHive.CurrentUser, RegistryView.Registry32)
        };

        var collected = WindowsRuntimeEnvironment.CollectInstallPaths(
            views,
            (hive, _) => hive == RegistryHive.LocalMachine ? null : "  ");

        Assert.Empty(collected);
    }

    [Fact]
    public void CollectInstallPaths_UntrimmedValues_TrimsThem()
    {
        var views = new[] { (RegistryHive.CurrentUser, RegistryView.Default) };

        var collected = WindowsRuntimeEnvironment.CollectInstallPaths(
            views,
            (_, _) => @"  C:\Program Files\Git  ");

        var installPath = Assert.Single(collected);
        Assert.Equal(@"C:\Program Files\Git", installPath);
    }

    [Fact]
    public void ReadGitForWindowsInstallPaths_HkcuInstallPathWritten_ContainsMarker()
    {
        const string keyPath = @"SOFTWARE\GitForWindows";
        const string valueName = "InstallPath";
        // Residual risk, accepted: during the millisecond write window a concurrently launched
        // CLIHub could observe the marker; it then probes marker\bin\bash.exe, finds nothing, and
        // moves on to the remaining candidates, so the window is benign.
        var marker = $@"C:\low-sweep-marker-{Guid.NewGuid():N}";

        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        RegistryKey? key = null;
        var keyCreated = false;
        try
        {
            using (var probeKey = baseKey.OpenSubKey(keyPath))
            {
                key = probeKey is null ? baseKey.CreateSubKey(keyPath, writable: true) : null;
                keyCreated = key is not null;
            }

            if (key is null)
            {
                key = baseKey.OpenSubKey(keyPath, writable: true) ?? throw new InvalidOperationException("The GitForWindows key vanished between probe and open");
            }

            var priorValueExisted = key.GetValue(valueName) is not null;
            var priorValue = key.GetValue(valueName) as string;

            key.SetValue(valueName, marker);

            try
            {
                var installPaths = new WindowsRuntimeEnvironment().ReadGitForWindowsInstallPaths();

                // Containment, never sequence: HKLM views hold real machine state, and the HKCU
                // default/32-bit views may alias the same physical key and yield the marker twice.
                Assert.Contains(marker, installPaths);
            }
            finally
            {
                if (priorValueExisted)
                {
                    key.SetValue(valueName, priorValue!);
                }
                else
                {
                    key.DeleteValue(valueName, throwOnMissingValue: false);
                }
            }
        }
        finally
        {
            key?.Dispose();
            if (keyCreated)
            {
                try
                {
                    baseKey.DeleteSubKeyTree(keyPath);
                }
                catch (System.IO.IOException)
                {
                    // Best-effort cleanup; a failed delete of a key this test created must not
                    // mask the assertion result.
                }
            }
        }
    }
}
