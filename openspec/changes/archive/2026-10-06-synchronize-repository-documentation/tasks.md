## 1. Documentation Synchronization

- [x] 1.1 Update `README.md` and `AGENTS.md` with the current project layout, test project split, version baseline, and build/test guidance; verify all referenced paths exist.
- [x] 1.2 Update `docs/repo-structure.md` and `docs/architecture.md` with current solution structure, configuration repository, plugin catalog boundary, registered services, test dependencies, and active/legacy windows.
- [x] 1.3 Update `docs/releasing.md` current release guidance to match the `0.9.0` development baseline and current CI/release workflow without altering historical release records.

## 2. Verification

- [x] 2.1 Verify documentation links and referenced project/file paths with repository searches, and run `openspec validate synchronize-repository-documentation`.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release`, verifying the documentation change does not disturb the solution build.
- [x] 2.3 Run `dotnet test CLIHub.sln -c Release`, verifying Core and UI test projects pass.
