# preferences-ui Specification

## Purpose

Provides a Settings window where users view and change application preferences — launch runtime, global hotkey, agent probe caching/timeout, and the startup update check — without editing `config.json` by hand.

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
- **THEN** the "Start with Windows" and "Start minimized to tray" toggles appear on "General & Startup", the global-hotkey capture field on "Hotkeys & Launchers", the default-runtime selector on "Terminal Profiles", the agents-probe `TTL, minutes` and `Timeout, seconds` fields on "CLI Agents", the path display selector on "Appearance", and the "Check for updates on startup" toggle and manual update check on "Updates"

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
- **THEN** the next application start shows the main window when the toggle is off and starts in the system tray when it is on

#### Scenario: Invalid draft does not save

- **WHEN** the user chooses "Save" with an invalid hotkey or invalid non-positive probe value
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

The probe fields SHALL accept blank values meaning "use defaults" and SHALL reject invalid numbers.

#### Scenario: Blank uses defaults
- **WHEN** either probe field is left empty and saved
- **THEN** the default for that field is used

#### Scenario: Invalid number rejected
- **WHEN** a probe field contains a non-numeric or non-positive value
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

The Terminal Profiles page SHALL offer the runtime hosts PowerShell, PowerShell 7, Command Prompt, and Git Bash, and SHALL default to PowerShell when no runtime has been stored.

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

### Requirement: Updates page options

The Updates page SHALL offer the "Check for updates on startup" toggle, the manual update check, and the toggles "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions", and SHALL show the current application version together with the check status.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Updates"
- **THEN** it shows "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" toggles, and the manual update check

#### Scenario: Defaults
- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on and "Notify about pre-release versions" is off

#### Scenario: Persist on save
- **WHEN** the user changes an update toggle and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Version status simplified
- **WHEN** the Updates page is shown
- **THEN** the version status shows the current version and the check status (up to date, or update available with the version) without commit, target, or agent-protocol detail

### Requirement: Telemetry and Logs page options

The Telemetry & Logs page SHALL offer a log-level selector (Verbose, Debug, Information, Warning, Error), a retention-days field, a file-size-limit field, the current log file path with a Copy action, and an "Open log folder" action.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Telemetry & Logs"
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

The Hotkeys & Launchers page SHALL offer a "Popup screen position" selector with the options "Center of active monitor", "Top-center of active monitor", "Cursor-relative", and "Primary screen", defaulting to "Center of active monitor".

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Hotkeys & Launchers"
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

### Requirement: Application theme selector

The Appearance page SHALL offer an "Application theme" selector with the options "Dark", "Light", and "System", showing the stored value.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the application theme selector offers "Dark", "Light", and "System" with the stored value selected

#### Scenario: Persist on save
- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and takes effect on the next start

#### Scenario: Restart note
- **WHEN** the theme selector is shown
- **THEN** a hint indicates that the theme applies after a restart

### Requirement: Accent color selector

The Appearance page SHALL offer an accent color selector with the preset swatches and a custom `#RRGGBB` field, showing the stored value.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the accent selector offers the presets and a custom field with the stored value selected

#### Scenario: Persist and apply on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the value is written to `config.json` and applied immediately
