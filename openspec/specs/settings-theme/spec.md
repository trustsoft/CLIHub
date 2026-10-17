# settings-theme Specification

## Purpose

Defines the shared theme of the Settings window, matching the launch window's visual language — the application palette (dark by default, light when the application theme is light), drawn chrome, card layout, and themed controls — so the window reads as part of the same application rather than a system dialog.

## Requirements

### Requirement: Dark settings window chrome

The Settings window SHALL be rendered with the application's shared theme palette (the dark palette by default, the light palette when the effective application theme is light) and drawn chrome instead of the system window: no OS title bar, a title bar showing the application icon and the "CLIHub - Settings" title, drawn maximize/restore and close buttons, header drag, rounded corners, and Escape closing the window. The title bar SHALL NOT show a version pill or a minimize button.

#### Scenario: Themed surfaces
- **WHEN** the Settings window is shown
- **THEN** the window background, header, content area, and footer use the palette shared with the launch window for the active theme

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

The hotkey capture field, probe TTL and timeout fields, checkboxes, and the "Check for updates" button SHALL use the themed input and control styling for the active palette, consistent with the launch window.

#### Scenario: Hotkey chips
- **WHEN** a combination is captured or loaded
- **THEN** the hotkey field shows the combination as bordered key chips (for example Ctrl, Alt, Space) on the themed input surface, with the capture hint below

#### Scenario: Text fields remain readable
- **WHEN** the probe TTL and timeout fields hold values
- **THEN** the values are rendered as text that contrasts with the input surface and has visible field boundaries

#### Scenario: Checkbox and button styling
- **WHEN** the startup, window-visibility, and update checkboxes and the "Check for updates" button are rendered
- **THEN** they use the themed control styling with the accent for the checked and enabled states

### Requirement: Themed footer

The Settings footer SHALL use the themed strip styling for the active palette: the CLIHub brand and version chip on the left, an accent Save button and a neutral Cancel button on the right.

#### Scenario: Footer matches the mockup
- **WHEN** the Settings window is shown
- **THEN** the footer shows the brand and version chip on the themed strip, with the accent Save and a neutral Cancel on the right

### Requirement: Update status presentation

The update check result SHALL be presented as a status line under the update controls, using the accent tone for a readable, mockup-consistent result message.

#### Scenario: Status line readable
- **WHEN** an update check completes
- **THEN** the result message appears under the update controls in the accent-toned status style and remains fully readable on the active palette's surface

### Requirement: Validation errors remain readable

Validation and error messages SHALL remain clearly readable on the active palette's surfaces, using a single error tone distinct from the accent and the primary text.

#### Scenario: Error message readable
- **WHEN** the window shows a validation or save error
- **THEN** the message is rendered in the error tone on the active palette's surface and remains fully readable

### Requirement: Sidebar navigation styling

The Settings window SHALL render a sidebar navigation region in the application palette: an uppercase muted label per group, one navigable item per page with a leading icon, and the active page highlighted with the accent. The active page's content SHALL be shown in the content area under a page header, and the sidebar SHALL remain readable on the active palette's surface.

#### Scenario: Group labels and items
- **WHEN** the sidebar is rendered
- **THEN** each group ("General", "Engines & Repos", "System") has an uppercase muted label and its pages appear as navigable items

#### Scenario: Each item shows a leading icon
- **WHEN** the sidebar is rendered
- **THEN** every page item shows a leading icon before its title

#### Scenario: Active page highlighted
- **WHEN** a page is active
- **THEN** its sidebar item is highlighted with the accent while the other items stay neutral

#### Scenario: Page header
- **WHEN** a page is shown
- **THEN** its content appears under a page header on the themed content surface

#### Scenario: Placeholder page readable
- **WHEN** a page without implemented settings is shown
- **THEN** its empty state is rendered in the muted style and remains readable on the active palette's surface

### Requirement: Setting cards

Each setting SHALL be presented as a card: a surface carrying the setting's leading icon, title, an optional inline badge, and a short hint, with the setting's control shown inline. A compact control (a checkbox, a short segmented selector, or a numeric field) SHALL be aligned on the trailing edge of the card; a wide or compound control SHALL be shown stacked below the title and hint, spanning the card width.

#### Scenario: Card content
- **WHEN** a setting card is shown
- **THEN** it shows the setting's leading icon, title, hint, and its control

#### Scenario: Optional badge
- **WHEN** a setting carries a status badge
- **THEN** the badge appears inline next to the title with a tone (neutral, accent, or success)

#### Scenario: Compact setting card
- **WHEN** a setting with a compact control (for example a toggle) is shown
- **THEN** its title and hint appear on the leading side and its control appears on the trailing side of the card

