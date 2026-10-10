## 1. Application boundary

- [x] 1.1 Replace the Settings ViewModel's direct Core checker dependency with `IUpdateWorkflow` and verify the solution composition resolves the shared singleton.
- [x] 1.2 Align the unregistered legacy MainWindow update-check dependency with the application workflow and verify it remains behaviorally unchanged.

## 2. Regression coverage

- [x] 2.1 Update Settings ViewModel fixtures to mock `IUpdateWorkflow` and verify Settings reports successful, failed, and cancelled checks through the existing status path.
- [x] 2.2 Add a shared-check regression proving a Settings request joins an active workflow check without creating a second underlying check, and verify the focused update presentation tests pass.

## 3. Verification

- [x] 3.1 Run `openspec validate --all` and `git diff --check`.
- [x] 3.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`.
- [x] 3.3 Run GitNexus `detect_changes` for the completed work and verify only the intended update boundary, tests, and OpenSpec artifacts are affected.
