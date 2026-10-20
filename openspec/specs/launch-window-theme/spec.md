# launch-window-theme Specification

## Purpose
Defines the shared theme applied to the launch window — the dark palette by default, the light palette when the application theme is light — so the launcher reads as a focused, low-glare tool that matches the approved mockup.

## Requirements

### Requirement: Dark launch window theme

The launch window SHALL be rendered with the application's shared theme palette rather than the
operating system's default window theme: the dark palette by default, the light palette when the
effective application theme is Light (or System resolved to light), and the blue palette when the
effective application theme is Blue.

#### Scenario: Dark surfaces
- **WHEN** the launch window is shown with the dark theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the dark palette

#### Scenario: Light surfaces
- **WHEN** the launch window is shown with the light theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the light palette

#### Scenario: Blue surfaces
- **WHEN** the launch window is shown with the blue theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the blue palette

#### Scenario: Readable text
- **WHEN** a pane header, a row, or the footer is rendered
- **THEN** primary text contrasts with its surface, and secondary text such as a project path uses a dimmed tone that still contrasts with the surface

#### Scenario: Separators remain visible
- **WHEN** rows are listed in a pane
- **THEN** the boundary between consecutive rows remains visible on the active palette's background

### Requirement: Theme state feedback

The system SHALL keep interaction states distinguishable on the active palette's surfaces.

#### Scenario: Hover and selection differ
- **WHEN** the pointer hovers a row and, separately, the row is selected
- **THEN** the hover feedback and the selection feedback are visually distinct from each other and from the resting row

#### Scenario: Dimmed state remains readable
- **WHEN** a row is shown dimmed because its agent is unavailable
- **THEN** the row is still readable and remains distinguishable from an available row

#### Scenario: Keyboard focus is visible
- **WHEN** a control receives keyboard focus
- **THEN** a focus indicator is visible against the palette's surface

### Requirement: Themed tooltips

Tooltips shown over controls in the launch window SHALL use the application's themed popover style consistent with the window's chrome and active palette, rather than the system default tooltip style.

#### Scenario: Tooltip matches the window chrome
- **WHEN** the pointer rests on a control in the launch window that shows a tooltip, such as an agent action button or a footer button
- **THEN** the tooltip appears as a themed popover consistent with the window's other popovers: a themed background, contrasting text, and rounded corners

#### Scenario: Tooltip text stays readable
- **WHEN** a themed tooltip is shown
- **THEN** its text contrasts with its surface and is fully readable, matching the window's primary text tone

#### Scenario: Tooltip timing unchanged
- **WHEN** the pointer rests on a control with a tooltip
- **THEN** the tooltip appears and disappears with the platform's default timing and placement behavior
