## Why

Busy labels with trailing ellipses ("Detecting…", "Checking…", "Downloading…") are static text. When a user reads one of these labels, they have no way to confirm that work is actually progressing; the label looks identical whether the operation is running, stalled, or has been waiting for seconds. An animated glyph marker provides immediate visual feedback that work is in progress, reduces ambiguity, and follows the braille-spinner pattern already referenced in the project's HTML design mockups.

## What Changes

- Add a new WPF custom control `BusyMarker` in `src/CLIHub/Controls/BusyMarker.cs` with a default style/template in the Common theme dictionary
- Extract UI-independent logic into `CLIHub.Core` as `BusyGlyphSequence` (frame iteration, speed clamping, reduced-motion static glyph choice)
- The control exposes `IsBusy` (bool), `Speed` (int, milliseconds per frame, default 80, clamped), and `MarkerTone` (enum: Busy / Success / Failure) — Busy shows the animated braille glyph, Success/Failure show static glyphs "✓"/"✗" with theme tone brushes
- The marker uses a fixed-width glyph slot (layout-stable cycling), honors `SystemParameters.ClientAreaAnimation` for reduced motion (static full-cell glyph "⣿" instead of cycling), and marks the glyph as decorative for accessibility (adjacent text carries meaning)
- Timer runs only while `IsBusy` is true AND the control is visible
- Adopt the marker on exactly two surfaces:
  - Agent card state chip: replace `AgentVersionText.Detecting` constant from `"Detecting…"` to `"Detecting"`, render the marker inline with the chip text while a probe runs
  - Update control button: replace `"Checking…"`, `"Downloading…"`, `"Downloading {v}…"` strings with marker + label (no ellipsis) — the marker composes inside the button's content template

## Capabilities

### New Capabilities

- `busy-marker-control`: defines the `BusyMarker` WPF control, its API, behavior (animation lifecycle, reduced-motion handling, accessibility), and testable Core sequence type

### Modified Capabilities

- `agent-ui`: adds a requirement that the agent card state chip shows an animated braille marker inline with the "Detecting" text while a probe runs (the chip text no longer has a trailing ellipsis), preserving the original scenario names
- `update-checking`: adds a requirement that the update control button label shows an animated braille marker inline with the checking/downloading text while those operations run (the button text no longer has a trailing ellipsis), preserving the original scenario names

## Impact

**New code:**
- `src/CLIHub.Core/UI/BusyGlyphSequence.cs` — UI-independent frame sequence type: the braille frames constant, speed clamping, reduced-motion static glyph, frame iteration
- `src/CLIHub/Controls/BusyMarker.cs` — WPF control: `IsBusy`, `Speed`, `MarkerTone`, `DefaultStyleKeyProperty.OverrideMetadata`, timer lifecycle (start/stop on IsBusy and visibility changes), tick handler calls Core sequence for next frame
- `src/CLIHub/Themes/Common/Controls.xaml` (or a new dictionary if warranted by existing conventions) — default style/template for `BusyMarker`: fixed-width TextBlock slot, tone-based brush bindings, template triggers
- `tests/CLIHub.Core.Tests/UI/BusyGlyphSequenceTests.cs` — unit tests for the Core sequence type following the `MethodOrScenario_Condition_ExpectedResult` naming convention

**Modified code:**
- `src/CLIHub/ViewModels/Agents/AgentVersionText.cs` line 13 — change `Detecting` constant from `"Detecting…"` to `"Detecting"` (no trailing ellipsis)
- `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs` lines 76-79 — remove trailing ellipses from `"Checking…"`, `"Downloading…"`, `"Downloading {v}…"` button text strings
- Agent card XAML (to be located — AgentCardItem binds to VersionChipText, consumed by Settings agents surfaces) — add `BusyMarker` inline with chip text, bind `IsBusy` to a new ViewModel property tracking whether a probe is running for that card
- Update control XAML (to be located — UpdateControlViewModel.UpdateButtonText is rendered on launch window and Settings) — compose `BusyMarker` + TextBlock inside button Content, bind `IsBusy` to a new ViewModel property tracking whether the control is in Checking or Downloading state

**Reuses:**
- The existing custom-control pattern from `SummaryCard` (sealed class, `DefaultStyleKeyProperty.OverrideMetadata`, style in Common themes)
- The existing `SummaryCardTone` enum pattern for the `MarkerTone` API
- `SystemParameters.ClientAreaAnimation` for reduced-motion detection
- The theme's `SuccessBrush` and `DangerBrush` for Success/Failure tone glyphs

**Test updates:**
- Existing tests that assert on the literal string `"Detecting…"` or `"Checking…"` must be updated to expect the ellipsis-free versions

## Dependencies

<!-- None: all referenced APIs (SystemParameters, theme brushes, MVVM base classes, test infrastructure) already exist. -->

## Non-Goals

- Other busy labels (BrowsePluginsDialog "Loading plugins...", WhatsNewViewModel, other agent card messages like "Updating {name}…", LaunchCommandCoordinator "{kind} {name}...", tray menu) are explicitly deferred
- Tray menu busy labels (e.g., "Downloading update…") remain text-only permanently — native Win32 menus do not repaint per-frame, record this as a permanent exclusion
- A second `GlyphSet` property or customizable frame set — the braille frames are a constant (YAGNI)
- Making the speed or tone user-configurable via preferences
- Animating other UI elements (progress bars, spinners) — scope is limited to the braille glyph marker

## Deviations from Brief

<!-- None: the approved decisions provided fixed visuals, API, and adoption scope. -->
