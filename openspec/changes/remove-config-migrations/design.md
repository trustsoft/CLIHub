## Context

See proposal.md - Why. The configuration migration subsystem (`ConfigMigrationRunner`, two migrations, schema version tracking) was built to handle config.json shape changes. JSON deserialization already provides the same compatibility (missing fields default, unknown fields ignored) without the migration layer. The two legacy fields (`terminalExecutable`, `showWindowOnStartup`) exist only to support migrations and have no current function.

## Verified Current State

All file references were verified on disk at commit 2703f94:

- `src/CLIHub.Core/Configuration/Migrations/` contains exactly 6 files: `ConfigMigrationRunner.cs`, `IConfigMigration.cs`, `IConfigMigrationRunner.cs`, `ConfigurationMigrationResult.cs`, `LegacyTerminalPreferenceMigration.cs`, `StartMinimizedMigration.cs`
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:68-70` registers two `IConfigMigration` singletons (`LegacyTerminalPreferenceMigration`, `StartMinimizedMigration`) and one `IConfigMigrationRunner` (`ConfigMigrationRunner`)
- `src/CLIHub.Core/Configuration/ConfigurationRepository.cs:18,39-45,107-130` — constructor takes `IConfigMigrationRunner? migrationRunner`, stores it at line 18, defaults it at line 45; `EnsureLoaded` (lines 107-130) validates `schemaVersion` against `LegacySchemaVersion`/`CurrentSchemaVersion`, calls `this.migrationRunner.Migrate`, and schedules a rewrite when `migrated.SchemaVersion != schemaVersion`
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:13,18,23,58` — `SchemaVersion` property at line 13, `CurrentSchemaVersion = 2` constant at line 18, `LegacySchemaVersion = 0` constant at line 23, `FromSnapshot` sets `SchemaVersion = CurrentSchemaVersion` at line 58
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:121,133,154,168,197,210` — `PreferencesDocument.TerminalExecutable` at line 121 (default `"wt.exe"`), `ShowWindowOnStartup` at line 133 (default `true`), both mapped in `From` at lines 154 and 168 (the latter derived as `!preferences.StartMinimized`), and both mapped in `ToPreferences` at lines 197 and 210
- `src/CLIHub.Core/Models/Configuration/AppConfig.cs:44,109` — `AppPreferences.TerminalExecutable` at line 44 (XML comment "Legacy path to the terminal executable. Superseded by DefaultRuntime; kept for backward compatibility..."), `ShowWindowOnStartup` at line 109 (XML comment "Legacy startup-visibility preference. Superseded by StartMinimized...")
- `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:65,77` — `ClonePreferences` maps `TerminalExecutable` at line 65 and `ShowWindowOnStartup` at line 77
- `src/CLIHub/Settings/SettingsApplicationService.cs:573` — `preferences.ShowWindowOnStartup = !draft.StartMinimized;`
- `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:30-31,64-103` — class XML summary mentions "resolves legacy terminal-executable values" at lines 30-31; `TryParse` at lines 64-103 accepts canonical tokens (`ps`, `ps7`, `cmd`, `bash`) plus executable/alias strings (`powershell`, `powershell.exe`, `pwsh`, `pwsh.exe`, `cmd.exe`, `command prompt`, `bash.exe`, `git bash`, etc.)
- `tests/CLIHub.Core.Tests/Configuration/Migrations/` contains 3 test files: `ConfigMigrationRunnerTests.cs`, `LegacyTerminalPreferenceMigrationTests.cs`, `StartMinimizedMigrationTests.cs`
- `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs` has migration-related assertions in `Read_LegacyDocumentWithoutSchemaVersion_ReadsExistingValues` (line not specified but test exists), unknown/invalid-schema-version tests, and `Update_Flush_WritesConfigFile` asserting `schemaVersion`
- `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs` has `schemaVersion` assertions and `TerminalExecutable` round-trip coverage
- `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs` includes `TerminalExecutable` and `ShowWindowOnStartup` in covered-property census lists, `CreateFullConfig`, and `AssertPreferencesPropertiesMatch`
- `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs:76` asserts `IConfigMigrationRunner.HasMigrations`
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:126` and `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:433` initialize `ShowWindowOnStartup = true`
- `tests/CLIHub.Core.Tests/Models/Processes/RuntimeKindsTests.cs` covers only `Parse` fallback and canonical `ToToken` tokens; alias table is exercised only incidentally through `LegacyTerminalPreferenceMigrationTests`
- `docs/architecture.md:48` references `migrations (Configuration/Migrations/)` and line 67 shows the subfolder example
- `docs/architecture/configuration.md:7` has the Migrations bullet describing `ConfigMigrationRunner` and the two migrations
- `docs/backlog.md:168-179` item "`RuntimeKinds.TryParse` alias table has no direct tests" references the deleted test file `LegacyTerminalPreferenceMigrationTests`
- `docs/images/tools/Capture-ReadmeImages.ps1:418,430,456` includes `terminalExecutable`, `showWindowOnStartup`, `schemaVersion` in the mock config
- `docs/adr/0002-single-configuration-document.md:19` decision bullet "Versioned schema with migrations" and consequence at line 30 "Every persisted field change ships with a migration"

