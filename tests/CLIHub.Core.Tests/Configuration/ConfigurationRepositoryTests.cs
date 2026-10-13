namespace CLIHub.Core.Tests.Configuration;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class ConfigurationRepositoryTests : IDisposable
{
    private readonly string tempDir;
    private readonly CapturingLogger logger = new();

    public ConfigurationRepositoryTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    private string ConfigPath => Path.Combine(this.tempDir, "config.json");

    private ConfigurationRepository CreateRepository(string? configPath = null) =>
        new(this.logger, configPath ?? this.ConfigPath);

    private static string ReadProjectId(string path)
    {
        var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("currentProjectId").GetString()!;
    }

    [Fact]
    public void Update_Flush_WritesConfigFile()
    {
        var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "abc");

        repository.Flush();

        Assert.True(File.Exists(this.ConfigPath));
        Assert.Contains("\"currentProjectId\": \"abc\"", File.ReadAllText(this.ConfigPath));
        Assert.Contains("\"schemaVersion\": 2", File.ReadAllText(this.ConfigPath));
    }

    [Fact]
    public void Read_LegacyDocumentWithoutSchemaVersion_ReadsExistingValues()
    {
        const string legacyJson = """
        {
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Alt+L" },
          "currentProjectId": null
        }
        """;
        File.WriteAllText(this.ConfigPath, legacyJson);

        using var repository = this.CreateRepository();

        Assert.Equal("Ctrl+Alt+L", repository.Read().Preferences.Hotkey);
    }

    [Fact]
    public void Read_UnknownSchemaVersion_UsesDefaultsWithoutOverwritingSource()
    {
        const string futureJson = """
        {
          "schemaVersion": 99,
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Alt+F" },
          "currentProjectId": "future-project"
        }
        """;
        File.WriteAllText(this.ConfigPath, futureJson);

        using var repository = this.CreateRepository();

        Assert.Null(repository.Read().CurrentProjectId);
        Assert.Equal(futureJson, File.ReadAllText(this.ConfigPath));
        Assert.Contains(this.logger.Warnings, warning => warning.Contains("Unsupported configuration schema version"));
    }

    [Fact]
    public void Read_InvalidSchemaVersion_UsesDefaultsWithoutOverwritingSource()
    {
        const string invalidJson = """
        {
          "schemaVersion": -1,
          "projects": [],
          "preferences": {},
          "currentProjectId": "invalid-project"
        }
        """;
        File.WriteAllText(this.ConfigPath, invalidJson);

        using var repository = this.CreateRepository();

        Assert.Null(repository.Read().CurrentProjectId);
        Assert.Equal(invalidJson, File.ReadAllText(this.ConfigPath));
        Assert.Contains(this.logger.Warnings, warning => warning.Contains("Unsupported configuration schema version"));
    }

    [Fact]
    public void Update_RapidConsecutiveUpdates_FlushPersistsLatest()
    {
        using var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "first");
        repository.Update(snapshot => snapshot.CurrentProjectId = "second");

        repository.Flush();

        var content = File.ReadAllText(this.ConfigPath);
        Assert.Contains("\"currentProjectId\": \"second\"", content);
        Assert.DoesNotContain("\"currentProjectId\": \"first\"", content);
    }

    [Fact]
    public async Task Update_ConcurrentUpdates_FollowedByKnownLatestUpdate_PersistsLatestJson()
    {
        using var repository = this.CreateRepository();
        var start = new Barrier(8);

        var saves = Enumerable.Range(0, 8)
            .Select(index => Task.Run(() =>
            {
                start.SignalAndWait();
                repository.Update(snapshot => snapshot.CurrentProjectId = $"concurrent-{index}");
            }))
            .ToArray();

        await Task.WhenAll(saves);
        repository.Update(snapshot => snapshot.CurrentProjectId = "known-latest");
        repository.Flush();

        Assert.Equal("known-latest", ReadProjectId(this.ConfigPath));
    }

    [Fact]
    public async Task Update_ConcurrentSections_PreservesBothChanges()
    {
        using var repository = this.CreateRepository();
        var start = new Barrier(2);

        var projectUpdate = Task.Run(() =>
        {
            start.SignalAndWait();
            repository.Update(snapshot => snapshot.CurrentProjectId = "project-1");
        });
        var preferencesUpdate = Task.Run(() =>
        {
            start.SignalAndWait();
            repository.Update(snapshot => snapshot.Preferences.Hotkey = "Ctrl+Alt+P");
        });

        await Task.WhenAll(projectUpdate, preferencesUpdate);
        repository.Flush();

        var snapshot = repository.Read();
        Assert.Equal("project-1", snapshot.CurrentProjectId);
        Assert.Equal("Ctrl+Alt+P", snapshot.Preferences.Hotkey);
    }

    [Fact]
    public void Update_CallbackFailure_DoesNotPublishPartialSnapshot()
    {
        using var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "before");

        Assert.Throws<InvalidOperationException>(() => repository.Update(snapshot =>
        {
            snapshot.CurrentProjectId = "partial";
            throw new InvalidOperationException("abort update");
        }));

        Assert.Equal("before", repository.Read().CurrentProjectId);
    }

    [Fact]
    public void Flush_ImmediatelyAfterUpdate_MakesLatestJsonDurable()
    {
        using var repository = this.CreateRepository();

        repository.Update(snapshot => snapshot.CurrentProjectId = "flush-now");
        repository.Flush();

        Assert.Equal("flush-now", ReadProjectId(this.ConfigPath));
    }

    [Fact]
    public void Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated()
    {
        using var repository = this.CreateRepository();
        repository.Update(snapshot =>
        {
            snapshot.CurrentProjectId = "project-1";
            snapshot.Projects.Add(new Project { Id = "project-1", Name = "Original", Path = "C:\\Original" });
            snapshot.Preferences.Hotkey = "Ctrl+Shift+A";
        });
        repository.Flush();

        var loaded = repository.Read();
        loaded.Projects[0].Name = "Mutated";
        loaded.Preferences.Hotkey = "Ctrl+Alt+M";

        var reread = repository.Read();

        Assert.Equal("Original", reread.Projects[0].Name);
        Assert.Equal("Ctrl+Shift+A", reread.Preferences.Hotkey);
    }

    [Fact]
    public void Update_WhileWorkerIsExiting_StartsReplacementWorkerAndPersistsValue()
    {
        using var repository = this.CreateRepository();
        using var workerExit = new ManualResetEventSlim();
        using var allowWorkerExit = new ManualResetEventSlim();

        repository.BeforeWorkerExitForTests = () =>
        {
            workerExit.Set();
            allowWorkerExit.Wait(TimeSpan.FromSeconds(5));
        };

        repository.Update(snapshot => snapshot.CurrentProjectId = "first-worker");
        Assert.True(workerExit.Wait(TimeSpan.FromSeconds(5)), "worker did not reach its exit barrier");

        repository.Update(snapshot => snapshot.CurrentProjectId = "second-worker");
        allowWorkerExit.Set();
        repository.Flush();

        Assert.Equal("second-worker", ReadProjectId(this.ConfigPath));
    }

    [Fact]
    public void Dispose_FlushesPendingWrites()
    {
        var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "pending");

        repository.Dispose();

        Assert.True(File.Exists(this.ConfigPath), "dispose must flush pending changes");
        Assert.Contains("\"currentProjectId\": \"pending\"", File.ReadAllText(this.ConfigPath));

        repository.Dispose();
    }

    [Fact]
    public void Update_FailedWrite_LogsWarningAndDoesNotThrow()
    {
        // The target file path is occupied by a directory, so the final rename must fail.
        Directory.CreateDirectory(this.ConfigPath);
        var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "abc");

        var exception = Record.Exception(repository.Flush);

        Assert.Null(exception);
        Assert.Contains(this.logger.Warnings, w => w.Contains("Could not write the configuration"));
    }

    [Fact]
    public void Update_WhenAtomicReplacementCannotCreateTempFile_PreservesPreviousValidJson()
    {
        using var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.CurrentProjectId = "baseline");
        repository.Flush();

        Directory.CreateDirectory(this.ConfigPath + ".tmp");

        repository.Update(snapshot => snapshot.CurrentProjectId = "replacement");
        repository.Flush();

        Assert.Equal("baseline", ReadProjectId(this.ConfigPath));
        Assert.Contains(this.logger.Warnings, w => w.Contains("Could not write the configuration"));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigurationRepository(null!, this.ConfigPath));
    }

    private sealed class CapturingLogger : ILogger<ConfigurationRepository>
    {
        public List<string> Warnings { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                this.Warnings.Add(formatter(state, exception));
            }
        }
    }
}
