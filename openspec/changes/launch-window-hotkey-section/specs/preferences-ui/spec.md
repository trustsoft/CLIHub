## Purpose

Moves the global hotkey capture field from a dedicated "Global Hotkey" Settings page onto the "Launch Window" page as its first section, and removes the now-empty "Global Hotkey" page from the sidebar.

## MODIFIED Requirements

### Requirement: Settings sections and pages

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Launch Window" as the first (`ACTIVATION`) section, the launcher window position and path display style on "Launch Window" under their respective sections, the default runtime on "Terminal Profiles", the agent detection fields on "Agent Detection", the theme and accent color on "Theme & Accent", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. The footer SHALL show the current application version.

#### Scenario: Sections shown

- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the ten pages in this order: "General & Startup", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", and "Updates"
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page

- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

#### Scenario: Launch Window page in the sidebar

- **WHEN** the Settings window opens
- **THEN** "Launch Window" appears under "General" as the first page after "General & Startup" and before "Projects"
- **AND** selecting it shows the global hotkey capture field, the launcher window position control, and the path display control

#### Scenario: Projects page in the sidebar

- **WHEN** the Settings window opens
- **THEN** "Projects" appears under "Engines & Repos" next to "Project Scanning"
- **AND** selecting it shows the registered project list and its management actions

#### Scenario: Agent Detection page in the sidebar

- **WHEN** the Settings window opens
- **THEN** "Agent Detection" appears under "Engines & Repos" immediately after "CLI Agents" and before "Terminal Profiles"
- **AND** selecting it shows the agent detection settings

#### Scenario: Existing controls on their pages

- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field appears on "Launch Window" in the `ACTIVATION` section (the first section on the page), the launcher window position and path display selectors appear on "Launch Window" under their respective sections, the default-runtime selector appears on "Terminal Profiles", the agent detection `Cache TTL, minutes` and `Version check timeout, seconds` fields appear on "Agent Detection", the theme and accent selectors appear on "Theme & Accent", and the "Check for updates on startup" toggle and manual update check appear on "Updates"

#### Scenario: Current values loaded

- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

#### Scenario: Hotkey section first on Launch Window

- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the page shows three sections in order: `ACTIVATION` (containing the global hotkey capture field), `POSITION` (launcher window position selector), and `PROJECT PATHS` (path display selector)
