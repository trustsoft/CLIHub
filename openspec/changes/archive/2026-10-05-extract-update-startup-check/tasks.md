## 1. Add Update Startup Boundary

- [x] 1.1 Add `IUpdateStartupCoordinator` and its implementation for enabled/disabled checks, available-version notification, and warning-only failures; verify all paths with focused tests.
- [x] 1.2 Register the coordinator in WPF composition and replace the inline `App.OnStartup` update check while preserving dispatcher invocation and fire-and-forget startup behavior; verify DI resolution.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that `IUpdateService`, update result handling, tray notification text, and download/restart workflow remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-update-startup-check` and verify the change has no spec deltas.
