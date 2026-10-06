namespace CLIHub.Core.Configuration;

/// <summary>
///   Applies registered configuration migrations to detached snapshots.
/// </summary>
public interface IConfigMigrationRunner
{
    /// <summary>
    ///   Indicates whether concrete migrations are registered.
    /// </summary>
    bool HasMigrations { get; }

    /// <summary>
    ///   Migrates a snapshot from its source schema version to the target version.
    /// </summary>
    /// <param name="sourceVersion"> The schema version read from the document. </param>
    /// <param name="snapshot"> The detached snapshot to migrate. </param>
    /// <param name="targetVersion"> The schema version required by the application. </param>
    /// <returns> The migrated snapshot and its resulting schema version. </returns>
    ConfigurationMigrationResult Migrate(
        int sourceVersion,
        ConfigurationSnapshot snapshot,
        int targetVersion);
}
