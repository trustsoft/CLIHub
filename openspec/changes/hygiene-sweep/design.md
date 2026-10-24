# Design: hygiene-sweep

## Current state (verified in code)

All citations re-verified against HEAD `3e9d873d76cc304977eaaf1aecaad5d108b7a993` (working tree
dirty only with the two pre-existing uncommitted backlog entries).

| Concern | Today | File |
|---|---|---|
| `RuntimeSelector` definition | `public sealed class RuntimeSelector` — a PATH-probing `wt.exe`/`cmd.exe` "best runtime" detector with a cached result (`SelectRuntime`, cache at `:22-37`, detection at `:39-57`) | `src/CLIHub.Core.Windows/RuntimeSelector.cs:11-86` |
| Its DI registration | `services.AddSingleton<RuntimeSelector>();` — the backlog cites `:37`; the line is `:38` today (drift) | `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:38` |
| Its only consumer | `RuntimeSelectorTests` (3 tests, whole file). No production code resolves or injects `RuntimeSelector` (exhaustive grep: the only other mentions are docs/backlog, ADR 0006, and archived changes) | `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs:6-61` |
| `RuntimeType.WindowsTerminal` producers | Exactly one: `RuntimeSelector.DetectRuntime` (`:47`). The only other references are the tests being deleted (`RuntimeSelectorTests.cs:21,48,52`) | `src/CLIHub.Core/Infrastructure/Processes/RuntimeType.cs:8-11`; `src/CLIHub.Core.Windows/RuntimeSelector.cs:47` |
| `RuntimeType` other members | `Cmd`/`PowerShell` are produced by `ProcessLauncher` (`RuntimeKind → RuntimeType` map `:124-129`; hardcoded cmd capture runtime `:185-189`) and consumed by runner tests; they stay | `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:124-129,185-189`; `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:13,30,39,58,85,141` |
| `RuntimeInfo.Type` reads in production | **None.** `CreateStartInfo` reads only `runtime.ExecutablePath` (`:85`); no `.Type` read exists in `src` (grep `runtime.Type\|\.Type ==\|\.Type is` → no matches). The property is required and set everywhere, so it stays | `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs:81-94`; `src/CLIHub.Core/Infrastructure/Processes/RuntimeInfo.cs:16` |
| Persisted runtime preference | `defaultRuntime` is a **string token** (`"ps"` default) written via `RuntimeKinds.ToToken` and read via `RuntimeKinds.Parse`, which falls back to `RuntimeKind.PowerShell` for null/unknown values. `RuntimeKind` is a separate enum (`PowerShell`/`PowerShell7`/`CommandPrompt`/`GitBash`) — it dropped its own `WindowsTerminal` member back in `2026-10-10-settings-terminal-profiles`. **`RuntimeType` never touches persistence** | `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:120,151,194`; `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:6-27,59-105` |
| Matcher's unused logger | `private readonly ILogger<AgentProcessMatcher> logger;` assigned in the ctor, never read — neither `GetCandidateProcessNames` (`:39-56`) nor `IsMatch` (`:69-80`) uses it. `using Microsoft.Extensions.Logging;` (`:4`) exists only for it | `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:11,17-20` |
| Matcher DI wiring | `services.AddSingleton<AgentProcessMatcher>();` — resolves the ctor parameter via DI; unchanged when the ctor disappears (implicit parameterless ctor) | `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:124` |
| Matcher test constructions | Six sites pass `NullLogger<AgentProcessMatcher>.Instance`: `AgentProcessMatcherTests.cs:14`, `AgentProcessMonitorTests.cs:22-23`, `AgentManagementServiceTests.cs:38`, `AgentManagementViewModelTests.cs:59`, `LaunchWindowViewModelBuilder.cs:279` and `:353` | tests tree (listed) |
| `.gitnexus/` exclusion | `.gitignore` ignores `.codegraph/` (`:115`) and `graphify-out/` (`:118`) but has no `.gitnexus/` entry; only the local, uncommitted `.git/info/exclude:7` hides it. `git ls-files .gitnexus` → empty: **nothing under `.gitnexus/` is tracked, including `run.cjs`** | `.gitignore:114-118`; `.git/info/exclude:7` |
| `ProjectPathPolicy` accessibility | `internal static class` with `internal` members `Normalize` (`:13-15`), `AreEqual` (`:23-24`), `IsUnder` (`:33-44`). `CLIHub.Core.csproj` grants `InternalsVisibleTo` only to the three **test** projects — the `CLIHub` app cannot see it, which is why the ViewModel duplicates it | `src/CLIHub.Core/Projects/ProjectPathPolicy.cs:6,13,23,33`; `src/CLIHub.Core/CLIHub.Core.csproj:28-30` |
| VM duplicate normalize/compare | `TryRegisterProject` validates input (`:179`), normalizes with `Path.TrimEndingDirectorySeparator(Path.GetFullPath(text))` in a try/catch for `ArgumentException`/`NotSupportedException`/`PathTooLongException` (`:185-194`), then duplicate-checks with `string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)` (`:196-200`) | `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:170-213` |
| Root-path semantics difference | For a drive/UNC **root**, the VM's `Path.TrimEndingDirectorySeparator` **keeps** the separator (`"C:\"` stays `"C:\"`) while the policy's `TrimEnd(DirectorySeparatorChar, AltDirectorySeparatorChar)` **strips** it (`"C:\"` → `"C:"`). `Path.GetFullPath` collapses duplicate trailing separators first, so the two agree on **every non-root input**. Note the service already stores the policy form: `AddProject` re-normalizes via `ProjectPathPolicy.Normalize` (`ProjectService.cs:74`), so a root is persisted as `"C:"` | `ProjectRegistryViewModel.cs:188` vs `ProjectPathPolicy.cs:13-15` |
| Add methods return live instances | `AddProject` returns `existing` (`:84`) or the freshly added `project` (`:91`); `AddDiscoveredProject` the same (`:112`, `:119`) — four leak points of the internal `Project` reference, while all read methods return `Copy()` (`:36`, `:49`, `:63`) | `src/CLIHub.Core/Projects/ProjectService.cs:67-120` |
| `Project.Copy()` | Full detached copy including the vestigial `LogoPath` | `src/CLIHub.Core/Models/Projects/Project.cs:54-63` |
| Add-method callers | Production callers read only `project.Id` (then `SetCurrentProject`) or `project.Name` — a copy is transparent: `TrayCommandHandlers.cs:90-91`, `ProjectPaneController.cs:134-137` (returns it; callers compare by value), legacy `MainWindow.xaml.cs:256-258` (untouched exception). `AddDiscoveredProject` has **no production caller** (tests + interface only) | src tree (listed) |
| Identity assertions in tests | None. `AddProject_DuplicatePath_ReturnsExisting` asserts `Assert.Equal(first.Id, second.Id)` (`:78`); `AddDiscoveredProject_ExistingManual_DoesNotReclassify` asserts value equality (`:291`); `GetAllProjects_ReturnsNewInstancesEachCall` uses `NotSame` on read results (`:151`); pane/controller tests mock the service and assert `Equal` on `Id` (`ProjectPaneControllerTests.cs:102`) | `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs:70-80,142-152,283-293`; `tests/CLIHub.Tests/ViewModels/Projects/ProjectPaneControllerTests.cs:100-104` |
| VM duplicate-error test coverage | `RegisterCommand_WithAlreadyRegisteredPath_KeepsSingleRowAndReportsError` covers the duplicate path with a non-root temp folder — unaffected by the root nuance | `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:146-160` |
| Backlog entries | Five swept entries at `docs/backlog.md:145-151` (AddProject), `:169-175` (ProjectPathPolicy), `:204-208` (matcher logger), `:210-220` (RuntimeSelector), `:327-334` (.gitnexus). Two pre-existing **uncommitted** parked entries to leave untouched: `:336-350` (foreign-repo shas), `:354-366` (resolver role override) | `docs/backlog.md` |

