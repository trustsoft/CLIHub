# Shared chromeless-window base (`ChromeWindow` + `WindowHeaderBar`)

## Why

The application's three chromeless secondary windows duplicate the same borderless-window
infrastructure, each in its own XAML and code-behind:

- the chromeless profile — `WindowStyle="None"`, a `WindowChrome` block (`CaptionHeight=0`,
  `CornerRadius=0`, `GlassFrameThickness=0`, `ResizeBorderThickness=6`,
  `UseAeroCaptionButtons=False`), `Background={DynamicResource ChromeBgBrush}`,
  `FontFamily={StaticResource Sizing.FontFamily.Text}` — three times:
  `SettingsWindow.xaml:14-30`, `WhatsNewWindow.xaml:9-27`, `BrowsePluginsDialog.xaml:11-27`;
- `SourceInitialized` wiring (DWM rounded corners + `HwndSource` hook) three times:
  `SettingsWindow.xaml.cs:52-57`, `WhatsNewWindow.xaml.cs:37-38` (corners only),
  `BrowsePluginsDialog.xaml.cs:28-33`;
- `WM_GETMINMAXINFO` handling twice, in two different shapes (`SettingsWindow.xaml.cs:66-93`
  min-track + work-area clamp; `BrowsePluginsDialog.xaml.cs:41-51` min-track only; WhatsNew has
  none);
- Esc-to-close (`OnKeyDown`) and header-drag (`OnHeaderMouseDown` + `DragMove` with the
  `InvalidOperationException` catch) three times each;
- the drawn header strip (title + right-aligned actions + close + separator hairline) in three
  visually-identical-but-hand-copied variants (`SettingsWindow.xaml:277-332`,
  `WhatsNewWindow.xaml:41-64`, `BrowsePluginsDialog.xaml:42-75`).

Adding a fourth chromeless window means copying all of this again, and the copies have already
drifted (WhatsNew lacks the min-size hook; Browse lacks the work-area clamp). `docs/architecture/ui.md:35-39`
documents the per-window recipe, so the duplication is also the documented way to build a window.

## What Changes

- Add `src/CLIHub/Views/ChromeWindow.cs` — an internal, C#-only (no XAML of its own) `Window`
  base class whose constructor sets the chromeless profile in code (`WindowStyle=None`,
  `WindowChrome` values above, `Background` via `SetResourceReference` to `ChromeBgBrush`,
  `FontFamily` via `SetResourceReference` to `Sizing.FontFamily.Text`), and which owns
  `OnSourceInitialized` (rounded corners + `HwndSource` hook), one unified `WM_GETMINMAXINFO`
  handler (min-track size + maximized work-area clamp), `OnKeyDown` Esc-to-close, and an
  `internal` header-drag helper (`DragMove` + catch).
- Add `src/CLIHub/Views/WindowHeaderBar.xaml(.cs)` — an internal reusable drawn-header
  `UserControl` with dependency properties `Title`, `IconSource` (null = text-only title),
  `Actions` (content slot for extra buttons), `ShowMaximize` (default false), `ShowSeparator`
  (default true), and routed events `CloseRequested` / `MaximizeRequested`. It builds on the
  existing `WindowHeader` / `WindowTitle` / `WindowCloseButton` styles
  (`Themes/Common/Controls.xaml:30-36, 79-82, 148-153`).
- Migrate the three windows to `<views:ChromeWindow>` + `views:WindowHeaderBar`. Window-specific
  sizing, icon, resources, content, and window-scoped style dictionaries stay in each window's
  XAML. Because `ChromeWindow` is internal, the three window classes become internal
  (`x:ClassModifier="NotPublic"` + `internal partial class`); all construction is already
  assembly-internal (DI + launchers).
- Add `tests/CLIHub.Tests/Views/ChromeWindowBaseMigrationTests.cs` — XAML text guards mirroring
  `BrowsePluginsDialogBindingTests` (windows rooted in `views:ChromeWindow`, header bar present,
  duplicated chrome attributes gone).
- Update the `docs/architecture/ui.md` "To add a window" recipe (`:35-39`) to describe the new
  pattern (root in `ChromeWindow`, use `WindowHeaderBar`, keep per-window style dictionary and
  sizing; LaunchWindow deliberately not on the base).

Pure refactor: no user-visible change is intended. Two documented micro-alignments come from the
unified `WM_GETMINMAXINFO` handler (WhatsNew gains min-track + clamp enforcement; Browse gains the
work-area clamp) — both no-ops in normal use and recorded in `design.md` D5.

## Capabilities

### New Capability: `chrome-window-base`

Documents the shared construction contract: what `ChromeWindow` guarantees for every derived
window (no OS chrome, WindowChrome metrics, themed background/font, rounded corners, min-track
size, maximized work-area clamp, Esc-to-close, header drag) and what `WindowHeaderBar` provides
(title, icon, action slot, optional maximize, close event, separator). Mirrors the existing
reusable-control capabilities `spaced-panel-layout` and `busy-marker-control`.

