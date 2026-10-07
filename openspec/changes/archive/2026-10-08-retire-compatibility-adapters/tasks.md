## 1. Compatibility Removal

- [x] 1.1 Remove `PluginManager`/`IPluginManager` and their registrations/tests while retaining canonical catalog coverage.
- [x] 1.2 Remove `IProcessLauncher` from production composition and move runtime preference application to `IInteractiveProcessRunner`.
- [x] 1.3 Update process test fakes and registration assertions to use the split contracts.

## 2. Verification

- [x] 2.1 Confirm no production references remain to removed compatibility APIs.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 2.3 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
