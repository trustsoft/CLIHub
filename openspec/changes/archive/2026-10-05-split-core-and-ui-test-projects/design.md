## Context

`tests/CLIHub.Tests/CLIHub.Tests.csproj` currently targets `net10.0-windows`, enables WPF, and references both `src/CLIHub.Core/CLIHub.Core.csproj` and `src/CLIHub/CLIHub.csproj`. Most tests exercise Core services and models, while a smaller set imports application namespaces or verifies WPF composition and ViewModel behavior.

The repository documentation already describes `CLIHub.Tests` as Core-only, so the implementation should make the project structure match that intended boundary and explicitly describe the remaining UI test project.

## Goals / Non-Goals

**Goals:**

- Make Core tests independently buildable against `CLIHub.Core` without a WPF project reference.
- Keep WPF/application tests in a clearly named UI-capable project.
- Preserve all existing test behavior and total coverage.
- Ensure solution, CI, and documentation consistently include both test projects.

**Non-Goals:**

- Rewrite tests or change production behavior.
- Introduce a shared test utility project unless the existing tests prove it is required.
- Split tests based only on folder names when their source dependencies indicate otherwise.
- Change target frameworks or package versions beyond what is required for the project boundary.

## Decisions

- Create `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` with the Core test packages and a project reference only to `src/CLIHub.Core/CLIHub.Core.csproj`.
- Retain `tests/CLIHub.Tests/CLIHub.Tests.csproj` as the WPF/application test project, keeping its WPF setting and application reference only for tests that need it.
- Move Core-only test files into `tests/CLIHub.Core.Tests`, including service, model, formatting, logging, hotkey, and Core workflow tests. Keep tests that import `CLIHub` application types or ViewModels in `tests/CLIHub.Tests`.
- Use project-local namespace updates only where the move requires them; preserve test names and assertions.
- Add both projects to `CLIHub.sln` and keep `dotnet build CLIHub.sln -c Release` / `dotnet test CLIHub.sln -c Release` as the canonical verification commands.
- Update CI only as needed to make explicit that the solution test run covers both projects, without creating a second divergent test command.

## Risks / Trade-offs

- **A test has an indirect WPF dependency** -> Build the Core project independently and move the failing test to the UI project or remove the unnecessary dependency after inspection.
- **Namespace/file moves create noisy diffs** -> Keep moves mechanical and avoid unrelated test refactoring.
- **CI runs only one test assembly** -> Verify solution membership and test output includes both projects.

## Migration Plan

1. Inventory test dependencies and classify files as Core-only or UI/application-specific.
2. Create the Core test project and move Core-only tests with minimal namespace/project-file changes.
3. Retain or move UI-dependent tests in the WPF test project and remove the Core project reference if no longer needed there; retain it where mixed UI/Core tests require it.
4. Add both projects to the solution and update CI/documentation.
5. Run both project-level checks and the full solution build/test commands.
6. Rollback consists of restoring the original test project membership and solution entry.

## Open Questions

None.
