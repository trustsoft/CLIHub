## 1. Add Release Notes Startup Boundary

- [x] 1.1 Add `IReleaseNotesStartupCoordinator` and its implementation, preserving `ReleaseNotesPrompt` action mapping and warning-only error handling; verify Show, RecordOnly, Skip, and failure paths with focused tests.
- [x] 1.2 Register the coordinator in WPF composition and replace the inline `App.OnStartup` release-notes workflow using the already loaded preferences snapshot; verify DI resolution.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that `ReleaseNotesPrompt`, `ReleaseNotesLauncher`, `IReleaseNotesService`, and `IUpdateService` remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-release-notes-startup` and verify the change has no spec deltas.
