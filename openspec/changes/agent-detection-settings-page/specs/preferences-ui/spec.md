# Spec Delta: preferences-ui

## Purpose

Provides a Settings window where users view and change application preferences — startup behavior, hotkey and popup position, terminal runtime, agents and probing, project scanning, appearance, logging, and updates — without editing `config.json` by hand.

## MODIFIED Requirements

### Requirement: Settings sections and pages

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Hotkeys & Quick Launcher", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Appearance", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Hotkeys & Quick Launcher", the default runtime on "Terminal Profiles", the agent detection fields on "Agent Detection", the path display style on "Appearance", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. The footer SHALL show the current application version.

#### Scenario: Sections shown

- **WHEN** the Settings window opens
- **THEN** the sidebar shows the "General", "Engines & Repos", and "System" groups and the ten pages
- **AND** "General & Startup" is active and its content is shown

#### Scenario: Switch page

- **WHEN** the user selects another page in the sidebar
- **THEN** that page's content is shown and its sidebar item is marked active

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
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field on "Hotkeys & Quick Launcher", the default-runtime selector on "Terminal Profiles", the agent detection `Cache TTL, minutes` and `Version check timeout, seconds` fields on "Agent Detection", the path display selector on "Appearance", and the "Check for updates on startup" toggle and manual update check on "Updates"

#### Scenario: Current values loaded

- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards showing the supported and installed agent counts, the default agent selection, and the agent update availability aggregation, and a "Default agent" selector. It SHALL show the stored default agent when opened. The "Default agent" selector SHALL offer every loaded agent plus a "None" option, SHALL mark agents that are not installed on the host as disabled entries that cannot be selected, and SHALL refresh the option availability when agent detection runs. The page SHALL offer an agent detection action labelled "Detect" above the agent cards that re-detects every supported agent's installed state and version, and refreshes the agent cards, the availability summary, and the option availability.

The summary cards showing supported and installed agent counts and update availability SHALL update immediately when agent detection completes, without requiring the Settings window to be closed and reopened. The UPDATES summary card SHALL show the number of installed agents with available updates when known, "None" when every installed agent is up to date, or "Unknown" while update availability is not checked, and SHALL refresh after the load-time availability pass, the header update check, per-card update checks, and detection passes that clear stale availability.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the summary cards with the supported and installed counts, default agent selection, and update availability, and the "Default agent" selector

#### Scenario: One card per agent

- **WHEN** the plugin catalog contains agents
- **THEN** the page shows one card for each loaded agent

#### Scenario: Metrics shown

- **WHEN** the page opens
- **THEN** the supported count is the number of loaded agents and the installed count is the number installed on the host

#### Scenario: Metrics update when Detect completes

- **WHEN** agent detection completes while the Settings window is open (from the Detect action or a per-card test)
- **THEN** the supported and installed counts on the CLI Agents page summary cards update immediately without reopening the window

#### Scenario: Stored default agent loaded

- **WHEN** a default agent has been saved
- **THEN** the "Default agent" selector shows it selected

#### Scenario: No stored default agent

- **WHEN** no default agent preference has been stored
- **THEN** the selector offers a "None" option and no agent is selected by default

#### Scenario: Uninstalled agents are not selectable

- **WHEN** the "Default agent" dropdown is opened
- **THEN** agents that are not installed on the host are shown dimmed and disabled
- **AND** they cannot be selected, while "None" and installed agents remain selectable

#### Scenario: Detection refreshes option availability

- **WHEN** an agent detection pass completes while the page is open, whether from a single-agent test or the detect action above the agent cards
- **THEN** the option availability reflects the fresh installed states without reopening the window

#### Scenario: Detect action re-probes every supported agent

- **WHEN** the user invokes the detect action above the agent cards
- **THEN** the installed state and version of every supported agent are re-detected against the host
- **AND** the agent cards, the availability summary, and the option availability refresh without reopening the window
- **AND** the action is unavailable while a detection pass is already running

#### Scenario: Detect action label

- **WHEN** the CLI Agents page is shown
- **THEN** the detection action above the agent cards is labelled "Detect"

#### Scenario: UPDATES card shown

- **WHEN** the CLI Agents page header is visible
- **THEN** the third summary card is labelled "UPDATES" with the Update icon and shows the agent update availability aggregation

#### Scenario: UPDATES card value while unknown

- **WHEN** update availability has not been checked for any installed agent (on page load, before the first check)
- **THEN** the UPDATES card shows "Unknown" and a muted tone

#### Scenario: UPDATES card value when all up to date

- **WHEN** every installed agent has been checked and has status UpToDate (or no agents are installed)
- **THEN** the UPDATES card shows "None" and a success tone

#### Scenario: UPDATES card value when updates available

- **WHEN** N installed agents (N > 0) have status UpdateAvailable
- **THEN** the UPDATES card shows the count as a string (e.g., "3") and an accent tone

#### Scenario: UPDATES card detail while unknown

- **WHEN** the UPDATES card value is "Unknown"
- **THEN** the detail line shows "Run Check updates" or equivalent

#### Scenario: UPDATES card detail when all up to date

- **WHEN** the UPDATES card value is "None"
- **THEN** the detail line shows "All installed agents up to date" or equivalent

#### Scenario: UPDATES card detail when updates available

