# Design: Consolidate dialog-owner and window-chrome duplication into two helpers

Verified against HEAD `8ca4c3f` on 2026-11-12. `git status --porcelain` lists only the ten
untracked `backup-port-*.md` analysis files (untouched by this change; 06 and 07 were read as
background). At preparation time `openspec/changes/` held only `archive/`; a concurrent session
added the non-overlapping `probe-cache-extraction` change (Core agents + Core tests only) while
this change was being prepared.

**Revision 2026-11-12 (follow-up session):** the user decided that D7's `WM_GETMINMAXINFO`
consolidation must be **included** in this change rather than parked. D7, D8, Alternatives
Rejected, Deviations, and Open Questions below are updated accordingly, and `tasks.md` Stage 3
now implements the extraction instead of the backlog entry. HEAD is now `8a243cf`
(`8ca4c3f..8a243cf` are docs-only proposal commits; the tracked tree is clean) and every
`file:line` below was re-verified against it — the preparation numbers are unchanged. A third
non-overlapping change (`simplify-mechanical-sweep`, model/converter files) is also active;
`openspec validate --all` now reports `Totals: 35 passed, 0 failed (35 items)`.

Baselines observed by this session:

- `dotnet test CLIHub.sln --no-build` → **1563 passed, 0 failed, 0 skipped** (839
  `CLIHub.Core.Tests` + 67 `CLIHub.Core.Windows.Tests` + 657 `CLIHub.Tests`).
- `openspec validate --all` → `Totals: 32 passed, 0 failed (32 items)`.

## Current State (Verified)

### A. Dialog-owner / MessageBox duplication (three services)

#### `src/CLIHub/Launching/ProjectDialogService.cs`

- `SelectFolder` passes `ResolveOwner()` to `OpenFolderDialog.ShowDialog` — `:23`.
- `ConfirmProjectRemoval` calls the private 4-arg `Show` with the literal title `"CLIHub"` — `:31-34`.
- Private `Show(message, title, buttons, image)` resolving the owner and branching between the two
  `MessageBox.Show` overloads — `:37-47`.
