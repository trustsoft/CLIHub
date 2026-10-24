# Tasks: hygiene-sweep

Stages 1–2 are code, 3 is repo hygiene, 4 is the backlog rewrite, 5 is final verification. Each
stage ends with a gate; do not start the next stage on a red gate. Per `AGENTS.md`, run GitNexus
impact (`node .gitnexus/run.cjs impact "<symbol>" --direction upstream --repo .`) before the
deletions/edits in stages 1–2 and `detect-changes` before any commit — the reference census in
`design.md` ("Current state") is the baseline any impact result must agree with; if impact reports
a caller not listed there, stop and reconcile before editing.

## 1. Stage 1 — dead code: `RuntimeSelector`, `RuntimeType.WindowsTerminal`, matcher logger

- [ ] 1.1 Run impact for `RuntimeSelector`, `RuntimeType`, and `AgentProcessMatcher` and compare
      with the census in `design.md` (RuntimeSelector: registration + tests only; WindowsTerminal:
      producer `RuntimeSelector.cs:47` + `RuntimeSelectorTests.cs:21,48,52` only; matcher: monitor
      + DI + six test constructions). Stop and reconcile on any surprise.
- [ ] 1.2 Delete `src/CLIHub.Core.Windows/RuntimeSelector.cs` and remove the registration line
      `services.AddSingleton<RuntimeSelector>();`
      (`src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:38`). Leave the file's XML
      docs and its `using CLIHub.Core.Infrastructure.Processes;` (other registrations need it).
- [ ] 1.3 Delete `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs` (whole file, 3 tests).
- [ ] 1.4 Remove the `WindowsTerminal` member and its XML doc from
      `src/CLIHub.Core/Infrastructure/Processes/RuntimeType.cs:8-11`, leaving `Cmd` and
      `PowerShell` and the enum's summary intact.
