## 1. Remove the migration code

- [x] 1.1 Delete `src/CLIHub.Core/Services/ConfigMigrator.cs` and verify `dotnet build CLIHub.sln` succeeds with no dangling `ConfigMigrator` reference in Core
- [x] 1.2 Remove the `new ConfigMigrator(fileSystem, paths).MigrateIfNeeded();` call in `src/CLIHub.App/App.xaml.cs` and verify the App project still builds
- [x] 1.3 Delete `tests/CLIHub.Core.Tests/ConfigMigratorTests.cs` and verify `dotnet test CLIHub.sln` passes with the migration tests gone

## 2. Update the documentation

- [x] 2.1 Remove the migration clause from the config-storage description in `docs/vision.md` and verify it no longer mentions `ConfigMigrator`
- [x] 2.2 Update the configuration bullet in `AGENTS.md` to drop the `ConfigMigrator` / `config.json.migrated` note and verify it reads coherently
- [x] 2.3 Update `docs/architecture-map.md` (app-config row and Configuration boundary) and verify no `ConfigMigrator` reference remains

## 3. Fold the model

- [x] 3.1 Remove `ConfigMigrator.cs` from the Configuration component boundary in the Scryer model and verify `scryer_get_health` no longer counts it
- [x] 3.2 Run `dotnet test --logger "junit;LogFilePath=TestResults/junit.xml"`, `scryer_ingest_test_report`, then fold `chg-2v8ksx` with `mark_implemented {change}`; verify `scryer_get_pending` is empty and `scryer_validate_model` is clean
- [x] 3.3 Run `scryer_get_drift`, record findings with `flag_drift`, and `reconcile_drift`; verify no drift remains in the Configuration scope

## 4. Close

- [x] 4.1 Archive `remove-config-migrator` and verify `openspec validate --specs` passes
