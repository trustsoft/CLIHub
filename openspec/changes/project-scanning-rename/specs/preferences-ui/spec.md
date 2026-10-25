# preferences-ui delta

## MODIFIED Requirements

### Requirement: Settings sections

The Settings window SHALL present a sidebar with the groups "General", "Engines & Repos", and "System" and the pages "General & Startup", "Hotkeys & Quick Launcher", "Projects", "Project Scanning", "CLI Agents", "Terminal Profiles", "Appearance", "Logs", and "Updates". Selecting a page SHALL show that page's content and mark it active, and the window SHALL open on "General & Startup". Each existing preference SHALL appear on its page: the startup toggles on "General & Startup", the global hotkey on "Hotkeys & Quick Launcher", the default runtime on "Terminal Profiles", the agent detection fields on "CLI Agents", the path display style on "Appearance", and the startup update check and manual update check on "Updates". The "Projects" page SHALL be grouped with "Project Scanning" under "Engines & Repos" and SHALL present the registered project list with its management actions. A page whose settings are not yet implemented SHALL show a neutral empty state instead of controls. The footer SHALL show the current application version.

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

#### Scenario: Page without implemented settings

- **WHEN** the user opens a page that has no implemented settings yet
- **THEN** the page shows a neutral empty state instead of controls

#### Scenario: Version shown

- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

### Requirement: Projects and Paths page options

The Project Scanning page SHALL offer a watched-directories list with add, per-directory scan, and remove actions, a "Repository scan depth" field, an "Ignored directory patterns" list, a "Repository discovery" selector with the options "Git only", "Agents only", and "Git and agents", and a read-only background watcher status. The per-directory scan action SHALL rescan only that directory's watched root, unlike the page-header action that rescans every saved root. Each watched-directory row SHALL show, beneath its path, a depth chip and a discovery chip that open a menu of allowed values, plus the number of discovered repositories beneath that directory. The "Repository scan depth" and "Repository discovery" fields SHALL act as defaults for a directory added from now on and SHALL NOT change the settings of existing rows. The page SHALL show the stored values when opened.

Changes to the scanning settings SHALL apply immediately, without requiring the footer Save: adding or removing a watched directory, changing a row's depth or discovery chip, changing the default depth or discovery mode, and adding or removing an ignored pattern SHALL each persist atomically, reconfigure the background scanner, and trigger a scan. A newly added watched directory SHALL therefore be scanned while the page is open, the page-header scan action and the summary cards SHALL always describe the list shown on the page, and the page SHALL state that scanning changes apply immediately. The footer Save and Cancel SHALL continue to govern the remaining settings, and Cancel SHALL NOT revert scanning changes that were already applied.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Project Scanning"
- **THEN** it shows the watched-directories list with add, scan, and remove actions, the depth and discovery defaults, the "Ignored directory patterns" list, and the watcher status
- **AND** it states that scanning changes apply immediately

#### Scenario: Stored values loaded

- **WHEN** the page opens
- **THEN** the watched directories show their stored depth, discovery mode, and repository count
- **AND** the default depth and discovery show the stored global values
- **AND** opening the page does not rescan

#### Scenario: Adding a watched directory applies immediately

- **WHEN** the user adds a watched directory
- **THEN** the list is persisted, the scanner is reconfigured, and that directory is scanned in the background
- **AND** the repository count for the new row and the summary card coverage include it without choosing Save
- **AND** the page-header scan action now rescans it too

#### Scenario: Removing a watched directory applies immediately

- **WHEN** the user removes a watched directory
- **THEN** the list is persisted and the scanner is reconfigured
- **AND** discovered projects that are no longer under any watched root and were not registered manually are removed by the resulting scan

#### Scenario: Ignored pattern changes apply immediately

- **WHEN** the user adds or removes an ignored directory pattern
- **THEN** the pattern list is persisted and the next scan uses it without choosing Save

#### Scenario: Row chips change only that row

- **WHEN** the user picks a depth or discovery value from a row's chip menu
- **THEN** only that row's setting changes
- **AND** the global defaults and the other rows keep their values
- **AND** the new value is persisted and used by the next scan

#### Scenario: Defaults

- **WHEN** no scanning preferences have been stored
- **THEN** the watched-directories list is empty, the default depth shows the built-in default, the ignored patterns show the built-in defaults, and the default discovery mode is "Git and agents"

#### Scenario: New directory starts from the defaults

- **WHEN** the user adds a watched directory
- **THEN** its depth and discovery chips start from the current default values

#### Scenario: Per-directory scan

- **WHEN** the user chooses the scan action on one watched directory
- **THEN** only that watched root is rescanned with that row's settings
- **AND** the other watched roots keep their current discovered projects

#### Scenario: Row summary shows the per-directory count

- **WHEN** a watched directory is listed
- **THEN** it shows the number of discovered repositories beneath that directory
- **AND** the count uses the singular "repository" for one and the plural "repositories" otherwise
- **AND** the count refreshes after a scan completes

#### Scenario: Watcher status is read-only

- **WHEN** the watcher status is shown
- **THEN** it reports the watcher state, the last scan time, and the discovered-project count, and it is not user-editable

#### Scenario: Cancel keeps applied scanning changes

- **WHEN** the user changes a scanning setting and then closes the window with Cancel
- **THEN** the scanning change remains stored and scanned
- **AND** the remaining unsaved settings are discarded

### Requirement: Projects and Paths validation

The Project Scanning page SHALL validate its input and reject invalid values without saving.

#### Scenario: Scan depth out of range rejected

- **WHEN** the scan depth is not a positive whole number within the allowed range
- **THEN** validation reports an error and no value is written to `config.json`

#### Scenario: Empty or duplicate patterns ignored

- **WHEN** an ignored pattern is blank or duplicates an existing pattern
- **THEN** the blank or duplicate entry is not stored

#### Scenario: Discard on cancel

- **WHEN** the user changes a setting that is not part of scanning and chooses "Cancel"
- **THEN** the stored values are unchanged and reopening Settings shows the stored values
