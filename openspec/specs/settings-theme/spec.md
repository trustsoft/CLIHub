# settings-theme Specification

## Purpose

Defines the dark visual theme of the Settings window, matching the launch window's visual language — the shared palette, drawn chrome, sectioned form layout, and dark controls — so the window reads as part of the same application rather than a light system dialog.

## Requirements

### Requirement: Dark settings window chrome

The Settings window SHALL be rendered with the shared dark palette and drawn chrome instead of the system light window: no OS title bar, a "Settings" header with a close button, header drag, rounded corners, and Escape closing the window.

#### Scenario: Dark surfaces
- **WHEN** the Settings window is shown
- **THEN** the window background, header, content area, and footer use the dark palette shared with the launch window

#### Scenario: Drawn header works
- **WHEN** the user drags the drawn header or clicks its close button
- **THEN** the window moves with the drag and the close button closes it

#### Scenario: Escape closes
- **WHEN** the user presses Escape in the Settings window
- **THEN** the window closes with no changes saved

### Requirement: Sectioned form layout

The settings content SHALL be laid out as sections introduced by uppercase muted section labels, with fields grouped under them and a short hint line under non-obvious fields, matching the mockup's rhythm.

#### Scenario: Section labels
- **WHEN** a section such as "Default runtime", "Global hotkey", "Agents probe", or "Updates" is rendered
- **THEN** it is introduced by an uppercase muted label in the section-label style shared with the launch window

#### Scenario: Hint lines
- **WHEN** the hotkey or agents-probe fields are rendered
- **THEN** their existing hint text appears under the fields in the muted hint style

### Requirement: Segmented selectors

The default-runtime and path-display selectors SHALL render as segmented controls: one bordered group of segments, one per option, with the selected segment visually highlighted using the accent.

#### Scenario: Selected segment is visible
- **WHEN** a runtime or path-display option is selected
- **THEN** its segment is highlighted with the accent while the others stay neutral

#### Scenario: Segments carry short labels
- **WHEN** the runtime selector is rendered
- **THEN** its segments show the short tokens `cmd`, `ps`, and `wt` rather than the long combo-box labels

### Requirement: Dark input controls

The hotkey capture field, probe TTL and timeout fields, checkboxes, and the "Check for updates" button SHALL use the dark input and control styling consistent with the launch window.

#### Scenario: Hotkey chips
- **WHEN** a combination is captured or loaded
- **THEN** the hotkey field shows the combination as bordered key chips (for example Ctrl, Alt, Space) on the dark input surface, with the capture hint below

#### Scenario: Text fields remain readable
- **WHEN** the probe TTL and timeout fields hold values
- **THEN** the values are rendered as light text on the dark input surface with visible field boundaries

#### Scenario: Checkbox and button styling
- **WHEN** the startup, window-visibility, and update checkboxes and the "Check for updates" button are rendered
- **THEN** they use the dark control styling with the accent for the checked and enabled states

### Requirement: Themed footer

The Settings footer SHALL use the dark strip styling: the CLIHub brand and version chip on the left, an accent Save button and a neutral Cancel button on the right.

#### Scenario: Footer matches the mockup
- **WHEN** the Settings window is shown
- **THEN** the footer shows the brand and version chip on the dark strip, with the accent Save and a neutral Cancel on the right

### Requirement: Update status presentation

The update check result SHALL be presented as a status line under the update controls, using the accent tone for a readable, mockup-consistent result message.

#### Scenario: Status line readable
- **WHEN** an update check completes
- **THEN** the result message appears under the update controls in the accent-toned status style and remains fully readable on the dark surface

### Requirement: Validation errors remain readable

Validation and error messages SHALL remain clearly readable on the dark surfaces, using a single error tone distinct from the accent and the primary text.

#### Scenario: Error message readable
- **WHEN** the window shows a validation or save error
- **THEN** the message is rendered in the error tone on the dark surface and remains fully readable
