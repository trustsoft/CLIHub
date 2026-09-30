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

The window SHALL present the sections "Startup", "Default runtime", "Global hotkey", "Agents probe", and "Updates", and SHALL show the current application version.

#### Scenario: Sections shown
- **WHEN** the Settings window opens
- **THEN** it shows "Start with Windows" and "Show window on startup" toggles, a default-runtime selector (`cmd` / `ps` / `wt`), a global-hotkey capture field, agents-probe `TTL, minutes` and `Timeout, seconds` fields, and a "Check for updates on startup" toggle

#### Scenario: Current values loaded
- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown
- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

### Requirement: Save applies preferences

Saving SHALL persist the preferences and apply them without restarting the application.

#### Scenario: Persist on save
- **WHEN** the user changes one or more settings and chooses "Save"
- **THEN** the values are written to `config.json` and the window closes

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
- **WHEN** the user changes "Show window on startup" and saves
- **THEN** the next application start shows or hides the main window accordingly

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
