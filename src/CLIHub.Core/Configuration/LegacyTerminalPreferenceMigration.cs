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
    public int TargetVersion => 1;

    /// <inheritdoc />
    public ConfigurationSnapshot Migrate(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var migrated = ConfigurationSnapshot.From(snapshot.ToAppConfig());
        // Only a Command Prompt executable maps to its own runtime; Windows Terminal (wt.exe),
        // PowerShell, and unknown values all resolve to the default PowerShell.
        migrated.Preferences.DefaultRuntime =
            RuntimeKinds.TryParse(migrated.Preferences.TerminalExecutable, out var runtime)
            && runtime == RuntimeKind.CommandPrompt
                ? RuntimeKind.CommandPrompt
                : RuntimeKind.PowerShell;

        return migrated;
    }
}
