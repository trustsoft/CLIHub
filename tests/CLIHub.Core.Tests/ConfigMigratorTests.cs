using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class ConfigMigratorTests
{
    private static ConfigMigrator CreateMigrator(string configDirectory) =>
        new(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory));

    private static T Load<T>(string configDirectory, string fileName)
        where T : class, new() =>
        new JsonDocumentStore<T>(
            new PhysicalFileSystem(),
            new StubPathProvider(configDirectory, configDirectory),
            fileName).Load();

    [Fact]
    public void Migrate_LegacyFile_SplitsIntoThreeFilesAndKeepsOriginal()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName), """
            {
              "schemaVersion": 1,
              "runtime": "ps",
              "hotkey": "Ctrl+Shift+K",
              "probe": { "ttlMinutes": 30, "timeoutSeconds": 5 },
              "update": { "checkOnStartup": false },
              "projects": [ { "id": "p1", "name": "Demo", "path": "C:\\demo" } ],
              "agents": { "claude": { "hostInstalled": true, "version": "1.2.3", "lastProbed": "2026-09-14T10:00:00+00:00" } }
            }
            """);

        CreateMigrator(temp.Path).MigrateIfNeeded();

        var settings = Load<SettingsDocument>(temp.Path, SettingsDocument.FileName);
        Assert.Equal("ps", settings.Runtime);
        Assert.Equal("Ctrl+Shift+K", settings.Hotkey);
        Assert.Equal(30, settings.Probe.TtlMinutes);
        Assert.False(settings.Update.CheckOnStartup);

        var project = Assert.Single(Load<ProjectsDocument>(temp.Path, ProjectsDocument.FileName).Projects);
        Assert.Equal("Demo", project.Name);

        var entry = Assert.Single(Load<AgentsDocument>(temp.Path, AgentsDocument.FileName).Agents);
        Assert.Equal("claude", entry.Key);
        Assert.True(entry.Value.HostInstalled);

        Assert.False(File.Exists(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName)));
        Assert.True(File.Exists(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName + ConfigMigrator.MigratedSuffix)));
    }

    [Fact]
    public void Migrate_UnknownTopLevelFields_LandInSettings()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName), """
            { "schemaVersion": 1, "hotkey": "Ctrl+Alt+Space", "futureKey": 42 }
            """);

        CreateMigrator(temp.Path).MigrateIfNeeded();

        var settingsPath = Path.Combine(temp.Path, SettingsDocument.FileName);
        Assert.Contains("futureKey", File.ReadAllText(settingsPath));
    }

    [Fact]
    public void Migrate_SettingsAlreadyExist_SkipsLegacyFile()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName), """
            { "schemaVersion": 1, "hotkey": "Ctrl+Shift+K" }
            """);
        var settingsStore = new JsonDocumentStore<SettingsDocument>(
            new PhysicalFileSystem(),
            new StubPathProvider(temp.Path, temp.Path),
            SettingsDocument.FileName);
        settingsStore.Save(new SettingsDocument { Hotkey = "Ctrl+F1" });
        var before = File.ReadAllText(settingsStore.Path);

        CreateMigrator(temp.Path).MigrateIfNeeded();

        Assert.Equal(before, File.ReadAllText(settingsStore.Path));
        Assert.True(File.Exists(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName)));
        Assert.False(File.Exists(Path.Combine(temp.Path, ProjectsDocument.FileName)));
    }

    [Fact]
    public void Migrate_NoLegacyFile_DoesNothing()
    {
        using var temp = new TempDirectory();

        CreateMigrator(temp.Path).MigrateIfNeeded();

        Assert.False(File.Exists(Path.Combine(temp.Path, SettingsDocument.FileName)));
        Assert.False(File.Exists(Path.Combine(temp.Path, ProjectsDocument.FileName)));
        Assert.False(File.Exists(Path.Combine(temp.Path, AgentsDocument.FileName)));
    }

    [Fact]
    public void Migrate_CorruptLegacyFile_SkipsWithoutCrash()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName), "{ broken ");

        CreateMigrator(temp.Path).MigrateIfNeeded();

        Assert.True(File.Exists(Path.Combine(temp.Path, ConfigMigrator.LegacyFileName)));
        Assert.False(File.Exists(Path.Combine(temp.Path, SettingsDocument.FileName)));
    }
}
