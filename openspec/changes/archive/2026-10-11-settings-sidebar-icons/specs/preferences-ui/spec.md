## MODIFIED Requirements

### Requirement: Settings sections

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Hotkeys & Quick Launcher", "Projects & Paths", "CLI Agents", "Terminal Profiles", "Appearance", "Telemetry & Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Hotkeys & Quick Launcher", the default runtime on "Terminal Profiles", the agents-probe fields on "CLI Agents", the path display style on "Appearance", and the startup update check and manual update check on "Updates". A page whose settings are not yet implemented SHALL show a neutral empty state instead of controls. The footer SHALL show the current application version.

#### Scenario: Sections shown
- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the eight pages
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page
- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

#### Scenario: Existing controls on their pages
- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field on "Hotkeys & Quick Launcher", the default-runtime selector on "Terminal Profiles", the agents-probe `TTL, minutes` and `Timeout, seconds` fields on "CLI Agents", the path display selector on "Appearance", and the "Check for updates on startup" toggle and manual update check on "Updates"

#### Scenario: Current values loaded
- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Page without implemented settings
- **WHEN** the user opens a page that has no implemented settings yet
- **THEN** the page shows a neutral empty state instead of controls

#### Scenario: Version shown
- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

### Requirement: Popup screen position

The Hotkeys & Quick Launcher page SHALL offer a "Popup screen position" selector with the options "Center of active monitor", "Top-center of active monitor", "Cursor-relative", and "Primary screen", defaulting to "Center of active monitor".

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Hotkeys & Quick Launcher"
- **THEN** the popup screen position selector offers the four options with the stored value selected

#### Scenario: Default position
- **WHEN** no popup screen position has been stored
- **THEN** "Center of active monitor" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a popup screen position and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different popup screen position
- **THEN** the launch window uses it the next time it is shown without restarting the application
