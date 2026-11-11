## Context

See proposal.md - Why. CLIHub currently renders busy state text with trailing ellipses ("Detecting…", "Checking…", "Downloading…"). The approved HTML design mockup `spinner-mockup.html` at the repository root demonstrates the target visual: an unboxed braille glyph cycling frames ["⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"] at 80ms per frame, replacing the trailing ellipsis. The marker is adopted on exactly two surfaces: the agent card state chip (Settings → CLI Agents) while a probe runs, and the update control button label (launch window footer and Settings) while checking or downloading. All other busy labels remain text-only or are permanently excluded (tray menu).

This change writes zero code; it only produces the planning artifacts.

## Verified Current State

**AgentVersionText.cs (lines 1-24):**
- Line 8: `public static class AgentVersionText`
- Line 13: `public const string Detecting = "Detecting…";` — will change to `"Detecting"` (no trailing ellipsis)
- Line 18: `public const string NotInstalled = "Not installed";` — unchanged
- Line 23: `public const string Installed = "Installed";` — unchanged

**AgentCardItem.cs (lines 170-184):**
- Line 178: `public string VersionChipText => this.InstallState switch` — switch returns `AgentVersionText.Detecting` when `InstallState` is `Detecting` (line 180)
- The ViewModel does not currently expose a boolean property tracking whether a probe is running; that will be needed to bind `BusyMarker.IsBusy`

**UpdateControlViewModel.cs (lines 73-84):**
- Line 73: `public string UpdateButtonText => this.state switch` — returns `"Checking…"` (line 76), `"Downloading…"` (line 78 fallback), or `$"Downloading {downloading}…"` (line 77-78) — trailing ellipses will be removed
- The ViewModel does not currently expose a boolean property for whether the control is busy; that will be needed to bind `BusyMarker.IsBusy`

**SummaryCard.cs (lines 1-311) — reference implementation:**
- Line 44: `public sealed class SummaryCard : Control` — pattern for new control
- Line 112-117: `static SummaryCard() { DefaultStyleKeyProperty.OverrideMetadata(typeof(SummaryCard), new FrameworkPropertyMetadata(typeof(SummaryCard))); }` — pattern for style lookup
- Lines 12-32: `SummaryCardTone` enum with `Neutral`, `Accent`, `Muted`, `Success`, `Danger` — pattern for `MarkerTone` enum

**SettingsStyles.xaml (lines 356-455+) — SummaryCard style:**
- Line 356: `<Style TargetType="{x:Type controls:SummaryCard}">` — implicit style in Common theme dictionary; BusyMarker will follow this pattern
- Lines 359-517: Full control template with triggers and bindings — reference for BusyMarker template

**Controls.xaml (lines 1-193) — shared control resources:**
- Lines 1-13: ResourceDictionary structure merging Sizing.xaml — BusyMarker style will merge into this file or a new one
- Lines 21-55: Typography and chip styles — context for where BusyMarker style will live

**Sizing.xaml — contains font and sizing tokens:**
- `Sizing.FontFamily.Icon` — used for Segoe MDL2 Assets glyphs in existing controls; not needed for BusyMarker (uses Unicode braille)
- `Sizing.FontSize.Body`, `Sizing.FontSize.Chip` — context for BusyMarker glyph sizing

**Theme brushes (referenced in styles, defined in theme palettes):**
- `DynamicResource TextPrimaryBrush` — used for the Busy tone glyph
- `DynamicResource SuccessBrush` — used for the Success tone glyph
- `DynamicResource DangerBrush` — used for the Failure tone glyph

**SystemParameters API:**
- `SystemParameters.ClientAreaAnimation` (bool) — WPF API for reduced-motion detection; already in use elsewhere in WPF projects, reusable here

## Goals / Non-Goals

**Goals:**
- New WPF custom control `BusyMarker` with `IsBusy`, `Speed`, `MarkerTone` API
- Extract UI-independent logic into `CLIHub.Core.UI.BusyGlyphSequence` (frame iteration, speed clamping, reduced-motion static glyph)
- Default style/template in `Themes/Common/` with fixed-width glyph slot, tone-based brush bindings, and reduced-motion support
- Adopt on agent card state chip (Settings → CLI Agents) and update control button (launch window, Settings)
- Remove trailing ellipses from busy label text constants (`AgentVersionText.Detecting`, `UpdateControlViewModel.UpdateButtonText` strings)
- Timer runs only while `IsBusy` is true AND the control is visible
- Accessibility: mark the glyph as decorative (adjacent text carries meaning)
- Unit tests for `BusyGlyphSequence` in `tests/CLIHub.Core.Tests/UI/`

