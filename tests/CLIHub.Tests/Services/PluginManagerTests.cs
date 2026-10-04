namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Services;

public class PluginManagerTests : IDisposable
{
    private readonly string _root;

    public PluginManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "clihub-plugins-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private void WritePlugin(string dirName, string json)
    {
        var dir = Path.Combine(_root, dirName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "plugin.json"), json);
    }

    private PluginManager CreateManager() =>
        new(NullLogger<PluginManager>.Instance, CreateCache(), _root);

    private LogoCacheService CreateCache() =>
        new(NullLogger<LogoCacheService>.Instance, Path.Combine(_root, "logos-state.json"));

    private const string ValidJson = """
    {
      "id": "opencode",
      "name": "OpenCode",
      "commands": { "launch": { "executable": "opencode" } },
      "detection": { "systemPaths": ["%USERPROFILE%\\.opencode"], "projectIndicators": [".opencode"] }
    }
    """;

    [Fact]
    public void LoadPlugins_ValidPlugin_IsLoaded()
    {
        WritePlugin("opencode", ValidJson);
        var manager = CreateManager();

        manager.LoadPlugins();

        var plugin = Assert.Single(manager.GetAllPlugins());
        Assert.Equal("opencode", plugin.Id);
        Assert.NotNull(plugin.Commands.Launch);
        Assert.Contains(".opencode", plugin.Detection.ProjectIndicators);
    }

    [Fact]
    public void LoadPlugins_MissingLaunchCommand_IsSkipped()
    {
        WritePlugin("nolau", """
        { "id": "nolau", "name": "NoLa", "commands": { "version": { "executable": "x" } } }
        """);
        var manager = CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_MissingName_IsSkipped()
    {
        WritePlugin("noname", """
        { "id": "noname", "commands": { "launch": { "executable": "x" } } }
        """);
        var manager = CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_MalformedJson_IsSkipped()
    {
        WritePlugin("broken", "{ not valid json ");
        var manager = CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_DuplicateId_LoadsOnlyOne()
    {
        WritePlugin("first", ValidJson);
        WritePlugin("second", ValidJson);
        var manager = CreateManager();

        manager.LoadPlugins();

        Assert.Single(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_IsIdempotent()
    {
        WritePlugin("opencode", ValidJson);
        var manager = CreateManager();

        manager.LoadPlugins();
        manager.LoadPlugins();

        Assert.Single(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_SecondManager_SharesCache_ReusesCachedLogoWithoutRescan()
    {
        WritePlugin("opencode", ValidJson);
        var pluginDir = Path.Combine(_root, "opencode");
        File.WriteAllBytes(Path.Combine(pluginDir, "logo.png"), [0x89, 0x50]);

        var cache = CreateCache();
        var first = new PluginManager(NullLogger<PluginManager>.Instance, cache, _root);
        first.LoadPlugins();
        var cachedLogo = first.GetAllPlugins().Single().LogoPath;

        File.Delete(Path.Combine(pluginDir, "logo.png"));
        var second = new PluginManager(NullLogger<PluginManager>.Instance, cache, _root);
        second.LoadPlugins();

        Assert.Equal(cachedLogo, second.GetAllPlugins().Single().LogoPath);
    }
}
