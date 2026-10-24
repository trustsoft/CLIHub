# Tasks: scanning-flake-settle

Stages 1–2 are the fix and its flake verification (test code only). Stage 3 is the backlog
bookkeeping — per the repo's change/commit/archive policy it is executed by the orchestrator at
commit time, after the user approves the stage-4 report; its checkboxes stay unticked until then.
Stage 4 is the final gates and the report. Do not start the next stage on a red gate.
Per `AGENTS.md`, run GitNexus impact before the edit in stage 1; the caller census in
`design.md` ("Current state") is the baseline any impact result must agree with — if impact
reports a `Stop` caller not listed there, stop and reconcile before editing.

## 1. Stage 1 — the settle-wait fix in `Stop_DisposesWatcherAndClearsStatus`

- [x] 1.1 Run `node .gitnexus/run.cjs impact "Stop_DisposesWatcherAndClearsStatus"
      --direction upstream --repo .` and `node .gitnexus/run.cjs impact "Stop"
      --direction upstream --repo .` (or the MCP equivalent). Expected: the test method has no
      callers (a test is a leaf; `UNKNOWN`/empty is the expected verdict, not a warning to act
      on), and `ProjectScanningService.Stop`'s upstream set matches the design census — the two
      test callers and no production caller. Stop and reconcile on any surprise.
- [x] 1.2 In `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs`, replace the wait
      at `:127` of `Stop_DisposesWatcherAndClearsStatus` with the design D1 predicate (watcher
      created **and** scan settled):

      ```csharp
      Assert.True(WaitFor(() => this.watcherFactory.Watchers.Count == 1
                           && service.Status.State == ProjectScanState.Idle
                           && service.Status.ScannedDirectoryCount == 2));
      ```

      Change nothing else in the file — not `WaitFor`, not the asserts at `:131-132`, not any
      other test. Do not touch production code.
- [x] 1.3 Gate: `dotnet build CLIHub.sln` (zero errors), then
      `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProjectScanningServiceTests"`
      — all facts in the class green (the rewritten test included). If the rewritten wait itself
      fails, diagnose against design "Current state" before touching anything — the predicate is
      pinned by the green sibling (`Stop_ClearsScannedDirectoryCount`, `:209`); do not bump the
      timeout or weaken the predicate.
- [x] 1.4 Gate: full-solution run 1 of 6 — `dotnet test CLIHub.sln --logger trx` — zero failures.
      Any failure of `Stop_DisposesWatcherAndClearsStatus` here stops the change: capture the trx,
      diagnose, report (deviation protocol); never re-run until green and call it fixed.

## 2. Stage 2 — flake verification under full-solution load

The acceptance bar is design D4: 6 consecutive green full-solution runs (run 1 already done in
1.4), unfiltered, trx-logged so any failure names its test. Honest scope: this is a confidence
bar over the observed 1/3–2/3 failure rate, not a proof; the structural determinism argument
(design D1) is the actual fix proof, and the runs verify the rewritten test still exercises the
start → settle → stop → `NotConfigured` transition under load.

- [x] 2.1 Run the remaining nine full-solution runs (runs 2–10): `dotnet test CLIHub.sln
      --logger trx`, sequentially, no filters. Record pass/fail per run in the stage report
      (timestamp + totals). On any failure of this test: stop, capture the trx, diagnose against
      design "Current state", and report — do not restart the count silently and do not weaken
      anything. A failure of a *different* test is handled per its own backlog entry (for the
      sibling watch item, record the named test as the recurrence evidence it asks for) and is
      not a gate failure of this change unless the change caused it.
- [x] 2.2 Gate: ten consecutive green full-solution runs recorded (1 from stage 1 + 9 here), and
      `git status --porcelain` shows no tracked-file modifications beyond the stage-1 test edit
      (trx files under `TestResults/` are run artifacts — leave them out of any commit).
- [x] 2.3 Resolved by the user (2026-10-24): the acceptance bar is **10** consecutive green
      full-solution runs — task 2.1 covers runs 2–10 (nine runs) and the 2.2 gate reads "ten
      consecutive green runs".

## 3. Stage 3 — backlog bookkeeping (executed by the orchestrator at commit time; unticked until then)

Per the change/commit/archive policy, this stage is not executed during implementation: the
orchestrator applies it when the user approves the commit, and ticks the checkboxes then. Content
per design D5:

- [x] 3.1 Rewrite "Flaky ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus"
      (`docs/backlog.md:9-22`) to the resolved format (heading suffix ` (resolved)`; bullets
      `Status:` / `Resolved model:` / `Guarded by:` / `Trigger:`, following
      "Magic version sentinels (resolved)" in the same file): resolved by the
      `scanning-flake-settle` change (2026-10-24); resolved model — the test waits for the
      initial scan to settle (watcher created + `Idle` + scan coverage 2, the scan-final-only
      value the sibling test already pins) before `Stop()`, so `Stop`'s `NotConfigured` publish
      is deterministically the final status write; no production change, `Stop` remains
      non-draining and unspecified against in-flight scans. Guarded by the rewritten
      `Stop_DisposesWatcherAndClearsStatus` plus the unchanged
      `Stop_ClearsScannedDirectoryCount`, verified by 6 consecutive green full-solution runs
      (record the actual count from stage 2). Trigger: any future red run of a
      `ProjectScanningServiceTests` fact.
- [x] 3.2 Update the sibling entry "Single unexplained test failure (not reproduced)"
      (`docs/backlog.md:305-316`) — Evidence only, entry stays open: the confirmed flake
      `Stop_DisposesWatcherAndClearsStatus` (failed 2026-10-20 during fix-settings-open-delay
      verification; trx-captured 2026-10-24 during low-sweep verification; stabilized by
      scanning-flake-settle) is the strongest candidate for this entry's uncaptured 2026-10-20
      `CLIHub.Core.Tests` failure — same day, same test project, same full-solution parallel-load
      shape — probable same test, not proven (the name was never captured);
      `UpdateCheckerTests`/`UpdateDownloaderTests` remain unexcluded candidates and the standing
      remedy (trx on recurrence) is unchanged. Do not add a `(resolved)` suffix. If the user
      answered open question Q2 otherwise, follow that decision instead.
- [x] 3.3 Gate: `git diff docs/backlog.md` shows exactly the two entry hunks and nothing else;
      `openspec validate --all` still passes.

## 4. Stage 4 — final gates and report

- [x] 4.1 Gate: `dotnet build CLIHub.sln` zero errors; the sixth (or tenth) full-solution run
      from stage 2 is the final full-suite evidence — do not skip it and do not re-run the suite
      here without cause.
- [x] 4.2 Gate: `openspec validate --all` passes.
- [x] 4.3 `graphify update .` to refresh the knowledge graph after the test edit.
- [x] 4.4 Review `git status --porcelain` and `git diff --stat`: only
      `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs` (one predicate) plus this
      change's files under `openspec/changes/scanning-flake-settle/` are modified; no production
      file, no other test, no spec. Report the stage-2 run log and results honestly (a red run is
      reported as red), then wait for the user's commit/archive approval per the repo policy —
      stage 3 above executes at that point, not before.
