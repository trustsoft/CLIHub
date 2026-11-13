# Design: Shared chromeless-window base (`ChromeWindow` + `WindowHeaderBar`)

All `file:line` references verified at HEAD `422789f1` (clean tree). This document supersedes the
earlier draft artifacts found untracked in this directory (its `specs/settings-theme` delta
modified a spec whose behavior does not change — see D8).

## Verified Current State

### The chromeless profile, duplicated three times

| Aspect | SettingsWindow | WhatsNewWindow | BrowsePluginsDialog |
|---|---|---|---|
| `Background` ChromeBgBrush | `.xaml:14` | `.xaml:9` | `.xaml:11` |
| `FontFamily` Sizing.FontFamily.Text | `.xaml:15` | `.xaml:10` | `.xaml:12` |
| `KeyDown="OnKeyDown"` | `.xaml:18` | `.xaml:13` | `.xaml:15` |
| `SourceInitialized` wiring | `.xaml:21` | `.xaml:16` | `.xaml:18` |
| `WindowStyle="None"` | `.xaml:23` | `.xaml:18` | `.xaml:20` |
| `WindowChrome` block (CaptionHeight=0, CornerRadius=0, GlassFrameThickness=0, ResizeBorderThickness=6, UseAeroCaptionButtons=False) | `.xaml:24-30` | `.xaml:19-27` | `.xaml:21-27` |
| `ResizeMode="CanResize"` | `.xaml:19` | `.xaml:14` | `.xaml:16` |
| Size / min size | 790x612, min 640x480 (`.xaml:16-17`) | 520x620, min 380x280 (`.xaml:11-12`) | 640x480, min 520x360 (`.xaml:13-14`) |
| `ShowInTaskbar` | True (`.xaml:20`) | True (`.xaml:15`) | False (`.xaml:17`) |
| Startup location | CenterScreen (`.xaml:22`) | CenterScreen (`.xaml:17`) | CenterOwner (`.xaml:19`) |

### Code-behind duplication

- `SourceInitialized`: `SettingsWindow.xaml.cs:52-57` and `BrowsePluginsDialog.xaml.cs:28-33` both
  do `WindowChromeHelper.ApplyRoundedCorners(this)` + `EnsureHandle` + `HwndSource.FromHwnd(...)?.AddHook(...)`;
  `WhatsNewWindow.xaml.cs:37-38` does rounded corners only.
- `WM_GETMINMAXINFO`: `SettingsWindow.xaml.cs:66-93` handles min-track **and** the maximized
  work-area clamp (monitor lookup `:73-83`, `ptMaxPosition`/`ptMaxSize` `:85-89`,
  `ApplyMinTrackSize` `:90`); `BrowsePluginsDialog.xaml.cs:41-51` handles min-track only;
  WhatsNew has no hook at all.
- Esc-to-close: `SettingsWindow.xaml.cs:121-130`, `WhatsNewWindow.xaml.cs:79-88`,
  `BrowsePluginsDialog.xaml.cs:79-88` — byte-identical bodies.
- Header drag: `SettingsWindow.xaml.cs:99-114`, `WhatsNewWindow.xaml.cs:44-59`,
  `BrowsePluginsDialog.xaml.cs:57-72` — byte-identical (left-button guard + `DragMove` +
  `catch (InvalidOperationException)`).
- Per-window keepers (not duplicated, stay put): `ShowSettings` with launch-height adoption
  (`SettingsWindow.xaml.cs:38-50`), `ShowNotes` (`WhatsNewWindow.xaml.cs:31-35`), the modal show
  in `PluginDialogService.ShowBrowsePluginsDialog` (`Launching/PluginDialogService.cs:123-128`),
  `OnMaximizeRestoreClick` (`SettingsWindow.xaml.cs:116-117`), WhatsNew's Check/Install handlers
  (`WhatsNewWindow.xaml.cs:63-77`).

### The drawn header, three hand-copied variants

