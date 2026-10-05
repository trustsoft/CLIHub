## Why

The repository currently has one WPF-enabled test project that references both `CLIHub.Core` and the WPF application. This obscures the Core/UI dependency boundary, makes the documented test architecture inaccurate, and forces Core-only tests to build through a UI project.

## What Changes

- Add a dedicated `tests/CLIHub.Core.Tests` project targeting the platform-independent Core test surface.
- Keep a separate `tests/CLIHub.Tests` project for WPF/application-specific tests and references.
- Move tests according to their actual dependencies rather than duplicating or changing test behavior.
- Update `CLIHub.sln`, project references, shared test settings, and CI commands so both test projects are built and executed.
- Correct repository architecture documentation to describe the actual test-project boundaries and locations.

## Capabilities

### New Capabilities

None. This is a test-structure, build, and documentation change with no runtime capability change.

### Modified Capabilities

None.

## Impact

- Affected files include the solution, test project files, test-file locations, CI workflow, and `docs/repo-structure.md`.
- Core tests must reference only `CLIHub.Core`; UI tests may reference `CLIHub` and `CLIHub.Core` as required.
- The full solution build and test commands must remain green, with both test assemblies included in CI.
- Runtime application code and user-facing behavior are unchanged.
