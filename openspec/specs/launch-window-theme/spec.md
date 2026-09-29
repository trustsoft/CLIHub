# launch-window-theme Specification

## Purpose
Defines the dark visual theme applied to the launch window, so the launcher reads as a focused, low-glare tool that matches the approved mockup.

## Requirements

### Requirement: Dark launch window theme

The launch window SHALL be rendered with the dark palette from the approved mockup rather than the system light theme.

#### Scenario: Dark surfaces
- **WHEN** the launch window is shown
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the dark palette

#### Scenario: Readable text
- **WHEN** a pane header, a row, or the footer is rendered
- **THEN** primary text is light on the dark surface, and secondary text such as a project path uses a dimmed light tone that still contrasts with the surface

#### Scenario: Separators remain visible
- **WHEN** rows are listed in a pane
- **THEN** the boundary between consecutive rows remains visible on the dark background

### Requirement: Theme state feedback

The system SHALL keep interaction states distinguishable on the dark surfaces.

#### Scenario: Hover and selection differ
- **WHEN** the pointer hovers a row and, separately, the row is selected
- **THEN** the hover feedback and the selection feedback are visually distinct from each other and from the resting row

#### Scenario: Dimmed state remains readable
- **WHEN** a row is shown dimmed because its agent is unavailable
- **THEN** the row is still readable and remains distinguishable from an available row

#### Scenario: Keyboard focus is visible
- **WHEN** a control receives keyboard focus
- **THEN** a focus indicator is visible against the dark surface

### Requirement: Theme scope

The dark theme SHALL be limited to the launch window in this capability.

#### Scenario: Settings window unchanged
- **WHEN** the user opens the Settings window
- **THEN** it keeps its existing light styling
