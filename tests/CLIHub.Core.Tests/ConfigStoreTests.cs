using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class ConfigStoreTests
{
    private static ConfigStore CreateStore(string configDirectory) =>
        new(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory));

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var temp = new TempDirectory();

        var config = CreateStore(temp.Path).Load();

        Assert.Equal(Config.DefaultHotkey, config.Hotkey);
        Assert.Null(config.Runtime);
        Assert.Empty(config.Projects);
    }

    [Fact]
    public void Load_ValidFile_ReturnsParsedValues()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "config.json"), """
            {
              "schemaVersion": 1,
              "runtime": "ps",
              "hotkey": "Ctrl+Shift+K",
              "projects": [
                { "id": "p1", "name": "Demo", "path": "C:\\demo", "logo": null }
              ]
            }
            """);

        var config = CreateStore(temp.Path).Load();

        Assert.Equal("ps", config.Runtime);
        Assert.Equal("Ctrl+Shift+K", config.Hotkey);
        var project = Assert.Single(config.Projects);
        Assert.Equal("Demo", project.Name);
        Assert.Equal("C:\\demo", project.Path);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "config.json"), "{ broken ");

        var config = CreateStore(temp.Path).Load();

        Assert.Equal(Config.DefaultHotkey, config.Hotkey);
        Assert.Empty(config.Projects);
    }
}
