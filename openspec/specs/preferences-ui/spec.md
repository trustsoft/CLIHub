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

### Requirement: Save applies preferences

Saving SHALL validate a typed settings draft, apply all required system preferences, persist the draft only after required application succeeds, and close the window only after persistence succeeds. If any required application or persistence step fails, the stored preferences SHALL remain unchanged and the user SHALL receive an error without the window closing. Any system changes made before a failure SHALL be rolled back to their previous values where the platform boundary supports rollback.

If the project scanner cannot reload with the newly saved scanning configuration after a successful write, the save SHALL still succeed and the scanner SHALL retry the reload on the next scan or application restart. A scanner reload failure SHALL NOT roll back system preferences that were already applied and persisted.

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

#### Scenario: Scanner reload failure after successful write does not roll back

- **WHEN** the user saves settings including scanning configuration and the project scanner fails to reload after the write
- **THEN** the settings are saved, system preferences are applied, and the window closes
- **AND** the scanner will retry the reload on the next scan or application restart
- **AND** previously applied system changes are not rolled back

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

The Settings page header for "Updates" SHALL show the current version and stable channel as chips next to the page title, an update-status chip indicating either that the build is current or that an update is available, a "Check for updates" action, and a "Release notes" action on the title row after the channel chip. The page body SHALL offer the "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-releases" controls. The page SHALL show the current application version and check status without commit, target, or agent-protocol detail.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Updates"
- **THEN** the page header shows the version and stable channel chips, the update-status chip, and the "Check for updates" action
- **AND** the title row shows "Release notes" after the channel chip
- **AND** the page body shows "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-releases" toggles

#### Scenario: Defaults

- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on and "Notify about pre-releases" is off

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

### Requirement: Application theme selector

The Theme & Accent page SHALL offer a "Theme" selector as a row of four preview cards with the options "Dark", "Light", "Deep Blue", and "System", showing the stored value. Each card SHALL show a miniature palette preview and the option name; the palette description — "Default palette", "Bright, high-contrast palette", "Dark with a blue tint", and "Follows the Windows light/dark mode" respectively — SHALL be exposed as the card's tooltip and accessible help text instead of visible body text. The selected card SHALL be marked by painting its border with the accent color; the selector SHALL NOT draw an additional selection ring or check badge.

The accessible name for each theme option SHALL include the word "theme" to provide screen-reader users with full context: "Dark theme", "Light theme", "Deep Blue theme", and "System theme".

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the theme selector offers "Dark", "Light", "Deep Blue", and "System" as preview cards with their option names
- **AND** each card exposes its palette description as a tooltip
- **AND** each card's accessible name includes "theme" for screen readers
- **AND** the stored value is selected and marked by its accent-colored border

#### Scenario: Persist on save

- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected palette immediately, without a restart

#### Scenario: Restart note

- **WHEN** the theme selector is shown
- **THEN** the "System" card's tooltip description indicates that it follows Windows personalization state

### Requirement: Accent color selector

The Theme & Accent page SHALL offer an accent color selector as preview cards for the presets Sky, Blue,
Teal, Emerald, Purple, Amber, Indigo, and Tangerine, plus a custom entry. Each card SHALL show a color
dot, the accent name, and its `#RRGGBB` value; the selected card SHALL be marked by painting its border
with the accent color, without a check inside the dot, and the card header SHALL show the active accent
name. The custom `#RRGGBB` field SHALL be shown only while the custom entry is selected. The selector
SHALL remember the last valid custom accent independently of the named presets and restore it whenever
the custom entry is selected, including after the Settings window is closed and reopened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the accent selector offers Sky, Blue, Teal, Emerald, Purple, Amber, Indigo, and Tangerine as
  preview cards with their hex values
- **AND** the stored value is selected and marked by its accent-colored border
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

The Project Scanning page SHALL offer a watched-directories list with add, per-directory scan, and remove actions, a "Scan depth" field, an "Ignored patterns" list, a "Discovery" selector with the options "Git only", "Agents only", and "Git and agents", and a read-only background watcher status. The per-directory scan action SHALL rescan only that directory's watched root, unlike the page-header action that rescans every saved root. Each watched-directory row SHALL show, beneath its path, a depth chip and a discovery chip that open a menu of allowed values, plus the number of discovered repositories beneath that directory. The "Scan depth" and "Discovery" fields SHALL act as defaults for a directory added from now on and SHALL NOT change the settings of existing rows. The page SHALL show the stored values when opened.

Changes to the scanning settings SHALL apply immediately, without requiring the footer Save: adding or removing a watched directory, changing a row's depth or discovery chip, changing the default depth or discovery mode, and adding or removing an ignored pattern SHALL each persist atomically, reconfigure the background scanner, and trigger a scan. A newly added watched directory SHALL therefore be scanned while the page is open, the page-header scan action and the summary cards SHALL always describe the list shown on the page, and the page SHALL state that scanning changes apply immediately. The footer Save and Cancel SHALL continue to govern the remaining settings, and Cancel SHALL NOT revert scanning changes that were already applied.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Project Scanning"
- **THEN** it shows the watched-directories list with add, scan, and remove actions, the depth and discovery defaults, the "Ignored patterns" list, and the watcher status
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

#### Scenario: Adding a watched directory with apply failure reports error

- **WHEN** the user adds a watched directory and the immediate apply fails (persistence or scanner reconfiguration error)
- **THEN** the directory is not added to the list
- **AND** the validation error describes the failure
- **AND** the input text is not cleared so the user can retry or correct

#### Scenario: Adding a duplicate watched directory reports a message

- **WHEN** the user adds a folder that is already in the watched-directories list (by normalized path), whether by typing the path or by browsing to it
- **THEN** no new row is added and the watched-root count is unchanged
- **AND** an explicit message states that the folder is already watched
- **AND** the input the user entered or selected is not cleared

#### Scenario: Scanner reload failure after a successful scanning apply

- **WHEN** the user changes a scanning setting and the project scanner fails to reload after the write
- **THEN** the change remains stored
- **AND** the page does not report an apply failure
- **AND** the scanner retries the reload on the next scan or application restart

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
- **THEN** the detail line shows "Check updates"

#### Scenario: UPDATES card detail when all up to date

- **WHEN** the UPDATES card value is "None"
- **THEN** the detail line shows "All installed agents up to date" or equivalent

#### Scenario: UPDATES card detail when updates available

- **WHEN** the UPDATES card value is a count N
- **THEN** the detail line shows "N of M agents have updates" with singular/plural handling: "1 of M agent(s) has an update" when N=1, otherwise "N of M agent(s) have updates", where the "agent(s)" part pluralizes based on M (the installed count)

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

- **WHEN** the user activates "Release notes"
- **THEN** the What's New window opens, or the existing one is brought to the front
- **AND** the running version is recorded as seen using the existing release-notes launcher behavior

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
- **THEN** the sidebar item shows the Process icon (IconGlyphs.Process) and the page description "Caching and timeout behavior for agent detection."

#### Scenario: DEFAULTS card shown

- **WHEN** the Agent Detection page header is visible
- **THEN** the third summary card is labelled "DEFAULTS" with the Diagnostic icon and shows the built-in default values for both detection parameters

#### Scenario: DEFAULTS card value

- **WHEN** the DEFAULTS card is visible
- **THEN** the value line shows the built-in defaults formatted as "N min · M sec" (e.g., "15 min · 10 sec") where N is the TTL default and M is the timeout default

#### Scenario: DEFAULTS card detail

- **WHEN** the DEFAULTS card is visible
- **THEN** the detail line shows "Cache TTL · Version timeout"

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
