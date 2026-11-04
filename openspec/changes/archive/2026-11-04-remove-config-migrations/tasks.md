## Stage 1: Remove the migration subsystem

- [x] 1.1 Delete the entire `src/CLIHub.Core/Configuration/Migrations/` folder (6 files: `ConfigMigrationRunner.cs`, `IConfigMigration.cs`, `IConfigMigrationRunner.cs`, `ConfigurationMigrationResult.cs`, `LegacyTerminalPreferenceMigration.cs`, `StartMinimizedMigration.cs`).
- [x] 1.2 Delete the entire `tests/CLIHub.Core.Tests/Configuration/Migrations/` folder (3 test files).
- [x] 1.3 Remove the two `IConfigMigration` registrations and the `IConfigMigrationRunner` registration from `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` (keep `IConfigurationRepository` and `IPreferencesStore`).
- [x] 1.4 In `src/CLIHub.Core/Configuration/ConfigurationRepository.cs`: remove the `IConfigMigrationRunner` constructor parameter, the `migrationRunner` field, its default assignment, and the class XML summary's migration reference.
- [x] 1.5 In `ConfigurationRepository.EnsureLoaded`: remove schema-version validation, the `migrationRunner.Migrate` call, and the version-triggered rewrite. Keep the missing-file defaults path, the deserialize-to-snapshot path, and the corrupt-file defaults/logging path.
- [x] 1.6 In `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs`: keep the document-without-version test (rename it to a current-contract name if needed) and replace the unknown/invalid-schema-version tests with one test proving that a document carrying unknown metadata (`schemaVersion` 99, `terminalExecutable`, `showWindowOnStartup`) still loads its known current values and leaves the source file unchanged at load.
- [x] 1.7 In `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs`: remove the `IConfigMigrationRunner.HasMigrations` assertion.
- [x] 1.8 Gate: `dotnet build CLIHub.sln`.
- [x] 1.9 Gate: `dotnet test CLIHub.sln` (all tests green).

## Stage 2: Remove schema-version metadata

- [x] 2.1 Remove the `SchemaVersion` property and the `CurrentSchemaVersion`/`LegacySchemaVersion` constants from `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`.
- [x] 2.2 Remove the `SchemaVersion = CurrentSchemaVersion` assignment from `AppConfigDocument.FromSnapshot`, so new writes no longer emit `schemaVersion`.
- [x] 2.3 In `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs`: remove the `schemaVersion` assertion from `Update_Flush_WritesConfigFile`.
- [x] 2.4 In `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs`: delete `AppConfigDocument_NewWrites_IncludeCurrentSchemaVersion` and remove `schemaVersion` from JSON fixtures.
- [x] 2.5 Gate: `dotnet build CLIHub.sln`.
- [x] 2.6 Gate: `dotnet test CLIHub.sln` (all tests green).

## Stage 3: Remove legacy preference fields

- [x] 3.1 In `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`: remove `PreferencesDocument.TerminalExecutable` and `ShowWindowOnStartup` and their assignments in `From` and `ToPreferences` (including the derived `ShowWindowOnStartup = !preferences.StartMinimized` write).
- [x] 3.2 In `src/CLIHub.Core/Models/Configuration/AppConfig.cs`: remove `AppPreferences.TerminalExecutable` and `AppPreferences.ShowWindowOnStartup` with their XML docs.
- [x] 3.3 In `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs`: remove the `TerminalExecutable` and `ShowWindowOnStartup` assignments from `ClonePreferences`.
- [x] 3.4 In `src/CLIHub/Settings/SettingsApplicationService.cs`: remove the `preferences.ShowWindowOnStartup = !draft.StartMinimized;` assignment.
- [x] 3.5 In `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs`: remove `TerminalExecutable` initialization and round-trip assertions.
- [x] 3.6 In `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs`: remove `TerminalExecutable` and `ShowWindowOnStartup` from the covered-property census lists, `CreateFullConfig`, and `AssertPreferencesPropertiesMatch`.
- [x] 3.7 In `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` and `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs`: remove the `ShowWindowOnStartup = true` initializers.
- [x] 3.8 Gate: `dotnet build CLIHub.sln`.
- [x] 3.9 Gate: `dotnet test CLIHub.sln` (all tests green).

