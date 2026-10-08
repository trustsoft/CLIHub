## 1. Shared workflow

- [ ] 1.1 Add `IUpdateWorkflow` and `UpdateWorkflow` with shared state, concurrent-check coalescing, download, apply, and download-and-apply behavior; verify with focused workflow tests.
- [ ] 1.2 Move tray download notifications and automatic-restart behavior into the workflow and forward outcomes through `ApplicationSession`; verify cancellation, failure, and success paths.
- [ ] 1.3 Route startup update checks through the workflow while preserving disabled and best-effort behavior.

## 2. Presentation adapters

- [ ] 2.1 Route tray and What's New manual checks/download requests through the shared workflow and verify shared state and duplicate-check coalescing.
- [ ] 2.2 Route `UpdateControlViewModel` through the workflow while preserving its explicit restart action and status messages.
- [ ] 2.3 Remove the old download coordinator contracts/registrations and verify the application composition graph resolves one workflow instance.

## 3. Documentation and verification

- [ ] 3.1 Update architecture docs and `improvements.md` to record the completed deferred workflow unification.
- [ ] 3.2 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate`, and `git diff --check`.
