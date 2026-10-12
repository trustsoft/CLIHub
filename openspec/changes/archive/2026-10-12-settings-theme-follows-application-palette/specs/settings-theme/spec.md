## MODIFIED Requirements

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