- [ ] 1.5 In `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`: remove the
      `logger` field (`:11`), the constructor and its XML docs (`:13-20`), and the now-unused
      `using Microsoft.Extensions.Logging;` (`:4`). Do **not** touch the DI registration
      (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:124`) — the implicit
      parameterless constructor resolves the same way.
- [ ] 1.6 Update the six matcher construction sites to `new AgentProcessMatcher()`:
      `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs:14`,
      `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:22-23`,
      `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:38`,
      `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:59`,
      `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:279` and `:353`. Remove
      `NullLogger`/`ILogger` usings only where nothing else in that file uses them (e.g. keep them
      in `AgentProcessMonitorTests`, which still uses `NullLogger<AgentProcessMonitor>`).
- [ ] 1.7 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures. The build
      is the proof that no residual `RuntimeSelector`/`RuntimeType.WindowsTerminal` reference
      exists.

## 2. Stage 2 — project domain: public `ProjectPathPolicy`, reuse, and Add-method copies

- [ ] 2.1 Run impact for `ProjectPathPolicy` and `ProjectService.AddProject` and compare with the
      census in `design.md` (policy: `ProjectService`, `ProjectScanningService`,
      `RepositoryDiscovery`, `ProjectPathPolicyTests`; AddProject callers read `Id`/`Name` only).
      Stop and reconcile on any surprise.
- [ ] 2.2 Make `ProjectPathPolicy` public: the class
      (`src/CLIHub.Core/Projects/ProjectPathPolicy.cs:6`) and the members `Normalize` (`:13`),
      `AreEqual` (`:23`), `IsUnder` (`:33`). XML docs already exist — keep them as-is.
      `tests/CLIHub.Core.Tests/Projects/ProjectPathPolicyTests.cs` must keep compiling unchanged.
- [ ] 2.3 In `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` (`TryRegisterProject`,
      `:170-213`): replace the normalization body (`:186-194`) with
      `path = ProjectPathPolicy.Normalize(text);` inside the existing try/catch (keep catching
      `ArgumentException`, `NotSupportedException`, `PathTooLongException` and the existing error
      text), and replace the duplicate check (`:196`) with
      `this.Projects.Any(item => ProjectPathPolicy.AreEqual(item.Path, path))`. Keep the blank/
      absolute/existence validation (`:179`) and both `using` directives (`System.IO` is still
      used at `:179`; `CLIHub.Core.Projects` at `:6` now also covers the policy). This is design
      D4 — the root-path duplicate-error alignment is intended; see the open question before
      implementing if the veto is still undecided.
- [ ] 2.4 In `src/CLIHub.Core/Projects/ProjectService.cs`, return detached copies at all four
      points: `return existing.Copy();` at `:84` (AddProject) and `:112` (AddDiscoveredProject);
      `return project.Copy();` at `:91` and `:119`. Change nothing else in the methods.
- [ ] 2.5 Update the `IProjectService` XML docs for `AddProject` and `AddDiscoveredProject`
      (`src/CLIHub.Core/Projects/IProjectService.cs:33-47`) to state that the returned project is
      a detached copy, mirroring the read-method docs at `:15-31` (e.g. "Returns the registered
      project as a detached copy; the existing project is returned if the folder is already
      registered (without changing its source).").
- [ ] 2.6 Add two tests to `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`:
      `AddProject_MutatingReturnedProject_DoesNotChangeRegisteredState` (mirror
      `GetAllProjects_MutatingReturnedProject_DoesNotChangeRegisteredState`, `:155-175` — add,
      mutate the returned `Name`/`IsFavorite`, assert later reads and persisted state unchanged)
      and, in `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`, the D4
      pin: a row with `Path = "C:"` plus `NewProjectPath = "C:\\"` executes Register and shows the
      duplicate error without calling `AddProject` (`Times.Never`). Do not modify any existing
      test — they assert value equality and stay green (verified in design "Current state").
- [ ] 2.7 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures, with no
      existing test edited or skipped.

## 3. Stage 3 — repo hygiene: `.gitnexus/` in `.gitignore`

- [ ] 3.1 Add to `.gitignore` after the graphify block (`:117-118`):
      `# GitNexus index and runner (local to each machine)` then `.gitnexus/`. Whole directory —
      nothing under it is tracked (`git ls-files .gitnexus` is empty) and `run.cjs` is bootstrapped
      per machine (design D6).
- [ ] 3.2 Verify: `git check-ignore -v .gitnexus/run.cjs` now resolves through `.gitignore` (not
      only `.git/info/exclude:7`), and `git status --porcelain` reports no new untracked
      `.gitnexus/` entries. Leave the local exclude line in place — harmless duplication.
- [ ] 3.3 Gate: none beyond 3.2 (no build impact); confirm `git diff .gitignore` shows exactly the
      two added lines.

## 4. Stage 4 — `docs/backlog.md`: five resolved-format rewrites

Rewrite each swept entry to the resolved format used by
"Version population loop consolidation (resolved)" and "Windows personalization changes are not
followed while the application runs (resolved)": heading gets an ` (resolved)` suffix, bullets
become `Status: Resolved by the hygiene-sweep change (2026-10-24).` / `Resolved model: …` /
`Guarded by: …` / `Trigger: …` (keep each entry's original trigger intent as the regression watch).
**One edit per entry — five separate hunks; every other line in the file untouched**, including the
two pre-existing uncommitted entries "pipeline-state check cannot validate commit shas from foreign
repositories" (`:336-350`) and "Resolver role override ignores exhaustedUntil" (`:354-366`).

- [ ] 4.1 Rewrite "AddProject returns the live instance" (`docs/backlog.md:145-151`): resolved
      model — Add methods return `Copy()` at all four return points; guarded by the new
      `AddProject_MutatingReturnedProject_DoesNotChangeRegisteredState` plus the unchanged
      `AddProject_DuplicatePath_ReturnsExisting` / `AddDiscoveredProject_ExistingManual_DoesNotReclassify`.
- [ ] 4.2 Rewrite "ProjectPathPolicy duplication" (`:169-175`): resolved model — policy is public;
      the ViewModel reuses `Normalize`/`AreEqual`; record the accepted root-path duplicate-error
      alignment; guarded by `ProjectPathPolicyTests`, the unchanged
      `RegisterCommand_WithAlreadyRegisteredPath_KeepsSingleRowAndReportsError`, and the new D4
      pin test.
- [ ] 4.3 Rewrite "Matcher's unused logger" (`:204-208`): resolved model — field/ctor removed,
      registration unchanged; guarded by the unchanged matcher/monitor/service/view-model tests.
- [ ] 4.4 Rewrite "Registered-but-unused `RuntimeSelector`" (`:210-220`): resolved model —
      selector, registration (`:38`), and tests deleted; `RuntimeType.WindowsTerminal` dropped
      (settings parse via `RuntimeKinds` unaffected — stale `defaultRuntime` tokens fall back to
      PowerShell); guarded by the full-solution build and suite.
- [ ] 4.5 Rewrite "`.gitnexus/` is excluded only by the local git exclude file" (`:327-334`):
      resolved model — `.gitnexus/` in `.gitignore`; note `run.cjs` stays untracked/bootstrapped
      per machine; guarded by `git check-ignore -v .gitnexus/run.cjs` resolving through
      `.gitignore`.
- [ ] 4.6 Gate: `git diff docs/backlog.md` shows exactly the five entry hunks plus the two
      pre-existing uncommitted entries' edits (already in the working tree before this change) and
      nothing else; `openspec validate --all` still passes.

## 5. Stage 5 — final verification

- [ ] 5.1 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures (full
      re-run, not cached results).
- [ ] 5.2 Gate: `openspec validate --all` passes.
- [ ] 5.3 `graphify update .` to refresh the knowledge graph after the source edits.
- [ ] 5.4 Review `git status --porcelain` and `git diff --stat`: only files belonging to this
      change (design/proposal/tasks under `openspec/changes/hygiene-sweep/`, the code/test files
      listed in proposal Impact, `.gitignore`, `docs/backlog.md`) are modified; the legacy
      `Views/MainWindow.xaml.cs` and every other parked backlog entry are untouched. Report, then
      wait for the user's commit/archive approval per the repo policy.
