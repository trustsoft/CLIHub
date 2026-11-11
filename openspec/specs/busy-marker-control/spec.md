# busy-marker-control Specification

## Purpose
The `BusyMarker` control provides visual feedback that an operation is in progress by displaying an animated braille glyph marker. It replaces trailing-ellipsis busy labels with an unboxed, cycling glyph that confirms work is progressing, supports reduced motion, and marks the glyph as decorative for accessibility.

## Requirements

### Requirement: BusyMarker control API

The system SHALL provide a `BusyMarker` WPF custom control with three dependency properties: `IsBusy` (bool, default false) controls whether the marker is animating, `Speed` (int, default 80, clamped to 40-200 milliseconds) sets the frame duration, and `MarkerTone` (enum: Busy / Success / Failure) selects the visual state. The Busy tone displays an animated braille glyph, the Success tone displays a static "✓" glyph in the success theme color, and the Failure tone displays a static "✗" glyph in the danger theme color.

#### Scenario: IsBusy false shows no animation

- **WHEN** `IsBusy` is false
- **THEN** the marker shows no glyph and occupies no layout space (or shows a static empty slot if pre-allocated)

#### Scenario: IsBusy true shows animated braille glyph

- **WHEN** `IsBusy` is true and `MarkerTone` is `Busy`
- **THEN** the marker cycles through braille frames ["⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"] at the speed specified by the `Speed` property

#### Scenario: Speed is clamped to 40-200ms

- **WHEN** `Speed` is set to a value below 40 or above 200
- **THEN** the effective frame duration is clamped to 40ms (minimum) or 200ms (maximum)

#### Scenario: Success tone shows static checkmark

- **WHEN** `MarkerTone` is `Success`
- **THEN** the marker displays a static "✓" glyph in the theme's `SuccessBrush` color

#### Scenario: Failure tone shows static cross

- **WHEN** `MarkerTone` is `Failure`
- **THEN** the marker displays a static "✗" glyph in the theme's `DangerBrush` color

### Requirement: Animation lifecycle

The system SHALL run the marker's timer only while `IsBusy` is true AND the control is visible. When either condition becomes false, the timer SHALL stop. When both become true, the timer SHALL start (or resume).

#### Scenario: Timer runs only while busy and visible

- **WHEN** `IsBusy` is true and the control is visible
- **THEN** the marker's timer runs and cycles frames at the specified speed

#### Scenario: Timer stops when IsBusy becomes false

- **WHEN** the timer is running and `IsBusy` becomes false
- **THEN** the timer stops within one frame interval

#### Scenario: Timer stops when control is hidden

- **WHEN** the timer is running and the control becomes hidden (collapsed, removed from visual tree)
- **THEN** the timer stops

#### Scenario: Timer resumes when conditions are met

- **WHEN** the timer is stopped and `IsBusy` becomes true while the control is visible
- **THEN** the timer starts and begins cycling frames

### Requirement: Fixed-width glyph slot

The system SHALL render the marker in a fixed-width slot so that cycling frames do not shift the surrounding layout. The slot width SHALL be set to the widest braille glyph's measured width at the default font size.

#### Scenario: Layout does not shift while cycling

- **WHEN** the marker cycles through braille frames
- **THEN** the surrounding layout (adjacent text, buttons, chips) does not shift or reflow

#### Scenario: Slot width matches widest glyph

- **WHEN** the marker is rendered
- **THEN** the glyph slot's minimum width is set to the measured width of the widest frame in the sequence

### Requirement: Reduced motion support

The system SHALL honor `SystemParameters.ClientAreaAnimation` for reduced-motion detection. When reduced motion is enabled (animation is disabled), the marker SHALL display a static full-cell braille glyph "⣿" instead of cycling frames, and the timer SHALL stop.

#### Scenario: Static glyph when reduced motion is enabled

- **WHEN** `SystemParameters.ClientAreaAnimation` is false (reduced motion enabled) and `IsBusy` is true
- **THEN** the marker displays a static "⣿" glyph
- **AND** the timer does not run

#### Scenario: Animated frames when reduced motion is disabled

- **WHEN** `SystemParameters.ClientAreaAnimation` is true (reduced motion disabled) and `IsBusy` is true
- **THEN** the marker cycles through the braille frames at the specified speed

### Requirement: Accessibility

The system SHALL mark the marker's glyph as decorative for screen readers using `AutomationProperties.AccessibilityView="Raw"` (or equivalent). Screen readers SHALL NOT announce the glyph or per-frame changes. Meaning is carried by the adjacent text label (e.g., "Detecting", "Checking").

#### Scenario: Glyph is excluded from automation tree

- **WHEN** a screen reader navigates to a control containing a `BusyMarker`
- **THEN** the marker's glyph is not announced
- **AND** only the adjacent text label is read

#### Scenario: Per-frame changes are not announced

- **WHEN** the marker cycles through frames
- **THEN** the screen reader does not announce each frame change

### Requirement: Testable core logic

The system SHALL extract UI-independent logic (frame iteration, speed clamping, reduced-motion static glyph selection) into a `BusyGlyphSequence` type in `CLIHub.Core.UI` that can be unit-tested without WPF. The control SHALL remain thin and depend on the Core logic.

#### Scenario: Frame iteration is testable

- **WHEN** `BusyGlyphSequence.GetFrame(index)` is called with index 0-9
- **THEN** it returns the corresponding braille frame without requiring WPF

#### Scenario: Speed clamping is testable

- **WHEN** `BusyGlyphSequence.ClampSpeed(milliseconds)` is called with values below 40, above 200, or within range
- **THEN** it returns the clamped or unchanged value without requiring WPF

#### Scenario: Reduced-motion glyph is testable

- **WHEN** `BusyGlyphSequence.GetReducedMotionGlyph()` is called
- **THEN** it returns the static full-cell glyph "⣿" without requiring WPF
