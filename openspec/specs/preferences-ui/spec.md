# preferences-ui Specification

## Purpose

Provides a Settings window where users view and change application preferences — startup behavior, hotkey and popup position, terminal runtime, agents and probing, project scanning, appearance, logging, and updates — without editing `config.json` by hand.

## Requirements

### Requirement: Settings window access

The system SHALL open a Settings window from the system tray.

#### Scenario: Open from the tray
- **WHEN** the user chooses "Settings" from the tray menu
- **THEN** the Settings window opens showing the current preferences

#### Scenario: Single window
- **WHEN** Settings is already open and the user chooses "Settings" again
- **THEN** the existing Settings window is brought to the front instead of opening a second one

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

### Requirement: Save applies preferences

Saving SHALL validate a typed settings draft, apply all required system preferences, persist the draft only after required application succeeds, and close the window only after persistence succeeds. If any required application or persistence step fails, the stored preferences SHALL remain unchanged and the user SHALL receive an error without the window closing. Any system changes made before a failure SHALL be rolled back to their previous values where the platform boundary supports rollback.

#### Scenario: Persist on save

- **WHEN** the user changes one or more settings and chooses "Save" with a valid draft
- **THEN** all required system preferences are applied
- **AND** the values are written to `config.json`
- **AND** the window closes

#### Scenario: Runtime applied

- **WHEN** the user saves a different default runtime
- **THEN** subsequent agent launches use the newly selected runtime

#### Scenario: Hotkey applied immediately

- **WHEN** the user saves a different global hotkey
- **THEN** the previous hotkey is unregistered and the new one is registered without restarting the application

#### Scenario: Startup update check applied

- **WHEN** the user disables "Check for updates on startup" and saves
- **THEN** subsequent application starts do not perform the automatic update check

#### Scenario: Autostart applied

- **WHEN** the user toggles "Start with Windows" and saves
- **THEN** the per-user Run registration is created or removed to match the toggle

#### Scenario: Startup window visibility applied

- **WHEN** the user changes "Start minimized to tray" and saves
- **THEN** the next application start shows the launch window when the toggle is off and starts in the system tray when it is on

#### Scenario: Invalid draft does not save

- **WHEN** the user chooses "Save" with an invalid hotkey or invalid non-positive detection value
- **THEN** validation reports an error
- **AND** no system preference is applied
- **AND** no value is written to `config.json`
- **AND** the window remains open

#### Scenario: System application failure rolls back

- **WHEN** a required system preference cannot be applied while saving a valid draft
- **THEN** previously applied system changes are rolled back where supported
- **AND** the stored preferences remain unchanged
- **AND** the window remains open with an error

### Requirement: Discard changes

Cancelling or closing the window SHALL discard unsaved changes.

#### Scenario: Cancel discards
- **WHEN** the user changes settings and chooses "Cancel" (or closes the window)
- **THEN** no changes are written and the stored preferences are unchanged

#### Scenario: Reopen shows stored values
- **WHEN** the user discards changes and opens Settings again
- **THEN** the controls show the previously stored values

### Requirement: Hotkey capture and validation

The hotkey field SHALL capture a key combination and reject invalid combinations.

#### Scenario: Capture a combination
- **WHEN** the user focuses the hotkey field and presses a modifier-plus-key combination
- **THEN** the field displays the captured combination

#### Scenario: Cancel capture
- **WHEN** the user presses Escape while capturing
- **THEN** capture is cancelled and the field keeps its previous value

#### Scenario: Reject combination without a modifier or key
- **WHEN** the captured combination has no modifier or no recognized key
- **THEN** the value is rejected and an error is shown, and the previous valid value is kept

### Requirement: Agents probe fields

The agent detection fields SHALL accept blank values meaning "use defaults" and SHALL reject invalid numbers.

#### Scenario: Blank uses defaults
- **WHEN** either detection field is left empty and saved
- **THEN** the default for that field is used

#### Scenario: Invalid number rejected
- **WHEN** a detection field contains a non-numeric or non-positive value
- **THEN** the value is rejected and an error is shown and the window does not save

### Requirement: Manual update check from Settings

The Updates section SHALL let the user trigger an update check and report the outcome.

#### Scenario: Check for updates
- **WHEN** the user chooses "Check for updates" in Settings
- **THEN** an update check runs and its outcome (up to date, update available with the version, not installed, or failed) is shown in the window

### Requirement: Path display preference

The Settings window SHALL offer a choice of how long project paths are shortened in the launch window.

#### Scenario: Options offered
- **WHEN** the Settings window opens
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

### Requirement: Terminal runtime options