- **WHEN** the UPDATES card value is a count N
- **THEN** the detail line shows "N of M installed agents" with singular/plural handling (e.g., "1 of 3 installed agents" or "3 of 5 installed agents")

#### Scenario: UPDATES card detail when no installed agents

- **WHEN** no agents are installed
- **THEN** the detail line shows "No installed agents" or equivalent

#### Scenario: UPDATES card refreshes after load-time availability pass

- **WHEN** the Settings window opens on CLI Agents and the load-time availability pass completes
- **THEN** the UPDATES card updates to show the aggregated update status without reopening the window

#### Scenario: UPDATES card refreshes after header update check

- **WHEN** the user invokes the "Check updates" action above the agent cards and the check completes
- **THEN** the UPDATES card updates to show the fresh aggregated update status

#### Scenario: UPDATES card refreshes after per-card update check

- **WHEN** the user invokes a per-card "Check updates" action on one agent card and the check completes
- **THEN** the UPDATES card updates to reflect the fresh availability for that agent

#### Scenario: UPDATES card refreshes after detection pass

- **WHEN** the user invokes the "Detect" action above the agent cards and the detection completes
- **THEN** the UPDATES card updates to reflect the cleared stale availability

## ADDED Requirements

### Requirement: Agent Detection page content

The Agent Detection page SHALL present the agent detection cache TTL and version check timeout fields under a "DETECTION" section header with a "Reset" action that clears both fields to empty (built-in defaults) in one action, and SHALL show three page-header summary cards reporting the effective cache TTL and timeout values (distinguishing built-in defaults from custom values) and the built-in default values.

#### Scenario: Page header summary cards shown

- **WHEN** the Settings window opens on "Agent Detection"
- **THEN** the page header shows three summary cards: CACHE TTL (effective value with unit "min" and a detail line), TIMEOUT (effective value with unit "sec" and a detail line), and DEFAULTS (built-in default values with a detail line)

#### Scenario: Summary card detail distinguishes defaults from custom values

- **WHEN** a detection field is empty (using the built-in default)
- **THEN** the corresponding summary card's detail line indicates "Built-in default" or shows the default numeric value
- **WHEN** a detection field holds a custom value
- **THEN** the corresponding summary card's detail line indicates "Custom" or distinguishes the custom value from the default

#### Scenario: Detection fields shown

- **WHEN** the Agent Detection page is shown
- **THEN** the page body shows a "DETECTION" section header followed by the "Cache TTL" field (with unit "min" and a placeholder showing the built-in default) and the "Version check timeout" field (with unit "sec" and a placeholder showing the built-in default)

#### Scenario: Fields accept valid numeric input

- **WHEN** the user enters a positive whole number in either detection field
- **THEN** the value is accepted and shown in the field

#### Scenario: Empty fields use defaults

- **WHEN** either detection field is left empty and saved
- **THEN** the default for that field is used

#### Scenario: Invalid number rejected

- **WHEN** a detection field contains a non-numeric or non-positive value
- **THEN** the value is rejected and an error is shown and the window does not save

#### Scenario: Current values loaded

- **WHEN** the Agent Detection page opens
- **THEN** the fields show the stored custom values or are empty (placeholder visible) when using defaults
- **AND** the summary cards show the effective values (custom or default) with appropriate detail

#### Scenario: Icon and description

- **WHEN** the Agent Detection page is selected in the sidebar
- **THEN** the sidebar item shows the Diagnostic icon (IconGlyphs.Diagnostic) and the page description "Caching and timeout behavior for agent detection."

#### Scenario: DEFAULTS card shown

- **WHEN** the Agent Detection page header is visible
- **THEN** the third summary card is labelled "DEFAULTS" with the Diagnostic icon and shows the built-in default values for both detection parameters

#### Scenario: DEFAULTS card value

- **WHEN** the DEFAULTS card is visible
- **THEN** the value line shows the built-in defaults formatted as "N min · M sec" (e.g., "15 min · 10 sec") where N is the TTL default and M is the timeout default

#### Scenario: DEFAULTS card detail

- **WHEN** the DEFAULTS card is visible
- **THEN** the detail line shows "Used when a field is empty" or equivalent

#### Scenario: Reset action shown

- **WHEN** the Agent Detection page body is visible
- **THEN** the "DETECTION" section header has a right-aligned "Reset" chip button with the Refresh icon

#### Scenario: Reset action disabled at defaults

- **WHEN** both detection fields are empty (using built-in defaults)
- **THEN** the Reset button is disabled (dimmed)

#### Scenario: Reset action enabled with custom values

- **WHEN** either detection field contains a custom value (non-empty)
- **THEN** the Reset button is enabled

#### Scenario: Reset action clears both fields

- **WHEN** the user clicks the Reset button with custom values in either or both fields
- **THEN** both fields are cleared to empty (placeholders/watermarks visible)
- **AND** the summary cards update to show "Built-in default" or equivalent
- **AND** the Reset button becomes disabled

#### Scenario: Reset edits the draft

- **WHEN** the user clicks Reset and then clicks "Cancel"
- **THEN** the Settings window closes and the stored custom values remain unchanged (Reset edits the draft only; Cancel discards the draft)

#### Scenario: Reset button tooltip

- **WHEN** the user hovers over the Reset button
- **THEN** a tooltip shows "Restore the built-in defaults" or equivalent
