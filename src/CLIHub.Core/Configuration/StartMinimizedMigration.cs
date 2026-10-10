namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Migrates the schema 1 window-on-startup preference to the schema 2 start-minimized preference.
/// </summary>
public sealed class StartMinimizedMigration : IConfigMigration
{
    /// <inheritdoc />
    public int SourceVersion => 1;

    /// <inheritdoc />
    public int TargetVersion => AppConfigDocument.CurrentSchemaVersion;

    /// <inheritdoc />
    public ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var migrated = ConfigurationSnapshot.From(snapshot.ToAppConfig());
        migrated.Preferences.StartMinimized = !migrated.Preferences.ShowWindowOnStartup;

        return migrated;
    }
}
