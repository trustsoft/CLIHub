namespace CLIHub.Core.Configuration;

/// <summary>
///   Applies a validated contiguous chain of configuration migrations.
/// </summary>
public sealed class ConfigMigrationRunner : IConfigMigrationRunner
{
    private readonly IReadOnlyDictionary<int, IConfigMigration> _migrations;

    /// <inheritdoc />
    public bool HasMigrations => _migrations.Count > 0;

    /// <summary>
    ///   Creates a runner from registered migrations.
    /// </summary>
    /// <param name="migrations"> The migrations to validate and apply. </param>
    public ConfigMigrationRunner(IEnumerable<IConfigMigration>? migrations = null)
    {
        var registered = (migrations ?? Array.Empty<IConfigMigration>()).ToList();

        if (registered.Any(m => m.TargetVersion != m.SourceVersion + 1))
        {
            throw new ArgumentException("Configuration migrations must advance exactly one schema version.", nameof(migrations));
        }

        if (registered.GroupBy(m => m.SourceVersion).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Configuration migrations cannot have duplicate source versions.", nameof(migrations));
        }

        _migrations = registered.ToDictionary(m => m.SourceVersion);
    }

    /// <inheritdoc />
    public ConfigurationMigrationResult Migrate(
        int sourceVersion,
        ConfigurationSnapshot snapshot,
        int targetVersion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (sourceVersion == targetVersion)
        {
            return new ConfigurationMigrationResult(ConfigurationSnapshot.From(snapshot.ToAppConfig()), targetVersion);
        }

        if (sourceVersion > targetVersion)
        {
            throw new InvalidOperationException($"Configuration schema version {sourceVersion} is newer than target version {targetVersion}.");
        }

        var currentVersion = sourceVersion;
        var currentSnapshot = ConfigurationSnapshot.From(snapshot.ToAppConfig());

        while (currentVersion < targetVersion)
        {
            if (!_migrations.TryGetValue(currentVersion, out var migration))
            {
                throw new InvalidOperationException(
                    $"No configuration migration is registered from schema version {currentVersion}.");
            }

            var migrated = migration.Migrate(currentSnapshot);
            currentSnapshot = ConfigurationSnapshot.From((migrated ?? throw new InvalidOperationException("Configuration migration returned null.")).ToAppConfig());
            currentVersion = migration.TargetVersion;
        }

        return new ConfigurationMigrationResult(currentSnapshot, currentVersion);
    }
}