## Governing requirements (quoted)

- `openspec/specs/project-management/spec.md:281-288` — *Detached project reads*: "The project
  service SHALL return detached project copies from its read methods. UI surfaces SHALL bind
  project display models, not project records returned by the service, and mutating a returned
  project record SHALL NOT change registered project state." Scenario **Reads return detached
  copies**: "WHEN the project service returns projects from a read method / THEN mutating a
  returned project changes neither later reads nor persisted state".
- `openspec/specs/project-management/spec.md:11-19` — *Project registration*: Scenario **Reject
  duplicate folder**: "WHEN a user selects a folder that is already registered / THEN no duplicate
  project is created and the existing project is selected".
- `openspec/specs/project-management/spec.md:223-226` — Settings Projects page: Scenario **Add an
  already registered folder**: "WHEN the user selects a folder that is already registered / THEN
  no duplicate project is created and the existing project remains".

No scenario pins an error message for duplicates, nor anything about what the Add methods return;
the "Detached project reads" requirement's final clause ("mutating a returned project record SHALL
NOT change registered project state") is the invariant item 6 closes a gap against.

## D1 — Drop `RuntimeType.WindowsTerminal` along with the selector (DECIDED: remove the member)

The backlog conditions the member removal on "the settings model no longer needs it". Verified —
it never did:

- The persisted preference is the `defaultRuntime` **string token** (`AppConfigDocument.cs:120`),
  written by `RuntimeKinds.ToToken` (`:151`) and parsed by `RuntimeKinds.Parse` (`:194`), whose
  `TryParse` returns false for any unknown value and whose `Parse` then falls back to
  `RuntimeKind.PowerShell` (`RuntimeKind.cs:59-105`). `RuntimeType` — a different enum — appears
  only on the in-memory `RuntimeInfo.Type` (`RuntimeInfo.cs:16`) and is never serialized or
  deserialized anywhere (grep: producers/consumers are `ProcessLauncher`, `RuntimeSelector`, and
  tests).
- A stale value in an existing user's `config.json` cannot break startup with or without the
  member: such a value is a `defaultRuntime` **token** (e.g. a legacy `"wt.exe"` executable string
  from before `2026-10-06-migrate-legacy-terminal-preference`), handled by `RuntimeKinds.Parse`'s
  fallback, not by `RuntimeType` parsing. No config format ever contained a `RuntimeType` value.
- After deleting `RuntimeSelector.cs` and `RuntimeSelectorTests.cs`, a full-tree grep leaves
  **zero** references to `RuntimeType.WindowsTerminal`; the solution build is the mechanical proof.
- The settings enum itself already dropped its `WindowsTerminal` member in the archived
  `2026-10-10-settings-terminal-profiles` change without a persistence incident — the same
  round-trip safety applies here.

`RuntimeType` keeps `Cmd` and `PowerShell` (both produced by `ProcessLauncher`), and
`RuntimeInfo.Type` stays as-is: removing the write-only property is a wider cleanup explicitly out
of scope.

**Alternative — keep the enum member "just in case":** rejected. There is no code path that can
ever produce or consume the value after the sweep; keeping it re-creates exactly the dead-weight
condition the backlog entry describes. (If the user prefers maximal caution anyway, keeping the
member is a zero-cost fallback — it changes nothing else in this change.)

## D2 — Selector deletion scope (DECIDED: delete file + registration + tests)

Delete `src/CLIHub.Core.Windows/RuntimeSelector.cs`, the single registration line
(`WindowsServiceCollectionExtensions.cs:38`), and `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs`.
Nothing else references the type (census above). The extension's XML doc summary
(`WindowsServiceCollectionExtensions.cs:8-11`) does not mention `RuntimeSelector`, so no doc edit is
needed there; its `using CLIHub.Core.Infrastructure.Processes;` stays (the other registrations use
that namespace). ADR 0006's mention (`docs/adr/0006-windows-platform-library.md:14`) is a
point-in-time record and stays.

## D3 — Matcher logger removal (DECIDED: remove field + ctor; registration unchanged)

Remove the field (`AgentProcessMatcher.cs:11`), the explicit constructor and its XML docs
(`:13-20`), and the now-unused `using Microsoft.Extensions.Logging;` (`:4`). The implicit
parameterless constructor keeps `services.AddSingleton<AgentProcessMatcher>();`
(`ServiceCollectionExtensions.cs:124`) working unchanged. Update the six test construction sites
(census above) to `new AgentProcessMatcher()` and drop `NullLogger`/`ILogger` usings only where
nothing else in the file uses them (e.g. `AgentProcessMonitorTests` still uses
`NullLogger<AgentProcessMonitor>.Instance` at `:28`, so its usings stay). This is a mechanical
compile fix on tests, not a weakening: the matcher's behavior (candidate derivation `:39-56`,
matching `:69-80`) is untouched and stays covered by `AgentProcessMatcherTests`,
`AgentProcessMonitorTests`, `AgentManagementServiceTests`, and `AgentManagementViewModelTests`.

## D4 — Make `ProjectPathPolicy` public and reuse it (DECIDED: reuse, accept the root-path alignment)

