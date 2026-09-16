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

    [Fact]
    public void Save_ValidDocument_WritesFileAndUpdatesCache()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var document = new SettingsDocument
        {
            Runtime = "ps",
            Hotkey = "Ctrl+Shift+K",
            Probe = new ProbeConfig { TtlMinutes = 30 },
            Update = new UpdateConfig { CheckOnStartup = false }
        };

        var result = store.Save(document);

        Assert.True(result.Saved);
        Assert.Empty(result.Errors);
        Assert.Equal("ps", store.Runtime);

        var reloaded = CreateStore(temp.Path);
        Assert.Equal("ps", reloaded.Runtime);
        Assert.Equal("Ctrl+Shift+K", reloaded.Hotkey);
        Assert.Equal(30, reloaded.Probe.TtlMinutes);
        Assert.Null(reloaded.Probe.TimeoutSeconds);
        Assert.False(reloaded.Update.CheckOnStartup);
    }

    [Fact]
    public void Save_ZeroTtl_Rejected()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var document = new SettingsDocument { Probe = new ProbeConfig { TtlMinutes = 0 } };

        var result = store.Save(document);

        Assert.False(result.Saved);
        Assert.Contains(result.Errors, error => error.Contains("TTL", StringComparison.OrdinalIgnoreCase));
        Assert.Null(store.Runtime);

        var reloaded = CreateStore(temp.Path);
        Assert.Equal(SettingsDocument.DefaultHotkey, reloaded.Hotkey);
    }

    [Fact]
    public void Save_NonPositiveTimeout_Rejected()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var document = new SettingsDocument { Probe = new ProbeConfig { TimeoutSeconds = -5 } };

        var result = store.Save(document);

        Assert.False(result.Saved);
        Assert.Contains(result.Errors, error => error.Contains("Таймаут", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Save_UnparseableHotkey_Rejected()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var document = new SettingsDocument { Hotkey = "Ctrl+Б" };

        var result = store.Save(document);

        Assert.False(result.Saved);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Save_RejectedSave_KeepsPreviousValues()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        Assert.True(store.Save(new SettingsDocument { Runtime = "wt" }).Saved);

        var result = store.Save(new SettingsDocument { Runtime = "ps", Probe = new ProbeConfig { TtlMinutes = 0 } });

        Assert.False(result.Saved);
        Assert.Equal("wt", store.Runtime);

        var reloaded = CreateStore(temp.Path);
        Assert.Equal("wt", reloaded.Runtime);
    }

    [Fact]
    public void Save_UpdatesRuntimeForConsumers()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var resolver = new RuntimeResolver();

        Assert.Equal(RuntimeResolver.DefaultRuntime, resolver.Resolve(null, store.Runtime));

        Assert.True(store.Save(new SettingsDocument { Runtime = "ps" }).Saved);

        Assert.Equal("ps", resolver.Resolve(null, store.Runtime));
    }
}
