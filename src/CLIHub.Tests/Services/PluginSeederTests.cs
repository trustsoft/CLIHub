using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CLIHub.Tests.Services;

public class PluginSeederTests : IDisposable
{
    private readonly string _pluginsPath;

    public PluginSeederTests()
    {
        _pluginsPath = Path.Combine(Path.GetTempPath(), "clihub-seed-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginsPath, recursive: true); } catch { }
    }

    private PluginSeeder CreateSeeder() =>
        new(NullLogger<PluginSeeder>.Instance, _pluginsPath);

    [Fact]
    public void SeedIfEmpty_EmptyFolder_WritesSixDescriptors()
    {
        var written = CreateSeeder().SeedIfEmpty();

        Assert.Equal(6, written);
        foreach (var id in new[] { "opencode", "pi", "cline-cli", "github-copilot", "openclaude", "qwen-code" })
        {
            Assert.True(File.Exists(Path.Combine(_pluginsPath, id, "plugin.json")), $"missing {id}");
        }
    }

    [Fact]
    public void SeedIfEmpty_EmptyFolder_WritesLogoForEachAgent()
    {
        CreateSeeder().SeedIfEmpty();

        foreach (var id in new[] { "opencode", "pi", "cline-cli", "github-copilot", "openclaude", "qwen-code" })
        {
            var logo = Path.Combine(_pluginsPath, id, "logo.png");
            Assert.True(File.Exists(logo), $"missing logo for {id}");
            Assert.True(new FileInfo(logo).Length > 0, $"empty logo for {id}");
        }
    }

    [Fact]
    public void SeedIfEmpty_DoesNotOverwriteExistingLogo()
    {
        var opencodeDir = Path.Combine(_pluginsPath, "opencode");
        Directory.CreateDirectory(opencodeDir);
        File.WriteAllText(Path.Combine(opencodeDir, "plugin.json"),
            """{ "id": "opencode", "name": "OpenCode", "commands": { "launch": { "executable": "opencode" } } }""");
        var customLogo = Path.Combine(opencodeDir, "logo.png");
        File.WriteAllBytes(customLogo, new byte[] { 1, 2, 3, 4 });

        CreateSeeder().SeedIfEmpty();

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(customLogo));
    }

    [Fact]
    public void SeedIfEmpty_ExistingPlugin_DoesNothing()
    {
        var existingDir = Path.Combine(_pluginsPath, "custom");
        Directory.CreateDirectory(existingDir);
        File.WriteAllText(Path.Combine(existingDir, "plugin.json"),
            """{ "id": "custom", "name": "Custom", "commands": { "launch": { "executable": "x" } } }""");

        var written = CreateSeeder().SeedIfEmpty();

        Assert.Equal(0, written);
        Assert.False(Directory.Exists(Path.Combine(_pluginsPath, "opencode")));
    }

    [Fact]
    public void SeedIfEmpty_IsIdempotent()
    {
        var seeder = CreateSeeder();
        Assert.Equal(6, seeder.SeedIfEmpty());
        Assert.Equal(0, seeder.SeedIfEmpty());
    }

    [Fact]
    public void SeedIfEmpty_DoesNotOverwriteExistingDescriptor()
    {
        // A plugin directory that has a descriptor makes the folder non-empty,
        // so seeding is skipped and the file is preserved verbatim.
        var opencodeDir = Path.Combine(_pluginsPath, "opencode");
        Directory.CreateDirectory(opencodeDir);
        const string content = """{ "id": "opencode", "name": "Edited", "commands": { "launch": { "executable": "opencode" } } }""";
        File.WriteAllText(Path.Combine(opencodeDir, "plugin.json"), content);

        var written = CreateSeeder().SeedIfEmpty();

        Assert.Equal(0, written);
        Assert.Equal(content, File.ReadAllText(Path.Combine(opencodeDir, "plugin.json")));
    }

    [Fact]
    public void SeededDescriptors_LoadAsValidPlugins()
    {
        CreateSeeder().SeedIfEmpty();

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _pluginsPath);
        manager.LoadPlugins();

        var plugins = manager.GetAllPlugins().ToList();
        Assert.Equal(6, plugins.Count);
        Assert.All(plugins, p => Assert.NotNull(p.Commands.Launch));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Id)));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
    }
}