- **Settings** (`SettingsWindow.xaml:277-332`): `WindowHeader` Grid; left = 16px `app.ico` image +
  "CLIHub - Settings" `FontSize=13 FontWeight=Medium` in a StackPanel `Margin="12,0,0,0"`
  (`:280-294`); right = maximize/restore button whose style, tooltip, and Maximize/Restore glyph
  flip on `WindowState` (`:297-320`) + close button (`:321-325`); separator hairline
  bottom-aligned in the header row (`:329-332`). A 1px top highlight sits above (`:274`).
- **WhatsNew** (`WhatsNewWindow.xaml:41-64`): `WindowHeader` Grid; left = `WindowTitle`-styled
  "What's New" (`:44`; style = Margin 14 / FontSize 15 / SemiBold, `Controls.xaml:30-36`); right =
  Check + Install buttons (`NotesInstallButton` style, `:46-57`) + close (`:58-62`). **No header
  separator.** The hairline at `:66-69` is `Grid.Row="1"` (the `*` content row)
  `VerticalAlignment="Bottom"` — a *window-bottom* line, not a header separator.
- **Browse** (`BrowsePluginsDialog.xaml:42-75`): Settings' shape — icon + "Browse Plugins"
  13/Medium title (`:45-59`), close only (`:61-69`) — plus its separator as a separate Rectangle
  at the *top* of the content row (`:72-75`) with a compensating `Margin="0,1,0,0"` on the
  ScrollViewer (`:79`).

Shared keyed styles: `WindowHeader` (`Controls.xaml:79-82`, height from
`Sizing.WindowHeader.Height` = 40, `Sizing.xaml:71`), `WindowTitle` (`Controls.xaml:30-36`),
`WindowCloseButton` (`Controls.xaml:148-153`, based on `IconChipButton` `:119-145`).

### Supporting facts

- `WindowChromeHelper` (`src/CLIHub/Views/WindowChromeHelper.cs`): `EnsureOwnedByLaunchWindow`
  `:22-30`, `ShowAndActivate` `:37-47`, `ApplyRoundedCorners` `:53-57`, `ApplyMinTrackSize`
  `:69-78` (per-window-DPI scale + `ptMinTrackSize`). All four remain in use after this change
  (`ChromeWindow` uses 2 + 4; LaunchWindow uses 3; Settings/WhatsNew show paths use 1 + 2; Browse's
  owner is set by `PluginDialogService`).
- `User32` (`src/CLIHub/Interop/User32.cs`): `WM_GETMINMAXINFO` `:18`,
  `MONITOR_DEFAULTTONEAREST` `:23`, `MonitorFromWindow` `:55`, `GetDpiForWindow` `:61`,
  `GetMonitorInfo` `:68`, `MONITORINFO` `:101`, `MINMAXINFO` `:113`. `DwmApi.TryRoundCorners` via
  the helper.
- `LaunchWindow` exclusion confirmed: `ResizeMode="NoResize"` (`.xaml:19`),
  `ShowInTaskbar="False"` `:20`, `SizeToContent="WidthAndHeight"` `:21`, `Topmost="True"` `:23`,
  `WindowStyle="None"` `:27`; `OnSourceInitialized` = rounded corners only (`.xaml.cs:101-102`);
  Esc hides the window after closing an open menu (`.xaml.cs:117-130`). It is a different render
  and lifetime profile, not a member of this family.
- Construction is entirely assembly-internal: `Composition/ServiceRegistration.cs:172,176,192,193`
  (DI), `Settings/SettingsLauncher.cs:11-18`, `Updates/ReleaseNotesLauncher.cs:16-29`,
  `Launching/PluginDialogService.cs:125`, `Diagnostics/ReadmeImageCapture.cs:85`. Tests never
  instantiate the windows — `tests/CLIHub.Tests/Views/` asserts on XAML file text
  (`BrowsePluginsDialogBindingTests.cs:17-54`).
- Architecture gate: files under `Views/` may reference WPF freely
  (`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:159`), so `ChromeWindow.cs` and
  `WindowHeaderBar.xaml.cs` need no allow-list entry.
- `docs/architecture/ui.md:35-39` is the "To add a window" recipe this change rewrites;
  `ui.md:33` explains why Settings pages (UserControls) must not rely on window-scoped resources —
  the same constraint applies to `WindowHeaderBar`.