## Goals / Non-Goals

**Goals:**
- Remove all migration code, interfaces, runner, tests, and DI registration
- Remove unused legacy fields `schemaVersion`, `terminalExecutable`, `showWindowOnStartup` from the persistence and domain models
- Simplify `ConfigurationRepository.EnsureLoaded` to a version-tolerant read (deserialize, convert, cache)
- Simplify `RuntimeKinds.TryParse` to canonical tokens only
- Document the decision and supersede ADR-0002's migration bullet
- Update tests to reflect the new contract (no assertions on removed fields, new assertions for canonical token parsing)

**Non-Goals:**
- Changing the config.json file location, camelCase property names, atomic write path, or flush behavior
- Changing `DefaultRuntime` semantics, `StartMinimized` semantics, or Settings UI behavior (beyond removing the legacy write)
- Touching plugin `schemaVersion` (unrelated plugin descriptor concept) or historical files under `openspec/changes/archive/`

## Decisions

### Decision 1: Version-tolerant read instead of migrations

**Chosen:** `ConfigurationRepository.EnsureLoaded` deserializes `AppConfigDocument`, converts to snapshot, caches it. No version validation, no migration runner, no version-triggered rewrite. JSON deserialization ignores unknown properties (`schemaVersion`, `terminalExecutable`, `showWindowOnStartup` become no-ops); missing current fields (`defaultRuntime`, `startMinimized`) get their defaults. The file is rewritten in the new shape on the next save (when the user changes a preference or project).

**Rationale:** JSON deserialization already provides the necessary compatibility. An old config.json with `schemaVersion: 0` or `terminalExecutable: "wt.exe"` loads successfully—those properties are ignored, `DefaultRuntime` defaults to PowerShell, and the application starts normally. This achieves the same result as the migrations without the code complexity.

**Alternatives considered:**
- Keep `schemaVersion` as inert metadata: Rejected—it has no function without migrations and clutters the document. JSON already handles unknown properties gracefully.
- Reject old config versions: Rejected—breaks existing installations and provides no benefit.

### Decision 2: Remove legacy fields from the domain model

**Chosen:** Remove `AppPreferences.TerminalExecutable` and `AppPreferences.ShowWindowOnStartup` (properties, XML docs, mappings in `AppConfigDocument`, `ConfigurationSnapshot.ClonePreferences`, and the derived write in `SettingsApplicationService`).

**Rationale:** These fields exist only to support migrations. Without migrations, they are unused clutter. `DefaultRuntime` and `StartMinimized` are the current equivalents and have always been the authoritative fields.