- Private `ResolveOwner()`: `Application.Current` null → null; first `IsActive` window in
  `application.Windows` → that window; else `application.MainWindow` — `:55-71`, with the doc
  comment explaining the policy ("a prompt triggered from the Settings window is modal to
  Settings") at `:49-54`.

#### `src/CLIHub/Launching/PluginDialogService.cs`

This class **did not exist** on `backup-before-reset` (it arrived with the plugin-package /
plugin-feed features, `eddeacf`/`d6ab6fd`) and copied the same pattern:

- `PickPluginPackage` passes `ResolveOwner()` to `OpenFileDialog.ShowDialog` — `:40`.
- `ShowInstallConfirmation` (`:85-88`), `ShowValidationError` (`:100`), and `ShowInstallOutcome`
  (`:110`, `:114-119`) call the private 4-arg `Show`.
- `ShowBrowsePluginsDialog` resolves the service's `BrowsePluginsDialog`, sets
  `dialog.Owner = ResolveOwner()`, and shows it modally with `ShowDialog()` — `:123-128`.
- Private `Show` — `:130-140`; private `ResolveOwner` — `:148-164` (byte-identical body to
  `ProjectDialogService.ResolveOwner`, same doc comment at `:142-147`).

#### `src/CLIHub/Launching/UserNotificationService.cs`

- `ShowInformation(message, title)` → private `Show` — `:11-12`.
- `ShowWarning(message)` → private `Show` with the literal title `"CLIHub"` — `:15-16`.
- Private `Show(message, title, image)`: owner is the simpler `Application.Current?.MainWindow`
  (`:20`), then the same two-branch OK-only `MessageBox.Show` — `:18-30`.

#### Architecture boundary

`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs`:

- `WpfAllowedApplicationFiles` (`:7-25`) lists the non-`Views/` application files allowed to
  contain the string `System.Windows`; the `Launching/` entries are `PluginDialogService.cs`
  (`:13`), `ProjectDialogService.cs` (`:14`), `UserNotificationService.cs` (`:17`).
- `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` (`:41-53`) fails for any file under
  `src/CLIHub` that contains `System.Windows` and is not allow-listed; `IsWpfAllowed`
  (`:155-167`) auto-allows `Views/`, `Behaviors/`, `Controls/`, `Converters/`, `Interop/`,
  `Hotkeys/`, `ViewModels/` prefixes. A new `Launching/MessageBoxHelper.cs` therefore **requires**
  an allow-list entry; a new `Views/WindowChromeHelper.cs` does not (auto-allowed by the `Views/`
  prefix, `:158`).

#### Test exposure of the three services

- Composition only: `ServiceRegistrationTests.cs:52-53` resolves `IProjectDialogService` and
  `IUserNotificationService` and asserts non-null — no dialog is ever shown.
- All behavioral tests mock the interfaces (`LaunchWindowViewModelBuilder.cs:264-267`,
  `SettingsViewModelFixture.cs:27`) or fake them (`Support/FakePluginDialogService.cs`); none
  constructs the WPF dialog code under test. No test can observe this split.

### B. Window-chrome duplication (four windows)

#### `EnsureHandle()` + `DwmApi.TryRoundCorners`

All four `SourceInitialized` handlers (each wired in XAML: `LaunchWindow.xaml:22`,
`SettingsWindow.xaml:21`, `WhatsNewWindow.xaml:16`, `BrowsePluginsDialog.xaml:18`):

- `LaunchWindow.xaml.cs:102-103` — expression-bodied
  `DwmApi.TryRoundCorners(new WindowInteropHelper(this).EnsureHandle())`.
- `SettingsWindow.xaml.cs:64-69` — block form; the `handle` is reused for
  `HwndSource.FromHwnd(handle)?.AddHook(this.OnWindowMessage)` (`:68`).
- `WhatsNewWindow.xaml.cs:52-53` — expression-bodied, identical to LaunchWindow's.
- `BrowsePluginsDialog.xaml.cs:30-35` — block form identical to SettingsWindow's (handle reused
  for `AddHook`, `:34`). **Did not exist on backup** (draggable/resizable dialog added by `77531ea`).

`DwmApi.TryRoundCorners(IntPtr)` is `internal` and no-ops on `IntPtr.Zero` and on unsupported OS
attributes — `src/CLIHub/Interop/DwmApi.cs:17-26`.

#### Owner-to-launch-window + Show + un-minimize + Activate

- `SettingsWindow.ShowSettings` (`:38-62`): comment + guarded owner assignment
  (`this.Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this)`)
  at `:42-47`; launch-window height sync at `:49-52` (Settings-specific); `Show()` `:54`;
  `Minimized → Normal` restore `:56-59`; `Activate()` `:61`.
- `WhatsNewWindow.ShowNotes` (`:33-50`): identical owner block at `:35-40` (with its own copy of
  the comment), then `Show()` `:42`, restore `:44-47`, `Activate()` `:49`.
- `LaunchWindow.ShowOnPointerMonitor` (`:56-84`): `Show()` `:67`, restore `:69-72`, **then
  `viewModel.MarkShown()` `:74`**, then `Activate()` `:75` — the sequence is interleaved with
  window-specific work (`WindowPositioner.Place`, `PrepareForShow`, deferred placement).
- `BrowsePluginsDialog`: no such sequence — its owner is assigned by
  `PluginDialogService.ShowBrowsePluginsDialog` (`PluginDialogService.cs:126`) and it is shown
  modally via `ShowDialog()` (`:127`), not shown/activated.

#### `WM_GETMINMAXINFO` hooks (shared-fragment duplication)

- `SettingsWindow.OnWindowMessage` (`:78-110`): guard `:80`; monitor lookup
  `User32.MonitorFromWindow`/`GetMonitorInfo` `:85-95`; DPI scale `:97-98`; work-area
  `ptMaxPosition`/`ptMaxSize` `:101-104`; `ptMinTrackSize` from `MinWidth`/`MinHeight` `:105-106`.