**Non-Goals:**
- Other busy labels (BrowsePluginsDialog "Loading plugins...", WhatsNewViewModel, etc.) — explicitly deferred
- Tray menu busy labels — permanently text-only (Win32 menu limitation, recorded as permanent exclusion)
- Customizable glyph set or user-configurable speed/tone
- Progress bars, spinners, or other animated UI elements

## Decisions

### Decision 1: Control API — IsBusy, Speed, MarkerTone

**Chosen:** `BusyMarker` exposes three dependency properties: `IsBusy` (bool, default false), `Speed` (int, milliseconds per frame, default 80, clamped to 40-200ms), and `MarkerTone` (enum: Busy / Success / Failure). The Busy tone shows the animated braille glyph in `TextPrimaryBrush`. Success shows a static "✓" in `SuccessBrush`. Failure shows a static "✗" in `DangerBrush`.

**Rationale:** Follows the existing `SummaryCardTone` enum pattern. The tone enum provides a clean extension point for future states (e.g., Warning) without adding boolean properties. Speed is clamped to a sane range to prevent flickering (too fast) or appearing frozen (too slow). Default 80ms per frame matches the approved HTML mockup.

**Alternatives considered:**
- Separate `IsSuccess` / `IsFailure` boolean properties: Rejected — enum is more maintainable and follows SummaryCard precedent
- No `Speed` property: Rejected — testability benefit and future-proofing outweigh YAGNI concerns for a constant

### Decision 2: Extract logic into CLIHub.Core as BusyGlyphSequence

**Chosen:** Create `src/CLIHub.Core/UI/BusyGlyphSequence.cs` with static braille frames, speed clamping logic, reduced-motion static glyph choice, and a method to get the next frame index. The control depends on Core, calls the sequence logic on timer tick, and keeps only the WPF-specific parts (timer lifecycle, dependency properties, template).

**Rationale:** CLIHub.Core must stay WPF-free. Frame iteration, speed clamping, and the reduced-motion glyph selection are testable logic that does not need WPF. Moving them into Core enables unit tests, keeps the control thin, and preserves the Core/UI boundary.

**Alternatives considered:**
- Keep all logic in the control: Rejected — violates Core/UI boundary and makes logic harder to test
- Inline the frames as a const in the control: Rejected — Core can own the sequence without WPF; extracting it makes the frames reusable for non-WPF contexts (CLI, tests)

### Decision 3: Style location — Themes/Common/Controls.xaml

**Chosen:** Add the `BusyMarker` implicit style (`<Style TargetType="{x:Type controls:BusyMarker}">`) to `src/CLIHub/Themes/Common/Controls.xaml` immediately after the existing `SummaryCard` style reference (which is in SettingsControls.xaml, but the Controls.xaml dictionary is the shared-controls file referenced at application level).

**Rationale:** `Controls.xaml` is the shared control styles dictionary merged at application level (see line 12 merging Sizing.xaml). `SummaryCard` style lives in `SettingsControls.xaml` because Settings pages are UserControls that load before attaching to the window tree. `BusyMarker` will be used in both Settings (agent cards) and the launch window (update button), so application-level merge is correct. The style goes in `Controls.xaml` or a new `BusyMarker.xaml` dictionary merged into `Controls.xaml` if the template is large.

**Alternatives considered:**
- New `BusyMarker.xaml` dictionary: Deferred — add only if the template exceeds ~50 lines; start inline in Controls.xaml
- SettingsControls.xaml: Rejected — the control is used in the launch window, which is not a UserControl; application-level merge is required

### Decision 4: Template structure — fixed-width TextBlock slot

**Chosen:** The control's template contains a single `TextBlock` with `TextAlignment="Center"`, `MinWidth` set to the widest braille glyph's measured width at the default font size, and `Text` bound to the control's current frame string. A template trigger switches the displayed glyph based on `MarkerTone` (Busy → animated frame, Success → "✓", Failure → "✗"). Another trigger switches `Foreground` based on tone (Busy → `TextPrimaryBrush`, Success → `SuccessBrush`, Failure → `DangerBrush`).

**Rationale:** Fixed-width slot prevents layout shift as frames cycle. A single TextBlock is simpler than overlaying multiple glyphs or using a ContentPresenter. Template triggers are the standard WPF pattern for state-based visuals.

