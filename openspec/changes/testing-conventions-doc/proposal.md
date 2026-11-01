## Why

The test suite (`tests/CLIHub.Core.Tests`, `tests/CLIHub.Core.Windows.Tests`, `tests/CLIHub.Tests`) follows
real conventions — strict mocks where interaction matters, hand-written fakes and builders elsewhere, an
injected `TimeProvider`, a shared STA harness, an architecture-boundary test suite — but there is no single
document that teaches them. `tests/CLIHub.Tests/README.md` covers naming, builders, and "mock what you
verify" for one of the three test projects only, and its own test-count claim ("1,282 tests ... as of
2026-10-30") is already stale against the current tree (1,340 across the three projects, see `design.md`).
A contributor adding a test today has to reconstruct the conventions by reading several existing test files
first.

## What Changes

- Add `docs/testing.md`: a repo-wide testing-conventions guide covering the project-to-test-project mapping,
  the `MethodOrScenario_Condition_ExpectedResult` naming rule, Arrange/Act/Assert and one-behavior-per-test,
  the mixed strict/loose Moq pattern actually in force (not "zero loose mocks" — see `design.md`), when a
  hand-written fake is preferred over a mock, the `FakeTimeProvider` and temp-directory patterns, the shared
  STA harness and other `TestHelpers` members, how to add a test for a new spec scenario, how to add a spec
  scenario for new behavior, and the gate commands actually run today (build, the three `dotnet test`
  invocations CI runs, `openspec validate --all`).
- Link `docs/testing.md` from `docs/architecture.md` (a new "Testing" entry under "Focused Documents") and
  from `docs/project-context.md` (a new "Testing conventions" row in the "Documentation Routing" table).
- Trim `tests/CLIHub.Tests/README.md` to stop duplicating conventions that move to `docs/testing.md` (naming,
  AAA, mock guidance, fakes, "Adding New Tests") and keep only what is specific to that one project: the
  `LaunchWindowViewModelBuilder` API walkthrough and its before/after example. Point the trimmed file at
  `docs/testing.md` for everything else, and drop its stale test-count claim.
- Delete the README's "## Future Improvements" section and relocate its non-dropped ideas to a new
  `docs/backlog.md` entry under "## Test suite" (see `design.md` Decision 7) so speculative ideas are parked,
  not silently discarded.
- Add a one-line "Delivered" marker next to the C6 entry in
  `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md` §5, naming this change and its date.

## Non-Goals

- No coverage collection or CI coverage gate (tracked separately as `test-coverage-in-ci`, roadmap item C1).
- No `TreatWarningsAsErrors` change (roadmap item C2) — the doc states the current posture (warnings do not
  fail the build) as a fact, not a target.
- No test-timing refactor, no new `IDelay` seam (roadmap item C3).
- No `xunit.runner.json`, no new shared `StaRunner.cs` helper, no `[Collection]` grouping (roadmap item C4) —
  the doc documents `TestHelpers.RunOnSta` and the existing `[SkippableFact]`/`Skip.If` pattern as they exist
  today, without adding new test infrastructure.
- No new tests for the 0%-coverage classes (roadmap item C5).
- No change to `AGENTS.md`'s "Documentation and OpenSpec" list — see `design.md` Decision 3 for why.
- No change to test, source, or build-configuration files other than the `tests/CLIHub.Tests/README.md` trim
  described above, which touches documentation prose only.

## Impact

- Affected: `docs/testing.md` (new), `docs/architecture.md`, `docs/project-context.md`,
  `tests/CLIHub.Tests/README.md`, `docs/backlog.md` (one new "Test suite" entry relocating the deleted
  README section), `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md` (one "Delivered" line next to
  the C6 entry; the roadmap is navigation only and is not validated by `openspec validate`).
- No application code, test code, project files, or CI workflow changes. No durable behavior-contract change;
  `.openspec.yaml` sets `skip_specs: true`.

## Dependencies

- None of the other roadmap items (C1-C5) are required before this one ships; the brief sequences C6 last so
  it documents the end state, but the content below is scoped to conventions already in force today and does
  not describe target-state-only mechanisms (no coverage threshold, no `xunit.runner.json`). If C1-C5 land
  before this change is implemented, the implementer should re-verify the facts in `design.md` against the
  tree at that time (see `tasks.md` Stage 3).
