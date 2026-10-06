## Why

Schema metadata now identifies legacy configuration, but migration behavior still has no explicit contract or composable execution layer. Without a migration runner, each future format change would be embedded in loading code, making ordering, idempotency, failure handling, and testability difficult to control.

## What Changes

- Add `IConfigMigration` and `ConfigMigrationRunner` contracts for ordered schema migrations.
- Define migration selection by source and target schema versions.
- Run migrations sequentially from a loaded legacy version to the current version.
- Require migrations to be deterministic and safe to run once; preserve the original document when a migration fails.
- Integrate the runner with configuration loading without implementing the specific terminal preference migration.
- Add tests for ordering, skipped/non-applicable migrations, idempotency expectations, failure behavior, and current-version no-op loads.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `configuration-snapshot`: Define the migration runner behavior between schema versions and its failure/non-destructive policy.

## Impact

- Affected Core configuration loading and persistence boundaries, including new migration contracts, runner, and `ConfigService` integration.
- Existing schema version 0/1 compatibility remains intact; the next change will register the concrete legacy terminal preference migration.
- No new config-file format beyond the existing `schemaVersion` field is introduced here.
- Tests and DI registration may change, but WPF behavior and user-facing configuration semantics remain unchanged until concrete migrations are added.