Change `internal static class ProjectPathPolicy` and its three `internal` members to `public`
(XML docs already exist and satisfy the public-API rule). `ProjectPathPolicyTests`
(`tests/CLIHub.Core.Tests/Projects/ProjectPathPolicyTests.cs`) keeps compiling unchanged, and the
policy's other consumers (`ProjectService`, `ProjectScanningService`, `RepositoryDiscovery`) are
unaffected — only accessibility changes.

In `ProjectRegistryViewModel.TryRegisterProject` replace the body of the normalization try-block
(`:186-194`) with `path = ProjectPathPolicy.Normalize(text);` (keeping the existing catch of
`ArgumentException`/`NotSupportedException`/`PathTooLongException` — the policy does not catch,
and must not) and the duplicate check (`:196`) with
`this.Projects.Any(item => ProjectPathPolicy.AreEqual(item.Path, path))`. The `using System.IO;`
stays (`Path.IsPathFullyQualified`/`Directory.Exists` at `:179`); `using CLIHub.Core.Projects;`
(`:6`) already covers the policy.

**The one deliberate nuance (challenge recorded):** the old local normalization
(`Path.TrimEndingDirectorySeparator`) and the policy (`TrimEnd` both separators) diverge only for
drive/UNC roots, where the VM used to keep `"C:\"` while the service stores `"C:"`. Consequences:

- For **every non-root input** (all realistic folder paths, including trailing separators and
  duplicate separators, which `GetFullPath` collapses) the two normalizations are byte-identical,
  so the check outcome is unchanged.
- For an **already-registered root**: today the VM's `"C:\"` fails to match the stored `"C:"`, the
  check falls through to `AddProject`, which finds the existing project and no-ops — the field is
  silently cleared with no error. After the change the check matches and shows the page's duplicate
  error ("This folder is already registered as a project."). Both outcomes satisfy the quoted spec
  scenarios (no duplicate created; the existing project remains); the change makes the page's check
  use **exactly** the rule the service uses instead of a near-copy that misses the root case.
- For a **new root** registration nothing observable changes in what gets stored: `AddProject`
  already normalizes with the policy (`ProjectService.cs:74`).

There is **no** way to reuse the policy and preserve the old root-path outcome byte-for-byte:
`AreEqual` normalizes both sides (`ProjectPathPolicy.cs:23-24`), so even a comparison-only swap
matches the root case. Preserving the old outcome means keeping the duplication, i.e. dropping
item 4. The alignment is therefore inherent to the remedy, spec-compatible, and strictly more
consistent; it is accepted here and flagged as an open question for veto. A small deterministic
unit test (seed a row with path `"C:"`, submit `"C:\"`, expect the duplicate error) pins the new
behavior — `"C:\"` exists on every Windows test machine, so no filesystem setup is needed.

**Alternatives rejected:**

- *Change the policy to use `Path.TrimEndingDirectorySeparator`* — that would change what the
  **service** normalizes, compares, and persists for roots (`"C:\"` instead of `"C:"`): a real
  behavior change across `ProjectService`, scanning, and the persistence boundary. Rejected.
- *Add `<InternalsVisibleTo Include="CLIHub" />` to `CLIHub.Core.csproj`* — one line, but it grants
  the app blanket access to all Core internals; every existing `InternalsVisibleTo` grant in this
  repo targets a **test** project (`CLIHub.Core.csproj:28-30`, `CLIHub.Core.Windows.csproj:19-20`,
  `CLIHub.csproj:25`). Friend-assembly access for production code blurs the Core public API
  boundary the architecture docs maintain. Rejected; the backlog remedy ("make the policy public")
  is the intended shape.

## D5 — Add methods return `Copy()` (DECIDED: all four return points + docs + one new test)

`AddProject` returns `existing.Copy()` (`ProjectService.cs:84`) and `project.Copy()` (`:91`);
`AddDiscoveredProject` the same (`:112`, `:119`). The duplicate-registration contract is value
preserved: `Copy()` copies every field including `Id` (`Project.cs:54-63`), so
`AddProject_DuplicatePath_ReturnsExisting` (`Assert.Equal(first.Id, second.Id)`) and
`AddDiscoveredProject_ExistingManual_DoesNotReclassify` stay green unchanged. No production caller
mutates the returned reference (census above), and the legacy `MainWindow.xaml.cs` keeps working
untouched.

