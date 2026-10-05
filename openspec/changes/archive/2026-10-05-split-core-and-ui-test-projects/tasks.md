## 1. Classify Existing Tests

- [x] 1.1 Inventory test files and classify Core-only tests versus tests that require WPF/application assemblies, using actual namespaces and project references.
- [x] 1.2 Identify any shared test helpers or implicit usings that must be copied or retained for each project.

## 2. Split Test Projects

- [x] 2.1 Add `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` with only the Core project reference and the required test packages/settings.
- [x] 2.2 Move Core-only tests into `CLIHub.Core.Tests`, preserving test behavior and updating namespaces/usings only as required.
- [x] 2.3 Keep UI-dependent tests in `CLIHub.Tests`, ensure their WPF/application references are explicit, and remove unnecessary cross-project references.

## 3. Update Repository Integration

- [x] 3.1 Add the new test project to `CLIHub.sln` and verify both test projects appear in solution builds.
- [x] 3.2 Update CI and `docs/repo-structure.md` to describe and execute the two test-project structure.

## 4. Verify Preserved Behavior

- [x] 4.1 Run `dotnet build CLIHub.sln -c Release`.
- [x] 4.2 Run `dotnet test CLIHub.sln -c Release` and verify both test assemblies execute successfully.
- [x] 4.3 Run `openspec validate split-core-and-ui-test-projects` and verify the change remains explicitly spec-free.