- Pinned behavior contracts: `settings-theme` "Dark settings window chrome"
  (`openspec/specs/settings-theme/spec.md:9-38`, maximize + work-area + Esc scenarios) and
  `plugin-feed` "Browse Plugins dialog drawn chrome" (`openspec/specs/plugin-feed/spec.md:366-398`,
  header drag, 6px resize borders, 520x360 minimum, rounded corners, Esc, modal, no taskbar).

## Decisions

### D1. C#-only base class, no XAML hierarchy (per brief; confirmed)

`ChromeWindow` is a pure C# `internal abstract class ChromeWindow : Window` with a protected
parameterless constructor that sets, in code: `WindowStyle = None`;
`WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, CornerRadius = new
CornerRadius(0), GlassFrameThickness = new Thickness(0), ResizeBorderThickness = new Thickness(6),
UseAeroCaptionButtons = false })`; `SetResourceReference(BackgroundProperty, "ChromeBgBrush")`;
`SetResourceReference(FontFamilyProperty, "Sizing.FontFamily.Text")`. Derived windows change their
XAML root to `<views:ChromeWindow x:Class="...">` and keep their own XAML files — no `ChromeWindow.xaml`,
no generic.xaml. Rationale: a XAML base class drags `InitializeComponent` ordering and
`StaticResource`-lookup pitfalls into every derived window; a code-only base is additive and keeps
each window's resources self-contained. `SetResourceReference` is used instead of a static read of
the font because the keys resolve at runtime from the application-level tree
(`Controls.xaml` → `Sizing.xaml`, `ui.md:14-31`), exactly as the current `DynamicResource`/`StaticResource`
attributes do.

### D2. `ChromeWindow` is internal → the three windows become internal (accessibility ripple)

A public class cannot derive from an internal base (CS0060), and the brief asks for `internal`.
The migration therefore changes each window to `internal partial class ... : ChromeWindow` **and**
adds `x:ClassModifier="NotPublic"` to its XAML (the XAML compiler generates the other partial with
`public` unless told otherwise; mismatched accessibility across partials is CS0262 — the build
fails loudly if a task misses it). This is safe because every constructor call is inside `CLIHub`
(Verified Current State) and tests read XAML text rather than instantiate. Alternative — make
`ChromeWindow` public with XML docs and keep the windows public — rejected: nothing outside the
assembly needs these types, and `WindowChromeHelper` set the internal-UI-helper precedent. The
public XML-doc rule does not apply to internal types, but the class still gets full XML docs.

### D3. `WindowHeaderBar` is a UserControl, not a templated control

The header is one fixed composition (drag surface + title + action slot + optional maximize +
close + optional separator) with parameters, not a skinnable control; no consumer needs to replace
its template. A `Control` subclass with a default style in a themes dictionary would add the
template-resolution machinery the repo's other templated controls (`SpacedPanel`, `BusyMarker`,
`NumericField`, `SummaryCard`) genuinely use, without a corresponding need here. A UserControl
also lets the header be written in XAML against the existing keyed styles, mirroring how Settings
pages are built. Internal, `src/CLIHub/Views/WindowHeaderBar.xaml(.cs)`, next to `ChromeWindow`
(it is chrome for that base, not a general-purpose `CLIHub.Controls` control).

Dependency properties (all with change-notification only where needed; defaults in parentheses):

- `Title` (string, empty) — the header title text.
- `IconSource` (ImageSource, null) — when non-null, renders the 16px icon + compact 13/Medium
  title in a `Margin="12,0,0,0"` StackPanel (the Settings/Browse variant); when null, renders the
  `WindowTitle`-styled title (the WhatsNew variant). Mode selection via `x:Null` DataTrigger — no
  inverse converter needed.
