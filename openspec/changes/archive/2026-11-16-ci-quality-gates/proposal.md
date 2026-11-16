## Why

`docs/backlog.md` records "CI enforces neither the warning baseline nor coverage" — the repository
maintains a zero-warning build discipline socially, and `coverlet.collector` 10.1.0 is already
referenced by all three test projects, yet the CI workflow runs plain `dotnet build`/`dotnet test`
with no warning enforcement and no coverage collection. A green CI run today can carry new warnings
or silently reduced coverage with no signal.

The code-style spec (`openspec/specs/code-style/spec.md`, Requirement "Naming conventions are
machine-enforced") says violations must be *flagged*. It does not say they must not break the build —
that is the current, deliberate local-build behavior (`docs/testing.md:317-323`). CI is the right
place to make the existing zero-warning discipline observable and enforceable for reviewers.

## What Changes

1. **Warning enforcement in CI:** the CI `dotnet build` step adds `-warnaserror`, so any compiler or
   analyzer warning breaks the CI build. This codifies the discipline the team already keeps locally
   (measured: the current `Release` build produces 0 warnings) without changing local build behavior.

2. **Coverage collection and artifact publication:** each of the three `dotnet test` invocations
   collects coverage via `--collect:"XPlat Code Coverage"` (already supported by the installed
   `coverlet.collector`), writes results to separate directories under `artifacts/coverage/`, and a
   single `actions/upload-artifact` step with `if: always()` publishes the combined `*.cobertura.xml`
   files for download. No threshold gate is applied — an 80% gate is explicitly rejected for a WPF
   codebase (`docs/backlog.md:282`).

3. **Spec delta:** `ci-build` capability gains warning enforcement and coverage scenarios within its
   existing requirements.

## Capabilities

### Modified Capabilities

- `ci-build` — the "Build and test on pull requests" and "Build and test on master" requirements
  gain warning-enforcement and coverage-collection behavior.

## Impact

- `.github/workflows/ci.yml`: the `Build` step gains `-warnaserror`; each `dotnet test` invocation
  gains `--collect:"XPlat Code Coverage" --results-directory artifacts/coverage/<project>`; one
  `actions/upload-artifact` step is added after tests, gated on `if: always()`.
- `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:147-154` —
  `CiWorkflow_RunsBothTestProjects` reads `ci.yml` as text; the change must keep the three project
  file names present (they stay).
- No new NuGet packages, no `.runsettings`, no `.editorconfig` or `Directory.Build.props` changes.
- `docs/backlog.md` entry is resolved at implementation/verification time (not now).

## Non-Goals

- Setting `TreatWarningsAsErrors` in `Directory.Build.props` (affects local builds; out of scope).
- Adding a coverage threshold gate, badge, or trend dashboard.
- Merging the three test invocations into one `dotnet test CLIHub.sln` call.
- Changing the `.editorconfig` rule severities from `warning` to `error`.
- Touching the release pipeline (`.github/workflows/release.yml`) or its spec.

## Dependencies

None. The change is self-contained in CI workflow configuration and its spec.