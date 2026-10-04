## 1. Constructor and test seam cleanup

- [x] 1.1 Remove the legacy aggregate-process constructor from `AgentCommandService` and update command service tests to pass the fake through the two narrow process contracts; verify agent command tests pass.
- [x] 1.2 Remove compatibility constructors from `AgentVersionService`, `AgentDetectionService`, and `ProjectService`; update tests to construct `PreferencesStore` and `ProjectStateStore` explicitly; verify focused tests pass.
- [x] 1.3 Replace `UpdateService` and `ReleaseNotesService` test-only constructors with named internal factories; verify update and release-note tests pass.

## 2. Composition regression coverage

- [x] 2.1 Expand `ServiceCollectionExtensionsTests` to resolve every Core service used during startup, including all agent services and process contracts; verify no constructor ambiguity occurs.
- [x] 2.2 Run the Debug executable through startup initialization and verify it remains running without an unhandled DI exception; stop it after the smoke check.

## 3. Documentation and final verification

- [x] 3.1 Document the one-public-production-constructor rule and named test-factory convention in the Core architecture guidance; verify the documented paths exist.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln -c Release`, and `openspec validate "remove-service-constructor-ambiguity"`; record the results.
