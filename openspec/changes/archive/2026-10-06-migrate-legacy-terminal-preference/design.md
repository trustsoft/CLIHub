## Context

`AppPreferences` contains the legacy `TerminalExecutable` value and the current `DefaultRuntime` token. `RuntimeKinds.TryParse` already knows how to recognize Windows Terminal, Command Prompt, and PowerShell executable forms. The migration runner transforms detached snapshots between schema versions and is registered through Core DI.

The migration must be the only place that translates the old field. Current schema documents must pass through unchanged, and the legacy field should remain available in the persistence model for compatibility until a later cleanup explicitly removes it.

## Goals / Non-Goals

**Goals:**

- Register one deterministic schema 0 → 1 migration.
- Translate supported legacy executable tokens through the canonical runtime parser.
- Use Windows Terminal (`wt`) as the fallback for missing or unrecognized legacy values.
- Preserve every unrelated snapshot field and keep migration independent of file I/O.
- Verify the migration is not applied to current-version documents.

**Non-Goals:**

- Remove `TerminalExecutable` from the model or JSON document.
- Change `RuntimeKinds` parsing rules beyond what is required by existing behavior.
- Add another schema version or migration runner abstraction.
- Change unknown-future-version handling.

## Decisions

- Add a sealed `IConfigMigration` implementation with `SourceVersion = 0` and `TargetVersion = 1`.
- In `Migrate`, copy the incoming detached snapshot, parse `Preferences.TerminalExecutable` through `RuntimeKinds.TryParse`, and write the corresponding token to `Preferences.DefaultRuntime`.
- If the legacy executable is null, empty, or unknown, write `RuntimeKinds.WindowsTerminalToken`.
- Keep `TerminalExecutable` unchanged in the migrated snapshot so the existing persisted document remains compatible and the later cleanup can make removal deliberately.
- Register the migration as `IConfigMigration` in Core DI before `ConfigMigrationRunner` is constructed. The runner then receives the concrete migration collection automatically.
- Keep migration exceptions non-destructive through the existing runner/ConfigService failure path.

## Risks / Trade-offs

- **Legacy values contain arbitrary paths** -> Parse executable names by the existing canonical mapping and fall back safely when unknown.
- **Migration accidentally changes current settings** -> Runner invokes it only when source version is 0; add a current-version no-op integration test.
- **Legacy field remains duplicated** -> Keep it intentionally for compatibility and defer removal to a separately scoped change.

## Migration Plan

1. Add the schema 0 → 1 terminal preference migration.
2. Register it in Core composition.
3. Add unit and ConfigService integration tests for mappings, fallback, preservation, and current-version no-op behavior.
4. Run full build/test, validate, sync the durable spec, and archive.
5. Rollback consists of removing the migration registration/type; existing documents remain readable through the prior schema-version fallback.

## Open Questions

None.
