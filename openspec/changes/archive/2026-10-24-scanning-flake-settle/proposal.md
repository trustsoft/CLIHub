# Fix the flaky `Stop_DisposesWatcherAndClearsStatus` scanning test

## Why

The backlog entry "Flaky ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus"
(`docs/backlog.md:9-22`) has fired its trigger with the test name captured:

- failed 1 of 3 full-solution runs during `fix-settings-open-delay` verification (2026-10-20),
  green in isolation and on re-run;
- recurred during the `low-sweep` verification (2026-10-24): failed 2 of 3 full-solution runs,
  green isolated, failing test name captured by a trx run.

Mechanism (verified in the current tree, see design "Current state"): the test calls `Stop()`
after waiting only for watcher creation (`tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:127`),
but `Start()` also fires a background scan whose completion publishes `Idle`/`IsWatching = true`
from a thread-pool continuation (`src/CLIHub.Core/Scanning/ProjectScanningService.cs:393-420`).
Nothing orders that late publish against `Stop()`'s `NotConfigured` publish (`:165-175`), so under
parallel load the late `Idle` can be the value the assert reads (`:132`).

## What Changes

1. **Test-only fix, one wait predicate.** `Stop_DisposesWatcherAndClearsStatus` waits for the
   initial scan to settle before stopping — the wait requires the watcher to exist, the state to
   be `Idle`, and the scan coverage to be `2` (the scan-final publish for this test's two-directory
   tree), mirroring the settle predicates already used by the green sibling
   `Stop_ClearsScannedDirectoryCount` (`ProjectScanningServiceTests.cs:209`) and
   `Start_ReportsExaminedFoldersAcrossRoots` (`:197-198`). After the settle point the service has
   no further status publisher, so `Stop()`'s `NotConfigured` is deterministically the final write.
   No production code changes.
2. **Flake acceptance verification.** Because the failure only reproduces under full-solution
   parallel load, the plan gates on 6 consecutive green full-solution `dotnet test CLIHub.sln`
   runs (trx-logged so any failure is named), with the structural determinism argument as the
   primary proof and the run count as a confidence bar whose limits are stated honestly.
3. **Backlog bookkeeping (docs stage, executed at commit time per policy).** The flake entry is
   rewritten to the resolved format, and the sibling watch item "Single unexplained test failure
   (not reproduced)" (`docs/backlog.md:305-316`) gets an evidence update: the now-confirmed flake
   is the strongest candidate for that uncaptured 2026-10-20 `CLIHub.Core.Tests` failure —
   probable same test, not proven; the entry stays open.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The change edits a single test's wait predicate; no observable system behavior changes, and
test-only changes never take spec deltas (precedent:
`openspec/changes/archive/2026-10-24-low-sweep/`, which also recorded that "Test-only changes
never take deltas").

For completeness, the rejected production-side alternative (making `Stop` await or drain the
in-flight scan) was spec-checked anyway: the `project-scanning` capability
(`openspec/specs/project-scanning/spec.md:96-124`, "Background watcher with status") pins
watching, debounce, status observability, per-root coverage, and the no-watched-directories case —
it never mentions `Stop`, and `docs/architecture/scanning.md:78` says only that `Stop` clears the
coverage count. The Stop-versus-in-flight-scan interplay is unspecified, so even that alternative
would have had no delta to write; see design D3.

Therefore the change opts out of specs (`skip_specs: true` in `.openspec.yaml`), following
`openspec/changes/archive/2026-10-24-low-sweep/` and `2026-10-24-hygiene-sweep/`.

## Impact

- Tests: `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs` — the wait predicate of
  `Stop_DisposesWatcherAndClearsStatus` (`:127`) only. No other test, no helper, no fake changes:
  `WaitFor` (`:72-86`), `InlineSynchronizationContext`
  (`tests/CLIHub.Core.Tests/Fakes/ScanningFakes.cs:66-68`), and the other 17 tests in the file are
  untouched.
- Docs: `docs/backlog.md` — two entries (the flake entry resolved rewrite, `:9-22`; the sibling
  watch item's evidence note, `:305-316`), executed by the orchestrator at commit time.
- Specs: none (see Capabilities).
- Production: none. `IProjectScanner.Stop` keeps its current semantics; its caller census
  (design "Current state") shows no production caller — only the two scanning tests call it.
- Risk: minimal. The new predicate values are already pinned by passing sibling tests
  (`ScannedDirectoryCount == 2` for the identical tree at `:209`; `Idle` settle waits at
  `:116`, `:197`, `:229-230`, `:255`). The residual cost of the fix is that the test now waits
  for a real (tiny) scan to finish before stopping — the same cost every settle-wait sibling
  already pays.

## Non-goals

- No change to `ProjectScanningService`, `IProjectScanner`, or any production code — `Stop`
  remains non-draining and fire-and-forget scans may still publish after it; that interplay is
  unobserved in production (no `Stop` callers) and unspecified by the spec.
- No rework of `WaitFor`, no new test infrastructure, no `Task`-completion seams injected into the
  service for determinism (design D1 explains why the settle-wait already reaches determinism
  through the service's real observables).
- No touching the other timing-sensitive tests named by the sibling backlog entry
  (`UpdateCheckerTests`, `UpdateDownloaderTests`); they stay parked under their own trigger.
- No proof that the flake is "gone forever": the acceptance bar is 6 consecutive green
  full-solution runs plus the structural argument, with the statistical limits recorded
  (design D4).
- No CHANGELOG entry now; entries land under the next release heading.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change).
- Implements the remedy recorded in the backlog entry "Flaky
  ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus" (`docs/backlog.md:9-22`).
- Updates the evidence of "Single unexplained test failure (not reproduced)"
  (`docs/backlog.md:305-316`); that entry remains open unless the user decides otherwise
  (open question Q2).
