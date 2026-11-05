namespace CLIHub.Core.Tests.Plugins;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

public class PluginSeederTests : IDisposable
{
    private static readonly string[] BuiltinIds = ["opencode", "pi", "omp", "qwen-code", "devin"];

    private readonly string pluginsPath;
    private readonly string stateFilePath;

    public PluginSeederTests()
    {
        this.pluginsPath = Path.Combine(Path.GetTempPath(), "clihub-seed-" + Guid.NewGuid().ToString("N"));
        this.stateFilePath = Path.Combine(Path.GetTempPath(), "clihub-seedstate-" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        try { Directory.Delete(this.pluginsPath, recursive: true); } catch { }
        try { File.Delete(this.stateFilePath); } catch { }
    }

    private PluginStateRepository CreateStateRepository() =>
        new(NullLogger<PluginStateRepository>.Instance, this.stateFilePath);

    private PluginSeeder CreateSeeder() =>
        new(NullLogger<PluginSeeder>.Instance, this.CreateStateRepository(), this.pluginsPath);

    private static string EmbeddedDescriptor(string id) =>
        EmbeddedPluginResources.EnumerateDescriptors(typeof(PluginSeeder).Assembly)
            .Single(d => d.Id == id)
            .Json;

    private void WriteDescriptor(string id, string json)
    {
        var directory = Path.Combine(this.pluginsPath, id);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "plugin.json"), json);
    }

    private void WriteState(string id, PluginStateEntry entry)
    {
        var repository = this.CreateStateRepository();
        var document = repository.Load();
        document.Plugins[id] = entry;
        repository.Save(document);
    }

    private PluginStateEntry ReadState(string id)
    {
        var entry = this.CreateStateRepository().Load().Plugins.GetValueOrDefault(id);
        Assert.NotNull(entry);
        return entry;
    }

    private static string ModifiedDescriptor(string id) => EmbeddedDescriptor(id)
        .Replace("\"name\": \"", "\"name\": \"Edited ", StringComparison.Ordinal);

    [Fact]
    public void EnsureBuiltinsExist_EmptyFolder_WritesAllBuiltinDescriptors()
    {
        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(5, written);
        Assert.Equal(BuiltinIds.Length, Directory.GetDirectories(this.pluginsPath).Length);
        foreach (var id in BuiltinIds)
        {
            Assert.True(File.Exists(Path.Combine(this.pluginsPath, id, "plugin.json")), $"missing {id}");
        }
    }

    [Fact]
    public void EnsureBuiltinsExist_EmptyFolder_WritesLogoForEachAgent()
    {
        this.CreateSeeder().EnsureBuiltinsExist();

        foreach (var id in BuiltinIds)
        {
            var logo = Path.Combine(this.pluginsPath, id, "logo.png");
            Assert.True(File.Exists(logo), $"missing logo for {id}");
            Assert.True(new FileInfo(logo).Length > 0, $"empty logo for {id}");
        }
    }

    [Fact]
    public void EnsureBuiltinsExist_EmptyFolder_RecordsBuiltinOriginAndHashes()
    {
        this.CreateSeeder().EnsureBuiltinsExist();

        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Builtin, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), entry.ContentHash);
        Assert.Equal(PluginHasher.ComputeFileHash(Path.Combine(this.pluginsPath, "opencode", "logo.png")), entry.LogoHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_NoStateDiskMatchesEmbedded_AdoptsAsBuiltin()
    {
        this.WriteDescriptor("opencode", EmbeddedDescriptor("opencode"));

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // Only the other four built-ins are written: the matching opencode descriptor is
        // adopted, not rewritten.
        Assert.Equal(4, written);
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Builtin, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_NoStateDiskDiffersFromEmbedded_PreservesAsUnknown()
    {
        var content = ModifiedDescriptor("opencode");
        this.WriteDescriptor("opencode", content);

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // Only the other four built-ins are written; the modified descriptor is preserved.
        Assert.Equal(4, written);
        Assert.Equal(content, File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Unknown, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(content), entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_NoStateModifiedDescriptorAndLogo_PreservesUserContent()
    {
        var content = ModifiedDescriptor("opencode");
        this.WriteDescriptor("opencode", content);
        var customLogo = Path.Combine(this.pluginsPath, "opencode", "logo.png");
        File.WriteAllBytes(customLogo, [1, 2, 3, 4]);

        this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(content, File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(customLogo));
    }

    [Fact]
    public void EnsureBuiltinsExist_NonBuiltinUserPlugin_UserPluginUntouchedAndBuiltinsSeeded()
    {
        const string customJson = """{ "id": "custom", "name": "Custom", "commands": { "launch": { "executable": "x" } } }""";
        this.WriteDescriptor("custom", customJson);

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(5, written);
        Assert.Equal(customJson, File.ReadAllText(Path.Combine(this.pluginsPath, "custom", "plugin.json")));
        Assert.True(File.Exists(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
    }

    [Fact]
    public void EnsureBuiltinsExist_IsIdempotent()
    {
        var seeder = this.CreateSeeder();
        Assert.Equal(5, seeder.EnsureBuiltinsExist());
        var snapshot = Directory.EnumerateFiles(this.pluginsPath, "*", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => (p, File.GetLastWriteTimeUtc(p)))
            .ToArray();
        var stateAfterFirst = File.ReadAllText(this.stateFilePath);

        Assert.Equal(0, seeder.EnsureBuiltinsExist());

        Assert.Equal(stateAfterFirst, File.ReadAllText(this.stateFilePath));
        Assert.Equal(
            snapshot,
            Directory.EnumerateFiles(this.pluginsPath, "*", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => (p, File.GetLastWriteTimeUtc(p)))
                .ToArray());
    }

    [Fact]
    public void EnsureBuiltinsExist_BuiltinUnmodifiedAndEmbeddedChanged_OverwritesWithEmbedded()
    {
        // Simulate an application update: the state holds the hash of the old content, the
        // disk still holds that unmodified old content, and the embedded content differs.
        const string oldContent = """{ "id": "opencode", "name": "OpenCode (old)", "commands": { "launch": { "executable": "opencode" } } }""";
        this.WriteDescriptor("opencode", oldContent);
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Builtin)
        {
            ContentHash = PluginHasher.ComputeHash(oldContent)
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // The refreshed opencode descriptor plus the four other built-ins written fresh.
        Assert.Equal(5, written);
        Assert.Equal(EmbeddedDescriptor("opencode"), File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), this.ReadState("opencode").ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_BuiltinUserModified_PreservesEditAndKeepsStoredHash()
    {
        const string oldContent = """{ "id": "opencode", "name": "OpenCode (old)", "commands": { "launch": { "executable": "opencode" } } }""";
        var editedContent = ModifiedDescriptor("opencode");
        this.WriteDescriptor("opencode", editedContent);
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Builtin)
        {
            ContentHash = PluginHasher.ComputeHash(oldContent)
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // Only the other four built-ins are written; the edited descriptor is preserved.
        Assert.Equal(4, written);
        Assert.Equal(editedContent, File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Builtin, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(oldContent), entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_InstalledPlugin_NeverTouched()
    {
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Installed)
        {
            ContentHash = "irrelevant"
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // The installed plugin is not seeded, not even when its directory is missing.
        Assert.Equal(4, written);
        Assert.False(Directory.Exists(Path.Combine(this.pluginsPath, "opencode")));
        Assert.Equal(PluginOrigin.Installed, this.ReadState("opencode").Origin);
    }

    [Fact]
    public void EnsureBuiltinsExist_InstalledPluginWithFiles_FilesUntouched()
    {
        const string content = """{ "id": "opencode", "name": "Installed variant", "commands": { "launch": { "executable": "opencode" } } }""";
        this.WriteDescriptor("opencode", content);
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Installed)
        {
            ContentHash = PluginHasher.ComputeHash(content)
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(4, written);
        Assert.Equal(content, File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
    }

    [Fact]
    public void EnsureBuiltinsExist_DisabledBuiltin_NotReSeeded()
    {
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Builtin)
        {
            Disabled = true
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(4, written);
        Assert.False(Directory.Exists(Path.Combine(this.pluginsPath, "opencode")));
        Assert.True(this.ReadState("opencode").Disabled);
    }

    [Fact]
    public void EnsureBuiltinsExist_DeletedBuiltin_ReSeeded()
    {
        this.CreateSeeder().EnsureBuiltinsExist();
        Directory.Delete(Path.Combine(this.pluginsPath, "opencode"), recursive: true);

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(1, written);
        Assert.Equal(EmbeddedDescriptor("opencode"), File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), this.ReadState("opencode").ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_UnknownOriginDirectoryMissing_WritesEmbeddedAndAdoptsAsBuiltin()
    {
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Unknown)
        {
            ContentHash = "gone-content"
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // The re-seeded opencode descriptor plus the four other built-ins written fresh.
        Assert.Equal(5, written);
        Assert.Equal(EmbeddedDescriptor("opencode"), File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Builtin, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_UnknownOriginDiskMatchesEmbedded_AdoptsAsBuiltin()
    {
        this.WriteDescriptor("opencode", EmbeddedDescriptor("opencode"));
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Unknown)
        {
            ContentHash = "stale-content"
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // Only the other four built-ins are written; the matching content is adopted.
        Assert.Equal(4, written);
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Builtin, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(EmbeddedDescriptor("opencode")), entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_UnknownOriginDiskDiffers_PreservesUserContent()
    {
        var content = ModifiedDescriptor("opencode");
        this.WriteDescriptor("opencode", content);
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Unknown)
        {
            ContentHash = "older-content"
        });

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // Only the other four built-ins are written; the user content is preserved.
        Assert.Equal(4, written);
        Assert.Equal(content, File.ReadAllText(Path.Combine(this.pluginsPath, "opencode", "plugin.json")));
        var entry = this.ReadState("opencode");
        Assert.Equal(PluginOrigin.Unknown, entry.Origin);
        Assert.Equal("older-content", entry.ContentHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_UnmodifiedLogoWithNewEmbeddedLogo_RefreshesLogo()
    {
        var customLogo = new byte[] { 1, 2, 3, 4 };
        this.WriteDescriptor("opencode", EmbeddedDescriptor("opencode"));
        File.WriteAllBytes(Path.Combine(this.pluginsPath, "opencode", "logo.png"), customLogo);
        // No state entry: adopting the matching descriptor records the current logo hash.
        this.CreateSeeder().EnsureBuiltinsExist();
        var adoptedLogoHash = this.ReadState("opencode").LogoHash;
        Assert.Equal(PluginHasher.ComputeHash(customLogo), adoptedLogoHash);

        // Simulate an application update shipping a different logo while the on-disk logo is
        // still the unmodified adopted one: the embedded logo overwrites it.
        var written = this.CreateSeeder().EnsureBuiltinsExist();

        Assert.Equal(0, written);
        var embeddedLogoHash = PluginHasher.ComputeFileHash(Path.Combine(this.pluginsPath, "opencode", "logo.png"));
        Assert.NotEqual(PluginHasher.ComputeHash(customLogo), embeddedLogoHash);
        Assert.Equal(embeddedLogoHash, this.ReadState("opencode").LogoHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_UserModifiedLogo_PreservedOnAppUpdate()
    {
        const string oldDescriptor = """{ "id": "opencode", "name": "OpenCode (old)", "commands": { "launch": { "executable": "opencode" } } }""";
        var modifiedLogo = new byte[] { 9, 8, 7, 6 };
        this.WriteDescriptor("opencode", oldDescriptor);
        this.WriteState("opencode", new PluginStateEntry(PluginOrigin.Builtin)
        {
            ContentHash = PluginHasher.ComputeHash(oldDescriptor),
            LogoHash = PluginHasher.ComputeHash(new byte[] { 1, 2, 3, 4 })
        });
        File.WriteAllBytes(Path.Combine(this.pluginsPath, "opencode", "logo.png"), modifiedLogo);

        var written = this.CreateSeeder().EnsureBuiltinsExist();

        // The refreshed descriptor plus the four other built-ins written fresh.
        Assert.Equal(5, written);
        // The descriptor refreshes (app update, unmodified), but the modified logo survives.
        Assert.Equal(modifiedLogo, File.ReadAllBytes(Path.Combine(this.pluginsPath, "opencode", "logo.png")));
        Assert.Equal(PluginHasher.ComputeHash(new byte[] { 1, 2, 3, 4 }), this.ReadState("opencode").LogoHash);
    }

    [Fact]
    public void EnsureBuiltinsExist_MissingLogo_WritesEmbeddedLogo()
    {
        this.WriteDescriptor("opencode", EmbeddedDescriptor("opencode"));

        this.CreateSeeder().EnsureBuiltinsExist();

        var logo = Path.Combine(this.pluginsPath, "opencode", "logo.png");
        Assert.True(File.Exists(logo));
        Assert.True(new FileInfo(logo).Length > 0);
    }

    [Fact]
    public void SeededDescriptors_LoadAsValidPlugins()
    {
        this.CreateSeeder().EnsureBuiltinsExist();

        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.pluginsPath, "logos-state.json"));
        var catalog = new PluginCatalog(
            NullLogger<PluginCatalog>.Instance,
            new PluginLogoResolver(cache, new LogoOptions(null)),
            this.pluginsPath);
        catalog.LoadPlugins();

        var plugins = catalog.GetAllPlugins().ToList();
        Assert.Equal(5, plugins.Count);
        Assert.All(plugins, p => Assert.NotNull(p.Commands.Launch));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Id)));
        Assert.All(plugins, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
    }

    [Fact]
    public void SeededDescriptors_ExposeExpectedCommandSets()
    {
        this.CreateSeeder().EnsureBuiltinsExist();

        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.pluginsPath, "logos-state.json"));
        var catalog = new PluginCatalog(
            NullLogger<PluginCatalog>.Instance,
            new PluginLogoResolver(cache, new LogoOptions(null)),
            this.pluginsPath);
        catalog.LoadPlugins();

        var plugins = catalog.GetAllPlugins().ToDictionary(p => p.Id);
        var full = new[] { AgentCommandKind.Launch, AgentCommandKind.Resume, AgentCommandKind.Version, AgentCommandKind.Update };

        Assert.Multiple(
            () => AssertCommandSet(plugins["opencode"], full),
            () => AssertCommandSet(plugins["pi"], full),
            () => AssertCommandSet(plugins["omp"], full),
            () => AssertCommandSet(plugins["qwen-code"], full),
            () => AssertCommandSet(plugins["devin"], full));
    }

    [Fact]
    public void SeededDescriptors_DeclareExpectedUpdateSources()
    {
        this.CreateSeeder().EnsureBuiltinsExist();

        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.pluginsPath, "logos-state.json"));
        var catalog = new PluginCatalog(
            NullLogger<PluginCatalog>.Instance,
            new PluginLogoResolver(cache, new LogoOptions(null)),
            this.pluginsPath);
        catalog.LoadPlugins();

        var plugins = catalog.GetAllPlugins().ToDictionary(p => p.Id);

        Assert.Multiple(
            () => Assert.Equal(new PluginUpdateCheck("npm", Package: "@opencode/cli"), plugins["opencode"].UpdateCheck),
            () => Assert.Equal(new PluginUpdateCheck("npm", Package: "@earendil-works/pi-coding-agent"), plugins["pi"].UpdateCheck),
            () => Assert.Equal(new PluginUpdateCheck("npm", Package: "@oh-my-pi/pi-coding-agent"), plugins["omp"].UpdateCheck),
            () => Assert.Equal(new PluginUpdateCheck("npm", Package: "@qwen-code/qwen-code"), plugins["qwen-code"].UpdateCheck),
            () => Assert.Equal(new PluginUpdateCheck("githubReleases", Repo: "CognitionAI/devin-cli"), plugins["devin"].UpdateCheck));
    }

    private static void AssertCommandSet(Plugin plugin, AgentCommandKind[] expectedKinds)
    {
        foreach (var kind in expectedKinds)
        {
            Assert.True(plugin.Commands.Get(kind) is not null, $"{plugin.Id} is missing the {kind} command");
        }
    }
}
