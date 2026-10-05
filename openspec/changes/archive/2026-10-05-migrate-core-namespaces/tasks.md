## 1. Baseline and migration inventory

- [x] 1.1 Record every `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` declaration and consumer, map it to the target namespace table in `design.md`, and verify no source file is left without an owner. Baseline inventory covered 40 Core declarations plus all host and test consumers.
- [x] 1.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test` before namespace edits, recording the baseline result for comparison. Baseline: build succeeded with 0 warnings and 0 errors; 338 tests passed.

## 2. Infrastructure namespace migration

- [x] 2.1 Move `AppPaths` and `DirectoryInitializer` to `CLIHub.Core.Infrastructure.FileSystem`, update all consumers, and verify the solution builds.
- [x] 2.2 Move `LogoCacheService` and `ILogoCacheService` to `CLIHub.Core.Infrastructure.Persistence`, update all consumers, and verify logo-cache tests pass. Result: 13 passed.
- [x] 2.3 Move process contracts, `ProcessLauncher`, and `WindowsCommandLineBuilder` to `CLIHub.Core.Infrastructure.Processes`, update all consumers, and verify process-launcher tests pass. Result: 10 passed.

## 3. Configuration and project namespace migration

- [x] 3.1 Move `ConfigService`, `IConfigService`, `PreferencesStore`, `IPreferencesStore`, and `CoreJson` to `CLIHub.Core.Configuration`, update all consumers, and verify configuration serialization tests pass. Result: configuration and serialization tests passed.
- [x] 3.2 Move project services, project-state contracts, and project policies to `CLIHub.Core.Projects`, update all consumers, and verify project service, path policy, and project-state store tests pass. Result: 27 focused configuration/project tests passed.

## 4. Plugin and agent namespace migration

- [x] 4.1 Move plugin manager and seeder implementations and contracts to `CLIHub.Core.Plugins`, update all consumers, and verify plugin manager and seeder tests pass.
- [x] 4.2 Move agent command, detection, version, availability, and contract types to `CLIHub.Core.Agents`, update all consumers, and verify command, detection, availability, and version tests pass. Result: 55 focused plugin/agent tests passed.

## 5. Update and Windows namespace migration

- [x] 5.1 Move update and release-notes implementations and contracts plus `UpdateControlLogic` to `CLIHub.Core.Updates`, update all consumers, and verify update and release-notes tests pass.
- [x] 5.2 Move `StartupService`, startup registry types, and `SingleInstanceGuard` to `CLIHub.Core.Infrastructure.Windows`, move the two legacy files out of `Core/Services/`, update all consumers, and verify startup and single-instance tests pass. Result: 45 focused update/Windows tests passed.

## 6. Composition, host, and test consumers

- [x] 6.1 Update `ServiceCollectionExtensions`, WPF application code, and all test imports to use subsystem namespaces while preserving the `CLIHub.Core.AddClIHubCoreServices` entry point, and verify the composition tests resolve every required service. Release build and composition tests pass.
- [x] 6.2 Search the full repository for obsolete `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` references, excluding only historical documentation where appropriate, and verify no active source consumer remains. No active `src/` or `tests/` source reference remains.

## 7. Documentation and final verification

- [x] 7.1 Update `docs/architecture.md` and `docs/repo-structure.md` with the final namespace ownership map, and verify every documented namespace maps to an existing source folder.
- [x] 7.2 Review public source-level API changes and document the old-namespace breaking change in the change artifacts and current developer documentation, then verify no compatibility alias was introduced.
- [x] 7.3 Run `dotnet build CLIHub.sln -c Release`, `dotnet test`, `openspec validate --specs --no-interactive`, and `openspec validate migrate-core-namespaces --no-interactive`; verify all checks pass with no behavior-test regressions. Build: 0 warnings/errors; tests: 338 passed; specs: 20 passed; change: valid.
