# Design: remove-legacy-main-window

## Context (verified current state)

All file:line references were verified by direct reads and a solution-wide `git grep -n "MainWindow"`
against the clean working tree at HEAD `144c284` (`git log --oneline -1` → `144c284 chore(openspec):
archive add-source-file-headers`; `git status --short` is empty; `openspec/changes/` holds no active
change, only `archive/`; `openspec --version` → `1.15.0`). The plan was refreshed against `e618cbf`
after 10 commits; every drifted anchor was corrected (see Deviations).

### A — The legacy window files

- `src/CLIHub/Views/MainWindow.xaml` is 164 lines; `x:Class="CLIHub.Views.MainWindow"` at `:1`.
  Its only non-theme resources are the shared converters `{StaticResource PathToImage}` (`:57`,
  `:114`) and `{StaticResource EmptyToCollapsedVisibility}` (`:118`), both declared in
  `App.xaml:33-45` and consumed by live views as well (see "No orphaned collaborators").
- `src/CLIHub/Views/MainWindow.xaml.cs` is 352 lines; `public partial class MainWindow : Window`
  at `:30`, with the remarks `///   Deprecated legacy reference window. It is not registered or
  constructed by the application.` at `:27-29`. Its constructor (`:57-90`) takes nine injected
  services; every one of them is live application surface used elsewhere.
- Nothing constructs the type. The complete `MainWindow` occurrence list in `src/` and `tests/`
  (git grep, excluding `openspec/changes/archive`) is:
  - the two window files themselves;
  - `src/CLIHub/App.xaml.cs:34` — `this.MainWindow = (Window)window;` assigns the WPF
    `Application.MainWindow` property; the argument is whatever window the bootstrapper supplies
    (`ApplicationBootstrapper.cs:73` passes `startupUi.LaunchWindow`);
  - `src/CLIHub/Views/SettingsWindow.xaml.cs:44,49`, `Views/WhatsNewWindow.xaml.cs:42`,
    `src/CLIHub/Launching/ProjectDialogService.cs:28,47`, `src/CLIHub/Launching/UserNotificationService.cs:25`,
    `src/CLIHub/Views/ReadmeImageCapture.cs:61` — all read the WPF
    `Application.Current?.MainWindow` property (or pattern-match it against `LaunchWindow`), never
    the `CLIHub.Views.MainWindow` type;
  - `src/CLIHub/Startup/*` and `tests/CLIHub.Tests/ApplicationHostTests.cs` — the `setMainWindow`
    delegate/parameter name, unrelated to the type;
  - the reconciliation targets in items C, D, and E below.
- No project-file edit is required: `src/CLIHub/CLIHub.csproj` (38 lines) declares no explicit
  `Page` or `Compile` items — WPF XAML and code-behind are SDK-globbed under `UseWPF` (`:9`) — and
  `App.xaml` has no `StartupUri` (startup object is `CLIHub.Program`, `CLIHub.csproj:12`).

### B — `AgentItem.Status` is legacy-window-only

- Property with doc comment at `src/CLIHub/ViewModels/Agents/AgentItem.cs:36-46`:
  the doc says `///   Legacy-window-only: bound only by MainWindow.xaml (the documented build-only
  exception).` and `///   Intentionally not wired into <see cref="Apply" /> ...` (`:38-40`).
- Sole writer: `MainWindow.xaml.cs:183` —
  `Status = $"System: {(inSystem ? "yes" : "no")}  |  Project: {(inProject ? "yes" : "no")}"`.
- Sole binding: `MainWindow.xaml:119` — `<TextBlock Text="{Binding Status}" .../>`.
- `AgentListSynchronizer` (the live row constructor) creates `AgentItem` with `Plugin`, `Name`,
  `LogoPath`, `IsAvailable` only (`src/CLIHub/ViewModels/Agents/AgentListSynchronizer.cs:46-52`),
  and `AgentItem.Apply` never writes `Status` (`AgentItem.cs:135-164`).
- No test references it: `tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs` covers
  `CanLaunch`/`CanResume`/`CanUpdate`/`CanShowVersion`/`Version`/notifications, never `Status`;
  `AgentListSynchronizerTests.cs` and the coordinator tests construct rows without `Status`.
