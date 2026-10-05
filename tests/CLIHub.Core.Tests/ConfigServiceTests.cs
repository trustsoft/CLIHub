namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CapturingLogger _logger = new();

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "clihub-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string ConfigPath => Path.Combine(_tempDir, "config.json");

    private ConfigService CreateService(string? configPath = null) =>
        new(_logger, configPath ?? ConfigPath);

    private static ConfigurationSnapshot Config(string projectId) => new()
    {
        CurrentProjectId = projectId
    };

    private static string ReadProjectId(string path)
    {
        var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("currentProjectId").GetString()!;
    }

    [Fact]
    public void Save_Flush_WritesConfigFile()
    {
        var service = CreateService();
        var config = service.Load();
        config.CurrentProjectId = "abc";
        service.Save(config);

        service.Flush();

        Assert.True(File.Exists(ConfigPath));
        Assert.Contains("\"currentProjectId\": \"abc\"", File.ReadAllText(ConfigPath));
        Assert.Contains("\"schemaVersion\": 1", File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Load_LegacyDocumentWithoutSchemaVersion_ReadsExistingValues()
    {
        const string legacyJson = """
        {
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Alt+L" },
          "currentProjectId": null
        }
        """;
        File.WriteAllText(ConfigPath, legacyJson);

        using var service = CreateService();

        Assert.Equal("Ctrl+Alt+L", service.Load().Preferences.Hotkey);
    }

    [Fact]
    public void Load_UnknownSchemaVersion_UsesDefaultsWithoutOverwritingSource()
    {
        const string futureJson = """
        {
          "schemaVersion": 99,
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Alt+F" },
          "currentProjectId": "future-project"
        }
        """;
        File.WriteAllText(ConfigPath, futureJson);

        using var service = CreateService();

        Assert.Null(service.Load().CurrentProjectId);
        Assert.Equal(futureJson, File.ReadAllText(ConfigPath));
        Assert.Contains(_logger.Warnings, warning => warning.Contains("Unsupported configuration schema version"));
    }

    [Fact]
    public void Load_InvalidSchemaVersion_UsesDefaultsWithoutOverwritingSource()
    {
        const string invalidJson = """
        {
          "schemaVersion": -1,
          "projects": [],
          "preferences": {},
          "currentProjectId": "invalid-project"
        }
        """;
        File.WriteAllText(ConfigPath, invalidJson);

        using var service = CreateService();

        Assert.Null(service.Load().CurrentProjectId);
        Assert.Equal(invalidJson, File.ReadAllText(ConfigPath));
        Assert.Contains(_logger.Warnings, warning => warning.Contains("Unsupported configuration schema version"));
    }

    [Fact]
    public void Save_RapidConsecutiveSaves_FlushPersistsLatest()
    {
        var service = CreateService();
        var first = service.Load();
        first.CurrentProjectId = "first";
        service.Save(first);

        var second = service.Load();
        second.CurrentProjectId = "second";
        service.Save(second);

        service.Flush();

        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("\"currentProjectId\": \"second\"", content);
        Assert.DoesNotContain("\"currentProjectId\": \"first\"", content);
    }

    [Fact]
    public async Task Save_ConcurrentSaves_FollowedByKnownLatestSave_PersistsLatestJson()
    {
        using var service = CreateService();
        var start = new Barrier(8);

        var saves = Enumerable.Range(0, 8)
            .Select(index => Task.Run(() =>
            {
                start.SignalAndWait();
                service.Save(Config($"concurrent-{index}"));
            }))
            .ToArray();

        await Task.WhenAll(saves);
        service.Save(Config("known-latest"));
        service.Flush();

        Assert.Equal("known-latest", ReadProjectId(ConfigPath));
    }

    [Fact]
    public void Flush_ImmediatelyAfterSave_MakesLatestJsonDurable()
    {
        using var service = CreateService();

        service.Save(Config("flush-now"));
        service.Flush();

        Assert.Equal("flush-now", ReadProjectId(ConfigPath));
    }

    [Fact]
    public void Load_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated()
    {
        using var service = CreateService();
        var initial = Config("project-1");
        initial.Projects.Add(new Project { Id = "project-1", Name = "Original", Path = "C:\\Original" });
        initial.Preferences.Hotkey = "Ctrl+Shift+A";
        service.Save(initial);
        service.Flush();

        var loaded = service.Load();
        loaded.Projects[0].Name = "Mutated";
        loaded.Preferences.Hotkey = "Ctrl+Alt+M";

        var reread = service.Load();

        Assert.Equal("Original", reread.Projects[0].Name);
        Assert.Equal("Ctrl+Shift+A", reread.Preferences.Hotkey);
    }

    [Fact]
    public void Save_WhileWorkerIsExiting_StartsReplacementWorkerAndPersistsValue()
    {
        using var service = CreateService();
        using var workerExit = new ManualResetEventSlim();
        using var allowWorkerExit = new ManualResetEventSlim();

        service.BeforeWorkerExitForTests = () =>
        {
            workerExit.Set();
            allowWorkerExit.Wait(TimeSpan.FromSeconds(5));
        };

        service.Save(Config("first-worker"));
        Assert.True(workerExit.Wait(TimeSpan.FromSeconds(5)), "worker did not reach its exit barrier");

        service.Save(Config("second-worker"));
        allowWorkerExit.Set();
        service.Flush();

        Assert.Equal("second-worker", ReadProjectId(ConfigPath));
    }

    [Fact]
    public void Dispose_FlushesPendingWrites()
    {
        var service = CreateService();
        var config = service.Load();
        config.CurrentProjectId = "pending";
        service.Save(config);

        service.Dispose();

        Assert.True(File.Exists(ConfigPath), "dispose must flush pending changes");
        Assert.Contains("\"currentProjectId\": \"pending\"", File.ReadAllText(ConfigPath));

        service.Dispose();
    }

    [Fact]
    public void Save_FailedWrite_LogsWarningAndDoesNotThrow()
    {
        // The target file path is occupied by a directory, so the final rename must fail.
        Directory.CreateDirectory(ConfigPath);
        var service = CreateService();

        var snapshot = service.Load();
        snapshot.CurrentProjectId = "abc";
        service.Save(snapshot);

        var exception = Record.Exception(service.Flush);

        Assert.Null(exception);
        Assert.Contains(_logger.Warnings, w => w.Contains("Could not write the configuration"));
    }

    [Fact]
    public void Save_WhenAtomicReplacementCannotCreateTempFile_PreservesPreviousValidJson()
    {
        using var service = CreateService();
        service.Save(Config("baseline"));
        service.Flush();

        Directory.CreateDirectory(ConfigPath + ".tmp");

        service.Save(Config("replacement"));
        service.Flush();

        Assert.Equal("baseline", ReadProjectId(ConfigPath));
        Assert.Contains(_logger.Warnings, w => w.Contains("Could not write the configuration"));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigService(null!, ConfigPath));
    }

    private sealed class CapturingLogger : ILogger<ConfigService>
    {
        public List<string> Warnings { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
