## 1. Boundary and tests

- [ ] 1.1 Add the application-layer async runner and verify successful operations preserve their result/status behavior.
- [ ] 1.2 Add tests proving unexpected exceptions are logged and reported through the optional status callback without escaping the fire-and-forget caller.

## 2. Integration

- [ ] 2.1 Route launch-window agent command fire-and-forget calls through the runner; verify expected command results and unexpected exception reporting.
- [ ] 2.2 Route update-control and applicable settings/application async actions through the runner without duplicating existing complete error boundaries; verify focused ViewModel tests pass.
- [ ] 2.3 Verify the retained legacy window and application startup paths do not introduce new unhandled fire-and-forget operations.

## 3. Verification and backlog

- [ ] 3.1 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify all boundary tests pass.
- [ ] 3.2 Update `improvements.md` to close the fire-and-forget item and retain only verified follow-up work.
- [ ] 3.3 Run `openspec validate "fire-and-forget-error-boundary"` and archive the change after all tasks pass.
