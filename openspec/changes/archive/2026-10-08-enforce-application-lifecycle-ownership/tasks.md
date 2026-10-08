## 1. Explicit lifecycle ownership

- [x] 1.1 Make project and agent pane controllers disposable and verify event unsubscription.
- [x] 1.2 Cancel/dispose pane-owned agent-version work on disposal and verify late completions are ignored.
- [x] 1.3 Make update control and launch-window ViewModel disposable, with named subscriptions and idempotent cleanup tests.

## 2. Legacy and architecture enforcement

- [x] 2.1 Mark the unregistered `MainWindow` as deprecated legacy source and verify DI does not register it.
- [x] 2.2 Extend architecture tests to assert lifecycle service registrations and legacy isolation.
- [x] 2.3 Update architecture and repository-structure documentation.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and require zero warnings and errors.
- [x] 3.2 Run `dotnet test CLIHub.sln`, `openspec validate`, and `git diff --check`.
