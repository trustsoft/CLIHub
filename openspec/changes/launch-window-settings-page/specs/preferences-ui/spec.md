# Spec Delta

## MODIFIED Requirements

### Requirement: Path display preference

The Settings window SHALL offer a choice of how long project paths are shortened in the launch window.

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

The Launch Window page SHALL offer a "Launcher window position" selector under a "LAUNCHER POSITIONING" section header, with the options "Active monitor — centered", "Active monitor — top-center", "Cursor-relative", and "Primary monitor — centered", defaulting to "Active monitor — centered". Options that target a monitor SHALL follow the "<monitor> — <placement>" label pattern so that both the monitor and the placement the launch window will use are explicit. "Cursor-relative" SHALL keep its pointer-relative wording because it does not target a monitor.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the "LAUNCHER POSITIONING" section shows the "Launcher window position" selector with the four options and the stored value selected
- **AND** the monitor-targeting options are labelled "Active monitor — centered", "Active monitor — top-center", and "Primary monitor — centered"

#### Scenario: Default position
- **WHEN** no launcher window position has been stored
- **THEN** "Active monitor — centered" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a launcher window position and chooses "Save"
- **THEN** the value is written to `config.json`
- **AND** an existing stored primary-monitor position remains selected without migration

#### Scenario: Applied without restart
- **WHEN** the user saves a different launcher window position
- **THEN** the launch window uses it the next time it is shown without restarting the application

### Requirement: Application theme selector

The Theme & Accent page SHALL offer an "Application theme" selector as a row of four preview cards with
the options "Dark Theme", "Light Theme", "Blue Theme", and "System Sync", showing the stored value.
Each card SHALL show a miniature palette preview, the option name, and a short description:
"Fluent Dark (default)", "High contrast daylight profile", "Blue-tinted dark chrome",
and "Follow Windows personalization state" respectively. The selected card SHALL be marked with an
accent ring and a check badge.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the application theme selector offers "Dark Theme", "Light Theme", "Blue Theme", and
  "System Sync" as preview cards with their descriptions
- **AND** the stored value is selected and marked with the accent ring and check badge

#### Scenario: Persist on save
- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected
  palette immediately, without a restart

#### Scenario: Restart note
- **WHEN** the theme selector is shown
- **THEN** the "System Sync" card description indicates that it follows Windows personalization state

### Requirement: Accent color selector

The Theme & Accent page SHALL offer an accent color selector as preview cards for the presets Sky, Blue,
Teal, Emerald, Purple, Amber, Indigo, and Tangerine, plus a custom entry. Each card SHALL show a color
dot, the accent name, and its `#RRGGBB` value; the selected card SHALL be marked with the accent ring
and a check inside the dot, and the card header SHALL show the active accent name. The custom
`#RRGGBB` field SHALL be shown only while the custom entry is selected. The selector SHALL remember
the last valid custom accent independently of the named presets and restore it whenever the custom
entry is selected, including after the Settings window is closed and reopened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the accent selector offers Sky, Blue, Teal, Emerald, Purple, Amber, Indigo, and Tangerine as
  preview cards with their hex values
- **AND** the stored value is selected and marked with the accent ring and check
- **AND** the card header shows the active accent name

#### Scenario: Custom field visibility
- **WHEN** a named preset is selected
- **THEN** the custom `#RRGGBB` field is hidden
- **WHEN** the custom entry is selected
- **THEN** the custom field is shown

#### Scenario: Custom value kept independently
- **GIVEN** the user has entered a custom accent
- **WHEN** the user selects a named preset and later selects the custom entry again
- **THEN** the custom field shows the last valid custom accent

#### Scenario: Persist and apply on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the value is written to `config.json` and applied immediately

### Requirement: Settings sections and pages

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Global Hotkey", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Global Hotkey", the launcher window position and path display style on "Launch Window", the default runtime on "Terminal Profiles", the agent detection fields on "Agent Detection", the theme and accent color on "Theme & Accent", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. The footer SHALL show the current application version.

#### Scenario: Sections shown

- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the eleven pages in this order: "General & Startup", "Global Hotkey", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", and "Updates"
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page

- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

#### Scenario: Launch Window page in the sidebar

- **WHEN** the Settings window opens
- **THEN** "Launch Window" appears under "General" immediately after "Global Hotkey" and before "Projects"
- **AND** selecting it shows the launcher window position and path display controls

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
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field on "Global Hotkey", the launcher window position and path display selectors on "Launch Window", the default-runtime selector on "Terminal Profiles", the agent detection `Cache TTL, minutes` and `Version check timeout, seconds` fields on "Agent Detection", the theme and accent selectors on "Theme & Accent", and the "Check for updates on startup" toggle and manual update check on "Updates"

#### Scenario: Current values loaded

- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version
