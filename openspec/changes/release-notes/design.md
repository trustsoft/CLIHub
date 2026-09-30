# Design

## Context

See `proposal.md` — Why. Current state that shapes the approach:

- `CLIHub.Core` (`net8.0`, no WPF) already embeds files as resources: `SeedPlugins\**\*.json|*.png` in `CLIHub.Core.csproj`, read back via `assembly.GetManifestResourceStream` (`PluginSeeder`). `InternalsVisibleTo("CLIHub.Tests")` is already set.
- Application version comes from one place, `Directory.Build.props` (`<Version>0.5.0</Version>`), and is read at runtime through `IUpdateService.GetCurrentVersion()` (Velopack-aware, falls back to the assembly informational version).
- Preferences live in `AppPreferences` (`config.json`, camelCase) and are read/written through `IConfigService` (`Load()` returns a cached instance; `Save(config)` writes atomically).
- Settings is the existing precedent for a tray-opened window: `SettingsWindow` + `SettingsViewModel` + `ISettingsLauncher`/`SettingsLauncher`, owned by the launch window so it is visible above the always-on-top popup, and single-instance.
- Two palettes exist: `Themes/LaunchTheme.xaml` (dark, launch window) is merged application-wide but is palette-only; `SettingsWindow` uses its own inline light palette.

## Goals / Non-Goals

**Goals:**
- One source of truth for the user-facing notes (the repository file), consumed by the application without a copy step and without network access.
- Notes parsing that is testable without WPF and tolerant of hand-editing mistakes.
- The **What's New** window behaves like Settings (single instance, owned, reachable from the tray) while presenting the dark, self-drawn chrome of the launch window, and stays usable when the user resizes it.
- The one-time-after-upgrade rule is a pure, unit-testable decision.

**Non-Goals:**
- Generating either document from commits or from OpenSpec (decision: hand-written; revisit only if it becomes tedious).
- Showing an update-available note or release notes from Velopack's release feed; notes come from the embedded file.
- Adding an entry point from the launch window's Actions menu or the Settings window (tray + automatic display only).
- Localization; notes are English, like all UI text.
- Persisting the window's size between sessions (the window opens at its default size every time).
- CI wiring and `vpk pack --releaseNotes`.

## Decisions

### 1. The notes file stays at the repository root and is embedded by link

`RELEASE-NOTES.md` lives at the repository root (next to `CHANGELOG.md`) and is embedded from there:

```xml
<EmbeddedResource Include="$(MSBuildThisFileDirectory)..\..\RELEASE-NOTES.md"
                  Link="ReleaseNotes\RELEASE-NOTES.md"
                  LogicalName="CLIHub.Core.ReleaseNotes.RELEASE-NOTES.md" />
```

- **Why:** the plan's requirement is a root-level document for contributors, and the application must ship it. Embedding by link keeps a single copy; a duplicated file under `src/` would drift.
- **Alternatives considered:** a copy under `src/CLIHub.Core/` (drift risk, two files to keep in sync); copying at build time with a target (custom MSBuild machinery for no benefit).

### 2. Parsing lives in `CLIHub.Core` and takes text, not a file

`IReleaseNotesService` exposes the parsed notes; `ReleaseNotesService` loads the embedded resource by default and has an internal constructor accepting note text so tests can pass inline markdown.

- **Why:** parsing is pure logic and must be testable without WPF; `InternalsVisibleTo` already exists.
- **Alternatives considered:** parsing in the view model (untestable without WPF, and Core would not own the note contract).

### 3. Hand-written, line-based parser — no Markdown dependency

The parser recognizes `## <version> — <date>` (em dash or hyphen) and, within a section, `### New|Improved|Fixed` plus `-`/`*` list items. Anything else (title, preamble, prose, other subsections) is ignored. Unparseable version or date → the section is skipped and logged, the rest still parse.

- **Why:** the format is a small, fixed contract; a Markdown library would add a dependency and lenient behavior that hides authoring mistakes.
- **Alternatives considered:** Markdig (new dependency, parses more than the contract needs); JSON notes (not a pleasant file to hand-edit or review in a diff).

### 4. Note entries are plain text, authored without Markdown emphasis

The parser preserves entry text verbatim, and `RELEASE-NOTES.md` entries are written as plain sentences; the window renders each entry as plain text. Markdown emphasis is reserved for `CHANGELOG.md`, which is not rendered in the application.

- **Why:** rendering a Markdown subset in WPF means building `Inline` runs, which adds UI complexity for a document that is meant to be read as friendly prose.
- **Alternatives considered:** an inline-formatting pass for `**bold**`/`` `code` `` (deferred; can be added later without a spec change, since the parser already preserves the text).