The Terminal Profiles page SHALL offer the runtime hosts PowerShell, PowerShell 7, Command Prompt, and Git Bash, and SHALL default to PowerShell when no runtime has been stored. When a runtime's executable cannot be resolved — Git for Windows `bash.exe` or PowerShell 7 `pwsh.exe` — the page SHALL label that option as not installed, SHALL keep that option selectable, and SHALL keep the stored selection unchanged.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Terminal Profiles"
- **THEN** the runtime selector offers PowerShell, PowerShell 7, Command Prompt, and Git Bash, with the stored runtime selected

#### Scenario: Default runtime
- **WHEN** no runtime has been stored or the stored value is not in the set
- **THEN** PowerShell is selected

#### Scenario: Persist on save
- **WHEN** the user selects a runtime and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different runtime
- **THEN** subsequent agent commands open in the newly selected runtime without restarting the application

#### Scenario: Git Bash is marked when it cannot be launched
- **WHEN** the Settings window opens on "Terminal Profiles" and no Git for Windows `bash.exe` can be resolved
- **THEN** the Git Bash option is labelled as not installed
- **AND** the other runtime options and the stored selection are shown unchanged

#### Scenario: Availability is re-evaluated when the page opens
- **WHEN** Git for Windows is installed or removed while the application is running and the Settings window is reopened
- **THEN** the Git Bash option's installed state reflects the current installation

#### Scenario: PowerShell 7 is marked when it cannot be launched
- **WHEN** the Settings window opens on "Terminal Profiles" and no PowerShell 7 `pwsh.exe` can be resolved
- **THEN** the PowerShell 7 option is labelled as not installed
- **AND** the other runtime options and the stored selection are shown unchanged

### Requirement: Updates page options

The Settings page header for "Updates" SHALL show the current version and stable channel as chips next to the page title, an update-status chip indicating either that the build is current or that an update is available, a "Check for updates" action, and a "View release notes" action aligned to the bottom-right of the header. The page body SHALL offer the "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" controls under an "UPDATE PREFERENCES" section. The page SHALL show the current application version and check status without commit, target, or agent-protocol detail.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Updates"
- **THEN** the page header shows the version and stable channel chips, the update-status chip, and the "Check for updates" action
- **AND** the bottom-right of the header shows "View release notes"
- **AND** the page body shows "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" toggles under "UPDATE PREFERENCES"

#### Scenario: Defaults

- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on and "Notify about pre-release versions" is off

#### Scenario: Persist on save

- **WHEN** the user changes an update toggle and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Version status simplified

- **WHEN** the Updates page is shown
- **THEN** the status shows the current version and the check status (up to date, update available with the version, failed, cancelled, or not installed) without commit, target, or agent-protocol detail

#### Scenario: Header status chip reports the check outcome

- **WHEN** a check started from the page header completes
- **THEN** the header status chip reflects the outcome
- **AND** an available update is reported as "Update available"
- **AND** the chip detail includes the current and available versions

#### Scenario: Pre-existing update availability

- **WHEN** an update was already found before the Settings page is opened
- **THEN** the header status chip shows that an update is available

### Requirement: Logs page options

The Logs page SHALL offer a log-level selector (Verbose, Debug, Information, Warning, Error), a retention-days field, a file-size-limit field, the current log file path with a Copy action, and an "Open log folder" action.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Logs"
- **THEN** it shows the log-level selector, the retention-days and file-size-limit fields, the current log path with a Copy action, and an "Open log folder" action

#### Scenario: Current values loaded
- **WHEN** the page opens
- **THEN** the log level and the retention/size fields show the stored values

#### Scenario: Log level applied on next start
- **WHEN** the user changes the log level and saves
- **THEN** the value is written to `config.json` and takes effect on the next application start

#### Scenario: Invalid retention or size rejected
- **WHEN** the retention-days field is non-positive or the size-limit field is negative
- **THEN** the value is rejected and an error is shown and the window does not save

### Requirement: Popup screen position

The Hotkeys & Quick Launcher page SHALL offer a "Launcher window position" selector under a "LAUNCHER POSITIONING" section header, with the options "Active monitor — centered", "Active monitor — top-center", "Cursor-relative", and "Primary monitor — centered", defaulting to "Active monitor — centered". Options that target a monitor SHALL follow the "<monitor> — <placement>" label pattern so that both the monitor and the placement the launch window will use are explicit. "Cursor-relative" SHALL keep its pointer-relative wording because it does not target a monitor.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Hotkeys & Quick Launcher"
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

The Appearance page SHALL offer an "Application theme" selector as a row of four preview cards with
the options "Dark Theme", "Light Theme", "Blue Theme", and "System Sync", showing the stored value.
Each card SHALL show a miniature palette preview, the option name, and a short description:
"Fluent Dark (default)", "High contrast daylight profile", "Blue-tinted dark chrome",
and "Follow Windows personalization state" respectively. The selected card SHALL be marked with an
accent ring and a check badge.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
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

