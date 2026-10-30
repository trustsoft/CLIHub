## REMOVED Requirements

### Requirement: Settings sections

## ADDED Requirements

### Requirement: Settings sections and pages

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Hotkeys & Quick Launcher", "Projects", "Project Scanning", "CLI Agents", "Terminal Profiles", "Appearance", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Hotkeys & Quick Launcher", the default runtime on "Terminal Profiles", the agent detection fields on "CLI Agents", the path display style on "Appearance", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. The footer SHALL show the current application version.

#### Scenario: Sections shown

- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the nine pages
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page

- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

#### Scenario: Projects page in the sidebar

- **WHEN** the Settings window opens
- **THEN** "Projects" appears under "Engines & Repos" next to "Project Scanning"
- **AND** selecting it shows the registered project list and its management actions

#### Scenario: Existing controls on their pages

- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field on "Hotkeys & Quick Launcher", the default-runtime selector on "Terminal Profiles", the agent detection `Cache TTL, minutes` and `Version check timeout, seconds` fields on "CLI Agents", the path display selector on "Appearance", and the "Check for updates on startup" toggle and manual update check on "Updates"

#### Scenario: Current values loaded

- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version
