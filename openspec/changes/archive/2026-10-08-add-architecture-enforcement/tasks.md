## 1. Architecture Tests

- [x] 1.1 Add Core WPF/project dependency checks.
- [x] 1.2 Add application WPF allowlist, project direction, retired-symbol, and CI test-project checks.

## 2. Verification

- [x] 2.1 Run architecture tests, `dotnet build CLIHub.sln -c Release`, and `dotnet test CLIHub.sln -c Release`.
- [x] 2.2 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
