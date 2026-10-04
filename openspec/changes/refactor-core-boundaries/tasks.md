## 1. Baseline and governance

- [x] 1.1 Record the current Core dependency map and configuration ownership map in the change artifacts; verify the map against `ServiceCollectionExtensions`, service constructors, and the existing architecture documentation.
- [x] 1.2 Run `dotnet build CLIHub.sln` and `dotnet test` from the repository root and record the passing baseline before source changes.
- [x] 1.3 Check `deviations.md` before each implementation wave and record any approved departure from `design.md` before applying it.

## 2. Configuration boundaries

- [x] 2.1 Add compatibility tests for the existing `config.json` shape and verify load/save behavior remains unchanged.
- [x] 2.2 Introduce `AppConfigDocument` as the persistence representation without changing the serialized JSON format; verify configuration serialization tests pass.
- [x] 2.3 Introduce `ProjectState` ownership for `Projects` and `CurrentProjectId`; verify `ProjectService` remains the only mutation owner.
- [x] 2.4 Introduce narrow configuration access contracts or snapshots for project state and preferences while keeping one `config.json` and one atomic persistence path; verify all Core tests pass.
- [x] 2.5 Record ownership of each preference group and remove direct full-document access from services where a narrower contract is sufficient; verify the dependency review finds no unowned `AppConfig` mutations.

## 3. Subsystem structure

- [x] 3.1 Create the target Core subsystem directories and namespaces described in `design.md`; verify Core still builds with no WPF dependency.
- [x] 3.2 Move configuration, project, plugin, agent, update, and infrastructure files one subsystem at a time without changing behavior; verify `dotnet build CLIHub.sln` after each subsystem move.
- [x] 3.3 Move or group interfaces according to subsystem ownership while preserving public contracts; verify all Core tests compile and pass.
- [x] 3.4 Move `ServiceCollectionExtensions` into the composition area and preserve the existing `AddClIHubCoreServices` entry point; verify `ServiceCollectionExtensionsTests` passes.

## 4. Projects and configuration policies

- [x] 4.1 Extract project path normalization and equality policy from `ProjectService`; verify existing project path tests cover relative paths, separators, and case-insensitive equality.
- [x] 4.2 Extract project logo resolution from `ProjectService` while retaining logo cache behavior and fallback behavior; verify project and logo cache tests pass.
- [x] 4.3 Verify that project operations remain the only owners of `Projects` and `CurrentProjectId` mutations; verify with focused project service tests and code review of all `AppConfig` writes.

## 5. Plugins and agents

- [x] 5.1 Separate plugin discovery, validation, and seeding responsibilities from agent command and availability policies; verify plugin manager and seeder tests pass unchanged in behavior.
- [x] 5.2 Place `AgentListComposer` with the availability/query policy and keep it free of persistence and process-launching dependencies; verify agent availability tests pass.
- [x] 5.3 Review agent command, detection, and version services against the dependency rules; verify each service depends on contracts rather than concrete services.

## 6. Process and platform boundaries

- [x] 6.1 Separate interactive process launch policy from captured-output policy while preserving the current `IProcessLauncher` compatibility surface where required; verify `ProcessLauncherTests` and `AgentCommandServiceTests` pass.
- [x] 6.2 Group Windows-specific services and filesystem persistence under explicit infrastructure namespaces; verify Core has no WPF references and Windows integration tests remain green.
- [x] 6.3 Keep update and release-note integration isolated behind their existing interfaces; verify `UpdateServiceTests` and `ReleaseNotesServiceTests` pass.

## 7. Composition and documentation

- [x] 7.1 Split Core DI registration into subsystem registration groups while retaining one public composition entry point; verify the composition test resolves every required service.
- [x] 7.2 Add or update focused tests for newly extracted policies and seams; verify the new tests cover ownership boundaries rather than mirroring implementation details.
- [x] 7.3 Update `docs/architecture.md` and `docs/repo-structure.md` with the durable Core boundaries and the accurate “UI-independent, Windows-aware” description; verify links and paths refer to existing files.
- [x] 7.4 Perform a final dependency review against `design.md`; verify no UI dependency, unintended infrastructure-to-application dependency, unrecorded `AppConfig` ownership, or unapproved deviation remains.
- [ ] 7.5 Run `dotnet build CLIHub.sln`, `dotnet test`, and `openspec validate "refactor-core-boundaries"`; record the results and confirm all tasks are complete.
