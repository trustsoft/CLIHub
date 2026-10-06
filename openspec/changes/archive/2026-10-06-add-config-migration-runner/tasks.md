## 1. Define Migration Contracts

- [x] 1.1 Add `IConfigMigration` with explicit source/target schema versions and an in-memory transformation contract.
- [x] 1.2 Add `ConfigMigrationRunner` with contiguous ordering, duplicate/branch validation, current-version no-op, and failure semantics.

## 2. Integrate Configuration Loading

- [x] 2.1 Integrate the runner into `ConfigService` loading while preserving legacy/current and unknown-future behavior.
- [x] 2.2 Register the runner in Core composition with no concrete migrations until the next migration change.
- [x] 2.3 Preserve atomic writes and source-file non-destructive behavior on migration failures.

## 3. Verify Migration Behavior

- [x] 3.1 Add tests for ordering, current-version no-op, missing paths, duplicate/branch validation, migration exceptions, and no partial publication.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.4 Run `openspec validate add-config-migration-runner` and verify the delta spec.