- Deletion is therefore a cascade of the window removal: with the window gone the property has no
  writer, no binding, and no reader.

### C — `ArchitectureBoundaryTests` reads the legacy file

- `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:72-84` —
  `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot`:
  - `:77` `var legacyWindow = File.ReadAllText(Path.Combine(root, "src", "CLIHub", "Views",
    "MainWindow.xaml.cs"));` — throws `FileNotFoundException` once the file is deleted;
  - `:82` `Assert.DoesNotContain("MainWindow", registration, StringComparison.Ordinal);` — the
    registration guard over `src/CLIHub/Composition/ServiceRegistration.cs`; verified accurate
    (no `MainWindow` in that file) and **kept**;
  - `:83` `Assert.Contains("Deprecated legacy reference window", legacyWindow, ...)` — reads the
    deleted file's remarks; must go with `:77`.
- The test name stays accurate after the edit: the workflows are registered, and the legacy window
  is not (not registered, and — with the new assertions — no longer present).

### D — `ThemeColorLiteralTests` allowlists the legacy XAML in three places

- `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs`:
  - doc remark `:11-16`: "Three files are allowlisted by design: the palettes
    (`Themes/*Theme.xaml`, colours only), `Themes/ThemePickerData.xaml` (...), and
    `Views/MainWindow.xaml` (the dead legacy window documented in `docs/backlog.md`).";
  - walk guard `:49`: `Assert.Contains(xamlFiles, path => IsFile(path, appRoot, "Views",
    "MainWindow.xaml"));` — fails once the file is deleted;
  - `IsAllowlisted` `:100-110`, entry at `:104`: `|| string.Equals(normalized,
    "Views/MainWindow.xaml", StringComparison.OrdinalIgnoreCase)`.
- Both zero-hit guards survive the removal: the walk still sees ≥ 20 XAML files (one fewer than
  today), and `allowlistedLiteralCount > 0` (`:52` in the foreach loop setup) still holds through
  the palettes and `ThemePickerData.xaml`, which remain allowlisted (`:103`, `:109` in
  `IsAllowlisted`).

### E — Documentation and the live spec describe the window as retained

- `docs/project-context.md:51` — "The launch window is the active WPF shell. `MainWindow` is
  retained as an unregistered legacy reference."
- `docs/architecture.md:37-39` — "The launch window is the active shell. Its ViewModel composes
  project and agent pane controllers, command and action coordinators, update state, and
  application services. `MainWindow` is retained as an unregistered legacy reference." (the last
  sentence spans lines 38-39).
- `docs/architecture/ui.md:138-139` — the `ThemeColorLiteralTests` bullet: "colour literals are
  rejected outside the palettes, `ThemePickerData.xaml`, and the legacy `MainWindow.xaml`."
- `docs/backlog.md:736-745` — active entry "Single-use FontSize and CornerRadius literals stay
  untokenized"; its Evidence cites "the legacy `Views/MainWindow.xaml` also keeps its pre-palette
  colours (allowlisted by `ThemeColorLiteralTests`)" (`:741-742`). Historical resolved entries that
  mention the window (`:276-277` within "Version population loop consolidation", `:493` within
  "`main-window-layout` names the wrong window") are history and stay untouched.
- `openspec/specs/launch-window-layout/spec.md:4` (Purpose) — "... The launch window
  (`src/CLIHub/Views/LaunchWindow.xaml.cs`) is the application's active shell and the subject of
  this capability; the legacy `MainWindow` class is an unregistered build-only reference, not the
  subject of this capability." The final clause becomes false when the window is deleted. No other
  live spec mentions it (git grep over `openspec/specs/`).
- Untouched by design (WPF property, not the type): `docs/architecture/startup.md:33`
  ("Assigns the session's launch window to `Application.MainWindow`.") and
  `docs/adr/0005-ui-dialog-boundaries.md:16` (`Application.Current.MainWindow` dialog ownership).
- `docs/repo-structure.md` and `README.md` contain no `MainWindow` reference (git grep).

### F — No orphaned collaborators

Everything the legacy code-behind consumes has other production consumers, so no cascade beyond
`AgentItem.Status` exists:

- `PathToImageConverter` — resource key declared at `App.xaml:33-35`, instance assigned from DI at
  `src/CLIHub/Startup/ApplicationHost.cs:143`; used by `LaunchWindow.xaml:184,303` and
  `SettingsAgentsPage.xaml:189`; tested by `PathToImageConverterTests` and
  `ServiceRegistrationTests.cs:60`.
- `EmptyToCollapsedVisibilityConverter` — declared at `App.xaml:45`; used by
  `LaunchWindow.xaml:318`, `SettingsWindow.xaml:394,422,437`, `SettingsAgentsPage.xaml:133`.
- `AsyncOperationRunner` (`src/CLIHub/Lifetime/AsyncOperationRunner.cs`) — used by
  `UpdateControlViewModel.cs:63`, `SettingsUpdateStatusPresenter.cs:110`,
  `LaunchCommandCoordinator.cs:66`, and its own tests.
- The nine injected services (`IPluginCatalog`, `IProjectService`, `IAgentCommandService`,
  `IAgentDetectionService`, `IAgentVersionService`, `IPreferencesStore`,
  `IUpdateVersionProvider`, `IUpdateWorkflow`, `ILogger<T>`) are all consumed by the launch
  window's view model, coordinators, or Settings.

### G — openspec 1.15.0 ignores a delta `## Purpose` for an existing spec

Verified in the installed CLI (`@fission-ai/openspec` 1.15.0,
`C:\Users\Maxim\AppData\Roaming\npm\node_modules\@fission-ai\openspec\dist\core\specs-apply.js:237-255`):

```js
const deltaPurpose = extractPurposeSection(changeContent);
...
// A delta Purpose only seeds a spec that does not exist yet. Say so rather
// than dropping it silently ...
if (deltaPurpose) {
    const existingPurpose = extractPurposeSection(targetContent);
    if (existingPurpose && existingPurpose !== deltaPurpose) {
        warn(`${specName} - delta Purpose ignored; ${specName} already has one. ` +
            `Edit ${update.target} directly to change it.`);
    }
}
```

`openspec/specs/launch-window-layout/spec.md` already has a `## Purpose` (`:3-4`), so a delta
`## Purpose` in a change would be ignored with exactly that warning, and `openspec sync-specs`
routes through the same code. The only way to correct the stale clause is to edit the live spec
file directly.

## Decisions

### D1 — `skip_specs: true`, no spec deltas

The change removes code that is provably unreferenced and corrects stale references to it. No
requirement in `openspec/specs/` describes the legacy window's behavior (its only spec mention is
the launch-window-layout Purpose clause saying it is *not* the subject of that capability). A pure
internal refactor declares `skip_specs: true` instead of inventing deltas — same pattern as
`openspec/changes/archive/2026-10-27-remove-dead-code/`.

### D2 — the launch-window-layout Purpose is edited directly in the live spec, as an implementation task

Because of the verified CLI behavior (Context G), a delta file for this one clause would be a
no-op that emits a warning. The edit therefore lands in
`openspec/specs/launch-window-layout/spec.md` directly: drop the clause "; the legacy `MainWindow`
class is an unregistered build-only reference, not the subject of this capability" and end the
sentence after "... is the application's active shell and the subject of this capability."
Requirements and scenarios stay byte-identical; this change's `.openspec.yaml` still declares
`skip_specs: true` because no requirement delta exists. The direct edit is quoted verbatim in
tasks (2.5) so a fresh session applies it exactly.

### D3 — `AgentItem.Status` is removed in the same change

The property exists solely for the legacy window (Context B); keeping it would convert a
documented legacy binding into genuinely dead API. It is not on any interface, has no test, and no
other XAML binds a `Status` path on an agent row (`LaunchWindow.xaml` binds `StatusMessage`, a
different property on a different type, at `:393`).

### D4 — `ArchitectureBoundaryTests`: keep the registration guard without file-existence assertions

