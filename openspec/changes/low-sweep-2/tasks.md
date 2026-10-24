# Tasks: low-sweep-2

Stages 1–2 are the two implemented items (code+tests, spec delta), 3 is the backlog rewrite,
4 is final verification. Item B (running-agent cache) has no implementation stage by design
(design B1–B3: dropped as resolved-by-design). Each stage ends with a gate; do not start the
next stage on a red gate. The items are independent — a red gate in one stage never justifies
reverting another stage's work. Per `AGENTS.md`, run GitNexus impact
(`node .gitnexus/run.cjs impact "<symbol>" --direction upstream --repo .`) before the edits in
each code stage and `detect-changes` before any commit; the caller census in `design.md`
("Current state") is the baseline any impact result must agree with — if impact reports a
caller not listed there, stop and reconcile before editing. Open questions Q1–Q4 (design
"Open questions") default as written; apply a contrary decision before its stage instead of
mid-stage.

## 1. Stage 1 — Item A: copy projects across the ProjectState boundary

- [x] 1.1 Run impact for `ProjectState`, `ProjectStateStore`, and `ProjectService` and compare
      with design "Current state" (callers: `ProjectStateStore.Load`/`Update`,
      `ProjectService.Persist` via `CopyTo`, the `AppConfigSerializationTests` pair, DI
      registration `ServiceCollectionExtensions.cs:85-88` — nothing else). Stop and reconcile
      on any surprise.
- [x] 1.2 Edit `src/CLIHub.Core/Models/Projects/ProjectState.cs` per design A1 — copy items at
      all five crossing points with `Project.Copy()`:
      `From(AppConfig)` (:31) and `From(ConfigurationSnapshot)` (:48) become
      `AddRange(source.Projects.Select(project => project.Copy()))`;
      `ApplyTo(AppConfig)` (:58) and `ApplyTo(ConfigurationSnapshot)` (:70) become
      `target.Projects = this.Projects.Select(project => project.Copy()).ToList();`
      (keep the null-guard on the snapshot overload); `CopyTo` (:83) becomes
      `AddRange(this.Projects.Select(project => project.Copy()))`. Update each method's XML doc
      `<summary>`/`<returns>` wording where it says "copies" so it states detached copies
      explicitly (e.g. "Copies the owned project state — each project detached — into …").
      No signature, namespace, or using changes beyond `System.Linq` if not already implicit.
- [x] 1.3 Create `tests/CLIHub.Core.Tests/Projects/ProjectStateTests.cs` per design A3: five
      facts, one per crossing point, each `cross → mutate one side → assert the other side
      unchanged` (suggested names:
      `From_AppConfig_MutatingSourceAfterwards_StateKeepsOriginalValues`,
      `From_Snapshot_MutatingSourceAfterwards_StateKeepsOriginalValues`,
      `ApplyTo_AppConfig_MutatingStateAfterwards_ConfigKeepsOriginalValues`,
      `ApplyTo_Snapshot_MutatingStateAfterwards_SnapshotKeepsOriginalValues`,
      `CopyTo_Target_MutatingSourceAfterwards_TargetKeepsOriginalValues`); seed one project
      with `IsFavorite = false`, flip it to `true` on the mutating side, assert `false` on the
      other. Follow the repo test naming pattern and the file shape of
      `ProjectStateStoreTests.cs` (namespace, usings after namespace).
- [x] 1.4 Extend `tests/CLIHub.Core.Tests/Projects/ProjectStateStoreTests.cs` per design A3:
      (a) `Load_MutatingReturnedProjects_DoesNotChangeStoredState` over the existing
      `FakeConfigurationRepository` (mutate `state.Projects[0]`, assert `store.Load()` keeps
      the original value — pins the already-correct direction); (b)
      `Update_MutatingCallbackProjectAfterCompletion_DoesNotChangeStoredState` over a **real**
      `ConfigurationRepository` created on a temp config path (mirror the temp-dir/logger
      construction from `ConfigurationRepositoryTests.cs:8-27`; implement `IDisposable` for
      cleanup the same way), capture `state.Projects[0]` inside the `Update` callback, flip
      `IsFavorite` after `Update` returns, assert `repository.Read()` still shows the original
      value. Include the one-line comment that the fake would mask this
      (`FakeConfigurationRepository.cs:39` re-copies; the real one adopts the callback's
      snapshot, `ConfigurationRepository.cs:81`).
- [x] 1.5 Per design A5 and open question Q2 (default: rename): rename
      `ProjectState_TransfersProjectOwnershipWithoutChangingValues`
      (`tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs:279`) to
      `ProjectState_ApplyTo_CopiesProjectsWithoutSharingReferences` — assertions and body
      unchanged. If Q2 was answered "keep", skip and record that in the stage report.
- [x] 1.6 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures; the
      seven new facts pass; the unchanged assertions in `ProjectStateStoreTests`,
      `ConfigurationRepositoryTests` (`Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated`,
      `Read_MutatingReturnedSnapshotCollections_DoesNotChangeStoredState`), and
      `AppConfigSerializationTests` pass unedited.

## 2. Stage 2 — Item C: agent-commands wording delta

