## Purpose

Provides a Settings window where users view and change application preferences — startup behavior, hotkey and popup position, terminal runtime, agents and probing, project scanning, appearance, logging, and updates — without editing `config.json` by hand.

## MODIFIED Requirements

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

#### Scenario: Adding a watched directory with apply failure reports error

- **WHEN** the user adds a watched directory and the immediate apply fails (persistence or scanner reconfiguration error)
- **THEN** the directory is not added to the list
- **AND** the validation error describes the failure
- **AND** the input text is not cleared so the user can retry or correct

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

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards defined by the `settings-theme` capability, and a "Default agent" selector, in addition to the existing agent detection fields. It SHALL show the stored default agent when opened. The "Default agent" selector SHALL offer every loaded agent plus a "None" option, SHALL mark agents that are not installed on the host as disabled entries that cannot be selected, and SHALL refresh the option availability when agent detection runs. The page SHALL offer an agent detection action labelled "Detect" above the agent cards that re-detects every supported agent's installed state and version, and refreshes the agent cards, the availability summary, and the option availability.

The summary cards showing supported and installed agent counts SHALL update immediately when agent detection completes, without requiring the Settings window to be closed and reopened.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the summary cards with the supported and installed counts, the "Default agent" selector, and the agent detection fields

#### Scenario: One card per agent

- **WHEN** the plugin catalog contains agents
- **THEN** the page shows one card for each loaded agent

#### Scenario: Metrics shown

- **WHEN** the page opens
- **THEN** the supported count is the number of loaded agents and the installed count is the number installed on the host

#### Scenario: Metrics update when Detect completes

- **WHEN** agent detection completes while the Settings window is open (from the Detect action or a per-card test)
- **THEN** the supported and installed counts on the Project Scanning page summary cards update immediately without reopening the window

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