- `BrowsePluginsDialog.OnWindowMessage` (`:44-60`): guard `:46`; DPI scale `:51-52` (identical);
  `ptMinTrackSize` `:55-56` (identical); **no** monitor/maximize part (the dialog cannot maximize).
- `User32.WM_GETMINMAXINFO`/`MINMAXINFO`/`GetDpiForWindow`/`MonitorFromWindow`/`MONITORINFO` are
  declared in `src/CLIHub/Interop/User32.cs` (internal).
- Spec-pinned behavior: `plugin-feed` requires the Browse Plugins dialog to have drawn chrome with
  "enforced minimum size (520x360)" and rounded corners and to stay modal and owned by the launch
  window — `openspec/specs/plugin-feed/spec.md:366-397`; the XAML pins `MinHeight="360"
  MinWidth="520"` (`BrowsePluginsDialog.xaml:14`).

### C. Specs constraining this surface

- `plugin-feed/spec.md:366-397` — Browse Plugins dialog drawn chrome (rounded corners, minimum
  size, modal, owned by the launch window, header drag, Escape).
- `app-lifecycle/spec.md:160-162` — "Modal dialogs do not hide the window": a folder picker or
  confirmation dialog opened from the launch window must not hide it (enforced via
  `PromptState.IsPromptOpen`, `LaunchWindow.xaml.cs:107-115` — not touched by this change).
- No spec constrains `MessageBox` captions, the `SettingsWindow`/`WhatsNewWindow` activation
  order, or where the owner-resolution code lives.

### D. Backup-branch source material (read via `git show`)

- `f0ca336` "consolidate project mix summary, failure status, and MessageBox owner": new
  `src/CLIHub/Launching/MessageBoxHelper.cs` (63 lines, `internal static`, members
  `ApplicationTitle`, `Owner => Application.Current?.MainWindow`, OK-only `Show`, 4-arg `Show`);
  `ProjectDialogService` and `UserNotificationService` rewired; one allow-list line added to
  `ArchitectureBoundaryTests`. Also `ProjectMixSummaryText`, `SettingsUpdateStatusPresenter`,
  `SettingsViewModel`, `ProjectRegistryViewModel` edits (obsolete on master — see Non-Goals).
  Rationale: `git show backup-before-reset:openspec/changes/archive/2026-10-28-small-duplication-sweep/`.
- `e141732` "consolidate window chrome operations and fix version display": new
  `src/CLIHub/Views/WindowChromeHelper.cs` (61 lines, `public static`, members
  `EnsureOwnedByLaunchWindow`, `ShowAndActivate`, `ApplyRoundedCorners`); `LaunchWindow`,
  `SettingsWindow`, `WhatsNewWindow` rewired. The same commit's version-display fixes are
  master-inapplicable (the regression never existed there; agent version chips were replaced by
  the update chip in `7414c41`).
- **Master divergence:** `72b1f25` "fix(ui): own project dialogs by the active window" upgraded
  `ResolveOwner` to active-window-first **after** the backup branch's helper was written. Master's
  code is the authority; the port adapts (D1).

## Decisions

### D1 — `MessageBoxHelper.Owner` carries master's active-window-first resolution, not backup's

Backup's `Owner => Application.Current?.MainWindow` matches `UserNotificationService`'s current
inline code but is a **regression** for `ProjectDialogService`/`PluginDialogService`, whose
`ResolveOwner` has walked `application.Windows` for the `IsActive` window first since `72b1f25`
(exactly so a prompt triggered from the Settings window is modal to Settings). The helper's `Owner`
property implements master's walk verbatim (null app → null; first active window; else
`MainWindow`), re-read live per access, with the doc comment adapted from
`ProjectDialogService.cs:49-54`. Rule applied: the plan contradicts the code → the code wins.

### D2 — All three services unify on the helper, including `UserNotificationService`'s owner policy