**Alternatives considered:**
- Variable-width slot: Rejected — layout shifts per frame are visually distracting
- Separate TextBlocks for each tone: Rejected — single TextBlock with triggers is cleaner
- ContentPresenter with DataTemplates: Rejected — overkill for a single glyph

### Decision 5: Timer lifecycle — run only while IsBusy AND visible

**Chosen:** The control subscribes to `IsVisibleChanged` and starts/stops the timer when `IsBusy` is true and the control is visible. When either becomes false, the timer stops. The timer is a `DispatcherTimer` with `Interval` bound to the `Speed` property.

**Rationale:** Running the timer while the control is not visible or not busy wastes CPU. `IsVisibleChanged` is the correct WPF event for detecting when a control is removed from the visual tree or collapsed. `DispatcherTimer` ensures tick events run on the UI thread.

**Alternatives considered:**
- Always run the timer: Rejected — wastes CPU when the control is collapsed or hidden
- Manual start/stop API: Rejected — automatic lifecycle based on `IsBusy` and visibility is more convenient and less error-prone

### Decision 6: Reduced motion — static full-cell glyph "⣿"

**Chosen:** When `SystemParameters.ClientAreaAnimation` is false (reduced motion enabled), the control displays a static full-cell braille glyph "⣿" instead of cycling frames. The glyph is chosen in `BusyGlyphSequence.GetStaticGlyph()` and returned by the Core logic. The control checks `ClientAreaAnimation` on every timer start and switches to the static glyph when appropriate.

**Rationale:** `SystemParameters.ClientAreaAnimation` is the WPF API for reduced-motion detection (follows Windows "Show animations" setting). A static full-cell glyph provides a visual indicator that work is in progress without animation. The glyph is recognizable as a braille pattern and visually distinct from the cycling frames.

**Alternatives considered:**
- No reduced-motion support: Rejected — accessibility requirement
- Empty glyph or space: Rejected — user loses the visual indicator that work is in progress
- Different static glyph (e.g., "⠿"): Acceptable — Core logic can be changed; "⣿" is a reasonable default

### Decision 7: Accessibility — decorative glyph

**Chosen:** The glyph is marked as decorative for screen readers using `AutomationProperties.AccessibilityView="Raw"` (or the equivalent WPF mechanism to exclude the element from the automation tree). Meaning is carried by the adjacent text label ("Detecting", "Checking", etc.), so screen readers announce the label once, not per-frame changes.

**Rationale:** The glyph is a visual-only progress indicator; its cycling frames convey no semantic information beyond what the label already says. Screen readers should announce the label text ("Detecting", "Checking") once and ignore the glyph updates. Marking the glyph as decorative prevents per-frame announcements.

**Alternatives considered:**
- Announce each frame: Rejected — extremely distracting for screen reader users
- Set `AutomationProperties.Name` on the glyph: Rejected — the label already carries the meaning

### Decision 8: Agent card adoption — bind IsBusy to probe-running state

**Chosen:** Add a boolean property to `AgentCardItem` (e.g., `IsProbeRunning`) that is true while the card's installation state is `Detecting`. The agent card XAML composes the `BusyMarker` inline with the state chip text, binds `IsBusy` to `IsProbeRunning`, and shows the marker only while `IsBusy` is true. The chip text no longer has a trailing ellipsis.

**Rationale:** The card ViewModel already exposes `InstallState` (an enum including `Detecting`). Deriving `IsProbeRunning` from that state is straightforward. The XAML composes the marker inline with the chip, so the visual reads as "Detecting [animated glyph]" with no layout shift.

**Alternatives considered:**
- Bind `IsBusy` directly to `InstallState == Detecting`: Acceptable — may be cleaner than a separate property
- Separate chip template for busy state: Rejected — inline composition is simpler

### Decision 9: Update control adoption — bind IsBusy to checking/downloading state

**Chosen:** Add a boolean property to `UpdateControlViewModel` (e.g., `IsUpdateControlBusy`) that is true while the state is `Checking` or `Downloading`. The update control XAML (button Content) composes `BusyMarker` + `TextBlock` in a horizontal StackPanel or similar, binds `BusyMarker.IsBusy` to `IsUpdateControlBusy`, and binds the TextBlock to the updated `UpdateButtonText` (which no longer has trailing ellipses). The marker is shown only while `IsBusy` is true.

**Rationale:** The ViewModel already exposes `state` (an enum including `Checking` and `Downloading`). Deriving a boolean from that state is straightforward. The button's Content is a composite (marker + label), so the visual reads as "Checking [animated glyph]" with no layout shift.

