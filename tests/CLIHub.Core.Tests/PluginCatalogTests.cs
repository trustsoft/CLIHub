namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Plugins;

public class PluginCatalogTests : IDisposable
{
    private readonly string root;

    public PluginCatalogTests()
    {
        this.root = Path.Combine(Path.GetTempPath(), "clihub-plugins-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.root);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.root, recursive: true); } catch { }
    }

    private void WritePlugin(string dirName, string json)
    {
        var dir = Path.Combine(this.root, dirName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "plugin.json"), json);
    }

    private PluginCatalog CreateManager() =>
        new(NullLogger<PluginCatalog>.Instance, this.CreateCache(), this.root);

    private LogoCacheService CreateCache() =>
        new(NullLogger<LogoCacheService>.Instance, Path.Combine(this.root, "logos-state.json"));

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
        this.WritePlugin("opencode", ValidJson);
        var manager = this.CreateManager();

        manager.LoadPlugins();

        var plugin = Assert.Single(manager.GetAllPlugins());
        Assert.Equal("opencode", plugin.Id);
        Assert.NotNull(plugin.Commands.Launch);
        Assert.Contains(".opencode", plugin.Detection.ProjectIndicators);
    }

    [Fact]
    public void LoadPlugins_MissingLaunchCommand_IsSkipped()
    {
        this.WritePlugin("nolau", """
        { "id": "nolau", "name": "NoLa", "commands": { "version": { "executable": "x" } } }
        """);
        var manager = this.CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_MissingName_IsSkipped()
    {
        this.WritePlugin("noname", """
        { "id": "noname", "commands": { "launch": { "executable": "x" } } }
        """);
        var manager = this.CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_MalformedJson_IsSkipped()
    {
        this.WritePlugin("broken", "{ not valid json ");
        var manager = this.CreateManager();

        manager.LoadPlugins();

        Assert.Empty(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_DuplicateId_LoadsOnlyOne()
    {
        this.WritePlugin("first", ValidJson);
        this.WritePlugin("second", ValidJson);
        var manager = this.CreateManager();

        manager.LoadPlugins();

        Assert.Single(manager.GetAllPlugins());
    }

    [Fact]
    public void LoadPlugins_DirectoriesAreLoadedInDeterministicOrder()
    {
        this.WritePlugin("zeta", ValidJson.Replace("opencode", "zeta", StringComparison.Ordinal));
        this.WritePlugin("Alpha", ValidJson.Replace("opencode", "alpha", StringComparison.Ordinal));
        var manager = this.CreateManager();

        manager.LoadPlugins();

        Assert.Equal(["alpha", "zeta"], manager.GetAllPlugins().Select(plugin => plugin.Id));
    }

    [Fact]
    public void LoadPlugins_DuplicateId_UsesDeterministicWinnerAndLogsBothDirectories()
    {
        this.WritePlugin("second", ValidJson);
        this.WritePlugin("first", ValidJson);
        var logger = new RecordingLogger<PluginCatalog>();
        var manager = new PluginCatalog(logger, this.CreateCache(), this.root);

        manager.LoadPlugins();

        var plugin = Assert.Single(manager.GetAllPlugins());
        Assert.Equal(Path.Combine(this.root, "first"), plugin.PluginDirectory);
        Assert.Contains(
            logger.Messages,
            message => message.Contains("Duplicate plugin ID opencode", StringComparison.Ordinal)
                && message.Contains(Path.Combine(this.root, "second"), StringComparison.Ordinal)
                && message.Contains(Path.Combine(this.root, "first"), StringComparison.Ordinal));
    }

    [Fact]
    public void LoadPlugins_IsIdempotent()
    {
        this.WritePlugin("opencode", ValidJson);
        var manager = this.CreateManager();

        manager.LoadPlugins();
        manager.LoadPlugins();

        Assert.Single(manager.GetAllPlugins());
    }

    [Fact]
    public void ReloadPlugins_RescansSnapshotAndRaisesNotificationAfterReplacement()
    {
        this.WritePlugin("opencode", ValidJson);
        var catalog = this.CreateManager();
        catalog.LoadPlugins();
        var notificationCount = 0;
        var snapshots = new List<int>();
        catalog.PluginsChanged += (_, _) =>
        {
            notificationCount++;
            snapshots.Add(catalog.GetAllPlugins().Count);
        };

        this.WritePlugin("pi", ValidJson.Replace("opencode", "pi", StringComparison.Ordinal));
        catalog.ReloadPlugins();

        Directory.Delete(Path.Combine(this.root, "pi"), recursive: true);
        catalog.ReloadPlugins();

        Assert.Equal(2, notificationCount);
        Assert.Equal([2, 1], snapshots);
        Assert.Equal(["opencode"], catalog.GetAllPlugins().Select(plugin => plugin.Id));
    }

    [Fact]
    public void LoadPlugins_DoesNotRaiseReloadNotification()
    {
        this.WritePlugin("opencode", ValidJson);
        var catalog = this.CreateManager();
        var notificationCount = 0;
        catalog.PluginsChanged += (_, _) => notificationCount++;

        catalog.LoadPlugins();

        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public void LoadPlugins_SecondManager_SharesCache_ReusesCachedLogoWithoutRescan()
    {
        this.WritePlugin("opencode", ValidJson);
        var pluginDir = Path.Combine(this.root, "opencode");
        File.WriteAllBytes(Path.Combine(pluginDir, "logo.png"), [0x89, 0x50]);

        var cache = this.CreateCache();
        var first = new PluginCatalog(NullLogger<PluginCatalog>.Instance, cache, this.root);
        first.LoadPlugins();
        var cachedLogo = first.GetAllPlugins().Single().LogoPath;

        File.Delete(Path.Combine(pluginDir, "logo.png"));
        var second = new PluginCatalog(NullLogger<PluginCatalog>.Instance, cache, this.root);
        second.LoadPlugins();

        Assert.Equal(cachedLogo, second.GetAllPlugins().Single().LogoPath);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}