`UserNotificationService` could keep its MainWindow-only owner (helper with two accessors or an
inline leftover). Rejected: that preserves the policy split this change exists to remove, and the
two policies already diverged once. Unifying on active-window-first reproduces `72b1f25`'s intent
for notifications too: a warning triggered while Settings is active becomes modal to Settings
instead of always to the launch window. Captions, buttons, icons, and return values are unchanged;
no spec constrains the notification owner; `PromptState` flow (the launch-window hide guard,
`app-lifecycle/spec.md:160-162`) is untouched. This is the change's single deliberate behavioral
nuance and is called out in the proposal.

### D3 — Helper shapes: names, accessibility, location, headers

- Members and names follow backup exactly: `ApplicationTitle`, `Owner`, the two `Show` overloads;
  `EnsureOwnedByLaunchWindow`, `ShowAndActivate`, `ApplyRoundedCorners`.
- `MessageBoxHelper` → `src/CLIHub/Launching/MessageBoxHelper.cs`, namespace `CLIHub.Launching`
  (same folder/namespace as its three consumers; matches backup and the existing
  `WpfAllowedApplicationFiles` `Launching/` entries).
- `WindowChromeHelper` → `src/CLIHub/Views/WindowChromeHelper.cs`, namespace `CLIHub.Views`
  (same folder/namespace as its four consumers; auto-allowed by the boundary test's `Views/`
  prefix).
- **Both helpers are `internal static class` with `internal` members.** `WindowChromeHelper` was
  `public` on backup; master's convention for this kind of helper is `internal`
  (`Interop/WindowPositioner.cs:13`, `Interop/DwmApi.cs:8`), and nothing outside the assembly can
  or should call either helper. Deliberate deviation from `e141732`, recorded below.
- **No `// --- <file name= …>` header blocks.** Backup's files carry them; zero files under
  `src/` do today (verified by grep). The port drops them.
- Style: file-scoped namespace first, `using` directives after it, `this.` qualification (static
  helpers have no instance members), XML docs on every member (master requires them on public APIs
  and writes them on internal helpers too — see `WindowPositioner.cs:23-27`).

### D4 — `WindowChromeHelper` scope: four windows, three operations

- `ApplyRoundedCorners(Window)` replaces the `EnsureHandle()+TryRoundCorners` pair in all four
  `OnSourceInitialized` handlers. In `SettingsWindow` and `BrowsePluginsDialog` the handler keeps
  its own `var handle = new WindowInteropHelper(this).EnsureHandle();` line for the `AddHook`
  call — `EnsureHandle` is idempotent (returns the already-created HWND), which is exactly the
  shape `e141732` shipped for `SettingsWindow`. Rejected alternative: `ApplyRoundedCorners`
  returning the `IntPtr` to avoid the second `EnsureHandle` call — marginally clever, deviates from
  the proven shape, saves nothing measurable.
- `EnsureOwnedByLaunchWindow` + `ShowAndActivate` replace the sequences in
  `SettingsWindow.ShowSettings` and `WhatsNewWindow.ShowNotes` statement-for-statement. The
  Settings-specific height sync (`:49-52`) stays between the two helper calls (order matters: the
  owner must be set before `Show`, the height before layout settles).
- `LaunchWindow.OnSourceInitialized` uses `ApplyRoundedCorners` only — see D5.

### D5 — `LaunchWindow`'s show sequence stays inline (deviation from `e141732`)