- [x] 2.1 Confirm the delta file `openspec/changes/low-sweep-2/specs/agent-commands/spec.md`
      against design C2/C3: MODIFIED requirement "Terminal vs captured execution" (name
      byte-identical), the requirement's bash clause plus the three scenario edits ("Git Bash
      resolves to Git for Windows" THEN, WSL scenario AND, "Unresolvable Git Bash fails
      safely" WHEN — see C2), all nine existing scenario names preserved, one ADDED scenario
      "PowerShell 7 resolves through any PATH entry". If review answered Q1 with "distribution
      validation instead" (design C4), stop — this stage becomes a rescope, not an edit.
- [x] 2.2 Gate: `openspec validate --all` passes (the change's delta parses and the delta's
      requirement/scenario names match the main spec). No build is needed — no production or
      test file changed in this stage; verify with `git status --porcelain` that only the
      change directory is touched.

## 3. Stage 3 — `docs/backlog.md`: three resolved-format rewrites

Rewrite each swept entry to the resolved format (heading ` (resolved)`, bullets
`Status:` / `Resolved model:` / `Guarded by:` / `Trigger:`), following the existing resolved
entries in `docs/backlog.md`. One edit per entry — three separate hunks; every other line
untouched.

- [x] 3.1 Rewrite "ProjectState shares live items at the persistence boundary"
      (`docs/backlog.md:220-225`): resolved by the low-sweep-2 change (2026-10-24). Resolved
      model: all five `ProjectState` crossing points (`From`/`ApplyTo` ×2, `CopyTo`) copy
      projects with the canonical `Project.Copy()`, and `ApplyTo` stores a fresh list of
      copies, so the repository's cached snapshot never aliases `ProjectService`'s live
      `Project` instances and in-place service mutations (`SetCurrentProject`,
      `ToggleFavorite`) can no longer leak into stored state outside an update transaction.
      Guarded by the five `ProjectStateTests` isolation facts and the two
      `ProjectStateStoreTests` facts (including the real-repository post-transaction mutation
      fact). Trigger: any future persistence-boundary change or a new `ProjectState` consumer.
- [x] 3.2 Rewrite "Monitor-level running-agent cache" (`docs/backlog.md:52-63`): status
      resolved-by-design, no code — the `commandline-agent-blocking` tier restructure
      (2026-10-24) is the resolution: the requery availability check runs the cheap
      executable-name tier (no WMI, spec-pinned advisory), both execution-time gates run the
      full command-line tier off the UI thread under the 2-second WMI budget, and the
      Settings blocked-state pass is async and cancellable; the residual per-requery cost is
      one name-filtered `Process.GetProcesses` snapshot. A cache was evaluated and rejected:
      gates must never be stale (`agent-process-matching` "Execution-time checks use the
      command-line tier"), and an advisory TTL cache has no provably fresh key and no
      profiling evidence. Guarded by the existing
      `WindowActionCoordinatorTests`/`AgentProcessMonitorTests` tier tests. Trigger (reopen):
      profiling shows the filtered enumeration as a hot spot on a requery path.
- [x] 3.3 Rewrite "Runtime executable PATH-scan identity (Git Bash and PowerShell 7)"
      (`docs/backlog.md:134-146`): resolved by the low-sweep-2 change (2026-10-24) via the
      spec-wording remedy — the `agent-commands` "Terminal vs captured execution" requirement
      now states the actual guarantee (fixed preference order, absolute path, WSL-launcher
      exclusion, PATH hits accepted without distribution verification for bash; pwsh PATH
      acceptance including the Store execution alias pinned by an ADDED scenario); no
      production change. Distribution validation of bash PATH hits was evaluated and rejected
      (heuristic false positives on Cygwin/MSYS2-with-git, false negatives on portable
      layouts, safe-fail regression risk). Guarded by the unchanged locator facts
      `Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed`,
      `Locate_GitBash_WslLauncherOnPath_ReturnsNull`,
      `Locate_PowerShell7_PathEntryIsUsed`,
      `Locate_PowerShell7_PathEntryInsideWindowsDirectoryIsUsed`. Trigger (reopen): a user
      report of a wrong bash/pwsh flavor being launched.
- [x] 3.4 Gate: `git diff docs/backlog.md` shows exactly the three entry hunks and nothing
      else; `openspec validate --all` still passes.

## 4. Stage 4 — final verification

- [x] 4.1 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures (full
      re-run, not cached results).
- [x] 4.2 Gate: `openspec validate --all` passes.
- [x] 4.3 `graphify update .` to refresh the knowledge graph after the source edits.
- [x] 4.4 Review `git status --porcelain` and `git diff --stat`: only files belonging to this
      change (the `openspec/changes/low-sweep-2/` tree, `src/CLIHub.Core/Models/Projects/ProjectState.cs`,
      the three test files, `docs/backlog.md`) are modified; `WindowsRuntimeLocator.cs`,
      `AgentProcessMonitor`/`WindowActionCoordinator`/`WindowsAgentProcessInspector`, the
      locator tests, and every other parked backlog entry are untouched. Run
      `node .gitnexus/run.cjs detect-changes --scope all --repo .` per `AGENTS.md`, report,
      then wait for the user's commit/archive approval per the repo policy.