### 5. Version ordering is semantic, comparison of the recorded version is simple equality

Notes are ordered with `System.Version` parsing, so `0.10.0` sorts above `0.9.0`; a note whose version does not parse keeps its document position at the end of the ordering rather than breaking the sort.

The one-time display rule compares strings: shown when the running version differs from the recorded one. `AppPreferences.LastSeenReleaseNotesVersion` (nullable) is recorded after the window is shown — automatically or manually — and a missing value means "first run".

- **Why:** "differ" covers upgrades and downgrades without a semantic-comparison edge case, and matches the spec. Version parsing is only needed where the user actually reads an order.
- **Alternatives considered:** semantic comparison for the gate (more code, and a prerelease or `unknown` version would need a fallback path anyway).

### 6. The gate is a separate, pure decision

A small Core type decides the outcome for a start-up: `Show` when the running version differs from the recorded one and notes exist for it, `RecordOnly` when the version differs but no notes exist or nothing has been recorded yet (a first run remembers the shipped version without announcing it, so the next upgrade is still announced), and `Skip` when the versions are equal or the running version is unknown.

- **Why:** the "once per upgrade" behavior is the part most worth testing and the hardest to exercise through the UI.
- **Alternatives considered:** logic inline in `App.OnStartup` (not unit-testable).

### 7. The window reuses the Settings lifecycle and draws its own dark chrome

**Lifecycle** is the Settings pattern: `WhatsNewWindow` + `WhatsNewViewModel` + `IReleaseNotesLauncher`/`ReleaseNotesLauncher` — single instance, owned by the launch window so it stays visible above the always-on-top popup, re-activation brings the existing window to the front (`Show()` + `Activate()`, as `SettingsLauncher` does).

**Presentation** is the launch window's technique with the dark palette: `WindowStyle="None"`, `SourceInitialized` → `DwmApi.TryRoundCorners`, a 1px `FieldStrokeBrush` border with a 1px inset top highlight, colours from `Themes/LaunchTheme.xaml` (`ChromeBgBrush` surface, `TextPrimaryBrush`/`TextMutedBrush`, `SectionLabelBrush` for group labels, `RowDividerBrush`/`HairlineBrush` dividers, `AccentBrush` for the version), and a window-scoped `Themes/WhatsNewStyles.xaml` for only the styles this window needs (header, group label, entry rows). `LaunchWindowStyles.xaml` stays window-scoped and is not copied.

The header is drawn in XAML — the title plus a close (×) button — and doubles as the drag area (`MouseLeftButtonDown` → `DragMove()`); Escape closes the window. The body is a scrollable notes view with a capped, resizable height (decision 8); notes render inside a `ScrollViewer` as an `ItemsControl` of version sections, and empty groups are collapsed.

Deliberately **not** inherited from the popup shell: no `Topmost`, no hide-on-focus-loss, no pin, no pointer-monitor placement — it uses `WindowStartupLocation="CenterScreen"` and stays open while the user works.

- **Why:** the project already ships this exact technique in the launch window (chromeless dark shell with DWM-rounded corners), so it is reuse rather than new machinery; and a dark content area under the light OS title bar is a mismatch that would otherwise need an interop patch, whereas a drawn header makes the window look intentional.
- **Alternatives considered:** OS chrome plus `DWMWA_USE_IMMERSIVE_DARK_MODE` (one interop call and it keeps native Snap Layouts/resize/System menu, but the result depends on OS support and still mixes a native frame with a custom look); the light Settings palette (contradicts the dark intent recorded in `ui/mockups/README.md`); a modal dialog (blocks the launcher and cannot stay open while the user works).
- **Fallback:** if drawing the chrome turns out to be the wrong trade-off (missing native window affordances, or drag/accessibility problems), switch this one window to OS chrome plus immersive dark mode. That is contained to `WhatsNewWindow` and its styles, and changes no spec.

### 8. Resizing uses the native frame; no drawn resize borders

The chrome is drawn, but resizing is not: the window keeps `ResizeMode="CanResize"` with `WindowStyle="None"`, which retains `WS_THICKFRAME` so the OS owns the resize hit zone — DPI-correct, with the standard resize cursors and the native edge snapping — instead of us hit-testing pixels. The layout is explicit rather than content-sized: a default size (about 520×620), `MinWidth`/`MinHeight` so the header and one note always fit, no `SizeToContent`, and a `Grid` of a header row (`Auto`) over a notes `ScrollViewer` (`*`).

