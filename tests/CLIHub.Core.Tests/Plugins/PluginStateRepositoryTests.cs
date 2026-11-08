namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

using Microsoft.Extensions.Logging.Abstractions;

public class PluginStateRepositoryTests : IDisposable
{
    private readonly string stateFilePath;

    public PluginStateRepositoryTests()
    {
        this.stateFilePath = Path.Combine(
            Path.GetTempPath(),
            "clihub-plugin-state-" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        try { File.Delete(this.stateFilePath); } catch { }
        try { File.Delete(this.stateFilePath + ".tmp"); } catch { }
    }

    private PluginStateRepository CreateRepository() =>
        new(NullLogger<PluginStateRepository>.Instance, this.stateFilePath);

    [Fact]
    public void Load_MissingStateFile_ReturnsEmptyState()
    {
        var document = this.CreateRepository().Load();

        Assert.Equal(1, document.SchemaVersion);
        Assert.Empty(document.Plugins);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsEmptyStateAndLogsWarning()
    {
        File.WriteAllText(this.stateFilePath, "{ not valid json");
        var logger = new RecordingLogger();
        var repository = new PluginStateRepository(logger, this.stateFilePath);

        var document = repository.Load();

        Assert.Empty(document.Plugins);
        Assert.Contains(logger.Messages, message => message.Contains("Failed to load plugin state", StringComparison.Ordinal));
    }

    [Fact]
    public void Save_WritesAtomicallyViaTempFileThenRename()
    {
        var repository = this.CreateRepository();
        var document = PluginStateDocument.Empty();
        document.Plugins["opencode"] = new PluginStateEntry(PluginOrigin.Builtin)
        {
            ContentHash = "abc123",
            LogoHash = "def456"
        };

        repository.Save(document);

        Assert.True(File.Exists(this.stateFilePath), "the state file must exist after save");
        Assert.False(File.Exists(this.stateFilePath + ".tmp"), "the temp file must be renamed away, not left behind");
        Assert.DoesNotContain("tmp", File.ReadAllText(this.stateFilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveCreatesMissingParentDirectory()
    {
        var nestedPath = Path.Combine(
            Path.GetTempPath(),
            "clihub-plugin-state-nested-" + Guid.NewGuid().ToString("N"),
            "plugin-state.json");
        try
        {
            var repository = new PluginStateRepository(NullLogger<PluginStateRepository>.Instance, nestedPath);

            repository.Save(PluginStateDocument.Empty());

            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(nestedPath)!, recursive: true); } catch { }
        }
    }

    [Fact]
    public void SaveThenLoad_RoundTripPreservesData()
    {
        var repository = this.CreateRepository();
        var document = PluginStateDocument.Empty();
        document.Plugins["opencode"] = new PluginStateEntry(PluginOrigin.Builtin)
        {
            ContentHash = "cafe01",
            LogoHash = null,
            Disabled = true
        };
        document.Plugins["community"] = new PluginStateEntry(PluginOrigin.Installed)
        {
            ContentHash = "cafe02",
            LogoHash = "cafe03",
            InstalledVersion = "1.2.3"
        };

        repository.Save(document);
        var loaded = repository.Load();

        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal(2, loaded.Plugins.Count);

        var builtin = loaded.Plugins["opencode"];
        Assert.Equal(PluginOrigin.Builtin, builtin.Origin);
        Assert.Equal("cafe01", builtin.ContentHash);
        Assert.Null(builtin.LogoHash);
        Assert.Null(builtin.InstalledVersion);
        Assert.True(builtin.Disabled);

        var installed = loaded.Plugins["community"];
        Assert.Equal(PluginOrigin.Installed, installed.Origin);
        Assert.Equal("cafe02", installed.ContentHash);
        Assert.Equal("cafe03", installed.LogoHash);
        Assert.Equal("1.2.3", installed.InstalledVersion);
        Assert.False(installed.Disabled);
    }

    [Fact]
    public void Load_UnknownOriginEntryWithoutOriginProperty_FallsBackToUnknown()
    {
        // A state file written before the origin was recorded (or trimmed by a user) must
        // degrade to unknown rather than fail the load.
        File.WriteAllText(this.stateFilePath, """
        {
          "schemaVersion": 1,
          "plugins": {
            "opencode": { "contentHash": "cafe01" }
          }
        }
        """);

        var document = this.CreateRepository().Load();

        var entry = Assert.Single(document.Plugins.Values);
        Assert.Equal(PluginOrigin.Unknown, entry.Origin);
        Assert.Equal("cafe01", entry.ContentHash);
    }

    private sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger<PluginStateRepository>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.Messages.Add(formatter(state, exception));
        }

        private sealed class NoScope : IDisposable
        {
            public static NoScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