The original choice was to replace the file-content assertions with two
`Assert.False(File.Exists(...))` checks after deleting the legacy files. The user revised that
choice on 2026-10-29: a filename assertion is not a behavior test and would be reverted by the
same commit that re-created the file, so it guards nothing. The legacy-file read and content
assertion are deleted, while `Assert.DoesNotContain("MainWindow", registration, ...)` remains the
sole legacy-window check. The test method name stays accurate because it verifies the window is not
registered.

### D5 — `ThemeColorLiteralTests` keeps both zero-hit guards; only the legacy entries go

The remark (`:16-21`), the walk assertion (`:54`), and the allowlist entry (`:109`) are edited so
the allowlist covers the palettes and `ThemePickerData.xaml` only. The remark is reworded to "Two
kinds of file are allowlisted by design" (the palettes are a glob, so "three files" was already
loose). The `xamlFiles.Length >= 20` and `allowlistedLiteralCount > 0` guards are untouched and
still hold (Context D), so a future glob or rename accident still fails loudly.

### D6 — stage 1 is atomic: files, both test files, and `AgentItem.Status` land before the first test gate

Deleting the window files alone makes `ArchitectureBoundaryTests.cs:82` throw and
`ThemeColorLiteralTests.cs:54` fail. Deleting `AgentItem.Status` alone breaks compilation of
`MainWindow.xaml.cs:183`. All of stage 1 therefore lands before the build/test gate, mirroring the
atomic-deletion decision in `2026-10-27-remove-dead-code` (its D3).

### D7 — documentation edits are minimal sentence deletions; history stays untouched

Each of the four documentation targets loses exactly the stale retained-window sentence/clause;
no surrounding text is rewritten. Historical resolved backlog entries and every
`Application.Current?.MainWindow` / `setMainWindow` mention are explicitly left alone (Context E)
— they describe the WPF property or past events, not the deleted type.

### D8 — no csproj, App.xaml, or ServiceRegistration edits

Verified none are needed (Context A): the SDK globs the XAML, there is no `StartupUri`, and there
is no registration to remove. The kept assertion `:82` continues to guard against reintroduction.

### D9 — historical resolved backlog entries with present-tense window references stay untouched

Two resolved entries contain present-tense clauses that become false when the window is deleted:
- `:276-277` (within "Version population loop consolidation (resolved)"): "The legacy
  `Views/MainWindow.xaml.cs` copy remains the documented untouched exception (the window is never
  constructed)."
- `:493` (within "`main-window-layout` names the wrong window (resolved)"): "while `MainWindow` is
  an unregistered legacy reference."