- `Actions` (object, null) — `ContentPresenter` content rendered before the maximize/close
  buttons in the right-aligned panel; WhatsNew hosts its Check/Install buttons here (their `Click`
  handlers resolve against the declaring window's partial class, since the buttons are declared in
  the window's XAML).
- `ShowMaximize` (bool, false) — shows the maximize/restore button verbatim from
  `SettingsWindow.xaml:297-320` (tooltip + glyph flip on ancestor `WindowState`). Only Settings
  sets it.
- `ShowSeparator` (bool, true) — the bottom hairline. WhatsNew sets `ShowSeparator="False"`
  (D6).

Routed events (bubbling): `CloseRequested`, `MaximizeRequested` — raised by the close/maximize
buttons; the window handles them in XAML (`CloseRequested="OnCloseClick"`,
`MaximizeRequested="OnMaximizeRestoreClick"`), keeping lifecycle decisions in the window. Named
"…Requested" (not `Close`/`Closed`) to avoid confusion with `Window.Closed` and .NET
event-naming conventions (refines the brief's "close raises Close").

Resource discipline: the control references only application-level keyed styles
(`WindowHeader`, `WindowTitle`, `WindowCloseButton`, `IconGlyphs`, `Sizing.*`) and palette brushes
via `DynamicResource` — never window-scoped styles. A UserControl's XAML loads before the control
attaches to the window tree, the same constraint that forced `SettingsStyles.xaml` to application
level (`ui.md:33`); the needed styles are already application-level (`ui.md:14-31`). No new keyed
styles are added to `Controls.xaml`; the merge tree is untouched.

### D4. Base owns `SourceInitialized`, the unified `WM_GETMINMAXINFO` handler, and Esc — via overrides, not event wiring

- `protected override void OnSourceInitialized(EventArgs e)`: `base.OnSourceInitialized(e);` then
  `WindowChromeHelper.ApplyRoundedCorners(this);` then `HwndSource.FromHwnd(new
  WindowInteropHelper(this).EnsureHandle())?.AddHook(this.OnWindowMessage);` — replacing the
  XAML `SourceInitialized` attributes and the three private handlers.
- `private IntPtr OnWindowMessage(...)`: the **Settings shape, verbatim** (`SettingsWindow.xaml.cs:66-93`):
  `WM_GETMINMAXINFO` guard; `MonitorFromWindow` + `GetMonitorInfo` with the two early returns;
  `ptMaxPosition`/`ptMaxSize` work-area clamp; `WindowChromeHelper.ApplyMinTrackSize(this, hwnd,
  lParam)`; `handled = true`. One handler for all three windows instead of today's two-and-a-half
  variants.
- `protected override void OnKeyDown(KeyEventArgs e)`: `base.OnKeyDown(e);` then `if (!e.Handled
  && e.Key == Key.Escape) { this.Close(); e.Handled = true; }`. The `base` call first preserves
  the current ordering (inner handlers run before the window sees the event; an already-handled
  Escape is left alone — e.g. an open Actions menu in a future consumer).

The XAML `KeyDown` / `SourceInitialized` attributes disappear from all three windows. `ResizeMode`,
`ShowInTaskbar`, `WindowStartupLocation`, sizes, `Title`, and `Icon` stay per-window: they are
genuine per-window policy (Browse is `CenterOwner` + no taskbar; a future chromeless window may be
`NoResize`), not shared chrome.

### D5. Unified `WM_GETMINMAXINFO` semantics — the two accepted micro-alignments

Today only Settings has the full handler; Browse enforces min-track without the work-area clamp;
WhatsNew relies on WPF's default handling of its declared `MinWidth=380`/`MinHeight=280`. With the
base:

- **WhatsNew gains min-track + work-area clamp.** In normal use nothing changes: it has no
  maximize affordance, and its declared minimums (`.xaml:12`) are now enforced through the same
  per-window-DPI path Browse already uses, matching the values the XAML has always declared.
- **Browse gains the work-area clamp** on top of its existing min-track. It is modal with no
  maximize affordance, so the clamp only matters if the user maximizes it by system means (e.g.
  Win+Up), where clamping to the work area is strictly more correct than covering the taskbar.
  `plugin-feed`'s pinned scenarios (520x360 floor, modal, drag, Esc, rounded corners) are
  untouched.

Both are recorded as deliberate alignments rather than silent drift; `settings-theme` and
`plugin-feed` requirements keep their exact texts. If the user rejects the WhatsNew alignment, the
fallback is a virtual `OnMinMaxInfo` seam — not planned (open question 1).

### D6. Pixel-exact header preservation: `IconSource` and `ShowSeparator`

The three headers are not visually identical today, and a non-goal is restyling. The two real
differences become explicit options: title variant (icon + 13/Medium/12-margin vs `WindowTitle`
15/SemiBold/14-margin) selected by `IconSource`; separator presence selected by `ShowSeparator`.
WhatsNew additionally keeps its window-bottom hairline (`WhatsNewWindow.xaml:66-69`) exactly where
it is — it is not a header separator (see Deviation 2). Browse's standalone separator Rectangle
(`:72-75`) and the ScrollViewer's compensating `Margin="0,1,0,0"` (`:79`) are deleted; the header
bar's built-in separator renders at the same pixel line (bottom edge of the 40px header), and the
ScrollViewer margin is removed so content starts at the same offset.

### D7. Drag helper is `internal`, close helper is dropped

`ChromeWindow.BeginHeaderDrag(MouseButtonEventArgs)` wraps the `DragMove` + `catch
(InvalidOperationException)` body and is called by `WindowHeaderBar`'s header
`MouseLeftButtonDown` handler after the left-button guard (`Window.GetWindow(this) is ChromeWindow
c` — the control and the base are an internal pair in the same assembly). It cannot be `protected`
because `WindowHeaderBar` is not a derived class (brief said "protected helpers"; deviation 3).
A separate protected close helper is dropped as redundant: the close button raises
`CloseRequested`, and the window's one-line handler already calls `this.Close()` — a base indirection
would add nothing.

### D8. Spec strategy: one new capability, no MODIFIED requirements

The change creates a durable construction capability (like `spaced-panel-layout` and
`busy-marker-control`) without changing any pinned behavior:

- **New `chrome-window-base`** (ADDED requirements): the base's guarantees (profile, rounded
  corners + hook, min-track + clamp, Esc, drag) and the header control's API/behavior
  (title/icon/actions/maximize/separator, `CloseRequested`/`MaximizeRequested`), plus the
  adoption requirement naming the three windows and the LaunchWindow exclusion.
- **`settings-theme` and `plugin-feed` are not modified**: every scenario they pin (maximize,
  work-area fit, Esc, drag, 520x360 minimum, modality, no taskbar entry) is preserved verbatim, so
  there is no requirement-text change to record. The earlier draft in this directory modified
  `settings-theme` with an implementation paragraph — wrong tool: that spec describes visible
  Settings behavior, which does not change, and OpenSpec deltas exist to record behavior changes,
  not implementation moves.
- Not `skip_specs: true`: unlike the archived `dialog-and-chrome-helpers` consolidation (literal
  code moves behind unchanged call shapes), this change introduces two new named, reusable types
  and rewrites the documented way windows are constructed — a contract worth pinning, and the
  parent brief explicitly prefers a real delta.

### D9. Verification: XAML text guards + manual smoke

A new `tests/CLIHub.Tests/Views/ChromeWindowBaseMigrationTests.cs` (naming per
`MethodOrScenario_Condition_ExpectedResult`) reads the three window XAML files and
`WindowHeaderBar.xaml` as text, mirroring `BrowsePluginsDialogBindingTests`: each window is rooted
in `views:ChromeWindow` with `x:ClassModifier="NotPublic"` and declares `views:WindowHeaderBar`
with its expected `Title`; the duplicated chrome markers
(`WindowChrome.WindowChrome`, `WindowStyle="None"`, `KeyDown=`, `SourceInitialized=`,
`Background="{DynamicResource ChromeBgBrush}"`, `FontFamily="{StaticResource Sizing.FontFamily.Text}"`)
are gone from the three windows; the header bar uses the shared keyed styles. This guards against
reintroducing the duplication and against a future window silently skipping the base. Visual
preservation cannot be unit-tested: the manual smoke (Settings maximize/min-size/Esc, WhatsNew
actions + drag + no separator, Browse modal/min-size/Esc, LaunchWindow Esc-hides) is the visual
gate and, per repository policy, commit/archive wait for the user's smoke confirmation.

## Alternatives Considered

1. **Status quo + more helpers** (extend `WindowChromeHelper` with `ApplyChromeSettings(window)`
   etc.): leaves every window wiring its own events, keeps three XAML chrome blocks, and cannot
   own `SourceInitialized`/hook lifetime — the drift (WhatsNew missing the hook) stays possible.
   Rejected; the helper consolidation (archived `2026-11-12-dialog-and-chrome-helpers`) already
   harvested the stateless parts.
2. **Attached behavior** (`ChromeBehavior.Enabled="True"` styling `WindowChrome` + hooking via
   `SourceInitialized` event subscription): keeps the XAML chrome partially declarative, but still
   needs per-window event wiring for Esc/drag, hides object lifetime behind an attached property,
   and is harder to trace than a base class. Rejected as overkill for an assembly-internal family
   of exactly-this-shape windows.
3. **XAML class hierarchy** (`ChromeWindow.xaml` with `InitializeComponent` in the base):
   described in D1 — resource-merge and initialization-order pitfalls; also fights the existing
   per-window resource dictionaries. Rejected (brief concurs).
4. **Public `ChromeWindow`, public windows** (avoid the D2 ripple): rejected in D2 — broader API
   surface with zero external consumers.
5. **Header as a plain `ContentPresenter` + per-window styles** (no new control): leaves the
   button wiring and drag hookup duplicated per window — the half of the duplication this change
   exists to remove. Rejected.
6. **Unify the three headers visually** (one title style, separator everywhere): rejected —
   restyling is an explicit non-goal; see D6.

## Deviations from Brief

1. **Hairline in the header control.** The brief described the extracted header as "title +
   action slot + close button + hairline separator, per WhatsNewWindow.xaml lines 33-69". Verified
   against the code: lines 66-69 render a `Grid.Row="1"` (the star-sized content row)
   `VerticalAlignment="Bottom"` Rectangle — a *window-bottom* hairline, not a header separator.
   WhatsNew has no separator under its header at all; Settings (`:329-332`) and Browse (`:72-75`)
   do. `WindowHeaderBar` therefore carries a `ShowSeparator` option (default true; WhatsNew sets
   false), and WhatsNew's bottom-of-window hairline stays in its own XAML.
2. **Drag helper accessibility.** Brief: "protected helpers for header drag (DragMove) and close".
   `BeginHeaderDrag` is `internal` because `WindowHeaderBar` (not a derived class) must call it;
   the close helper is dropped as redundant (D7). Esc-close lives in the `OnKeyDown` override.
3. **Event naming.** Brief: "close raises Close". Implemented as routed events
   `CloseRequested`/`MaximizeRequested` — an event named `Close` on a control inside a `Window`
   invites confusion with `Window.Close`/`Closed`.
4. **`x:ClassModifier` ripple** is an addition the brief did not mention: making `ChromeWindow`
   internal forces the three window classes internal (D2). Mechanically small (one attribute per
   XAML + `internal partial`), caught by the compiler if missed.

## Open Questions

1. **Accept the two micro-alignments in D5?** (WhatsNew gains min-track + work-area clamp; Browse
   gains the clamp.) Recommended: yes — both match each window's declared intent and pinned specs
   are unaffected. If rejected, the fallback is a virtual min-max seam the windows can override.
2. **Accept the internal-accessibility ripple (D2)?** Recommended: yes (brief asked for internal;
   all usage is assembly-internal). The alternative is a public `ChromeWindow` with the windows
   staying public — one word in the class declaration if the user prefers that.
3. **WhatsNew's window-bottom hairline** (`WhatsNewWindow.xaml:66-69`) looks like it may have
   *been intended* as a header separator (it is bottom-aligned in the content row). This change
   preserves it exactly (non-goal: restyling). Should a follow-up move it under the header?
4. **Guard-test appetite (D9):** the XAML text guards are written as a task; if the user prefers
   manual-smoke-only verification, stage 5 can drop the test file without affecting the rest.