Backup's `e141732` replaced `LaunchWindow`'s Show/restore/Activate with
`ShowAndActivate`, moving the `shownAt` assignment before the call — i.e. it accepted a reorder.
Master's sequence is `Show()` `:67` → restore `:69-72` → **`viewModel.MarkShown()` `:74`** →
`Activate()` `:75`, where `MarkShown` arms the deactivation grace period
(`LaunchWindowViewModel.ShouldHideOnDeactivate` compares against `shownAt`). A
`ShowAndActivate(this)` substitution forces either MarkShown-before-Show (grace armed even if
`Show` throws or the window is closed — different from today) or MarkShown-after-Activate (a
deactivation slipping between `Activate` and `MarkShown` would bypass the grace window). Neither is
behavior-identical, `LaunchWindow` is itself the launch window (so `EnsureOwnedByLaunchWindow`
never applies to it), and its show path carries placement and deferred-layout work that is
window-specific anyway. Per the brief ("owner+show+activate sequence is duplicated in
SettingsWindow and WhatsNewWindow"), the port extracts only `ApplyRoundedCorners` from
`LaunchWindow`. The brief's author scoped it this way; this design confirms the reasoning.

### D6 — One allow-list line in `ArchitectureBoundaryTests`

`"Launching" + Path.DirectorySeparatorChar + "MessageBoxHelper.cs",` appended after the
`UserNotificationService.cs` entry (`:17`), mirroring `f0ca336`'s exact edit. This is the change's
only test-file edit and is additive. `Views/WindowChromeHelper.cs` needs no entry (`IsWpfAllowed`
auto-allows the `Views/` prefix, `ArchitectureBoundaryTests.cs:158`).

### D7 — `WM_GETMINMAXINFO`: consolidate the shared min-track fragment in this change (user decision)

**Decision history.** Preparation recommended parking this in `docs/backlog.md`; on 2026-11-12
the user decided the consolidation **must be included in this change**. Parking is recorded as a
rejected alternative (below and in Alternatives Rejected). The shape designed here is exactly
the one the preparation-time override criteria allowed: a single shared method, no behavioral
seam, work-area maximize logic staying in `SettingsWindow`.

**Verified duplication (re-confirmed at `8a243cf`).**

- `BrowsePluginsDialog.OnWindowMessage` (`:44-60`, the `HwndSource` hook registered by
  `OnSourceInitialized :30-35`): `WM_GETMINMAXINFO` guard `:46-49`; DPI scale
  (`User32.GetDpiForWindow` → `dpi == 0 ? 1.0 : dpi / 96.0`) `:51-52`;
  `Marshal.PtrToStructure<User32.MINMAXINFO>` `:54`; `ptMinTrackSize.X/Y =
  (int)Math.Ceiling(this.MinWidth/this.MinHeight * scale)` `:55-56`; `Marshal.StructureToPtr`
  `:57`; `handled = true; return IntPtr.Zero` `:58-59`. Doc rationale `:37-43` (WPF's own
  handler is bypassed, so layout units are converted to physical pixels explicitly).
- `SettingsWindow.OnWindowMessage` (`:78-110`): the same guard `:80-83`; monitor lookup
  `User32.MonitorFromWindow`/`GetMonitorInfo` with two early returns that leave `handled` unset
  `:85-95`; the same DPI scale `:97-98`; the same marshal `:100`; `ptMaxPosition`/`ptMaxSize`
  from the monitor work area `:101-104` (Settings-only: only this window can maximize); the same
  `ptMinTrackSize` pair `:105-106`; marshal-back + handled + return `:107-109`.

**Extraction.** One new member on the stage-2 `WindowChromeHelper` (D3/D4 file; `internal` per
D3; `Views/` is auto-allowed by the boundary test, so no allow-list edit):

```csharp
/// <summary>
///   Writes <see cref="System.Windows.FrameworkElement.MinWidth" />/
///   <see cref="System.Windows.FrameworkElement.MinHeight" /> into the message's
///   <see cref="User32.MINMAXINFO" /> as the minimum tracking size, converting layout units
///   to physical pixels with the window's DPI because handling the message bypasses WPF's
///   own conversion.
/// </summary>
internal static void ApplyMinTrackSize(Window window, IntPtr hwnd, IntPtr lParam)
```

Body = the byte-identical shared fragment (the `dpi`/`scale` pair, `Marshal.PtrToStructure`, the
two `Math.Ceiling` assignments — `window.` in place of `this.` — `Marshal.StructureToPtr`), doc
rationale adapted from `BrowsePluginsDialog.xaml.cs:37-43`.

**Callers (behavior-identical).**

- `BrowsePluginsDialog.OnWindowMessage` keeps its guard and its `handled = true;
  return IntPtr.Zero;` tail; the body between them becomes the single call
  `WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);`. The hook's doc comment shrinks to
  the delegation statement (the DPI-conversion rationale moves to the helper's doc).
- `SettingsWindow.OnWindowMessage` keeps its guard, the monitor lookup with both early returns,
  the `Marshal.PtrToStructure` (`:100`), and the four `ptMax*` work-area assignments
  (`:101-104`); the local DPI-scale pair (`:97-98`) and the inline `ptMinTrackSize` pair
  (`:105-106`) are deleted and replaced by `WindowChromeHelper.ApplyMinTrackSize(this, hwnd,
  lParam);` placed immediately before `handled = true;`. The helper performs its own marshal
  round trip, which **preserves** the `ptMax*` fields the caller just wrote, so the final struct
  contents, the `handled` flag, and both hooks' return values are identical. Doc comment
  `:71-77` stays as is (both of its statements remain true).

**Accepted trade-off.** The Settings hook marshals `MINMAXINFO` twice per message (one extra
40-byte struct copy per `WM_GETMINMAXINFO`, which arrives at size/position boundaries, not every
frame). Accepted so that one shared member also empties the dialog's hook; the alternative that
avoids it is rejected below.

**Constraints honored.** Behavior-identical (same guards, same DPI math, same `Math.Ceiling`
conversion, same field sources read live at message time, same early returns, same
`handled = true`); the `plugin-feed` 520x360 minimum stays pinned by
`openspec/specs/plugin-feed/spec.md:366-397` (resize scenario `:374-377`) and remains the
acceptance bar; Settings keeps its current constraints (its XAML `MinWidth`/`MinHeight` are read
by the helper exactly where the inline code read them); thin code-behind preserved (the dialog's
hook collapses to guard + one call + handled); master style per D3. Using consequences:
`BrowsePluginsDialog` drops `using System.Runtime.InteropServices;` (`:3`; `Marshal` moves
wholly to the helper) and keeps `System.Windows.Interop` (`HwndSource`/`WindowInteropHelper`)
and `CLIHub.Interop` (the guard's `User32.WM_GETMINMAXINFO`); `SettingsWindow` keeps every using
(`Marshal.SizeOf`/`PtrToStructure`/`StructureToPtr` and the `User32` monitor APIs remain);
`WindowChromeHelper.cs` adds `using System.Runtime.InteropServices;` (ordering per
`.editorconfig`).

**Alternatives rejected for this decision.**

- **Park in `docs/backlog.md`** (the preparation recommendation; low severity, revisit on a
  third min-track consumer). Reversed by explicit user decision on 2026-11-12 — the
  consolidation ships in this change. The preparation's residual objections (review surface in
  WndProc-adjacent code, growing beyond the backup port) are answered by keeping the extraction
  to the single assessed shape, with Settings' work-area logic untouched.
- **Two-member ref shape** — `GetDpiScale(IntPtr)` plus
  `ApplyMinTrackSize(Window, double, ref User32.MINMAXINFO)`, which keeps a single marshal round
  trip in Settings: two members for six shared lines, the dialog keeps its marshal boilerplate,
  and the saved 40-byte copy is unmeasurable.
- **Move the guard into the helper** — Settings' monitor lookup must be gated by the same
  message check anyway, so the guard would remain duplicated in Settings; no net removal, more
  coupling.
- **Helper-owned hook registration** (the helper calls `AddHook` with its own lambda over the
  whole message) — cannot host Settings' local work-area logic without a callback seam, and
  contradicts D4's proven `EnsureHandle`+`AddHook`-in-caller shape from `e141732`.

### D8 — `skip_specs: true`, no delta specs

Pure internal refactor; the affected specs (`plugin-feed`, `app-lifecycle`) pin preserved behavior
(see Current State C). Including the D7 min-track extraction does not change this: `plugin-feed`
pins the 520x360 minimum and resize floor (`:366-397`), which the extraction preserves exactly
(same guard, DPI math, assignments, and `handled` flag) — this supersedes the preparation-time
note that parking kept the skip-specs story "strongest". No requirement or scenario changes, so
no `specs/` directory is created and `.openspec.yaml` declares `skip_specs: true`. Precedent:
`openspec/changes/archive/2026-11-09-tidy-application-folders/.openspec.yaml`.

### D9 — No new tests

The concrete WPF dialog services are exercised only by composition checks; all behavioral tests
mock or fake the interfaces, and the four windows have no direct tests (thin code-behind by
convention). A unit test for either helper would have to mock `Application.Current`/`MessageBox`/
`DwmApi` — new abstractions whose only purpose is the test, contradicting the repository's
no-new-abstraction guidance and the backup change's rationale (D7 there). The moved code keeps its
existing coverage: boundary tests (with the one added line), composition tests, and the spec-level
behavior that stays intact.

## Alternatives Rejected

- **Port backup's `Owner` accessor verbatim** (MainWindow-only) — regresses `72b1f25` for two of
  the three services (D1).
- **Keep `UserNotificationService` on a MainWindow-only owner** — preserves the policy split and
  duplicates the duplication the change removes (D2).
- **Extract `LaunchWindow`'s show sequence as backup did** — impossible without reordering
  `MarkShown` relative to `Show`/`Activate`; behavior-identity wins over port fidelity (D5).
- **`public WindowChromeHelper` as on backup** — no external consumer; master's helper convention
  is `internal` (D3).
- **`ApplyRoundedCorners` returning the HWND** — avoids a second idempotent `EnsureHandle` call but
  deviates from the proven `e141732` shape for no measurable gain (D4).
- **Move `MessageBoxHelper` into `Views/` or `Interop/` to avoid the allow-list edit** — wrong
  location for dialog-owner policy used by `Launching` services; the allow-list exists precisely to
  force a conscious decision here (`f0ca336` made the same one).
- **Consolidate `GlobalHotkeyService.cs:36`'s `EnsureHandle` too** — different purpose (hotkey
  HWND, no rounding, no chrome); not duplication of this pattern.
- **Delta spec for window activation/dialogs** — invents a contract this refactor does not change
  (D8).
- **Park the `WM_GETMINMAXINFO` extraction in `docs/backlog.md`** — the preparation-time
  recommendation; reversed by explicit user decision on 2026-11-12, so the consolidation is in
  scope in the single-member shape (D7, which also rejects the ref-shape, guard-in-helper, and
  helper-owned-registration variants).

## Deviations (from the backup source material)

1. `MessageBoxHelper.Owner` resolves the active window first (master's `72b1f25`); backup's
   accessor was `Application.Current?.MainWindow` only (D1). `UserNotificationService` inherits
   this nuance deliberately (D2).
2. `WindowChromeHelper` is `internal`, not `public` (D3); neither new file carries backup's
   `// --- <file name= …>` header block (D3).
3. `PluginDialogService` (third service) and `BrowsePluginsDialog` (fourth window) are rewired;
   neither existed on backup (D4).
4. `LaunchWindow` keeps its inline show sequence; `e141732` had extracted it with a reorder (D5).
5. The version-display fixes riding in `e141732` are dropped (never applicable to master);
   `ProjectMixSummaryText`/`SettingsUpdateStatusPresenter` from `f0ca336` are dropped (obsolete).
6. The `WM_GETMINMAXINFO` min-track extraction (D7) is not part of either backup commit — this
   change deliberately grows beyond a pure port there, on the user's 2026-11-12 decision.

## Open Questions

1. **Owner-property shape.** This design specifies `internal static Window? Owner` (property, live
   re-read), mirroring backup's call-site shape (`MessageBoxHelper.Owner`). If the implementer
   prefers `internal static Window? ResolveOwner()` (method, mirroring master's current private
   name), that is an acceptable equivalent — pick one, do not ship both.
2. **Confirm-no-smoke.** The design expects no manual smoke (literal moves only). If the
   implementation session observes a visible difference in dialog ownership, window rounding,
   z-order, or — new with D7 — minimum-size enforcement while resizing Settings or the Browse
   Plugins dialog, it must stop and report the path rather than proceed to the final gates.
   (The preparation-time open question on the D7 park-or-include choice is resolved by the
   user's 2026-11-12 decision and is recorded in D7, not here.)