## Stage 4: Simplify runtime token parsing

- [x] 4.1 In `src/CLIHub.Core/Models/Processes/RuntimeKind.cs`: reduce `RuntimeKinds.TryParse` to the canonical tokens `ps`, `ps7`, `cmd`, `bash` (trimmed, case-insensitive); remove the executable/alias cases. Keep `Parse` falling back to `PowerShell` and `ToToken` unchanged.
- [x] 4.2 Update the `RuntimeKinds` class XML summary to remove "and resolves legacy terminal-executable values".
- [x] 4.3 In `tests/CLIHub.Core.Tests/Models/Processes/RuntimeKindsTests.cs`: add direct coverage for canonical token parsing (case variants and surrounding whitespace), `TryParse` returning false with `out` = `PowerShell` for unknown/missing values, and the `TryParse(ToToken(kind), out var parsed)` identity for all four kinds.
- [x] 4.4 Gate: `dotnet build CLIHub.sln`.
- [x] 4.5 Gate: `dotnet test CLIHub.sln` (all tests green).

## Stage 5: Update documentation and the ADR record

- [x] 5.1 In `docs/architecture.md`: remove the `migrations (Configuration/Migrations/)` ownership reference and the `Configuration/Migrations/` subfolder example.
- [x] 5.2 In `docs/architecture/configuration.md`: remove the Migrations bullet and restate the Schema paragraph without `schemaVersion`, `terminalExecutable`, and `showWindowOnStartup`; keep the surviving preference list and the forward-compatibility sentence (unknown fields ignored, missing fields default).
- [x] 5.3 In `docs/backlog.md`: resolve the `RuntimeKinds.TryParse` alias-table item, which cites the deleted `LegacyTerminalPreferenceMigrationTests`; record that direct canonical-token coverage now exists (or mark the item resolved and remove the dead reference).
- [x] 5.4 In `docs/images/tools/Capture-ReadmeImages.ps1`: remove `terminalExecutable`, `showWindowOnStartup`, and `schemaVersion` from the mock `config.json` it writes.
- [x] 5.5 Write `docs/adr/0014-remove-configuration-migrations.md`: context (why migrations existed, compatibility available without them), decision (version-tolerant read, no migration layer, canonical runtime tokens), consequences (no migration bookkeeping; old documents load as-is; removed legacy values are ignored and dropped on the next save; future persisted fields ship with defaults instead of migrations). Status: Accepted — 2026-11-04. Supersedes the migration bullet in ADR-0002.
- [x] 5.6 In `docs/adr/0002-single-configuration-document.md`: add a superseded-by note pointing to ADR-0014 for the "Versioned schema with migrations" decision and the "Every persisted field change ships with a migration" consequence.
- [x] 5.7 Gate: `dotnet build CLIHub.sln`.
- [x] 5.8 Gate: `dotnet test CLIHub.sln` (all tests green).
- [x] 5.9 Gate: `openspec validate --all` (0 errors).

## Stage 6: Final verification

- [x] 6.1 Gate: `dotnet build CLIHub.sln`.
- [x] 6.2 Gate: `dotnet test CLIHub.sln` (all tests green).
- [x] 6.3 Gate: `openspec validate --all` (0 errors).
- [x] 6.4 Refresh the repository knowledge graph: `graphify update .`.

## Notes

- Every stage ends green: build and tests pass before the next stage starts.
- No stage relies on a later stage to repair compile or test failures.
- Stage 5 is documentation/tooling only and does not change runtime behavior.
- Stage 6 is the final verification gate before reporting completion.

## Verification

- Manual smoke (2026-11-04): user confirmed that an existing `config.json` carrying `schemaVersion`, `terminalExecutable`, and `showWindowOnStartup` loads its known values without being rewritten at startup, and that the next settings save drops the removed fields.
