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
}
