# settings-theme Specification

## Purpose

Defines the dark visual theme of the Settings window, matching the launch window's visual language — the shared palette, drawn chrome, sectioned form layout, and dark controls — so the window reads as part of the same application rather than a light system dialog.

## Requirements

### Requirement: Dark settings window chrome

The Settings window SHALL be rendered with the shared dark palette and drawn chrome instead of the system light window: no OS title bar, a title bar showing the application icon and the "CLIHub - Settings" title, drawn maximize/restore and close buttons, header drag, rounded corners, and Escape closing the window. The title bar SHALL NOT show a version pill or a minimize button.

#### Scenario: Dark surfaces
- **WHEN** the Settings window is shown
- **THEN** the window background, header, content area, and footer use the dark palette shared with the launch window

#### Scenario: Title bar shows identity
- **WHEN** the Settings window is shown
- **THEN** its title bar shows the application icon and the "CLIHub - Settings" title, and no version pill or minimize button

#### Scenario: Drawn header works
- **WHEN** the user drags the drawn header
- **THEN** the window moves with the drag

#### Scenario: Maximize toggles window state
- **WHEN** the user activates the maximize button
- **THEN** the window is maximized and the button becomes a restore button; activating it again restores the previous size

#### Scenario: Maximized window fits the work area
- **WHEN** the window is maximized
- **THEN** it fills the monitor's work area only, so the footer stays visible above the taskbar

#### Scenario: Close button closes
- **WHEN** the user activates the close button
- **THEN** the window closes with no changes saved

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

The default-runtime and path-display selectors SHALL render as segmented controls: one bordered group of segments, one per option, with the selected segment visually highlighted using the accent. Each runtime segment SHALL show an icon together with its label; each path-display segment SHALL show its label.

#### Scenario: Selected segment is visible
- **WHEN** a runtime or path-display option is selected
- **THEN** its segment is highlighted with the accent while the others stay neutral

#### Scenario: Segments carry short labels
- **WHEN** the runtime selector is rendered
- **THEN** each segment shows an icon (PowerShell, PowerShell 7, Command Prompt, Git Bash) with its label rather than a long combo-box label

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

### Requirement: Sidebar navigation styling

The Settings window SHALL render a sidebar navigation region in the shared dark palette: an uppercase muted label per group, one navigable item per page, and the active page highlighted with the accent. The active page's content SHALL be shown in the content area under a page header, and the sidebar SHALL remain readable on the dark surface.

#### Scenario: Group labels and items
- **WHEN** the sidebar is rendered
- **THEN** each group ("General", "Engines & Repos", "System") has an uppercase muted label and its pages appear as navigable items

#### Scenario: Active page highlighted
- **WHEN** a page is active
- **THEN** its sidebar item is highlighted with the accent while the other items stay neutral

#### Scenario: Page header
- **WHEN** a page is shown
- **THEN** its content appears under a page header on the dark content surface

#### Scenario: Placeholder page readable
- **WHEN** a page without implemented settings is shown
- **THEN** its empty state is rendered in the muted style and remains readable on the dark surface
