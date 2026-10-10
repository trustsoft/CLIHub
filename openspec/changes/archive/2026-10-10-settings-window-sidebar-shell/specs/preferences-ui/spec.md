## MODIFIED Requirements

### Requirement: Settings sections

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Hotkeys & Launchers", "Projects & Paths", "CLI Agents", "Terminal Profiles", "Appearance", "Telemetry & Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Hotkeys & Launchers", the default runtime on "Terminal Profiles", the agents-probe fields on "CLI Agents", the path display style on "Appearance", and the startup update check and manual update check on "Updates". A page whose settings are not yet implemented SHALL show a neutral empty state instead of controls. The footer SHALL show the current application version.

#### Scenario: Sections shown
- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the eight pages
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page
- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

#### Scenario: Existing controls on their pages
- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" and "Show window on startup" toggles appear on "General & Startup", the global-hotkey capture field on "Hotkeys & Launchers", the default-runtime selector on "Terminal Profiles", the agents-probe `TTL, minutes` and `Timeout, seconds` fields on "CLI Agents", the path display selector on "Appearance", and the "Check for updates on startup" toggle and manual update check on "Updates"

#### Scenario: Current values loaded
- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Page without implemented settings
- **WHEN** the user opens a page that has no implemented settings yet
- **THEN** the page shows a neutral empty state instead of controls

#### Scenario: Version shown
- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version