Decision: leave both untouched. Rationale: these are historical resolved entries describing the
state at resolution time (2026-10-24). Rewriting them to past tense would falsify the record of
what was true when those changes landed. The entries are timestamped and explicitly marked
"resolved"; a reader encountering them understands they describe a past state. The genuinely
current active entry at `:736-745` is the one that gets corrected. The same principle already
applies to other historical references in the file (the original plan's D7). No task change is
required — the plan already leaves historical entries alone; this decision documents why these two
specific present-tense clauses are still considered "historical" for this purpose.

## Alternatives considered

1. **Keep the window as a reference.** Rejected: the user explicitly asked for complete removal;
   the 2026-09-30 retention rationale ("available for comparison and as a fallback reference",
   `2026-09-30-launch-window-ui/design.md:40`) has been obsolete since the launch window
   stabilized.
2. **Delete the files but keep `AgentItem.Status` and the test allowlists.** Rejected: leaves new
   dead code and permanently failing carve-outs (the architecture test's file read cannot survive
   the deletion at all) — the change would not pass its own gates.
3. **Split the legacy-window non-existence check into its own `[Fact]`.** Rejected as the default:
   keeping it inside `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot`
   preserves the one-test-per-concern layout and the existing name; the assertion count and
   semantics stay local to where the legacy window was previously asserted. Low-stakes enough that
   the implementation session may restructure it if the maintainers prefer (Open question 2).
4. **Express the launch-window-layout Purpose correction as a spec delta.** Rejected: verified
   no-op with a warning in openspec 1.15.0 (Context G). `openspec sync-specs` routes through the
   same ignore path, so it cannot rescue a delta either.
5. **Leave the Purpose clause as-is (it only says the window exists).** Rejected: the clause would
   assert a false fact in the durable contract; specs must describe the current system.
6. **Also update the historical resolved backlog entries that mention the window.** Rejected:
   `docs/backlog.md` resolved entries are history; rewriting them falsifies the record.

## Deviations

- **Plan refreshed from 144c284 to e618cbf.** The plan was originally written at commit `144c284`;
  after 10 intervening commits (plugin-catalog-diagnostics, plugin-disabled-ui, ADR-0013,
  persistence-service-slice investigation, etc.) many line numbers drifted. All anchors were
  re-verified at `e618cbf` and corrected:
  - `AgentItem.cs`: Status property `:41-51` → `:36-46`
  - `App.xaml.cs`: MainWindow assignment `:39` → `:34`
  - `ArchitectureBoundaryTests.cs`: test method `:77-89` → `:72-84`; legacyWindow read `:82` →
    `:77`; registration guard `:87` → `:82`; content assert `:88` → `:83`
  - `SettingsWindow.xaml.cs`: MainWindow references `:49,54` → `:44,49`
  - `ThemeColorLiteralTests.cs`: doc remark paragraph `:16-21` → `:11-16`; walk guard `:54` →
    `:49`; `IsAllowlisted` method `:105-115` → `:100-110`; MainWindow entry `:109` → `:104`;
    `allowlistedLiteralCount` initialization reference changed from `:88-90` to `:52`; palette
    entry `:108` → `:103`; ThemePickerData return `:114` → `:109`
  - `docs/backlog.md`: "Single-use FontSize..." entry `:566-575` → `:736-745`; Evidence citation
    `:569-572` → `:741-742`; resolved entries added: `:276-277` ("Version population loop
    consolidation") and `:493` ("`main-window-layout` names the wrong window")
  - No change: `docs/project-context.md:51`, `docs/architecture.md:37-39` (sentence at `:38-39`),
    `docs/architecture/ui.md:138-139`, `openspec/specs/launch-window-layout/spec.md:4`,
    `ApplicationBootstrapper.cs:53,68`, `ApplicationHost.cs:47,61,76`
- Every anchor in the original brief matched the tree exactly at `144c284`:
  - `AgentItem.cs:41-51` — doc `:41-46`, property `:47-51` (the brief's range covers both).
  - `ArchitectureBoundaryTests.cs:77-89` — read at `:82`, guard at `:87`, content assert at `:88`.
  - `ThemeColorLiteralTests.cs` — remark `:16-21`, walk assertion `:54`, `IsAllowlisted` entry at
    `:109` inside `:105-115`.
  - `docs/project-context.md:51`, `docs/architecture.md:37-39` (sentence spans `:38-39`),
    `docs/architecture/ui.md:138-139`, `docs/backlog.md:566-575` (citation at `:569-572`,
    historical at `:106-107` and `:323`),
    `openspec/specs/launch-window-layout/spec.md:4`.
- The claimed openspec 1.15.0 delta-Purpose behavior was independently confirmed in the
  installed CLI source (Context G).
- **Post-implementation revision (2026-10-29).** Per the user's decision, the two file-existence
  assertions were removed from `ArchitectureBoundaryTests`; the registration guard is the sole
  legacy-window check. The corresponding task, proposal, and D4 decision were updated to match.

## Open questions

1. **Purpose replacement wording (default fixed in task 2.5).** The default is to drop the final
   clause and end the sentence after "... is the application's active shell and the subject of
   this capability." If the implementation session finds the trailing sentence reads awkwardly, it
   may tighten the wording, provided no requirement or scenario text changes and the sentence
   still names the launch window as the subject of the capability.
2. **Legacy-window regression assertion.** The revised decision is to add no file-existence
    assertion; keep the `Assert.DoesNotContain("MainWindow", registration, ...)` guard in the
    existing `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` test.
3. **Manual smoke scope.** The default note asks the user to start the application
   (`dotnet run --project src/CLIHub/CLIHub.csproj`) and confirm normal startup (tray icon and
   launch window appear, theme renders). Since the deleted window was never constructed, the smoke
   is a sanity check, not a behavioral gate; the implementation session should still wait for the
   user's confirmation before asking for commit/archive approval, per repo policy.