**Alternatives considered:**
- Bind `IsBusy` directly to a multi-value converter on `state`: Acceptable — may be cleaner than a separate property
- DataTemplate for button Content: Acceptable — may be cleaner than inline composition

### Decision 10: Test updates — expect ellipsis-free strings

**Chosen:** Existing tests that assert on the literal string `"Detecting…"` or `"Checking…"` (identified via grep) must be updated to expect the ellipsis-free versions (`"Detecting"`, `"Checking"`, `"Downloading"`).

**Rationale:** The constants change as part of this work; tests must match the new values. The grep already found tests in `AgentVersionTextTests.cs` that assert on `"Detecting…"`. Update them to expect `"Detecting"`.

**Alternatives considered:**
- Leave tests failing: Rejected — all tests must pass after implementation

## Risks / Trade-offs

**[Risk] Animation may be distracting on high-DPI or large displays** → Mitigation: The glyph is small and cycles at 80ms per frame (12.5 fps), which is slow enough to not flicker. Reduced-motion support disables the animation entirely. If feedback indicates it is too fast, the `Speed` property can be increased (or made user-configurable later).

**[Risk] Fixed-width slot may be wider than the ellipsis it replaces** → Trade-off: The glyph occupies space even when not busy, which may shift layout slightly compared to the current text-only labels. The benefit (layout stability while cycling) outweighs the cost. If the shift is too large, the marker can be conditionally shown only while `IsBusy` is true (not pre-allocated).

**[Trade-off] Core sequence logic is testable, but timer lifecycle is not** → Acceptable: The timer lifecycle (start/stop on IsBusy and visibility changes) is WPF-dependent and cannot be unit-tested without a UI test harness. The Core sequence logic (frame iteration, speed clamping, reduced-motion glyph) is fully testable and holds the bulk of the complexity.

**[Risk] SystemParameters.ClientAreaAnimation may not reflect all reduced-motion preferences** → Mitigation: `ClientAreaAnimation` follows the Windows "Show animations" setting, which is the standard WPF API for this. If other reduced-motion sources are needed (e.g., browser prefers-reduced-motion), they can be added later.

## Alternatives Considered

**Alternative 1: Use a third-party animation library (e.g., WPF Animation)**

**Rejected.** A third-party library adds a dependency for a simple 10-frame cycling glyph. A `DispatcherTimer` with a TextBlock is sufficient and keeps the control lightweight.

**Alternative 2: Use a Storyboard or WPF animation timeline**

**Rejected.** WPF Storyboards are more complex than a `DispatcherTimer` for discrete frame cycling. Storyboards are better suited for continuous property animations (opacity, transforms). A timer is the standard WPF pattern for frame-based animations.

**Alternative 3: Adopt the marker on all busy labels in this change**

**Rejected.** The approved scope is exactly two surfaces (agent card state chip, update control button). Other busy labels are deferred to reduce change risk and implementation size. Tray menu labels are permanently text-only due to Win32 menu constraints.

**Alternative 4: Make the glyph set user-configurable**

**Rejected.** YAGNI. The braille frames are a constant and match the approved HTML mockup. A second glyph set is added only when actually needed.

## Open Questions

- **Where exactly is the agent card state chip XAML?** The agent card is rendered on the Settings → CLI Agents page. The ViewModel is `AgentCardItem`, which exposes `VersionChipText` (a string switch on `InstallState`). The XAML file that consumes this ViewModel and renders the chip needs to be located during implementation.

- **Where exactly is the update control button XAML?** The ViewModel is `UpdateControlViewModel`, which exposes `UpdateButtonText`. The button is rendered on the launch window footer and in the Settings → Updates page. The XAML files that consume this ViewModel and render the button need to be located during implementation.

- **Should the marker be pre-allocated (fixed-width slot always present) or conditionally shown (only while IsBusy)?** Pre-allocated avoids layout shift when the marker appears/disappears, but occupies space when not busy. Conditionally shown avoids the space cost, but may shift layout. The design.md recommends pre-allocated; implementation can test both and choose based on visual feedback.

- **Should the marker's font size match the adjacent label, or be slightly smaller?** The HTML mockup uses the same font size. The design.md recommends matching the adjacent label font size (Sizing.FontSize.Chip for the agent card, Sizing.FontSize.Body for the update button). Implementation can test both and choose based on visual feedback.

- **Should the marker have a left or right margin to separate it from the adjacent label?** The HTML mockup shows a small gap. The design.md recommends a 4-6px margin. Implementation can test and choose based on visual feedback.