The existing specs that pin the three windows' chrome behavior — `settings-theme` ("Dark settings
window chrome") and `plugin-feed` ("Browse Plugins dialog drawn chrome", `:366-398`) — are
deliberately **not** modified: this change preserves their scenarios exactly and adds no behavior
to them.

## Impact

**New code (all in `src/CLIHub`, `CLIHub.Core` untouched):**

- `src/CLIHub/Views/ChromeWindow.cs` — internal base class (≈100 lines).
- `src/CLIHub/Views/WindowHeaderBar.xaml` + `.xaml.cs` — internal UserControl (≈90 + ≈80 lines).
- `tests/CLIHub.Tests/Views/ChromeWindowBaseMigrationTests.cs` — XAML text guards.

**Modified code:**

- `src/CLIHub/Views/SettingsWindow.xaml(.cs)` — root in `ChromeWindow`; header block
  (`:277-332`) replaced by `WindowHeaderBar` (`ShowMaximize` on, icon variant); code-behind drops
  `OnSourceInitialized`, `OnWindowMessage`, `OnHeaderMouseDown`, `OnKeyDown` and the chrome
  attributes; keeps `ShowSettings`, maximize/close handlers, VM lifecycle.
- `src/CLIHub/Views/WhatsNewWindow.xaml(.cs)` — same migration; Check/Install buttons move into
  the header's `Actions` slot verbatim; `ShowSeparator="False"` (WhatsNew has no header separator
  today — its hairline is a window-bottom line, `design.md` deviation 2); keeps its bottom
  hairline.
- `src/CLIHub/Views/BrowsePluginsDialog.xaml(.cs)` — same migration; its separate separator
  Rectangle (`:72-75`) and the compensating ScrollViewer top margin (`:79`) are removed because
  the header bar now carries the separator.
- `docs/architecture/ui.md` — "To add a window" recipe.
- The three windows become `internal` (`x:ClassModifier="NotPublic"`); every constructor call
  site is in-assembly (`Composition/ServiceRegistration.cs:172-193`,
  `Settings/SettingsLauncher.cs`, `Updates/ReleaseNotesLauncher.cs`,
  `Launching/PluginDialogService.cs:125`, `Diagnostics/ReadmeImageCapture.cs:85`) and tests never
  instantiate the windows (they read XAML text — `tests/CLIHub.Tests/Views/`).

**Unchanged:** `WindowChromeHelper` (still used by `ChromeWindow` and `LaunchWindow`), all theme
dictionaries and merge tree, `LaunchWindow`, DI registration shapes, no `.csproj` edit (WPF SDK
globs the new files).

**Verification:** build + full test suite after each stage, the new XAML guard tests, and a manual
smoke of all three windows plus the launch window (the visual no-change claim cannot be
unit-verified). Per repository policy, `git commit` / `openspec archive` wait for the user's smoke
confirmation and approval.

## Non-Goals

- **LaunchWindow is out of scope.** It is a topmost `SizeToContent` popup with `ResizeMode=NoResize`
  (`LaunchWindow.xaml:19-27`), no resize borders, no min-size hook, no drawn header drag, and Esc
  *hides* rather than closes (`LaunchWindow.xaml.cs:117-130`); it keeps its current structure and
  continues calling `WindowChromeHelper.ApplyRoundedCorners` directly.
- No visual restyle: every window's header must render the same pixels as today (two title
  variants and the separator differences are preserved via `IconSource` / `ShowSeparator`).
- No new chrome features (no minimize button, no maximize for WhatsNew/Browse, no theme changes).
- No XAML class hierarchy (`ChromeWindow` has no XAML; derived windows keep their own).
- No move of `EnsureOwnedByLaunchWindow` / `ShowAndActivate` into the base — show/ownership policy
  stays per-window (Browse is modal via `PluginDialogService`).
- No `.csproj`, DI, configuration, logging, or Core-project changes; no commits or archive in the
  tasks.

## Dependencies

- None on other active changes (`openspec list` shows only this change active). No file overlaps
  with concurrent work.
- Builds on the archived `2026-11-12-dialog-and-chrome-helpers` `WindowChromeHelper` (consumed,
  not modified).
- Blocked-by: nothing. Nothing else depends on this change.

## Risk

- **Accessibility ripple:** `internal ChromeWindow` forces the three windows internal; missed
  `x:ClassModifier="NotPublic"` fails the build (CS0262 accessibility mismatch), so the compiler
  catches it.
- **Pixel drift in the header:** mitigated by `IconSource` / `ShowSeparator` variants and the
  manual smoke.
- **Subtle window-message changes:** WhatsNew/Browse adopt the unified handler; guarded by the
  smoke steps that exercise minimum sizes and maximize (Settings) and by `plugin-feed`'s pinned
  520x360 floor.
- `WindowHeaderBar` must resolve styles at load time before being attached to a window — it uses
  only application-level keyed styles (same constraint as Settings pages, `ui.md:33`).
