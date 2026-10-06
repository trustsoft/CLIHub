namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Migrates the legacy terminal executable preference to the runtime token.
/// </summary>
public sealed class LegacyTerminalPreferenceMigration : IConfigMigration
{
    /// <inheritdoc />
    public int SourceVersion => AppConfigDocument.LegacySchemaVersion;

    /// <inheritdoc />
    public int TargetVersion => AppConfigDocument.CurrentSchemaVersion;

    /// <inheritdoc />
    public ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var migrated = ConfigurationSnapshot.From(snapshot.ToAppConfig());
        migrated.Preferences.DefaultRuntime = RuntimeKinds.TryParse(
            migrated.Preferences.TerminalExecutable,
            out var runtime)
            ? RuntimeKinds.ToToken(runtime)
            : RuntimeKinds.WindowsTerminalToken;

        return migrated;
    }
}