- **Why:** the drawn 1px border sits inside the client area and cannot itself be the grab zone, so a hit zone has to come from somewhere; delegating it to the frame is DPI-correct, behaves like every other window the user resizes, and costs no interop.
- **Alternatives considered:** hit-testing manually in a `WM_NCHITTEST` hook — available in this codebase (`GlobalHotkeyService` already hooks `HwndSource`, and `Interop/User32.cs` already declares source-generated P/Invokes), but it means reimplementing hot zones, resize cursors, DPI scaling, and edge snapping; `WindowChrome` with `CaptionHeight=0`/`GlassFrameThickness=0` — less code than a hook, but it reintroduces a native caption/frame model that competes with the drawn border and the DWM rounding the launch window established.
- **Fallback:** if `ResizeMode="CanResize"` turns out to leave a grab zone that is too thin or that swallows clicks meant for the notes area, add the `WM_NCHITTEST` hook with an explicit DPI-scaled hot zone at the edges and corners and re-verify. The layout described here does not change; `WindowChrome` is the last resort.

### 9. Startup wiring reuses `IUpdateService.GetCurrentVersion()`

`App.OnStartup`, after the tray exists, evaluates the gate with the current version from `IUpdateService`, shows the window on the dispatcher when required, and persists the recorded version through `IConfigService`. Persistence failures are logged and non-fatal.

- **Why:** one authoritative version source already used by the footer version chip and the update check.

### 10. Documentation updates are part of the change

`docs/architecture.md` (window inventory, the drawn dark chrome, config preference, resource embedding), `docs/repo-structure.md` (the two root files, `Themes/WhatsNewStyles.xaml`), `docs/changelog-and-release-notes.md` (record the decisions in the decision log and mark the plan implemented), `ui/mockups/README.md` (the dark palette no longer applies to the launch window only), and `README.md` (the tray entry and autostart disclosure) are updated in the same change.

- **Why:** the repository treats these documents as the single source of truth per topic; a new window and preference that are undocumented would contradict `AGENTS.md`.

## Risks / Trade-offs

- **Embedded resource outside the project directory may not resolve on some SDK/MSBuild combinations** → the packaging task verifies the resource is present in the built assembly, and a unit test asserts the shipped resource parses into at least the current version's note, so a broken link fails the build or the test run rather than shipping silently.
- **Notes drifting from the shipped version** (a version bump without a notes section) → a unit test asserts a note exists for the version the assembly was built with; the check makes the omission a test failure.
- **Two hand-written documents can fall out of sync** → accepted trade-off of the hand-written decision, recorded in `docs/changelog-and-release-notes.md`; both use the same version heading so a release lines up.
- **Plain-text entries cannot express emphasis** → documented authoring rule; a Markdown-subset renderer can be added later without changing the specs, because the parser preserves the raw text.
- **Existing users see nothing after upgrading to the version that introduces this feature** (no recorded version means "first run") → intentional: showing a popup for an arbitrary past release is more surprising than silence; the tray entry is available immediately, and the version is recorded so the *next* upgrade is announced.
- **Config write timing** — recording the version mutates the cached `AppConfig` and saves it; a save failure leaves the value unrecorded, so the window may appear again on the next start. Logged, non-fatal, and self-healing.
- **A drawn chrome loses some native window affordances** (Win11 Snap Layouts from a maximise button, double-click-to-maximise, the System menu, and the OS close button) → the window keeps the native resize frame (decision 8), so dragging an edge, the resize cursors, and the Aero Snap keyboard shortcuts work; it is not maximisable by design (a header drag moves the window) and carries its own close button. The immersive-dark-mode fallback above remains a change to this one window if the trade-off proves wrong.
- **`ResizeMode="CanResize"` with `WindowStyle="None"` may yield a thin or missing grab zone, or a native edge that fights the drawn 1px border** → confirmed by hand when the window lands; the `WM_NCHITTEST` hot zone in decision 8 is the contingency, and neither outcome changes the layout, the specs, or the task breakdown.
- **Drawn chrome can be missed by keyboard and screen-reader users** (no OS title bar to perceive as the window's identity) → the header carries the visible title, Escape closes the window, and the close control is a real button reachable by Tab.
- **Very long notes history** costs a little startup work: parsing is on demand (the window and the gate parse when they need it), so start-up cost is one small embedded file read.

## Migration Plan

No data migration. `lastSeenReleaseNotesVersion` is absent in existing `config.json` files and deserializes as `null`, which the gate treats as "first run" (no automatic window). Rollback is deleting the preference and the files; nothing structural depends on the change. The documents are additive; the version in `Directory.Build.props` is unchanged (the seed section matches `0.5.0`).
