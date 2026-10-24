# Design: scanning-flake-settle

All line numbers were verified against the current working tree on 2026-10-24 (clean tree at HEAD,
no active changes). The backlog's citation `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:120-133`
was re-verified: the `[Fact]` attribute is at `:119` and the method `Stop_DisposesWatcherAndClearsStatus`
spans `:120-133` — the backlog span is accurate.

## Current state (verified)

### The flaky test and its race

`Stop_DisposesWatcherAndClearsStatus` (`tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:119-133`):

```csharp
var root = this.CreateTempDir();                          // :122
Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
this.preferences.WatchedDirectories = [root];
var service = this.CreateService();
service.Start();                                          // :126
Assert.True(WaitFor(() => this.watcherFactory.Watchers.Count == 1));  // :127

service.Stop();                                           // :129

Assert.True(this.watcherFactory.Watchers[0].Disposed);    // :131
Assert.Equal(ProjectScanState.NotConfigured, service.Status.State);  // :132
```

Trace of what each side does (production code cited):

- `Start()` (`src/CLIHub.Core/Scanning/ProjectScanningService.cs:101-115`) runs `Configure()`
  synchronously on the test thread: creates the timer and watcher (`:243-248`), assigns them under
  the lock (`:250-260`), posts the pre-scan status `Idle`/`IsWatching = true`
  (`:262-272` — note: this `Idle` is published *before* the scan runs), then calls `RequestScan()`
  (`:273`). The watcher exists in `watcherFactory.Watchers` from `Create` (`:245`), so `:127`'s
  wait is satisfied the moment `Start()` returns — it says nothing about the scan.
- `RequestScan()` (`:335-343`) is fire-and-forget: `_ = this.ScanLoopAsync()` (`:342`). The loop
  (`:345-372`) takes the scan gate and runs `RunScanAsync` (`:374-421`), which publishes
  `Scanning` (`:380-386`), runs the real disk enumeration via `Task.Run` (`:388-391`), and on
  completion posts the reconcile plus the final status `Idle`/`IsWatching = true`
  (`:393-420`) — inline on a thread-pool continuation thread.
- `Stop()` (`:151-176`) detaches and disposes the watcher synchronously on the calling thread
  (`:164`, `Detach` at `:303-317`), then posts the cleared status `NotConfigured`/not watching/
  count 0 (`:165-175`).
- Both publishes go through `Publish` → `Post` (`:581-598`). The test injects
  `InlineSynchronizationContext` (`tests/CLIHub.Core.Tests/Fakes/ScanningFakes.cs:66-68`, whose
  `Post` executes the callback inline on the posting thread), so `Stop()`'s `NotConfigured` write
  to `this.status` (`:583`) happens synchronously on the test thread, while the scan's final
  `Idle` write happens on a thread-pool thread. Nothing orders these two writes.

