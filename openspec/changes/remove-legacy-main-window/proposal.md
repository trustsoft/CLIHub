# Remove the legacy MainWindow completely

## Why

The application carries a dead WPF window. `src/CLIHub/Views/MainWindow.xaml` and
`MainWindow.xaml.cs` define `CLIHub.Views.MainWindow`, which nothing constructs: it is absent from
`ServiceRegistration.cs` (guarded by `ArchitectureBoundaryTests.cs:87`), absent from `App.xaml`
(no `StartupUri`; startup runs through `CLIHub.Program` per `src/CLIHub/CLIHub.csproj:12`), and
documented in its own remarks as a "Deprecated legacy reference window ... not registered or
constructed by the application" (`MainWindow.xaml.cs:27-29`). It was retained on 2026-09-30 as a
build-only reference when `LaunchWindow` took over the shell role
(`openspec/changes/archive/2026-09-30-launch-window-ui/design.md:40`). The launch window has been
the active shell ever since; the user has now asked to remove the legacy window completely.

Dead code is not free: the window forces permanent carve-outs in the architecture test that reads
and asserts on its file (`ArchitectureBoundaryTests.cs:82,88`), in the colour-literal guard that
allowlists its pre-palette colours (`ThemeColorLiteralTests.cs:54,109`), and it keeps one property
alive on a live view model solely for its binding (`AgentItem.Status`, `AgentItem.cs:41-51`).
Four documentation files and one live spec Purpose still describe the window as "retained", which
is stale the moment it is deleted.

## What Changes

All items were verified against the clean tree at `144c284` and refreshed against `e618cbf` after 10
intervening commits (anchor corrections in `design.md` Deviations). Zero behavior change: no executable path, binding, resource, or DI registration is
altered, because none references the deleted window.

**Code and test reconciliation (stage 1) — one atomic set before the first test gate:**

- Delete `src/CLIHub/Views/MainWindow.xaml` (164 lines) and `src/CLIHub/Views/MainWindow.xaml.cs`
  (352 lines). No project-file or startup edit is needed: `CLIHub.csproj` declares no explicit
  `Page`/`Compile` items (SDK globs under `UseWPF`), `App.xaml` has no `StartupUri`, and
  `ServiceRegistration.cs` contains no `MainWindow` registration.
- Delete `AgentItem.Status` with its doc comment (`src/CLIHub/ViewModels/Agents/AgentItem.cs:36-46`).
  It is legacy-window-only: the sole writer is `MainWindow.xaml.cs:183`, the sole binding is
  `MainWindow.xaml:119`, `AgentListSynchronizer` never sets it (`AgentListSynchronizer.cs:46-52`),
  and no test references it.
- `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:72-84`: delete the legacy-file read
  (`:77`, which would throw `FileNotFoundException` after deletion) and the content assertion
  (`:83`); keep the registration guard `Assert.DoesNotContain("MainWindow", registration, ...)`
  (`:82`). No file-existence assertions are added: the registration guard is the sole legacy-window
  check because a filename assertion would be reverted by the same commit that re-created the file.
  The test method name stays — it still describes exactly what it checks.
- `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs`: remove all three references to
  `Views/MainWindow.xaml` — the doc remark naming it (`:12-15`, reworded from "Three files are
  allowlisted" to two), the walk-guard assertion (`:49`), and the `IsAllowlisted` entry (`:104`).
  The allowlist then covers the palettes and `ThemePickerData.xaml` only; both zero-hit guards
  (`xamlFiles.Length >= 20` and `allowlistedLiteralCount > 0`) still hold.

**Documentation and live spec reconciliation (stage 2):**

- `docs/project-context.md:51` — drop "`MainWindow` is retained as an unregistered legacy reference."
- `docs/architecture.md:38-39` — drop the same retained-window sentence.
- `docs/architecture/ui.md:138-139` — the `ThemeColorLiteralTests` bullet drops "and the legacy
  `MainWindow.xaml`" from the allowlist description.
- `docs/backlog.md:736-745` — the active "Single-use FontSize and CornerRadius literals stay
  untokenized" entry drops its stale clause citing the legacy window's allowlisted pre-palette
  colours. Historical resolved entries (`:276-277` within "Version population loop consolidation",
  `:493` within "`main-window-layout` names the wrong window") and unrelated
  `Application.Current?.MainWindow` mentions stay untouched, as does
  `docs/architecture/startup.md:33` and `docs/adr/0005-ui-dialog-boundaries.md:16` (WPF property,
  not the type).
- `openspec/specs/launch-window-layout/spec.md:4` — edit the Purpose **directly in the live spec**:
  drop the clause "the legacy `MainWindow` class is an unregistered build-only reference, not the
  subject of this capability". A delta `## Purpose` cannot do this: openspec 1.15.0 ignores it when
  the target spec already has a Purpose (verified CLI evidence in `design.md`). Requirements and
  scenarios of that spec stay byte-identical.

**Final gates (stage 3):** full build and test, `openspec validate --all`, a solution-wide
reference re-check, `graphify update .`, and a scope check. The app-start smoke is manual for the
user; `git commit` and `openspec archive` wait for the smoke result and explicit approval per repo
policy.

## Impact

- Source files: `src/CLIHub/Views/MainWindow.xaml` (deleted), `src/CLIHub/Views/MainWindow.xaml.cs`
  (deleted), `src/CLIHub/ViewModels/Agents/AgentItem.cs` (one property removed).
- Test files: `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` and
  `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs` (assertions adjusted; no test method added
  or deleted — total test count unchanged).
- Documentation: `docs/project-context.md`, `docs/architecture.md`, `docs/architecture/ui.md`,
  `docs/backlog.md`.
- Live spec: `openspec/specs/launch-window-layout/spec.md` (one Purpose clause; requirements and
  scenarios untouched).
- No orphaned collaborators: the converters and runner the legacy code-behind used
  (`PathToImage`, `EmptyToCollapsedVisibility`, `AsyncOperationRunner`) all have other production
  consumers (verified in `design.md`).
- User-visible impact: none — the window was never constructed.
- No behavior deltas to record in `openspec/specs/`: the change removes dead code and reconciles
  stale references, so it declares `skip_specs: true` in `.openspec.yaml` and creates no spec
  deltas (precedent: `openspec/changes/archive/2026-10-27-remove-dead-code/`). The one live-spec
  edit is a factual Purpose correction performed directly, not a delta.

## Non-goals

- No behavior change of any kind; no DI registration changes (there is no `MainWindow`
  registration to remove); no startup path changes.
- No view-model redesign beyond deleting the single legacy-only `AgentItem.Status` property; the
  rest of `AgentItem` is live launch-window code.
- No theming or token work: the `FontSize`/`CornerRadius` backlog entry keeps its remit; only its
  stale legacy-window citation is updated.
- No edits to historical resolved backlog entries, archived changes, ADRs, or any text that
  mentions the WPF `Application.MainWindow` property rather than the `CLIHub.Views.MainWindow` type.
- No commit, push, or archive in this change's tasks; those follow the repo approval policy and
  wait for the manual app-start smoke.

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (only `archive/`),
  and the working tree is clean at `144c284`.
- No spec dependencies: pure internal refactor, `skip_specs: true`. The direct Purpose edit in
  `openspec/specs/launch-window-layout/spec.md` is a one-sentence factual correction, not a delta.
