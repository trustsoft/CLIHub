# Consolidate dialog-owner and window-chrome duplication into two helpers

## Why

Two families of copy-pasted WPF glue have spread across the application layer, and master now
carries **more** copies than the branch the consolidations were originally written on:

- **Dialog owner + MessageBox plumbing.** Three launcher services each resolve a dialog owner
  window and pick a `MessageBox.Show` overload from it. `ProjectDialogService`
  (`src/CLIHub/Launching/ProjectDialogService.cs:49-71`) and `PluginDialogService`
  (`src/CLIHub/Launching/PluginDialogService.cs:142-164`) carry a byte-identical private
  `ResolveOwner` (PluginDialogService did not exist when the original consolidation was written and
  copied the pattern); `UserNotificationService` inlines a simpler
  `Application.Current?.MainWindow` owner plus the `"CLIHub"` caption literal
  (`src/CLIHub/Launching/UserNotificationService.cs:16-30`). The modal-owner policy now lives in
  three places and has already drifted once (MainWindow-only vs active-window-first).
- **Window chrome.** `EnsureHandle()` + `DwmApi.TryRoundCorners` is duplicated across **four**
  windows — `LaunchWindow.xaml.cs:102-103`, `SettingsWindow.xaml.cs:66-67`,
  `WhatsNewWindow.xaml.cs:52-53`, `BrowsePluginsDialog.xaml.cs:32-33` — and the
  owner-to-launch-window + `Show` + un-minimize + `Activate` sequence is duplicated verbatim in
  `SettingsWindow.ShowSettings` (`SettingsWindow.xaml.cs:42-61`) and `WhatsNewWindow.ShowNotes`
  (`WhatsNewWindow.xaml.cs:35-49`). Divergence here produces hard-to-spot visual and z-order bugs
  (rounded corners silently missing, a window hidden behind the always-on-top launch window).

This change ports two proven consolidations from the archived branch `backup-before-reset` —
`MessageBoxHelper` (commit `f0ca336`, archive `2026-10-28-small-duplication-sweep`) and
`WindowChromeHelper` (commit `e141732`) — adapted to master's current windows and services. Master
grew a third dialog service and a fourth window after the backup branch diverged, so the helpers
close strictly more duplication here than they did there.

## What Changes

Pure internal refactor: no user-visible behavior change, no public API, DI, XAML, configuration, or
logging change. Verified against HEAD `8ca4c3f` (working tree holds only the ten unrelated untracked
`backup-port-*.md` analysis files, which this change does not touch). Evidence with `file:line` is
in `design.md`.

### 1. `MessageBoxHelper` (adapted from `f0ca336`)

Add `src/CLIHub/Launching/MessageBoxHelper.cs` — `internal static class MessageBoxHelper` owning:

- `internal const string ApplicationTitle = "CLIHub"` — the caption used by dialog titles;
- `internal static Window? Owner` — **master's active-window-first resolution**, not backup's
  `MainWindow`-only one: the currently active application window, then `Application.Current`'s
  main window, then `null` (this is the behavior master's `ResolveOwner` has had since `72b1f25`;
  porting backup's simpler accessor would silently regress it);
- `internal static void Show(string message, string title, MessageBoxImage image)` — OK-only dialog;
- `internal static MessageBoxResult Show(string message, string title, MessageBoxButton buttons,
  MessageBoxImage image)` — confirmation dialog.

Rewire three services to it and delete their private copies:

- `ProjectDialogService`: `SelectFolder` (`:23`) uses `MessageBoxHelper.Owner`,
  `ConfirmProjectRemoval` (`:31-34`) uses the 4-arg `Show` with `MessageBoxHelper.ApplicationTitle`;
  delete private `Show` (`:37-47`) and `ResolveOwner` (`:49-71`).
- `PluginDialogService` (did not exist on backup): `PickPluginPackage` (`:40`) and
  `ShowBrowsePluginsDialog` (`:126`) use `MessageBoxHelper.Owner`; the three `Show…` methods
  (`:85-88`, `:100`, `:110-119`) use the 4-arg `Show`; delete private `Show` (`:130-140`) and
  `ResolveOwner` (`:142-164`).
- `UserNotificationService`: `ShowInformation`/`ShowWarning` delegate to the 3-arg `Show` with
  `MessageBoxHelper.ApplicationTitle` (`:15-16`); delete the private `Show` (`:18-30`).

One intentional nuance: `UserNotificationService`'s owner policy unifies from MainWindow-only to the
helper's active-window-first resolution — the same intent as master's `72b1f25` (a dialog triggered
from the Settings window becomes modal to Settings instead of always to the launch window). See
`design.md` D2.

One test-file line, mirroring `f0ca336`: a new `Launching/*.cs` file referencing `System.Windows`
fails `ArchitectureBoundaryTests.ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` until
`MessageBoxHelper.cs` is added to `WpfAllowedApplicationFiles`
(`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:7-25`). Additive only.

### 2. `WindowChromeHelper` (adapted from `e141732`)

Add `src/CLIHub/Views/WindowChromeHelper.cs` — `internal static class WindowChromeHelper` with
backup's three operations:

- `EnsureOwnedByLaunchWindow(Window)` — the owner-to-main-window assignment with the
  null-and-not-self guard;
- `ShowAndActivate(Window)` — `Show`, restore from `Minimized`, `Activate`;
- `ApplyRoundedCorners(Window)` — `WindowInteropHelper.EnsureHandle()` + `DwmApi.TryRoundCorners`.

Rewire four windows:

- `LaunchWindow` (`:102-103`): `ApplyRoundedCorners` only. Its show sequence stays inline —
  `viewModel.MarkShown()` sits between the restore and `Activate` (`:69-75`), so a
  behavior-identical extraction is impossible, and as the launch window itself it needs no owner
  (see `design.md` D5; backup's `e141732` accepted a reorder here, this port does not).
- `SettingsWindow` (`ShowSettings :38-62`, `OnSourceInitialized :64-69`):
  `EnsureOwnedByLaunchWindow` + `ShowAndActivate` (the launch-window height sync `:49-52` stays) and
  `ApplyRoundedCorners`; the `WM_GETMINMAXINFO` hook (`:78-110`) keeps its monitor/work-area logic
  and delegates its min-track fragment to the helper (section 3).
- `WhatsNewWindow` (`ShowNotes :33-50`, `OnSourceInitialized :52-53`): both show-sequence
  operations + `ApplyRoundedCorners`.
- `BrowsePluginsDialog` (did not exist on backup; `OnSourceInitialized :30-35`):
  `ApplyRoundedCorners` only — its owner is set by `PluginDialogService.ShowBrowsePluginsDialog`
  and it is shown modally via `ShowDialog`, so the owner/show/activate sequence does not apply. Its
  `WM_GETMINMAXINFO` hook (`:44-60`) collapses to the guard + helper call + `handled` (section 3).

Usings that become unused (`System.Windows.Interop`, `CLIHub.Interop` in `LaunchWindow` and
`WhatsNewWindow`) are removed; `SettingsWindow` keeps all of its usings (`HwndSource`, `User32`,
and `Marshal` for the work-area logic are still used); `BrowsePluginsDialog` additionally drops
`System.Runtime.InteropServices` once `Marshal` moves to the helper (section 3).

### 3. Consolidate the `WM_GETMINMAXINFO` min-track fragment

The min-track-size fragment duplicated between `SettingsWindow.xaml.cs:78-110` and
`BrowsePluginsDialog.xaml.cs:44-60` **is** extracted here (user decision 2026-11-12, reversing
the preparation-time park recommendation). One new `WindowChromeHelper` member —
`ApplyMinTrackSize(Window, IntPtr, IntPtr)` — owns the byte-identical DPI-scale +
`ptMinTrackSize` fragment (~10 lines per hook); each window keeps its `WM_GETMINMAXINFO` guard
and `handled = true` tail, and `SettingsWindow` keeps its maximized-bounds/work-area logic
(`:85-104`) locally in its own hook. Behavior-identical: same DPI math, same `Math.Ceiling`
conversion, same early returns; the `plugin-feed` 520x360 minimum-size scenarios
(`openspec/specs/plugin-feed/spec.md:366-397`) remain the acceptance bar (design.md D7).

### Final gates

Clean rebuild, full test run (baseline **1563** passed = 839 `CLIHub.Core.Tests` + 67
`CLIHub.Core.Windows.Tests` + 657 `CLIHub.Tests`; expected unchanged by this change, which adds and
removes no tests), `openspec validate --all` (baseline `Totals: 32 passed, 0 failed (32 items)`;
`35` observed at revision with this change, `probe-cache-extraction`, and
`simplify-mechanical-sweep` all active), and
`graphify update .`. Note: concurrent sessions added `probe-cache-extraction` and started
`simplify-mechanical-sweep` during preparation; validation item counts below are stated per-run.

## Justification for `skip_specs: true`

Both consolidations are behavior-preserving internal refactors behind unchanged call shapes. The
only specs that constrain this surface — `plugin-feed` (Browse Plugins drawn chrome: rounded
corners, minimum size 520x360, modal, owned by the launch window; `openspec/specs/plugin-feed/spec.md:366-397`)
and `app-lifecycle` (modal dialogs must not hide the launch window, `openspec/specs/app-lifecycle/spec.md:160-162`) —
pin behavior that this change preserves exactly: same `DwmApi.TryRoundCorners` call, same
owner-assignment guard, same `ShowDialog(Owner)` modality, same `PromptState` flow, and — with
the `WM_GETMINMAXINFO` extraction included (design.md D7) — the same minimum-tracking-size
enforcement (guard, DPI math, `ptMinTrackSize`/work-area fields, `handled` flag) behind the
520x360 floor. No requirement,
scenario, or scenario name changes, so no `specs/` delta is created and `.openspec.yaml` declares
`skip_specs: true` (precedent: `openspec/changes/archive/2026-11-09-tidy-application-folders/`).

## Impact

- Source files changed (5 modified, 2 new):
  - new `src/CLIHub/Launching/MessageBoxHelper.cs`, new `src/CLIHub/Views/WindowChromeHelper.cs`;
  - modified `src/CLIHub/Launching/ProjectDialogService.cs`, `src/CLIHub/Launching/PluginDialogService.cs`,
    `src/CLIHub/Launching/UserNotificationService.cs` (delegate to the helper, delete private
    `Show`/`ResolveOwner` copies);
  - modified `src/CLIHub/Views/LaunchWindow.xaml.cs`, `src/CLIHub/Views/SettingsWindow.xaml.cs`,
    `src/CLIHub/Views/WhatsNewWindow.xaml.cs`, `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`
    (delegate to the helper, drop now-unused usings).
- Test files: one line added to `ArchitectureBoundaryTests.cs:7-25` (the `MessageBoxHelper.cs`
  allow-list entry). No test added, deleted, or weakened; expected total stays **1563**.
- Documentation: none — the preparation-time `docs/backlog.md` park entry is superseded by
  including the extraction (design.md D7); no backlog edit.
- No `.csproj` change (the WPF SDK globs the two new `.cs` files); no XAML change (the
  `SourceInitialized="OnSourceInitialized"` handlers keep their names and signatures); no DI change
  (helpers are static).
- User-visible impact: none intended. The single deliberate nuance is `UserNotificationService`'s
  owner resolution unifying on active-window-first (design.md D2) — dialog captions, buttons,
  icons, and modality are unchanged.
- Net line effect ≈ backup's `e141732` plus the third service and fourth window: roughly
  −100 duplicated lines, +2 focused helper files (the min-track move is ≈ net-neutral in lines
  but removes the second copy of the DPI/min-track policy).

## Non-Goals

- No behavior change beyond the documented `UserNotificationService` owner-policy unification; no
  public API, DI, XAML, command, configuration, or logging change.
- **Not** the version-display fix from `e141732` (Settings footer `{Binding Version}` regression
  and the agent-version `v` prefix): the regression never existed on master, and agent version
  chips were replaced by the update chip (`7414c41`).
- **Not** `ProjectMixSummaryText` and **not** the `SettingsUpdateStatusPresenter` failure-status
  dedup from `f0ca336`: master no longer has the second project-mix formatter or the presenter, so
  both sub-items are obsolete there.
- **Not** consolidating `LaunchWindow`'s show/un-minimize/activate sequence (design.md D5).
- **Not** the other eight `backup-port-*.md` port candidates; only 06 and 07 apply here.
- **Not** introducing a dialog abstraction/port to make `MessageBoxHelper` testable, and no new
  tests for either helper (design.md D9).
- No commit, push, or archive in this change's tasks; those follow the repository approval policy.

## Dependencies

- None on other active changes: this change touches only `src/CLIHub/Launching`, `src/CLIHub/Views`,
  one line of `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs`. Concurrent sessions
  added `probe-cache-extraction` (exclusively `src/CLIHub.Core/Agents` + `tests/CLIHub.Core.Tests`
  — zero file overlap) and proposed `simplify-mechanical-sweep` (model/converter/XAML-resource
  files — no `.xaml.cs` overlap); their test additions may raise the
  suite total if they land first, so this change's test-count gate is phrased as "no count delta
  attributable to this change".
- No spec dependencies: `skip_specs: true`, pure internal consolidation.
- Source material (read-only): branch `backup-before-reset` commits `f0ca336` and `e141732`, and
  archive `2026-10-28-small-duplication-sweep` for rationale. Nothing depends on this change.

## Risk

- **Owner-policy identity.** The one regression trap is porting backup's `Owner` accessor verbatim:
  master's `ResolveOwner` is active-window-first since `72b1f25`. The helper must carry master's
  walk, and the gate is a review of the moved body, not the backup diff.
- **Overload selection.** The helpers keep the same two-branch `MessageBox.Show` shape (null owner →
  ownerless overload), so overload resolution and null-owner behavior are unchanged.
- **Show sequencing.** `SettingsWindow`/`WhatsNewWindow` sequences are replaced statement-for-
  statement; `LaunchWindow`'s interleaved sequence is deliberately left alone (D5).
- **Min-track identity (D7).** The `WM_GETMINMAXINFO` rewires are guard-for-guard and
  field-for-field identical; the only mechanical difference is a second `MINMAXINFO` marshal
  round trip in `SettingsWindow` (accepted trade-off, design.md D7). The no-smoke expectation
  below therefore also covers minimum-size enforcement.
- **Boundary gate.** The single likely gate failure is the missing `WpfAllowedApplicationFiles`
  entry; the task names the failing test explicitly.
- **Test risk.** The suite is unchanged in size (1563) and the only test-file edit is additive, so
  any other count at a gate is a signal to stop rather than adjust expectations.
- **No manual smoke is required**: all replaced code paths are literal moves of the same calls. If
  the implementation session concludes otherwise, it must name the path and reason.