The Appearance page SHALL offer an accent color selector as preview cards for the presets Sky, Blue,
Teal, Emerald, Purple, Amber, Indigo, and Tangerine, plus a custom entry. Each card SHALL show a color
dot, the accent name, and its `#RRGGBB` value; the selected card SHALL be marked with the accent ring
and a check inside the dot, and the card header SHALL show the active accent name. The custom
`#RRGGBB` field SHALL be shown only while the custom entry is selected. The selector SHALL remember
the last valid custom accent independently of the named presets and restore it whenever the custom
entry is selected, including after the Settings window is closed and reopened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
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

### Requirement: Scanning preferences apply on save

Saving the scanning preferences SHALL persist them and take effect without restarting the application.

#### Scenario: Persist on save
- **WHEN** the user changes a scanning setting and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves changed watched directories, scan depth, ignored patterns, or the discovery mode
- **THEN** the watcher and scanner use the new configuration immediately, without restarting the application

#### Scenario: Empty watched list stops watching
- **WHEN** the user saves an empty watched-directories list
- **THEN** the watcher stops and the status reports that no watched directories are configured

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards defined by the `settings-theme` capability, and a "Default agent" selector, in addition to the existing agent detection fields. It SHALL show the stored default agent when opened. The "Default agent" selector SHALL offer every loaded agent plus a "None" option, SHALL mark agents that are not installed on the host as disabled entries that cannot be selected, and SHALL refresh the option availability when agent detection runs. The page SHALL offer an agent detection action labelled "Detect" above the agent cards that re-detects every supported agent's installed state and version, and refreshes the agent cards, the availability summary, and the option availability.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the summary cards with the supported and installed counts, the "Default agent" selector, and the agent detection fields

#### Scenario: One card per agent

- **WHEN** the plugin catalog contains agents
- **THEN** the page shows one card for each loaded agent

#### Scenario: Metrics shown

- **WHEN** the page opens
- **THEN** the supported count is the number of loaded agents and the installed count is the number installed on the host

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

### Requirement: Default agent persists and applies

Saving the "Default agent" selector SHALL persist the choice and take effect when the launch window is next shown.

#### Scenario: Persist on save
- **WHEN** the user selects an agent as the default and chooses "Save"
- **THEN** the value is written to `config.json` as `defaultAgentId`

#### Scenario: Cleared default
- **WHEN** the user selects "None" and chooses "Save"
- **THEN** no default agent is stored

#### Scenario: Applied without restart
- **WHEN** a default agent is saved
- **THEN** the launch window preselects it the next time it is shown, without restarting the application

#### Scenario: Removed default agent falls back
- **WHEN** `defaultAgentId` references an agent that is no longer loaded
- **THEN** no agent is preselected and the application starts normally

#### Scenario: Discard on cancel
- **WHEN** the user changes the default agent and chooses "Cancel"
- **THEN** the stored value is unchanged and reopening Settings shows the stored value

### Requirement: Agent card actions

Each agent card SHALL offer "Test", "Update", and "Edit Config" actions, and SHALL report the outcome of the actions it runs. A process matches an agent per the `agent-process-matching` capability, including a process hosted inside a shared runtime whose command line identifies the agent.

#### Scenario: Test action

- **WHEN** the user chooses "Test" on an agent card
- **THEN** the agent's installation status and version are re-evaluated and the result is shown on the card

#### Scenario: Update action

- **WHEN** the user chooses "Update" on an agent card and no process matching that agent is running
- **THEN** the agent's update command runs interactively in the user profile directory

#### Scenario: Update blocked while running

- **WHEN** the user chooses "Update" and a process matching that agent is running
- **THEN** the update does not start and the card reports that it is blocked

#### Scenario: Other running agents do not block the update

- **WHEN** the user chooses "Update" and only processes of other agents are running
- **THEN** the update starts normally

#### Scenario: Edit Config action

- **WHEN** the user chooses "Edit Config" on an agent card
- **THEN** the agent's `plugin.json` is opened with the operating-system default handler

#### Scenario: Action availability reflects capability

- **WHEN** an agent does not declare an update command
- **THEN** its "Update" action is unavailable

#### Scenario: Update blocked by a runtime-hosted agent

- **WHEN** the user chooses "Update" and the agent runs inside a shared runtime host (for example a `node.exe` process started by the agent's npm shim whose command line names the agent's declared npm package), with no executable-name match
- **THEN** the update does not start and the card reports that it is blocked

### Requirement: Release notes from Settings

The Updates page SHALL let the user open the application release notes from the status card.

#### Scenario: Open release notes

- **WHEN** the user activates "View release notes"
- **THEN** the What's New window opens, or the existing one is brought to the front
- **AND** the running version is recorded as seen using the existing release-notes launcher behavior
