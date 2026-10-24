namespace CLIHub.Core.Tests.Projects;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Core.Tests.Fakes;

public class ProjectStateStoreTests : IDisposable
{
    private readonly string tempDir;
    private readonly CapturingLogger logger = new();

    public ProjectStateStoreTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-projstate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    private string ConfigPath => Path.Combine(this.tempDir, "config.json");

    private ConfigurationRepository CreateRepository() => new(this.logger, this.ConfigPath);

    [Fact]
    public void Update_UpdatesProjectState_AndPreservesPreferences()
    {
        var config = new AppConfig
        {
            Preferences = new AppPreferences { Hotkey = "Ctrl+Alt+P" }
        };
        var repository = new FakeConfigurationRepository(config);
        var store = new ProjectStateStore(repository);
        store.Update(state =>
        {
            state.CurrentProjectId = "project-1";
            state.Projects.Add(new Project
            {
                Id = "project-1",
                Name = "Project",
                Path = "C:\\Project"
            });
        });

        var saved = repository.Read();
        Assert.Equal("project-1", saved.CurrentProjectId);
        Assert.Single(saved.Projects);
        Assert.Equal("Ctrl+Alt+P", saved.Preferences.Hotkey);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Update_CallbackFailure_DoesNotPersistPartialProjectState()
    {
        var repository = new FakeConfigurationRepository();
        var store = new ProjectStateStore(repository);

        Assert.Throws<InvalidOperationException>(() => store.Update(state =>
        {
            state.CurrentProjectId = "project-1";
            throw new InvalidOperationException("cancel update");
        }));

        Assert.Null(store.Load().CurrentProjectId);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Load_MutatingReturnedProjects_DoesNotChangeStoredState()
    {
        var config = new AppConfig();
        config.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });
        var repository = new FakeConfigurationRepository(config);
        var store = new ProjectStateStore(repository);

        var state = store.Load();
        state.Projects[0].IsFavorite = true;

        Assert.False(store.Load().Projects.Single().IsFavorite);
    }

    [Fact]
    public void Update_MutatingCallbackProjectAfterCompletion_DoesNotChangeStoredState()
    {
        // The fake would mask this: FakeConfigurationRepository.Update re-copies the snapshot
        // after the callback, while the real ConfigurationRepository adopts the callback's
        // snapshot object directly as its cached state.
        using var repository = this.CreateRepository();
        repository.Update(snapshot => snapshot.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        }));
        var store = new ProjectStateStore(repository);
        Project? captured = null;

        store.Update(state => captured = state.Projects.Single());
        captured!.IsFavorite = true;

        Assert.False(repository.Read().Projects.Single().IsFavorite);
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
