namespace CLIHub.Core.Configuration;

/// <summary>
///   Result of applying configuration migrations.
/// </summary>
/// <param name="Snapshot"> The migrated detached snapshot. </param>
/// <param name="SchemaVersion"> The resulting schema version. </param>
public sealed record ConfigurationMigrationResult(ConfigurationSnapshot Snapshot, int SchemaVersion);
