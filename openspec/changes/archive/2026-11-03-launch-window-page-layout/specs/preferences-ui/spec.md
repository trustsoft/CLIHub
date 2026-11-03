# Capability: Preferences UI

## MODIFIED Requirements

### Requirement: Path display preference

The Settings window SHALL offer a choice of how long project paths are shortened in the launch window, under "Project path shortening" on the Launch Window page.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the path display control shows the stored style and offers `Left trim` and `Middle ellipsis`

#### Scenario: Default style
- **WHEN** no path display style has been saved
- **THEN** left trim is used

#### Scenario: Persist on save
- **WHEN** the user selects a path display style and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different path display style
- **THEN** the project rows in the launch window use the new style immediately, without restarting the application

#### Scenario: Discarded on cancel
- **WHEN** the user changes the path display style and chooses "Cancel"
- **THEN** the stored style is unchanged and reopening Settings shows the stored value

#### Scenario: Invalid stored value falls back to the default
- **WHEN** `config.json` contains an unrecognized path display style
- **THEN** left trim is used and the invalid value does not prevent the application from starting

### Requirement: Popup screen position

The Launch Window page SHALL offer a "Launcher placement" selector with the options "Active monitor – centered", "Active monitor – top-center", "Cursor-relative", and "Primary monitor – centered", defaulting to "Active monitor – centered". Options that target a monitor SHALL follow the "<monitor> – <placement>" label pattern so that both the monitor and the placement the launch window will use are explicit. "Cursor-relative" SHALL keep its pointer-relative wording because it does not target a monitor.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the "Launcher placement" selector shows the four options and the stored value is selected
- **AND** the monitor-targeting options are labelled "Active monitor – centered", "Active monitor – top-center", and "Primary monitor – centered"

#### Scenario: Default position
- **WHEN** no launcher window position has been stored
- **THEN** "Active monitor – centered" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a launcher window position and chooses "Save"
- **THEN** the value is written to `config.json`
- **AND** an existing stored primary-monitor position remains selected without migration

#### Scenario: Applied without restart
- **WHEN** the user saves a different launcher window position
- **THEN** the launch window uses it the next time it is shown without restarting the application

### Requirement: Settings sections and pages

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the launcher shortcut on "Launch Window" as the first setting in the grouped card, the launcher placement and project path shortening on "Launch Window" in the same grouped card, the default runtime on "Terminal Profiles", the agent detection fields on "Agent Detection", the theme and accent color on "Theme & Accent", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. The footer SHALL show the current application version.

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
- **AND** selecting it shows the launcher shortcut capture field, the launcher placement control, and the project path shortening control

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
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the launcher shortcut capture field appears on "Launch Window" as the first setting in a grouped card, the launcher placement and project path shortening selectors appear on "Launch Window" in the same grouped card, the default-runtime selector appears on "Terminal Profiles", the agent detection `Cache TTL, minutes` and `Version check timeout, seconds` fields appear on "Agent Detection", the theme and accent selectors appear on "Theme & Accent", and the "Check for updates on startup" toggle and manual update check appear on "Updates"

#### Scenario: Current values loaded

- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

#### Scenario: Hotkey section first on Launch Window

- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the page shows one grouped card with three settings in order: launcher shortcut (containing the hotkey capture field), launcher placement, and project path shortening
