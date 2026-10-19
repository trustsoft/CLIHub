namespace CLIHub.Core.Tests.Plugins;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

public class PluginSeederTests : IDisposable
{
    private readonly string pluginsPath;

    public PluginSeederTests()
    {
        this.pluginsPath = Path.Combine(Path.GetTempPath(), "clihub-seed-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try { Directory.Delete(this.pluginsPath, recursive: true); } catch { }
    }

    private PluginSeeder CreateSeeder() =>
        new(NullLogger<PluginSeeder>.Instance, this.pluginsPath);

    [Fact]
    public void SeedIfEmpty_EmptyFolder_WritesSixDescriptors()
    {
        var written = this.CreateSeeder().SeedIfEmpty();

        Assert.Equal(6, written);
        foreach (var id in new[] { "opencode", "pi", "cline-cli", "github-copilot", "openclaude", "qwen-code" })
        {
            Assert.True(File.Exists(Path.Combine(this.pluginsPath, id, "plugin.json")), $"missing {id}");
        }
    }

    [Fact]
    public void SeedIfEmpty_EmptyFolder_WritesLogoForEachAgent()
    {
        this.CreateSeeder().SeedIfEmpty();

        foreach (var id in new[] { "opencode", "pi", "cline-cli", "github-copilot", "openclaude", "qwen-code" })
        {
            var logo = Path.Combine(this.pluginsPath, id, "logo.png");
            Assert.True(File.Exists(logo), $"missing logo for {id}");
            Assert.True(new FileInfo(logo).Length > 0, $"empty logo for {id}");
        }
    }

    [Fact]
    public void SeedIfEmpty_DoesNotOverwriteExistingLogo()
    {
        var opencodeDir = Path.Combine(this.pluginsPath, "opencode");
        Directory.CreateDirectory(opencodeDir);
        File.WriteAllText(Path.Combine(opencodeDir, "plugin.json"),
            """{ "id": "opencode", "name": "OpenCode", "commands": { "launch": { "executable": "opencode" } } }""");
        var customLogo = Path.Combine(opencodeDir, "logo.png");
        File.WriteAllBytes(customLogo, new byte[] { 1, 2, 3, 4 });

        this.CreateSeeder().SeedIfEmpty();

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(customLogo));
    }

    [Fact]
    public void SeedIfEmpty_ExistingPlugin_DoesNothing()
    {
        var existingDir = Path.Combine(this.pluginsPath, "custom");
        Directory.CreateDirectory(existingDir);
        File.WriteAllText(Path.Combine(existingDir, "plugin.json"),
            """{ "id": "custom", "name": "Custom", "commands": { "launch": { "executable": "x" } } }""");

        var written = this.CreateSeeder().SeedIfEmpty();

        Assert.Equal(0, written);
        Assert.False(Directory.Exists(Path.Combine(this.pluginsPath, "opencode")));
    }

    [Fact]
    public void SeedIfEmpty_IsIdempotent()
    {
        var seeder = this.CreateSeeder();
        Assert.Equal(6, seeder.SeedIfEmpty());
        Assert.Equal(0, seeder.SeedIfEmpty());
    }

    [Fact]
    public void SeedIfEmpty_DoesNotOverwriteExistingDescriptor()
    {
        // A plugin directory that has a descriptor makes the folder non-empty,
        // so seeding is skipped and the file is preserved verbatim.
        var opencodeDir = Path.Combine(this.pluginsPath, "opencode");
        Directory.CreateDirectory(opencodeDir);
        const string content = """{ "id": "opencode", "name": "Edited", "commands": { "launch": { "executable": "opencode" } } }""";
        File.WriteAllText(Path.Combine(opencodeDir, "plugin.json"), content);

        var written = this.CreateSeeder().SeedIfEmpty();

        Assert.Equal(0, written);
        Assert.Equal(content, File.ReadAllText(Path.Combine(opencodeDir, "plugin.json")));
    }

    [Fact]
    public void SeededDescriptors_LoadAsValidPlugins()
    {
        this.CreateSeeder().SeedIfEmpty();

        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.pluginsPath, "logos-state.json"));
        var catalog = new PluginCatalog(
            NullLogger<PluginCatalog>.Instance,
            new PluginLogoResolver(cache, new LogoOptions(null)),
            this.pluginsPath);
        catalog.LoadPlugins();

        var plugins = catalog.GetAllPlugins().ToList();
        Assert.Equal(6, plugins.Count);
        Assert.All(plugins, p => Assert.NotNull(p.Commands.Launch));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Id)));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
    }

    [Fact]
    public void SeededDescriptors_ExposeExpectedCommandSets()
    {
        this.CreateSeeder().SeedIfEmpty();

        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.pluginsPath, "logos-state.json"));
        var catalog = new PluginCatalog(
            NullLogger<PluginCatalog>.Instance,
            new PluginLogoResolver(cache, new LogoOptions(null)),
            this.pluginsPath);
        catalog.LoadPlugins();

        var plugins = catalog.GetAllPlugins().ToDictionary(p => p.Id);
        var full = new[] { AgentCommandKind.Resume, AgentCommandKind.Version, AgentCommandKind.Update };
        var withoutResume = new[] { AgentCommandKind.Version, AgentCommandKind.Update };

        Assert.Multiple(
            () => AssertCommandSet(plugins["opencode"], full),
            () => AssertCommandSet(plugins["pi"], full),
            () => AssertCommandSet(plugins["openclaude"], full),
            () => AssertCommandSet(plugins["github-copilot"], full),
            () => AssertCommandSet(plugins["qwen-code"], full),
            () => AssertCommandSet(plugins["cline-cli"], withoutResume),
            // Cline CLI can only resume by explicit session id (cline --id <session-id>);
            // there is no resume-most-recent invocation, so the kind stays omitted.
            () => Assert.Null(plugins["cline-cli"].Commands.Resume));
    }

    private static void AssertCommandSet(Plugin plugin, AgentCommandKind[] expectedKinds)
    {
        foreach (var kind in expectedKinds)
        {
            Assert.True(plugin.Commands.Get(kind) is not null, $"{plugin.Id} is missing the {kind} command");
        }
    }
}
