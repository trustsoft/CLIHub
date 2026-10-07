## 1. Contract Split

- [x] 1.1 Add version, check, state, download, and installer interfaces; make `IUpdateService` the composed contract without changing result models.
- [x] 1.2 Keep `UpdateService` as the Velopack-backed implementation of all ports and preserve timeout, cancellation, duplicate-download, failure, and apply semantics.

## 2. Consumer Wiring

- [x] 2.1 Register all narrow ports as aliases to the same update service singleton.
- [x] 2.2 Update startup, release notes, settings, main window, tray actions, update control, and download coordinator constructors to use narrow contracts.
- [x] 2.3 Migrate tests and add registration coverage proving check-only consumers do not require download/apply contracts.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 3.2 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