Update `IProjectService` XML docs so `AddProject`/`AddDiscoveredProject`
(`src/CLIHub.Core/Projects/IProjectService.cs:33-47`) state the returned project is a detached
copy, mirroring the read-method docs (`:15-31`); the implementation's `<inheritdoc />` picks it up.

Beyond the backlog's "one line plus XML docs": add one mutation-invariant test in
`ProjectServiceTests` mirroring `GetAllProjects_MutatingReturnedProject_DoesNotChangeRegisteredState`
(`:155-175`) — add a project, mutate the returned record's `Name`/`IsFavorite`, assert later reads
and persisted state are unchanged. Rationale: the repo already expresses this invariant as a test
for every read method; the Add methods gain the same guard the moment they join the contract. This
is an addition, not a weakening, of the suite.

## D6 — `.gitnexus/` ignore strategy (DECIDED: ignore the whole directory)

Add a `.gitnexus/` entry to `.gitignore` beside the other machine-local index ignores
(`:114-118`). Confirmed facts: nothing under `.gitnexus/` is tracked (`git ls-files .gitnexus` →
empty), the runner `run.cjs` is untracked in this working copy, and the repo's GitNexus
instructions bootstrap `run.cjs` per machine ("No `.gitnexus/run.cjs` yet? Bootstrap with `npx`,
`bunx`, or `pnpm dlx`"), so nothing is lost by ignoring the directory wholesale.

**Alternative — ignore only the generated index files and un-ignore `run.cjs`:** rejected; it
manufactures a negation pattern for a file nobody versions, and the backlog's evidence explicitly
notes it is untracked. If the team later decides to version the runner, a `!.gitnexus/run.cjs`
negation can be added then. The redundant local `.git/info/exclude:7` entry is harmless and is not
removed (it is a local file outside the change).

## D7 — `skip_specs: true` (DECIDED: no spec deltas)

None of the six edits changes a scenario in any capability spec:

- Items 1–3 (D1–D3, D6) have no observable runtime behavior at all — dead code, constructor
  wiring, and an ignore file.
- Item 4 (D4) changes no scenario outcome: the quoted duplicate scenarios pin "no duplicate
  project is created and the existing project remains/selected", which holds on both sides of the
  root-path nuance; no scenario pins the page's error message or its absence.
- Item 5 (D5) moves the Add methods **into** compliance with the "Detached project reads"
  requirement's mutation clause; no scenario describes what Add methods return.

Following `2026-09-28-extract-user32-interop` and `2026-10-20-fix-settings-open-delay`, the change
declares `skip_specs: true` in `.openspec.yaml`. The escape hatch is narrow: if review rejects the
D4 root-path alignment, item 4 is **dropped**, not converted into a spec delta — no scenario
changes either way.

## Deviations from the backlog text (verified against the tree)

1. The `RuntimeSelector` registration line is `WindowsServiceCollectionExtensions.cs:38` today,
   not `:37` as the backlog (and the archived `git-bash-resolution` design) records — one-line
   drift; everything else in that entry is accurate.
2. "AddProject returns the live instance … one line plus XML docs" understates the edit: each
   method has **two** leak points (the `existing` early return and the `project` return), so it is
   two lines per method plus interface docs (D5).
3. "ProjectPathPolicy duplication … keep the behavior identical" is impossible to satisfy
   literally: reusing the policy necessarily aligns the duplicate check with service normalization,
   which differs only for drive/UNC roots (D4). The divergence is recorded, spec-compatible, and
   accepted — with a veto open question.
4. The `.gitnexus` entry's alternative ("ignore only its generated index files if the runner is
   meant to be versioned") is resolved as "not meant to be versioned" — `run.cjs` is untracked
   here and bootstrapped per machine (D6).

## Open questions

1. **D4 root-path alignment (veto):** re-registering an already-registered drive/UNC root from the
   Settings Projects page now shows the duplicate error instead of silently accepting the no-op.
   Accept (recommended — it makes the page use the service's exact rule), or drop item 4 from the
   sweep? There is no middle option (see D4).
2. **D1 fallback:** if the user prefers maximal caution, `RuntimeType.WindowsTerminal` can be kept
   (zero cost, but it re-creates the dead-weight the entry describes). Default is removal.
3. Should the new D4 root-path duplicate test (task 2.6) use the real `"C:\"` root (deterministic
   on every Windows machine, no filesystem setup) — accepted default — or be skipped if the
   implementation session finds CI-style constraints on asserting against real drives?
