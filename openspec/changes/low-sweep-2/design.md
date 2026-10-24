# Design: low-sweep-2

All line numbers below were verified against the current working tree on 2026-10-24 (clean tree,
no active changes, `openspec validate --all` baseline: 29 passed / 0 failed). Backlog citations
that drifted are called out inline. Item letters: A = ProjectState boundary, B = running-agent
cache, C = runtime PATH-scan identity.

## Current state (verified)

### Item A — ProjectState shares live items at the persistence boundary

Backlog claim ("`ProjectState.CopyTo`/`ApplyTo` share `Project` references with the cached
snapshot", citing `src/CLIHub.Core/Models/Projects/ProjectState.cs:31,48,58,70,83`):
**confirmed exactly — all five line numbers are byte-accurate today.**

| Crossing point | Code | What is shared |
|---|---|---|
| `From(AppConfig)` | `ProjectState.cs:31` — `state.Projects.AddRange(config.Projects);` | item references from the config |
| `From(ConfigurationSnapshot)` | `ProjectState.cs:48` — `state.Projects.AddRange(snapshot.Projects);` | item references from the snapshot |
| `ApplyTo(AppConfig)` | `ProjectState.cs:58` — `config.Projects = this.Projects;` | the live `List<Project>` **and** its items |
| `ApplyTo(ConfigurationSnapshot)` | `ProjectState.cs:70` — `snapshot.Projects = this.Projects;` | the live `List<Project>` **and** its items |
| `CopyTo(ProjectState)` | `ProjectState.cs:82-83` — `target.Projects.Clear(); target.Projects.AddRange(this.Projects);` | item references (target keeps its own list) |

The live cycle that makes this a real (today benign) bug, verified end to end:

1. `ProjectService` holds one `ProjectState` for its lifetime
   (`src/CLIHub.Core/Projects/ProjectService.cs:30`).
2. Every mutation funnels through `Persist()` (`ProjectService.cs:272-276`) →
   `ProjectStateStore.Update` (`src/CLIHub.Core/Projects/ProjectStateStore.cs:30-35`):
   the repository clones its cached snapshot (`src/CLIHub.Core/Configuration/ConfigurationRepository.cs:77`),
   `ProjectState.From(snapshot)` hands the clone's items to a fresh state, the service's
   `CopyTo` replaces them **with the service's live instances**, and `ApplyTo` stores the
   state's list into the snapshot — which the repository then adopts as
   `cachedSnapshot` (`ConfigurationRepository.cs:81`) **without re-copying**.
3. From that moment on, `ProjectService`'s in-place mutators — `SetCurrentProject`
   (`ProjectService.cs:238`, `project.LastUsed = DateTime.UtcNow;`) and `ToggleFavorite`
   (`ProjectService.cs:252`, `project.IsFavorite = !project.IsFavorite;`) — mutate the very
   instances inside the repository's cached snapshot, outside any update transaction. The
   repository's own boundary is deep-copy on the way out
   (`Read()` → `cachedSnapshot.Clone()`, `ConfigurationRepository.cs:59-66`;
   `Clone()` = `From(ToAppConfig())`, both copy projects,
   `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:36,48,57`), so `Load()` results stay
   detached — the leak is one-directional: **service-owned state into stored state**.

Why "today benign": both in-place mutators immediately persist equivalent values, and
`ProjectService` read methods hand out `Copy()` (`ProjectService.cs:36,49,63,84,91,112,119`).
Why still worth fixing: the documented boundary contract is violated
(`ConfigurationSnapshot` is "Detached, mutable-in-isolation configuration state",
`ConfigurationSnapshot.cs:6`; `IProjectStateStore.Update` promises "detached project state",
`src/CLIHub.Core/Projects/IProjectStateStore.cs:17-19`), and the atomicity guarantee
("Failed store update does not persist partial state",
`openspec/specs/configuration-snapshot/spec.md:27-30`) has a hole — an in-place mutation
already sits in `cachedSnapshot` before the transaction runs, so a failing `Persist` leaves
stored state diverged from disk. Any future in-place mutator that does not persist, or any new
`ProjectState` consumer that mutates a returned project, corrupts registered state silently.

Caller census of the `ProjectState` boundary (codegraph blast radius + grep
`\.CopyTo\(|\.ApplyTo\(|ProjectState\.From\(` over `src/` and `tests/`):

| Caller | Site | Expectation |
|---|---|---|
| `ProjectStateStore.Load` | `ProjectStateStore.cs:23` | read-only; result is service-owned (fed to `ProjectService`) |
| `ProjectStateStore.Update` | `ProjectStateStore.cs:32,34` | detached write-through; deep-copying only strengthens the documented "detached" contract |
| `ProjectService.Persist` | `ProjectService.cs:274` (the only `CopyTo` caller) | writes the service's values; relies on value equality (`Id`/`Path`/`ProjectPathPolicy.AreEqual`, e.g. `ProjectService.cs:81,140,156,231,246`), never on reference identity |
| `AppConfigSerializationTests` | `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs:290,294` | the only `From(AppConfig)`/`ApplyTo(AppConfig)` consumers (production-unused pair); assertions are value-based (`:296-297`) and pass under copy semantics |
| `PluginSeeder.cs:118` | `source.CopyTo(destination)` | unrelated local `CopyTo`, not `ProjectState` |

No `ReferenceEquals` on `Project` exists anywhere in `src/` (grep; the hits are WPF plumbing in
`src/CLIHub/Views/*` and a converter). DI shape: `IProjectStateStore` → `ProjectStateStore`
singleton (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:85-88`); unchanged.

Existing tests that touch the boundary and must keep passing:
`ProjectStateStoreTests` (`tests/CLIHub.Core.Tests/Projects/ProjectStateStoreTests.cs` — two
value-based facts), `ConfigurationRepositoryTests`
(`Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated`,
`Read_MutatingReturnedSnapshotCollections_DoesNotChangeStoredState` — the repository side),
`AppConfigSerializationTests.ProjectState_TransfersProjectOwnershipWithoutChangingValues`
(`:278-298`). Note the last test's **name** documents the aliasing semantics ("transfers
ownership") even though its assertions are value-based and survive the fix — see decision A5.

Spec check (the skip_specs gate for this item): `configuration-snapshot` already requires
detached owned values at this exact boundary — "The preferences and project state stores SHALL
expose read operations and explicit update operations that work on detached owned values"
(`openspec/specs/configuration-snapshot/spec.md:71-73`) with "Read values are detached from the
store" (`:80-83`). No scenario anywhere pins sharing; the fix moves the implementation **under**
the existing contract. No delta.

Test-mechanics fact that shapes the tests: `FakeConfigurationRepository.Update` re-copies after
the callback (`this.snapshot = ConfigurationSnapshot.From(updated.ToAppConfig());`,
`tests/CLIHub.Core.Tests/Fakes/FakeConfigurationRepository.cs:39`), which launders the aliasing —
store-level isolation tests that must fail today have to run against the **real**
`ConfigurationRepository` (its `Update` adopts the callback's snapshot object directly,
`ConfigurationRepository.cs:81`). The real-repo construction pattern (temp dir + logger +
`Flush`/`Dispose`) is `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs:8-27`.

### Item B — monitor-level running-agent cache

Backlog claim (`docs/backlog.md:52-63`): `GetRunningAgents` "enumerates on every call — once
per Settings load, on every `CommandManager` requery of the launch window Update command
(`WindowActionCoordinator.CanExecuteUpdate`), and in the execution-time gate
(`AgentManagementService.UpdateAsync`)"; remedy "only with an explicit invalidation story".
**The three cost sites are real, but every one of them was restructured after the item was
parked.** Verified current state:

| Original cost site | Current state | Evidence |
|---|---|---|
| Settings load (synchronous) | Blocked-state pass is async, cancellable, generation-guarded, off the UI thread, runs once per `Load` and only when a plugin declares `update` | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:346-392` (enumeration at `:363` inside `Task.Run`), resolved by the `async-blocked-state` change (2026-10-24) |
| Every `CommandManager` requery | Advisory tier only: executable-name matching, **no WMI, no command-line retrieval** | `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:146-164` (`CanExecuteUpdate`, tier at `:158`, advisory comment `:154-157`); pinned by `openspec/specs/agent-process-matching/spec.md:93-96` ("Requery availability uses the executable-name tier") |
| Execution-time gates | Full tier, once per user action, inside `Task.Run`, bounded by the hard 2-second WMI budget | `src/CLIHub.Core/Agents/AgentManagementService.cs:79-81`; `WindowActionCoordinator.cs:193`; budget at `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:26` and `:168-201` |

What one advisory-tier call actually costs today
(`WindowsAgentProcessInspector.GetRunningProcesses`, `WindowsAgentProcessInspector.cs:51-156`):
one `Process.GetProcesses()` snapshot (`:62`), a name pre-filter against the candidate set
(`:80`), and — only for candidate hits — `MainModule?.FileName` (`:91`) and `StartTime` (`:109`)
reads. Under `ExecutableName` detail the WMI path is never entered (`:137-143`). This matches
the backlog's own residual assessment: "the residual cost is low"
(`docs/backlog.md:58-59`).

Caller census (grep `GetRunningAgents|HasRunningAgents` over `src/`, cross-checked with
codegraph): the only recurring call is `WindowActionCoordinator.cs:158` (requery);
`:176` (click-time fast block) and `AgentManagementService.cs:80` /
`WindowActionCoordinator.cs:193` / `AgentManagementViewModel.cs:363` are per-action or
per-load. The parameterless `HasRunningAgents()` (`AgentProcessMonitor.cs:41-44`) has no
production caller.

### Item C — runtime executable PATH-scan identity

Backlog claim: the PATH steps of `LocateGitBash` (`docs/backlog.md:137` cites
`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:233-303`) and `LocatePowerShell7` (`:304-369`)
accept the first matching executable, and the `agent-commands` wording ("never an arbitrary
`bash.exe` resolved from the operating system path") is stronger than that guarantee.
**Confirmed with corrected line numbers** — the backlog spans drifted because `low-sweep`
restructured the registry step into a verify-then-prefer loop, moving everything down:

- `LocateGitBash` is now `WindowsRuntimeLocator.cs:268-336`; its PATH step is `:294-335` —
  entries under the Windows directory are rejected (`:297-303`), and the first existing
  `bash.exe` on PATH wins (`:316-328`) with **no distribution verification** (no `git.exe`
  sibling or version-file check anywhere in the method).
- `LocatePowerShell7` is now `:338-402`; its PATH step is `:369-395` with the deliberate no-
  Windows-directory-exclusion comment at `:366-368` (Microsoft Store installs surface as a
  `WindowsApps` execution alias on PATH).
- The registry step that `low-sweep` added verifies only **existence** of `<InstallPath>\bin\bash.exe`
  (`TryGitCandidate`, `:446-471`, existence check `:464`) inside a registry-declared install
  root — there is no pattern to "extend" to PATH hits, because a PATH hit carries no install
  root to verify against (see decision C4).

Spec tension, verified: the requirement "Terminal vs captured execution"
(`openspec/specs/agent-commands/spec.md:64-66`) says the interactive command "SHALL start a Git
for Windows `bash.exe` and **never an arbitrary `bash.exe` resolved from the operating system
path**" (`:66`) — while the same requirement's scenario sanctions "a PATH entry outside the
Windows directory" as the third source ("Git Bash resolves to Git for Windows", `:99-103`), and
the WSL scenario (`:105-109`) pins only the Windows-directory exclusion. The requirement prose
over-claims; the scenario's phrase "the absolute path of a Git for Windows `bash.exe` …
resolved from … a PATH entry outside the Windows directory" also frames an unverified PATH hit
as a Git for Windows executable.

Existing coverage that constrains any remedy: locator tests
`Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed`
(`tests/CLIHub.Core.Windows.Tests/WindowsRuntimeLocatorTests.cs:80`),
`Locate_GitBash_WslLauncherOnPath_ReturnsNull` (`:94`),
`Locate_PowerShell7_PathEntryIsUsed` (`:188`),
`Locate_PowerShell7_PathEntryInsideWindowsDirectoryIsUsed` (`:205`) — the first pins that a
PATH hit is *used* (so remedy C4's rejection would be a behavior change, not a wording change),
the pwsh pair pins the deliberate acceptance. The `low-sweep` design explicitly left this item
parked ("Validating Git Bash PATH hits against the distribution … stays parked under its own
trigger", archived design `:310-311`; proposal `:126-127`).

## Decisions

### Item A

**A1 — Copy items at all five crossing points, using `Project.Copy()`.** The backlog's remedy
direction, verbatim. `Project.Copy()` is the canonical detached copy
(`src/CLIHub.Core/Models/Projects/Project.cs:54-63`, documented "so callers can mutate a
returned record without corrupting registered project state") and is what the sibling boundary
methods already use (`ConfigurationSnapshot.From`/`ToAppConfig`,
`ConfigurationSnapshot.cs:36,48`; every `ProjectService` read). Concretely:

- `From(AppConfig)` / `From(ConfigurationSnapshot)`:
  `state.Projects.AddRange(source.Projects.Select(project => project.Copy()));`
- `ApplyTo(AppConfig)` / `ApplyTo(ConfigurationSnapshot)`:
  `target.Projects = this.Projects.Select(project => project.Copy()).ToList();` — keeps the
  existing assignment shape (callers treat the target as replaced; nobody holds the old list
  across the call — verified: the only snapshot target is the repository's fresh per-update
  clone, `ConfigurationRepository.cs:77`, and the `AppConfig` target is test-only).
- `CopyTo`: `target.Projects.Clear(); target.Projects.AddRange(this.Projects.Select(project => project.Copy()));`

**A2 — Observable-behavior audit: no caller changes.** The only observable differences are the
removal of the leak (intended) and the list-instance swap in `ApplyTo` (no holder exists). All
existing callers were enumerated (Current state table); comparisons are value-based; the two
in-place mutators persist equivalent values immediately, so persisted output is byte-identical
before and after. `ProjectStateStoreTests` and `AppConfigSerializationTests` assertions are
value-based and stay green.

**A3 — Tests: five boundary facts (fail today) + two store facts (one fails today).**
New `tests/CLIHub.Core.Tests/Projects/ProjectStateTests.cs`, one fact per crossing point, each
shaped `cross the boundary → mutate one side → assert the other side unchanged` (e.g.
`From_Snapshot_MutatingSourceAfterwards_StateKeepsOriginalValues`,
`ApplyTo_Snapshot_MutatingStateAfterwards_SnapshotKeepsOriginalValues`,
`CopyTo_Target_MutatingSourceAfterwards_TargetKeepsOriginalValues`, plus the two `AppConfig`
variants). All five fail on the current tree (shared references). In
`ProjectStateStoreTests`: (a) `Load_MutatingReturnedProjects_DoesNotChangeStoredState` — passes
today (repository `Read` clones); pins the returned-state → snapshot direction; (b)
`Update_MutatingCallbackProjectAfterCompletion_DoesNotChangeStoredState` — real
`ConfigurationRepository` over a temp config path (construction mirrored from
`ConfigurationRepositoryTests.cs:8-27`), capture `state.Projects[0]` inside the callback, mutate
it after `Update` returns, assert `repository.Read()` still shows the original value. This one
**fails today** precisely because `ConfigurationRepository.cs:81` adopts the aliased snapshot;
the fake would mask it (`FakeConfigurationRepository.cs:39` re-copies) — the test comment must
say so. Together (a)+(b) prove isolation in both directions the task requires
(snapshot → returned state, returned state → snapshot).

**A4 — No `ApplyTo` list-mutation alternative.** Mutating the target's existing list
(`Clear`+`AddRange`) instead of assigning a new list was considered and rejected: it changes
more code for zero caller benefit (A1 audit) and blurs the "the target is replaced" reading of
the current assignment shape.

**A5 — Rename the misnomer test.** `ProjectState_TransfersProjectOwnershipWithoutChangingValues`
(`AppConfigSerializationTests.cs:279`) names the aliasing semantics being removed; after A1 its
name would document false behavior. Rename to
`ProjectState_ApplyTo_CopiesProjectsWithoutSharingReferences` (assertions unchanged, matching
the repo's `MethodOrScenario_Condition_ExpectedResult` pattern). Optional by open question Q2.

### Item B

**B1 — Drop the item as resolved-by-design.** The precondition that made a cache thinkable —
an expensive enumeration recurring on a hot path — no longer exists. The requery path is the
cheap tier (no WMI; `WindowActionCoordinator.cs:158`), spec-pinned as advisory
(`agent-process-matching` spec.md:85-91, scenario `:93-96`); the authoritative gates are
full-tier but per-action, off the UI thread, and WMI-bounded
(`AgentManagementService.cs:79-81`, `WindowActionCoordinator.cs:193`,
`WindowsAgentProcessInspector.cs:26`); the Settings-load site is async and cancellable
(`AgentManagementViewModel.cs:346-392`). The backlog's own residual-cost note ("low",
`docs/backlog.md:58-59`) and its trigger ("profiling shows the filtered enumeration as a hot
spot on a requery path", `:63`) both point the same way: no profiling evidence exists, and the
trigger never fired.

**B2 — Every cache design fails the invalidation bar.** (i) TTL on the advisory tier: nothing
is provably fresh — process starts/exits are unobservable without new infrastructure — so a
stopped agent keeps the launch-window Update command disabled for up to the TTL (a UX regression
the advisory/gate split deliberately avoids), and the cache would interact with the
selection-change requery requirement ("update command enabled/disabled states immediately when
agent selection changes", `openspec/specs/agent-ui/spec.md:109-111`) for zero evidenced benefit.
(ii) Caching the gates: forbidden outright — "Execution-time checks use the command-line tier"
(`agent-process-matching` spec.md:98-102) and the blocked-state scenarios
(`agent-ui` spec.md:233-236, `preferences-ui` spec.md:546-551) do not permit a stale gate to
authorize an update while an agent runs. (iii) WMI process-start/stop watchers for eager
invalidation: always-on background infrastructure with admin/policy sensitivity and new failure
modes — disproportionate for a Low item with no observed cost. If profiling ever fires the
reopen trigger, (i) with a short TTL is the least-bad design and should be specced then.

**B3 — The backlog entry is rewritten to resolved-by-design** (implementation stage 3): the
`commandline-agent-blocking` tier design is recorded as the resolution; reopen trigger stays
"profiling shows the filtered enumeration as a hot spot on a requery path". No code, no tests,
no delta.

### Item C

**C1 — Remedy: tighten the spec wording (option a), not distribution validation.** The
behavior is deliberate, shipped, test-pinned (`WindowsRuntimeLocatorTests.cs:80,188,205`), and
was approved as a lookup order (archived `2026-10-20-git-bash-resolution`; reaffirmed by
`low-sweep` design E6). The defect is that the requirement prose promises more than that order
delivers. A wording fix removes the over-claim without touching behavior; see C4 for why the
code-side remedy loses.

**C2 — Delta shape: MODIFIED "Terminal vs captured execution", scenario names preserved.** The
bash clause becomes: the interactive command SHALL start an absolutely resolved `bash.exe`
found through the fixed preference order — `GitForWindows` registry `InstallPath`, a well-known
install location, then a PATH entry outside the Windows directory — SHALL never use the WSL
launcher `bash.exe` in the Windows directory, and a PATH hit is accepted as-is without
verifying that it belongs to a Git for Windows installation. The pwsh clause ("resolved
PowerShell 7 `pwsh.exe` in the same way rather than a bare name") is accurate under the new
wording — "the same way" now names the same preference-order shape — and stays. All nine
existing scenario names are kept byte-for-byte; three content edits, each removing the same
over-claim: the "Git Bash resolves to Git for Windows" THEN line (it currently frames an
unverified PATH hit as "a Git for Windows `bash.exe`"), the WSL scenario's AND line ("starts a
resolved Git for Windows `bash.exe`" → "starts a `bash.exe` resolved through the preference
order"), and the "Unresolvable Git Bash fails safely" WHEN ("no Git for Windows `bash.exe` can
be resolved" → "no `bash.exe` can be resolved through the preference order"; its THEN stays —
the actual failure log names Git for Windows, `WindowsRuntimeLocator.cs:331-334`). One ADDED
scenario pins the deliberate pwsh PATH acceptance (C3).

**C3 — ADDED scenario for the pwsh PATH acceptance.** The pwsh half of the item ("for pwsh
deliberate, because Microsoft Store installs surface as a WindowsApps execution alias",
`docs/backlog.md:139-141`) is today only a code comment
(`WindowsRuntimeLocator.cs:366-368`) and two tests; the spec never says PATH acceptance is
intended. One scenario under the same requirement states it: a PowerShell 7 install that
surfaces only through a PATH entry (for example the Store execution alias) resolves and launches
from that entry, and — unlike Git Bash — PATH entries inside the Windows directory are not
excluded because `pwsh.exe` has no WSL-launcher namesake there. This is the contract-level
record of the deliberateness the backlog asserts.

**C4 — Rejected: distribution validation for bash PATH hits (option b).** Extending the
registry step's verify-then-prefer pattern to PATH hits is category-confused: the registry step
verifies existence of `<InstallPath>\bin\bash.exe` inside a root the registry names
(`WindowsRuntimeLocator.cs:446-471`); a PATH hit has no declared root. Real validators are
heuristics with both failure directions: a `git.exe` sibling accepts Cygwin/MSYS2 layouts that
also install `git.exe` into the same `bin` (false positive), and a `..\cmd\git.exe` probe
(the closest Git-for-Windows-specific marker) rejects portable or hand-assembled layouts whose
`bash.exe` today resolves and launches fine (false negative — and the failure mode is "Git Bash
unresolvable", the safe-fail path, a regression for a currently-working setup). All of this is
behavior change gated on a heuristic, for a Low item whose actual defect is documentation
honesty. If a wrong-flavor report ever lands (the original trigger), C4 can be reopened with
real-world layout data; the rewritten backlog entry keeps that trigger.

**C5 — No production or test changes for item C.** The locator, its warning text
(`WindowsRuntimeLocator.cs:331-334` — "no Git for Windows bash.exe was found" stays honest as
the failure-path summary), and all locator tests are untouched; wording-only deltas cannot
break tests. `openspec validate` gates the delta instead of a build.

## Alternatives considered

- **Item A: leave it benign** (document the invariant instead of enforcing it): rejected — the
  boundary's own docs already promise detachment (`IProjectStateStore.cs:17-19`), so the code
  is the outlier, and the atomicity hole (Current state) is exactly the "caller mutating state
  corrupts registered state" class the task asks to close with tests.
- **Item A: fix only `ApplyTo`/`CopyTo`, leave `From` sharing** (load-time sharing is
  effectively private): rejected — `From(ConfigurationSnapshot)` items flow into
  `ProjectService`'s live state; a future second consumer reading through the store would get
  entangled instances. Five one-line fixes are cheaper than the audit needed to prove two of
  them safe to skip.
- **Item A: make `Project` immutable / a record**: rejected — changes the config-document
  model shape far beyond the boundary bug; not the backlog remedy.
- **Item B: short-TTL advisory cache anyway**: rejected — see B2; no evidence, new staleness
  class, spec-adjacent risk.
- **Item B: move `CanExecuteUpdate`'s check fully off the requery path** (drop the advisory
  check and rely on gates only): rejected — the advisory early-out is what keeps the menu
  honest for directly-named agents; the spec explicitly permits it
  (`agent-process-matching` spec.md:88-91).
- **Item C: distribution validation (option b)**: rejected — see C4.
- **Item C: also reword the pwsh requirement clause**: rejected — the existing clause is
  accurate; only its referent ("in the same way") needed defining, which C2 does.

## Deviations

None. All three items were verified as open before planning; item B's drop is the brief's
explicitly-sanctioned outcome ("recommend DROPPING the item as resolved-by-design … that is a
legitimate outcome, not padding"), recorded here and in the proposal, not a deviation from the
code. Item C's backlog line numbers were corrected (`WindowsRuntimeLocator.cs` PATH steps are
now `:294-335` / `:369-395`); the claim itself needed no correction.

## Open questions — resolved by the user (2026-10-24)

All four resolved by accepting the defaults; the item-B drop was separately confirmed:

1. **Item C remedy: wording (C1/C2)** — accepted; distribution validation stays rejected per C4.
2. **Rename the misnomer test** — accepted (A5).
3. **Real-repo test placement: `ProjectStateStoreTests` with a temp-dir helper (A3(b))** — accepted.
4. **ADDED pwsh scenario: include (C3)** — accepted.
5. **Item B (running-agent cache): DROPPED, user-confirmed.** The change records the
   resolved-by-design decision (B1–B3); the commit-time backlog rewrite marks the parked entry
   accordingly. No cache work in this change.
