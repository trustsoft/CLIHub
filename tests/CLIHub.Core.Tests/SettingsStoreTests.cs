using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class SettingsStoreTests
{
    private static SettingsStore CreateStore(string configDirectory) =>
        new(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory));

    [Fact]
    public void MissingFile_DefaultsApply()
    {
        using var temp = new TempDirectory();

        var store = CreateStore(temp.Path);

        Assert.Null(store.Runtime);
        Assert.Equal(SettingsDocument.DefaultHotkey, store.Hotkey);
        Assert.Equal(ProbeConfig.DefaultTtlMinutes, store.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, store.Probe.EffectiveTimeoutSeconds);
        Assert.True(store.Update.EffectiveCheckOnStartup);
    }

    [Fact]
    public void UnparseableFile_DefaultsApply()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), "{ broken ");

        var store = CreateStore(temp.Path);

        Assert.Equal(SettingsDocument.DefaultHotkey, store.Hotkey);
        Assert.True(store.Update.EffectiveCheckOnStartup);
    }

    [Fact]
    public void ValidFile_ExposesParsedValues()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), """
            {
              "schemaVersion": 1,
              "runtime": "wt",
              "hotkey": "Ctrl+Shift+K",
              "probe": { "ttlMinutes": 30, "timeoutSeconds": 5 },
              "update": { "checkOnStartup": false }
            }
            """);

        var store = CreateStore(temp.Path);

        Assert.Equal("wt", store.Runtime);
        Assert.Equal("Ctrl+Shift+K", store.Hotkey);
        Assert.Equal(30, store.Probe.TtlMinutes);
        Assert.Equal(5, store.Probe.TimeoutSeconds);
        Assert.False(store.Update.CheckOnStartup);
    }
}
