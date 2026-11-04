## Why

The configuration migration subsystem was introduced to handle schema version transitions when the persisted config.json shape changed. Over time, this approach proved to be unnecessary overhead:

1. **Limited utility**: Only two migrations exist (schema 0→1 for `terminalExecutable` → `defaultRuntime`, and schema 1→2 for `showWindowOnStartup` → `startMinimized`), both mapping legacy fields to current equivalents.
2. **Compatibility without migrations**: JSON deserialization already handles missing fields (defaults) and ignores unknown fields (forward compatibility). A version-tolerant read achieves the same result without migration code.
3. **Maintenance burden**: Every persisted field change requires writing, testing, and maintaining a migration, even when the change is backward-compatible.
4. **Unused legacy fields**: `terminalExecutable` and `showWindowOnStartup` exist only to support migrations; they have no current function and clutter the domain model.

A version-tolerant read—where old config.json documents load their known fields, unknown/removed properties are ignored, and missing current fields get defaults—achieves the same compatibility without the migration layer.

## What Changes

**Removed:**
- The entire `src/CLIHub.Core/Configuration/Migrations/` folder (6 files: `ConfigMigrationRunner`, `IConfigMigration`, `IConfigMigrationRunner`, `ConfigurationMigrationResult`, `LegacyTerminalPreferenceMigration`, `StartMinimizedMigration`)
- DI registration for `IConfigMigration` (2 instances) and `IConfigMigrationRunner` in `ServiceCollectionExtensions`
- `IConfigMigrationRunner` constructor parameter, field, and version-triggered rewrite logic in `ConfigurationRepository`
- `SchemaVersion` property and both version constants (`CurrentSchemaVersion`, `LegacySchemaVersion`) from `AppConfigDocument`
- `TerminalExecutable` and `ShowWindowOnStartup` properties from `AppPreferences` and their mappings in `AppConfigDocument.PreferencesDocument`
- The derived `ShowWindowOnStartup = !StartMinimized` write in `AppConfigDocument.PreferencesDocument.From` and `SettingsApplicationService`
- Legacy executable/alias parsing (e.g., `powershell.exe`, `command prompt`, `git bash`) from `RuntimeKinds.TryParse` (only canonical tokens `ps`, `ps7`, `cmd`, `bash` remain)
- Tests for the migration subsystem: `tests/CLIHub.Core.Tests/Configuration/Migrations/` folder (3 test files)

**Modified:**
- `ConfigurationRepository.EnsureLoaded`: simplified to deserialize `AppConfigDocument`, convert to snapshot, cache it; removed version validation, migration runner calls, and version-triggered rewrite; kept missing-file and corrupt-file default paths
- `RuntimeKinds.TryParse`: simplified to accept only canonical tokens (`ps`, `ps7`, `cmd`, `bash`) plus case/whitespace tolerance; removed executable and alias tables
- Test updates to reflect the new contract (version/unknown properties ignored, known values load, source file not rewritten at load)

**Documentation:**
- `docs/architecture.md`: removed `migrations (Configuration/Migrations/)` reference
- `docs/architecture/configuration.md`: removed Migrations bullet; restated Schema paragraph without `schemaVersion`, `terminalExecutable`, `showWindowOnStartup`
- `docs/backlog.md`: resolved or updated the `RuntimeKinds.TryParse alias table has no direct tests` item
- `docs/images/tools/Capture-ReadmeImages.ps1`: removed `terminalExecutable`, `showWindowOnStartup`, `schemaVersion` from mock config
- New ADR `docs/adr/0014-remove-configuration-migrations.md` superseding the "Versioned schema with migrations" decision in ADR-0002

## Capabilities

### Modified Capabilities

- `configuration-snapshot`: version-tolerant read of old config documents (unknown/missing fields handled gracefully, no migration, no destructive rewrite)

## Impact

**Benefits:**
- Removes ~700 lines of migration code, tests, and legacy field handling
- Simplifies `ConfigurationRepository` loading path (no version checks, no migration orchestration)
- Eliminates the requirement that every persisted field change ships with a migration
- Cleans up the domain model (removes unused `terminalExecutable`, `showWindowOnStartup`)

**Compatibility:**
- An existing config.json with `schemaVersion` 0/1/2 (or missing) loads its known current fields; `schemaVersion`, `terminalExecutable`, and `showWindowOnStartup` are ignored as unknown properties
- Missing current fields (`DefaultRuntime`, `StartMinimized`) get their defaults
- Old values are dropped on the next save (when the document is rewritten with only current fields)
- No migration, no rejection based on schema version, no destructive rewrite at load

**Modified code:**
- `src/CLIHub.Core/Configuration/ConfigurationRepository.cs` (constructor, `EnsureLoaded`, XML summary)
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` (DI registration)
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` (remove `SchemaVersion`, `TerminalExecutable`, `ShowWindowOnStartup`, version constants)
- `src/CLIHub.Core/Models/Configuration/AppConfig.cs` (remove `AppPreferences.TerminalExecutable`, `AppPreferences.ShowWindowOnStartup`)
- `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` (remove legacy fields from `ClonePreferences`)
- `src/CLIHub.Core/Models/Processes/RuntimeKind.cs` (simplify `TryParse`, update class XML summary)
- `src/CLIHub/Settings/SettingsApplicationService.cs` (remove legacy write)
- Test updates and new test coverage for canonical token parsing

**No breaking changes** to the existing config.json read path for users on schema 0/1/2; the file is just read as-is without migration.

## Non-Goals

- Changing the config.json file location, camelCase property names, atomic write path, or flush behavior
- Changing `DefaultRuntime` or `StartMinimized` semantics
- Touching plugin `schemaVersion` (unrelated plugin descriptor concept)
- Modifying `PopupScreenPosition`, theme, accent, or other preference behaviors

## Dependencies

None. This change is self-contained.
