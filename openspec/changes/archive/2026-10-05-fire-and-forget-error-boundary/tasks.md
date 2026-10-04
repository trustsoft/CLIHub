## 1. Boundary and tests

- [x] 1.1 Add the application-layer async runner and verify successful operations preserve their result/status behavior.
- [x] 1.2 Add tests proving unexpected exceptions are logged and reported through the optional status callback without escaping the fire-and-forget caller.

## 2. Integration

- [x] 2.1 Route launch-window agent command fire-and-forget calls through the runner; verify expected command results and unexpected exception reporting.
- [x] 2.2 Route update-control and applicable settings/application async actions through the runner without duplicating existing complete error boundaries; verify focused ViewModel tests pass.
- [x] 2.3 Verify the retained legacy window and application startup paths do not introduce new unhandled fire-and-forget operations.

## 3. Verification and backlog

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify all boundary tests pass.
- [x] 3.2 Update `improvements.md` to close the fire-and-forget item and retain only verified follow-up work.
- [x] 3.3 Run `openspec validate "fire-and-forget-error-boundary"` and archive the change after all tasks pass.
