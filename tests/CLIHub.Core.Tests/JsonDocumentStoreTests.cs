using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class JsonDocumentStoreTests
{
    private static JsonDocumentStore<T> CreateStore<T>(string configDirectory, string fileName)
        where T : class, new() =>
        new(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory), fileName);

    // Generic document store behavior

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        var document = store.Load();

        Assert.Equal(1, document.SchemaVersion);
        Assert.Empty(document.Projects);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectsDocument.FileName), "{ broken ");
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        var document = store.Load();

        Assert.Empty(document.Projects);
    }

    [Fact]
    public void Save_MissingFile_CreatesFileWithoutTempLeftover()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        store.Save(new ProjectsDocument());

        Assert.True(File.Exists(store.Path));
        Assert.False(File.Exists(store.Path + JsonDocumentStore<ProjectsDocument>.TempSuffix));
    }

    [Fact]
    public void Save_UnknownFields_SurviveRoundTrip()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectsDocument.FileName), """
            { "schemaVersion": 1, "projects": [], "futureKey": { "nested": true } }
            """);
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        store.Save(store.Load());

        var written = File.ReadAllText(store.Path);
        Assert.Contains("futureKey", written);
        Assert.Contains("nested", written);
    }

    [Fact]
    public void Save_CorruptFile_BacksUpBeforeWriting()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectsDocument.FileName), "{ broken ");
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        store.Save(new ProjectsDocument());

        Assert.True(File.Exists(store.BackupPath));
        Assert.Equal("{ broken ", File.ReadAllText(store.BackupPath));
        Assert.Empty(store.Load().Projects);
    }

    [Fact]
    public void Save_OverExistingFile_ReplacesContent()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        store.Save(new SettingsDocument { Hotkey = "Ctrl+Shift+K" });
        store.Save(new SettingsDocument { Hotkey = "Ctrl+Shift+J" });

        Assert.Equal("Ctrl+Shift+J", store.Load().Hotkey);
        Assert.False(File.Exists(store.Path + JsonDocumentStore<SettingsDocument>.TempSuffix));
    }

    // Per-document round-trips

    [Fact]
    public void SettingsDocument_Sections_RoundTrip()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        store.Save(new SettingsDocument
        {
            Runtime = "ps",
            Hotkey = "Ctrl+Shift+K",
            Probe = new ProbeConfig { TtlMinutes = 5, TimeoutSeconds = 3 },
            Update = new UpdateConfig { CheckOnStartup = false }
        });
        var loaded = store.Load();

        Assert.Equal("ps", loaded.Runtime);
        Assert.Equal("Ctrl+Shift+K", loaded.Hotkey);
        Assert.Equal(5, loaded.Probe.TtlMinutes);
        Assert.Equal(3, loaded.Probe.TimeoutSeconds);
        Assert.False(loaded.Update.CheckOnStartup);
    }

    [Fact]
    public void ProjectsDocument_Projects_RoundTrip()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        store.Save(new ProjectsDocument
        {
            Projects = new List<ProjectConfig>
            {
                new() { Id = "p1", Name = "Demo", Path = "C:\\demo", Logo = "custom.png" }
            }
        });
        var loaded = store.Load();

        var project = Assert.Single(loaded.Projects);
        Assert.Equal("p1", project.Id);
        Assert.Equal("Demo", project.Name);
        Assert.Equal("C:\\demo", project.Path);
        Assert.Equal("custom.png", project.Logo);
    }

    [Fact]
    public void Projects_UnknownEntryFields_SurviveRoundTrip()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectsDocument.FileName), """
            { "schemaVersion": 1, "projects": [ { "id": "p1", "name": "Demo", "path": "C:\\demo", "custom": "keep" } ] }
            """);
        var store = CreateStore<ProjectsDocument>(temp.Path, ProjectsDocument.FileName);

        store.Save(store.Load());

        var written = File.ReadAllText(store.Path);
        Assert.Contains("custom", written);
        Assert.Contains("keep", written);
    }

    [Fact]
    public void AgentsDocument_Entries_RoundTrip()
    {
        using var temp = new TempDirectory();
        var lastProbed = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        var store = CreateStore<AgentsDocument>(temp.Path, AgentsDocument.FileName);

        store.Save(new AgentsDocument
        {
            Agents = new Dictionary<string, AgentProbeEntry>
            {
                ["claude"] = new() { HostInstalled = true, Version = "1.2.3", LastProbed = lastProbed }
            }
        });
        var loaded = store.Load();

        var entry = Assert.Single(loaded.Agents);
        Assert.Equal("claude", entry.Key);
        Assert.True(entry.Value.HostInstalled);
        Assert.Equal("1.2.3", entry.Value.Version);
        Assert.Equal(lastProbed, entry.Value.LastProbed);
    }

    // Probe and update effective defaults from the settings document

    [Fact]
    public void Settings_MissingOrInvalidProbe_FallBackToDefaults()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), """
            { "probe": { "ttlMinutes": 0, "timeoutSeconds": -5 } }
            """);
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        var loaded = store.Load();

        Assert.Equal(ProbeConfig.DefaultTtlMinutes, loaded.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, loaded.Probe.EffectiveTimeoutSeconds);
    }

    [Fact]
    public void Settings_NoProbeSection_UsesDefaults()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        var loaded = store.Load();

        Assert.Equal(ProbeConfig.DefaultTtlMinutes, loaded.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, loaded.Probe.EffectiveTimeoutSeconds);
    }

    [Fact]
    public void Settings_NoUpdateSection_DefaultsToEnabled()
    {
        using var temp = new TempDirectory();
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        Assert.True(store.Load().Update.EffectiveCheckOnStartup);
    }

    [Fact]
    public void Settings_EmptyUpdateSection_DefaultsToEnabled()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), """
            { "update": {} }
            """);
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        Assert.True(store.Load().Update.EffectiveCheckOnStartup);
    }

    [Fact]
    public void Settings_ExplicitFalse_DisablesStartupCheck()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), """
            { "update": { "checkOnStartup": false } }
            """);
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        Assert.False(store.Load().Update.EffectiveCheckOnStartup);
    }

    [Fact]
    public void Settings_NullSections_FallBackToDefaults()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, SettingsDocument.FileName), """
            { "probe": null, "update": null }
            """);
        var store = CreateStore<SettingsDocument>(temp.Path, SettingsDocument.FileName);

        var loaded = store.Load();

        Assert.Equal(ProbeConfig.DefaultTtlMinutes, loaded.Probe.EffectiveTtlMinutes);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, loaded.Probe.EffectiveTimeoutSeconds);
        Assert.True(loaded.Update.EffectiveCheckOnStartup);
    }
}
