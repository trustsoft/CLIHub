using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class ConfigPersistenceTests
{
    private static ConfigStore CreateStore(string configDirectory) =>
        new(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory));

    [Fact]
    public void Save_MissingFile_CreatesFileWithoutTempLeftover()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);

        store.Save(new Config());

        Assert.True(File.Exists(store.Path));
        Assert.False(File.Exists(store.Path + ConfigStore.TempSuffix));
    }

    [Fact]
    public void Save_PreservesUnknownFields()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigStore.FileName), """
            { "schemaVersion": 1, "hotkey": "Ctrl+Alt+Space", "probe": { "ttlMinutes": 5 } }
            """);
        var store = CreateStore(temp.Path);

        store.Save(store.Load());

        var written = File.ReadAllText(store.Path);
        Assert.Contains("probe", written);
        Assert.Contains("ttlMinutes", written);
    }

    [Fact]
    public void Save_CorruptFile_BacksUpBeforeWriting()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigStore.FileName), "{ broken ");
        var store = CreateStore(temp.Path);

        store.Save(new Config());

        Assert.True(File.Exists(store.BackupPath));
        Assert.Equal("{ broken ", File.ReadAllText(store.BackupPath));
        Assert.Empty(store.Load().Projects);
    }

    [Fact]
    public void Save_OverExistingFile_ReplacesContent()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);

        store.Save(new Config { Hotkey = "Ctrl+Shift+K" });
        store.Save(new Config { Hotkey = "Ctrl+Shift+J" });

        Assert.Equal("Ctrl+Shift+J", store.Load().Hotkey);
        Assert.False(File.Exists(store.Path + ConfigStore.TempSuffix));
    }

    [Fact]
    public void Save_ProbeAndAgentsSections_RoundTrip()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);
        var config = new Config
        {
            Probe = new ProbeConfig { TtlMinutes = 5, TimeoutSeconds = 3 },
            Agents = new Dictionary<string, AgentProbeEntry>
            {
                ["claude"] = new()
                {
                    HostInstalled = true,
                    Version = "1.2.3",
                    LastProbed = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero)
                }
            }
        };

        store.Save(config);
        var loaded = store.Load();

        Assert.Equal(5, loaded.Probe.TtlMinutes);
        Assert.Equal(3, loaded.Probe.TimeoutSeconds);
        var entry = Assert.Single(loaded.Agents);
        Assert.Equal("claude", entry.Key);
        Assert.True(entry.Value.HostInstalled);
        Assert.Equal("1.2.3", entry.Value.Version);
        Assert.Equal(new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero), entry.Value.LastProbed);
    }

    [Fact]
    public void Load_MissingOrInvalidProbeSettings_FallBackToDefaults()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigStore.FileName), """
            { "probe": { "ttlMinutes": 0, "timeoutSeconds": -5 } }
            """);
        var store = CreateStore(temp.Path);

        var loaded = store.Load();

        Assert.Equal(ProbeConfig.DefaultTtlMinutes, loaded.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, loaded.Probe.EffectiveTimeoutSeconds);
    }

    [Fact]
    public void Load_NoProbeSection_UsesDefaults()
    {
        using var temp = new TempDirectory();
        var store = CreateStore(temp.Path);

        var loaded = store.Load();

        Assert.Equal(ProbeConfig.DefaultTtlMinutes, loaded.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, loaded.Probe.EffectiveTimeoutSeconds);
    }
}
