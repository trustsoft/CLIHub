namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class ConfigMigrationRunnerTests
{
    [Fact]
    public void Migrate_CurrentVersion_IsNoOp()
    {
        var migration = new RecordingMigration(1, 2);
        var runner = new ConfigMigrationRunner([migration]);
        var snapshot = new ConfigurationSnapshot { CurrentProjectId = "current" };

        var result = runner.Migrate(2, snapshot, 2);

        Assert.Equal(2, result.SchemaVersion);
        Assert.Equal("current", result.Snapshot.CurrentProjectId);
        Assert.Empty(migration.Calls);
    }

    [Fact]
    public void Migrate_AppliesMigrationsInAscendingOrder()
    {
        var calls = new List<int>();
        var runner = new ConfigMigrationRunner(
        [
            new RecordingMigration(1, 2, calls),
            new RecordingMigration(0, 1, calls)
        ]);

        var result = runner.Migrate(0, new ConfigurationSnapshot(), 2);

        Assert.Equal([0, 1], calls);
        Assert.Equal(2, result.SchemaVersion);
    }

    [Fact]
    public void Migrate_MissingPath_Throws()
    {
        var runner = new ConfigMigrationRunner([new RecordingMigration(1, 2)]);

        Assert.Throws<InvalidOperationException>(() =>
            runner.Migrate(0, new ConfigurationSnapshot(), 2));
    }

    [Fact]
    public void Constructor_DuplicateSourceVersions_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ConfigMigrationRunner(
        [
            new RecordingMigration(0, 1),
            new RecordingMigration(0, 1)
        ]));
    }

    [Fact]
    public void ConfigurationRepository_MigrationFailure_LeavesSourceDocumentUnchanged()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clihub-migration-{Guid.NewGuid():N}.json");
        const string original = """
        {
          "schemaVersion": 0,
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Alt+M" },
          "currentProjectId": "legacy"
        }
        """;
        File.WriteAllText(path, original);

        try
        {
            var runner = new ConfigMigrationRunner([new ThrowingMigration(0, 1)]);
            using var repository = new ConfigurationRepository(
                NullLogger<ConfigurationRepository>.Instance,
                path,
                runner);

            Assert.Null(repository.Read().CurrentProjectId);
            Assert.Equal(original, File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class RecordingMigration : IConfigMigration
    {
        private readonly List<int>? _calls;

        public RecordingMigration(int sourceVersion, int targetVersion, List<int>? calls = null)
        {
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            _calls = calls;
        }

        public int SourceVersion { get; }
        public int TargetVersion { get; }
        public List<int> Calls { get; } = new();

        public ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot)
        {
            Calls.Add(SourceVersion);
            _calls?.Add(SourceVersion);
            return snapshot;
        }
    }

    private sealed class ThrowingMigration(int sourceVersion, int targetVersion) : IConfigMigration
    {
        public int SourceVersion { get; } = sourceVersion;
        public int TargetVersion { get; } = targetVersion;

        public ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot)
        {
            snapshot.CurrentProjectId = "partial";
            throw new InvalidOperationException("migration failed");
        }
    }
}