#### Scenario: Wide setting card
- **WHEN** a setting with a wide or compound control (for example the hotkey field, a runtime selector, the accent swatches, or the log-file row) is shown
- **THEN** its title and hint appear on top and its control appears below, spanning the card width

#### Scenario: Card visual language
- **WHEN** a setting card is shown
- **THEN** it uses the shared palette with a rounded border and a themed surface, consistent with the launch window

### Requirement: Grouped setting surface

A group of related settings SHALL be renderable either as standalone cards or as rows on one shared bordered surface, where the rows are separated by hairlines and the first row has no separator.

#### Scenario: Standalone cards
- **WHEN** settings are shown as standalone cards
- **THEN** each card has its own rounded border and cards are separated by a gap

#### Scenario: Grouped rows
- **WHEN** settings are shown as a group
- **THEN** the rows share one rounded bordered surface, consecutive rows are separated by a hairline, and the first row has no top separator

### Requirement: Page description and sections

A settings page SHALL show a short description under its page title, and SHALL group its settings under section headers where the page has more than one group of settings. A section header SHALL show an uppercase muted label and an optional accent marker.

#### Scenario: Page description
- **WHEN** a settings page is shown
- **THEN** a one-line description appears under the page title in the muted hint style

#### Scenario: Section headers
- **WHEN** a page groups settings into more than one area
- **THEN** each area is introduced by an uppercase muted section header above its cards, with an accent marker, without a trailing caption

#### Scenario: Single-group page
- **WHEN** a page has a single group of settings
- **THEN** its cards appear under the page description without a redundant section header

### Requirement: Cards on the data pages

The Projects & Paths and CLI Agents pages SHALL present their settings as cards in the shared palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, an icon block, a title, and a hint).

#### Scenario: Projects page cards
- **WHEN** the Projects & Paths page is shown
- **THEN** watched directories, scan depth, discovery mode, ignored patterns, and the watcher status each appear as cards under the page description and section headers

#### Scenario: List editor card
- **WHEN** a list setting (watched directories or ignored patterns) is shown
- **THEN** its list and its add/remove controls appear inside a card whose title and hint are on top and the content is stacked

#### Scenario: CLI Agents page cards
- **WHEN** the CLI Agents page is shown
- **THEN** the default-agent selector and the agents-probe fields appear as cards under the page description and section headers, and the agent list follows under the management section

#### Scenario: Default agent dropdown
- **WHEN** the default agent is shown
- **THEN** it is a dropdown selector (a combo box) offering "None" and the loaded agents, showing the stored choice

#### Scenario: Agent cards share the visual language
- **WHEN** a per-agent card is shown
- **THEN** it uses the same rounded bordered surface, title typography, and icon block treatment as the other setting cards

### Requirement: Card content alignment

A setting card SHALL let its content be aligned within the card both horizontally and vertically. Compact cards SHALL align their content to the trailing edge by default, stacked cards SHALL let their content fill the card width by default, and a page SHALL be able to override the alignment (for example, left-aligning a segmented selector so the control does not stretch to the full card width).

#### Scenario: Compact card default alignment
- **WHEN** a compact setting card is shown without an explicit content alignment
- **THEN** its control is aligned to the trailing edge of the card

#### Scenario: Stacked card default alignment
- **WHEN** a stacked setting card is shown without an explicit content alignment
- **THEN** its content fills the card width

#### Scenario: Overridden alignment
- **WHEN** a page sets a card's content alignment (for example to the leading edge)
- **THEN** the card's content is aligned accordingly instead of the default

### Requirement: Numeric input fields

A numeric setting SHALL render as an editable number on the themed input surface with a decrement control on the leading edge and an increment control on the trailing edge. A numeric field SHALL accept an optional unit suffix shown in the muted style next to the number, and stepped values SHALL stay within the field's allowed range.

#### Scenario: Stepping with the controls
- **WHEN** the user activates the decrement or increment control
- **THEN** the displayed number changes by one step and remains within the field's minimum and maximum

#### Scenario: Range limits
- **WHEN** the value reaches the field's minimum or maximum
- **THEN** the control that would move the value beyond that bound is disabled

#### Scenario: Keyboard stepping
- **WHEN** a numeric field has focus
- **THEN** pressing Up or Down changes the value by one step within the allowed range

#### Scenario: Typed values stay editable
- **WHEN** the user types a number into a numeric field
- **THEN** the typed text is kept and the existing form validation applies when the form is saved

#### Scenario: Empty field with a default
- **WHEN** a numeric field is empty and shows a default placeholder
- **THEN** the first step starts from that default value

#### Scenario: Unit suffix
- **WHEN** a numeric setting declares a unit such as minutes, seconds, days, or megabytes
- **THEN** the unit appears as a muted suffix next to the number on the input surface
