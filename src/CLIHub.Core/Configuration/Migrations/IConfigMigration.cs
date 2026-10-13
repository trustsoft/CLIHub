namespace CLIHub.Core.Configuration;

/// <summary>
///   Transforms a detached configuration snapshot between adjacent schema versions.
/// </summary>
public interface IConfigMigration
{
    /// <summary>
    ///   The schema version accepted by this migration.
    /// </summary>
    int SourceVersion { get; }

    /// <summary>
    ///   The schema version produced by this migration.
    /// </summary>
    int TargetVersion { get; }

    /// <summary>
    ///   Transforms a detached configuration snapshot.
    /// </summary>
    /// <param name="snapshot"> The snapshot to transform. </param>
    /// <returns> The transformed detached snapshot. </returns>
    ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot);
}
