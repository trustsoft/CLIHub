## 1. Add Shared Download Workflow

- [x] 1.1 Add `IUpdateDownloadCoordinator`, `IUpdateDownloadNotifier`, and their implementations for downloaded, failed, no-update, already-downloading, delay, apply/restart, menu refresh, and warning handling; verify all result paths with focused tests.
- [x] 1.2 Register the coordinator/notifier in WPF composition and route tray plus What's New update requests through the same coordinator; verify DI resolution and event wiring.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that `IUpdateService`, `UpdateControlViewModel`, notification text, restart delay, and apply/restart behavior remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-update-download-workflow` and verify the change has no spec deltas.