**Alternatives considered:**
- Keep them as no-op properties: Rejected—they occupy memory, require maintenance, and confuse future contributors.

### Decision 3: Simplify `RuntimeKinds.TryParse` to canonical tokens only

**Chosen:** Accept only `ps`, `ps7`, `cmd`, `bash` (case-insensitive, trimmed). Remove executable/alias parsing (`powershell.exe`, `command prompt`, `git bash`, etc.). Unknown/missing values still return false; `Parse` still falls back to PowerShell.

**Rationale:** The executable/alias table existed to read the legacy `terminalExecutable` field. Without that field, the aliases have no function. The canonical tokens are the only values ever written to `defaultRuntime`, so that's all that needs parsing.

**Alternatives considered:**
- Keep the alias table: Rejected—it's dead code. The config.json write path has always used canonical tokens; aliases were never written.

### Decision 4: Test updates reflect the new contract

**Chosen:** Delete `tests/CLIHub.Core.Tests/Configuration/Migrations/` folder. Update existing tests:
- `ConfigurationRepositoryTests`: keep "document without version reads known values"; replace unknown/invalid-schema-version tests with behavior matching the new contract (version/unknown properties ignored, known values load, source file not rewritten at load); remove `schemaVersion` assertion from write tests
- `AppConfigSerializationTests`: remove `schemaVersion` and `TerminalExecutable` coverage
- `PersistenceCloneRoundTripTests`: remove `TerminalExecutable` and `ShowWindowOnStartup` from census lists, `CreateFullConfig`, and `AssertPreferencesPropertiesMatch`
- `ServiceCollectionExtensionsTests`: remove `IConfigMigrationRunner.HasMigrations` assertion
- `SettingsNavigationTests`, `SettingsApplicationServiceTests`: remove `ShowWindowOnStartup = true` initializers
- `RuntimeKindsTests`: add direct coverage for canonical token parsing (case/whitespace, fallback), replacing the incidental coverage from deleted migration tests

**Rationale:** Deleted code needs no tests; updated code needs assertions matching the new contract. The canonical token parsing was previously untested directly (only exercised through migration tests), so this change closes that gap.

**Alternatives considered:** None—test updates are mandatory for correctness.

### Decision 5: Document the decision with a new ADR

**Chosen:** Write `docs/adr/0014-remove-configuration-migrations.md` (the next number after 0013). Context: the original migration layer, compatibility without migrations. Decision: version-tolerant read, removed migration layer. Consequences: no migration code, old configs load as-is, new persisted fields ship with defaults instead of migrations. Add a superseded-by note to ADR-0002 pointing to ADR-0014.

**Rationale:** ADR-0002's "Versioned schema with migrations" decision is now reversed. The ADR should document why the decision changed and what replaced it, without rewriting the historical record of why migrations were originally chosen.

**Alternatives considered:**
- Amend ADR-0002: Rejected—ADRs are immutable historical records. A new ADR that supersedes the old one is the standard pattern.

## Risks / Trade-offs

**[Risk] Old config.json values are silently dropped on next save** → Mitigation: This is intentional and safe. `schemaVersion`, `terminalExecutable`, and `showWindowOnStartup` have no current function; dropping them on save is correct behavior. Users on old configs are migrated to the new shape transparently (load their known values, save in the new shape).

**[Trade-off] No migration tracking** → Benefit: Simpler code, no version bookkeeping. Cost: No audit trail of what migrations ran. Given that only two migrations ever existed (both mapping legacy fields to current equivalents), this is acceptable.

**[Trade-off] Cannot distinguish "never had StartMinimized" from "StartMinimized defaulted"** → Impact: None. The default is `true`, and that's the correct value for users who never set a preference. The distinction has no product value.

## Open Questions

None. All decisions are explicit and verified against the current codebase.

## Deviations from Brief

None. All code locations match the brief's `file:line` references, and the user's two decisions (full cleanup, read old config as-is) are preserved exactly.