Failure interleaving (matches the backlog's diagnosis and both capture dates): under full-solution
parallel load the test thread is preempted or GC-paused in the window between `Stop()`'s
`NotConfigured` write and `:132`'s read; the scan-final `Idle` write lands inside that window; the
assert reads `Idle`. In isolation the thread-pool dispatch plus the disk walk essentially never
complete inside that sub-microsecond window, which is why the test is green in isolation. Note
that even a green run leaves the service publishing a stale `Idle` after `Stop` when the scan
loses the race the other way — the test just does not read it; this is why a wait *after* `Stop`
cannot fix the test (see Alternatives).

Two precision notes beyond the backlog entry:

1. The `Watchers[0].Disposed` assert (`:131`) is not racy in this test: `Start()` runs `Configure()`
   to completion on the test thread, so the watcher is in `this.watcher` before `Stop()` can run —
   `Stop()` always observes and disposes it. Only `:132` races.
2. After the scan-final post, no further status write can occur in this test: `scanPending` is 0
   (cleared by the loop head, `:347`), the fake timer never fires on its own
   (`ScanningFakes.cs:45-51`), and no `Reload`/`Rescan` is called. The loop's remaining work
   (gate release, exit) does not touch `this.status`.

### The green sibling is the existence proof

`Stop_ClearsScannedDirectoryCount` (`ProjectScanningServiceTests.cs:201-214`) has the identical
Start → `WaitFor` → `Stop` shape and identical tree (`root/repo/.git`), but waits for
`service.Status.ScannedDirectoryCount == 2` (`:209`) before `Stop()` (`:211`).
`ScannedDirectoryCount` is part of the `ProjectScanStatus` record
(`src/CLIHub.Core/Scanning/ProjectScanStatus.cs:41-47`); for this tree the value `2` (root plus
`repo`; the walk stops at the repository and never descends `.git`) is written only by the
scan-final post (`ProjectScanningService.cs:398-402, :410-419`) — the pre-scan `Configure` post
publishes the sum of an empty coverage map, i.e. 0 (`:270`). So `:209`'s predicate is true only
after the scan has settled. The 2026-10-20 run reported exactly one failed test and the 2026-10-24
trx runs captured only `Stop_DisposesWatcherAndClearsStatus`, so the sibling passed the same
full-solution runs where the flaky test failed. The same settle-predicate shape also guards
`Start_WithWatchedDirectory_WatchesAndReconcilesDiscoveredRepository` (`:112-116`, reconcile +
`Idle`), `Start_ReportsExaminedFoldersAcrossRoots` (`:197-198`, `Idle` + count 3 for two roots),
and `Rescan_UpdatesOnlyTheRescannedRootsCoverage` (`:229-230, :236-237`).

Why `State == Idle` alone is not a sufficient predicate: `Configure()` publishes
`Idle`/`IsWatching = true` *before* the scan starts (`ProjectScanningService.cs:262-272`), so a
bare `Idle` wait can catch the pre-scan publish and leave the exact race in place. The predicate
must require a value only the scan-final post writes — `ScannedDirectoryCount == 2` for this tree
(the alternative, `reconciliations.Count > 0`, is equally valid; see Alternatives).

### `Stop` caller census (scope evidence)

`IProjectScanner` consumers in `src/`:

| Consumer | Use |
|---|---|
| `src/CLIHub/Startup/ApplicationBootstrapper.cs:83` | `Start()` after startup state loads |
| `src/CLIHub/Settings/SettingsApplicationService.cs:360, :503` | `Reload()` after scanning-settings saves |
| `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:529, :1586` | `Reload()` (Scan Now), `Rescan(directory)` |
| `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:532-533, :1386, :1671, :1728` | `StatusChanged`, `Status`, `CountDiscoveredProjects`, `CountManualProjects` |
| `src/CLIHub/Composition/ServiceRegistration.cs:177`, `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:49` | DI registration/resolution |

No production code calls `Stop()` — a repo-wide grep finds exactly two callers, both tests:
`ProjectScanningServiceTests.cs:129` (the flaky test) and `:211` (the sibling). Shutdown goes
through DI disposal of the singleton (`docs/architecture/scanning.md:87-90`), which is `Dispose()`
(`ProjectScanningService.cs:179-201`), a different path. `IProjectScanner.Stop`'s XML doc promises
only "Stops watching and clears the watching state. Idempotent."
(`src/CLIHub.Core/Scanning/IProjectScanner.cs:37-40`).

Consequence: a production-side change to `Stop` would alter semantics that nothing in production
observes, solely to fix a test that a one-line test edit fixes deterministically.

### Spec and documentation check

`openspec/specs/project-scanning/spec.md:96-124` ("Background watcher with status") pins: change
triggers a re-scan, debounce coalesces bursts, status is observable (scanning flag, last scan
time, discovered count, examined-folder count), single-root rescan refreshes only its coverage,
and no watched directories means no watching. **No scenario mentions `Stop`, and none constrains
what an in-flight scan may publish relative to a stop.** `docs/architecture/scanning.md:78` says
only "`Stop` clears it" (the coverage count), which the test keeps true. The Stop-versus-
in-flight-scan interplay is therefore unspecified and unobserved — relevant both to this change's
skip-specs decision and to the rejected production alternative (D3).

### Backlog state

- Flake entry: `docs/backlog.md:9-22` — evidence (1 of 3 on 2026-10-20 during
  fix-settings-open-delay verification; 2 of 3 on 2026-10-24 during low-sweep verification,
  trx-captured), mechanism, remedy, and the fired trigger, exactly as the brief states.
- Sibling watch item: "Single unexplained test failure (not reproduced)" (`docs/backlog.md:305-316`)
  — an uncaptured 1 failed / 553 passed `CLIHub.Core.Tests` run during `git-bash-resolution`
  pipeline verification on 2026-10-20, with likely candidates named as `UpdateCheckerTests`,
  `UpdateDownloaderTests`, and `ProjectScanningServiceTests`. The low-sweep design had already
  predicted `Stop_DisposesWatcherAndClearsStatus` as the strongest candidate
  (`openspec/changes/archive/2026-10-24-low-sweep/design.md`, open question 3, citing this test's
  wait-then-assert shape); the 2026-10-24 trx capture confirmed the prediction for the new
  failures, but the 2026-10-20 uncaptured failure remains a probabilistic match — same day, same
  test project, same full-solution parallel-load shape, same mechanism — not a proof.

## Decisions

**D1 — Fix placement: test-side settle-wait before `Stop`, not a production completion signal.**

The edit replaces `:127`'s watcher-count wait with a settle wait:

```csharp
service.Start();
Assert.True(WaitFor(() => this.watcherFactory.Watchers.Count == 1
                     && service.Status.State == ProjectScanState.Idle
                     && service.Status.ScannedDirectoryCount == 2));

service.Stop();
```

(the asserts at `:131-132` are unchanged). Rationale:

1. **Determinism argument (the actual proof).** The predicate is satisfiable only by the scan-final
   post (Current state). Once the test thread observes it, the service has no remaining status
   publisher in this scenario (precision note 2), so `Stop()`'s `NotConfigured` post is the final
   write and `:131-132` cannot observe anything else. The race is removed structurally, not by
   narrowing a timing window.
2. **The brief's poll-loop concern is answered, not ignored.** A poll loop is a timing trap when
   the polled event may never arrive (timeout flake) or when the poll papers over a missing
   synchronization so the assert still races. Neither holds here: the initial scan of a
   two-directory temp tree always completes (the same `WaitFor` with a 5 s budget already guards
   every settle sibling, and those tests are stable), and after the predicate the system is
   quiescent by construction. A poll *after* `Stop` would be the trap: once a late `Idle` lands,
   nothing ever republishes `NotConfigured`, so the poll would time out and fail — see
   Alternatives.
3. **"Fully synchronous scan" is not reachable from the test boundary.** The service offers no
   seam for it: `RequestScan` is fire-and-forget with the task discarded
   (`ProjectScanningService.cs:342`), and `RunScanAsync` wraps the real
   `RepositoryDiscovery.Enumerate` in `Task.Run` directly (`:388-391`) with no injectable
   discovery dependency. The observable completion signals are exactly the status publish and the
   reconcile callback — the settle-wait consumes the former, so it *is* "complete the background
   task deterministically before `Stop`", expressed through the service's real observables.
   Making the scan injectable would be a production change larger than the rejected Stop-drain.
4. **A production `Stop` change is unjustified (scope + risk).** `Stop` has no production caller
   (census), so nothing would benefit; `Stop` is a synchronous `void`, so "awaits/drains" means
   either blocking the calling thread for the duration of a disk walk (unbounded on slow disks)
   or an epoch/generation guard on publishes (a real behavior change to `IProjectScanner`
   semantics for all future callers, and new production code to test). The documented scanning
   behavior would not be violated (the interplay is unspecified, D3), but "not prohibited" is not
   a reason to ship production churn for a test problem with a deterministic test-side fix.
5. **Precedent.** The settle-before-stop shape is the established pattern in this file; the flaky
   test is the one Start→Stop test that does not use it.

**D2 — Predicate choice: `ScannedDirectoryCount == 2` (with `Idle`), not `Idle` alone and not a
post-`Stop` wait.** `Idle` alone is ambiguous (pre-scan `Configure` post, `:262-272`);
`ScannedDirectoryCount == 2` is scan-final-only for this tree and is byte-identical to the value
the green sibling already pins for the identical tree (`:209`), so the predicate cannot drift from
established expectations without the sibling failing too. Keeping `Watchers.Count == 1` in the
predicate preserves the clean "watcher was created" failure mode (an explicit message instead of
an `ArgumentOutOfRangeException` at `:131`). `reconciliations.Count > 0` was the equal alternative
(scan-final-only as well, since reconcile happens inside the same post at `:396`); the count
predicate wins only for mirroring the sibling exactly. Rejected micro-expansion: asserting
`IsWatching == false` / `ScannedDirectoryCount == 0` after `Stop` — minimal-diff principle; the
sibling already pins the count-clearing contract.

**D3 — skip_specs: true.** The change touches one test's wait predicate; no requirement's
observable truth changes, and the repo precedent is that test-only changes take no deltas
(low-sweep proposal, "Test-only changes never take deltas"). The spec check for the *rejected*
production path (Current state) shows there would have been no scenario to modify there either —
`Stop`'s interplay with an in-flight scan is unspecified — so no hidden delta obligation exists
on either branch of the fix-placement decision.

**D4 — Flake acceptance bar: structural argument + 6 consecutive green full-solution runs,
honestly bounded.** The failure only reproduces under full-solution parallel load, so the plan
is:

1. Primary proof: the D1 determinism argument (after the settle predicate, `Stop`'s post is the
   only remaining status write in the scenario).
2. Empirical bar: 6 consecutive green `dotnet test CLIHub.sln` runs — unfiltered, the same load
   context the failures came from — with `--logger trx` so any failure names its test (the
   sibling backlog entry's standing remedy). Statistical rationale, stated honestly: the observed
   failure rate under load was 1/3 (2026-10-20) and 2/3 (2026-10-24); if the unfixed test failed
   at ≥ 1/3 per run, six consecutive greens have probability ≤ (2/3)^6 ≈ 8.8% (≈ 1.6% at a 1/2
   rate). That is a confidence check, not a proof — a rarer interleaving or a different
   machine/CI load profile could evade six runs — which is exactly why the structural argument
   carries the weight and the runs are the regression check. If the user wants a tighter bound,
   open question Q1 offers 10 runs (≤ (2/3)^10 ≈ 1.7%).
3. Failure protocol: any red run of this test during verification stops the stage — capture the
   trx, diagnose against design "Current state", and report; never weaken the predicate or bump
   the timeout to make a run green (deviation protocol).

**D5 — Backlog bookkeeping lands as a commit-time docs stage.** Per current policy the flake
entry's rewrite to the resolved format happens at commit/archive time (the orchestrator executes
it; the tasks stay unticked until then), matching how low-sweep staged its backlog rewrites with
the checkboxes ticked only at the end. The sibling entry gets an evidence update, not a closure:
add that the confirmed flake (`Stop_DisposesWatcherAndClearsStatus`; failed 2026-10-20 during
fix-settings-open-delay verification and 2026-10-24 during low-sweep verification,
trx-captured) is the strongest candidate for the uncaptured 2026-10-20 `CLIHub.Core.Tests`
failure — same day, same test project, same full-solution load shape, and the low-sweep planning
prediction — but the name was never captured, so it is "probable same test, not proven";
`UpdateCheckerTests`/`UpdateDownloaderTests` remain unexcluded candidates and the entry stays
open under its existing trigger. Default disposition: keep open (open question Q2).

## Alternatives considered

- **Wait after `Stop` for `NotConfigured` (poll the asserted state):** rejected — provably wrong.
  Once a late scan-final `Idle` lands after `Stop`'s publish, no code path ever republishes
  `NotConfigured` (`Stop` is a single post; the scan loop exits; the fake timer never fires), so
  the poll would run to its 5 s timeout and the test would still fail — slower, same flake.
- **Production: `Stop` blocks until the in-flight scan drains (e.g. waiting on the scan gate):**
  rejected — `Stop` is synchronous `void`; blocking the (UI) caller for a disk walk is unbounded,
  and no production caller exists to benefit (census). Rejected even though spec-compatible (D3).
- **Production: generation/epoch guard so stale scan posts are discarded after `Stop`:** rejected
  for this change — a real `IProjectScanner` semantics change (post-stop reconcile/publish
  suppression), new production code, and new tests, all to fix a test that settles
  deterministically today; the repo uses that pattern where a surface *is* re-entrant (agent
  version population), not for a lifecycle stop with no callers.
- **Settle predicate `reconciliations.Count > 0`:** equal-valid alternative (reconcile is inside
  the scan-final post, `:396`); `ScannedDirectoryCount == 2` chosen to mirror the sibling test
  exactly (D2).
- **Inject a fake discovery/task seam to drive the scan synchronously:** rejected — no such seam
  exists (`RunScanAsync` calls `RepositoryDiscovery.Enumerate` via `Task.Run` directly,
  `:388-391`); adding one is a bigger production change than the Stop alternatives above (D1.3).
- **Strengthening the post-`Stop` asserts (`IsWatching`, count 0):** rejected — scope creep; the
  sibling `Stop_ClearsScannedDirectoryCount` already pins the count-clearing contract.
- **Also stabilizing `UpdateCheckerTests`/`UpdateDownloaderTests` (the other sibling-entry
  candidates):** rejected — unconfirmed, own backlog trigger; bundling would blur this change's
  verification story.

## Deviations

None. The backlog entry's mechanism and line citation were re-verified and hold as written; the
trigger fired as stated. One precision worth recording: only the status assert (`:132`) is racy —
the `Disposed` assert (`:131`) cannot fail in this test because `Configure` completes on the test
thread inside `Start()` (Current state, precision note 1). This narrows the entry's mechanism
description without changing it.

## Open questions

1. **Acceptance-bar run count — resolved by the user (2026-10-24): raised to 10.** Stage 2 gates on
   10 consecutive green full-solution runs (task 2.3's extension applies; the statistical bound at
   the worst observed failure rate is ≈ 1.7%).
2. **Sibling backlog entry disposition (default: keep open, evidence updated).** D5 updates
   "Single unexplained test failure (not reproduced)" with the probable-identity evidence but
   leaves it open. If the user prefers to resolve it now (accepting the unproven identity) or to
   drop the other candidates from its evidence, the docs-stage task wording changes accordingly.
