## 1. Application session and startup ownership

- [x] 1.1 Extract first-instance application event subscriptions into a disposable session.
- [x] 1.2 Dispose the session before stopping application operations and the DI provider.
- [x] 1.3 Preserve startup ordering and cover event wiring, unsubscription, and second-instance behavior with tests.
- [x] 1.4 Synchronize startup and architecture documentation.

## 2. Manual update checks

- [x] 2.1 Add the idle/checking manual-check action to the tray menu.
- [x] 2.2 Add the idle/checking manual-check action to the What's New window.
- [x] 2.3 Use the shared checker, update state, and tracked operation lifetime without changing download/apply behavior.
- [x] 2.4 Add focused tests for tray and What's New manual-check entry points.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln --no-restore`.
- [x] 3.2 Run `dotnet test CLIHub.sln --no-restore`.
- [x] 3.3 Run `git diff --check` and refresh the graphify index.
