# settings-window Specification

## Purpose
Provides the settings window surface for editing application-wide preferences
(runtime, hotkey, probe tuning, update check) and for triggering an update
check on demand, so users configure CLIHub without editing JSON files by hand.

## Requirements

### Requirement: Open the settings window

The system SHALL open the settings window when the user activates the settings
item in the tray menu. The system SHALL keep at most one settings window:
activating the settings item while the window is open SHALL activate the
existing window instead of opening another one.

#### Scenario: Opened from the tray menu

- **WHEN** the user activates the settings item in the tray menu
- **THEN** the settings window opens showing the current saved values

#### Scenario: Already open

- **WHEN** the user activates the settings item while the settings window is
  already open
- **THEN** the existing window is activated and no second window appears

### Requirement: Load current settings when opened

The window SHALL show the current values of the runtime, the hotkey, the probe
TTL and timeout, and the update-check toggle from the settings store each time
it is opened.

#### Scenario: Values reflect the store

- **WHEN** the settings window opens
- **THEN** it shows the runtime, hotkey, probe TTL and timeout, and
  update-check toggle as currently stored

#### Scenario: Reopen shows saved changes

- **WHEN** the user saves changes, closes the window, and opens it again
- **THEN** the window shows the saved values

### Requirement: Edit settings sections

The window SHALL let the user edit the default runtime (cmd, ps, wt), the
hotkey combination, the probe TTL and timeout, and the update-check-on-startup
toggle.

Hotkey editing SHALL use a capture control: while the control is focused, the
next key combination is captured and shown in canonical form; pressing only
modifier keys is ignored; Esc cancels the capture without closing the window.

#### Scenario: Runtime choice

- **WHEN** the user selects a runtime in the window
- **THEN** the window shows it as the default runtime to be saved

#### Scenario: Hotkey capture

- **WHEN** the capture control is focused and the user presses Ctrl+Alt+K
- **THEN** the control shows the combination in canonical form

#### Scenario: Modifier-only press ignored

- **WHEN** the capture control is focused and the user presses and releases
  only Ctrl
- **THEN** the shown combination does not change

#### Scenario: Esc cancels the capture

- **WHEN** the user presses Esc while the capture control has focus
- **THEN** the capture ends, the shown combination is unchanged, and the
  window stays open

### Requirement: Save with validation

The window SHALL write all edited values to the settings store when the user
activates «Save». The system SHALL block the save and report the problem
in the window when the probe TTL or timeout is not a positive number, or when
the hotkey combination cannot be parsed.

#### Scenario: Valid save

- **WHEN** the user activates «Save» with valid values
- **THEN** `settings.json` contains the edited runtime, hotkey, probe, and
  update values

#### Scenario: Non-positive probe value blocks the save

- **WHEN** the user activates «Save» with a zero or negative probe TTL or
  timeout
- **THEN** nothing is saved and the problem is reported in the window

#### Scenario: Unparseable hotkey blocks the save

- **WHEN** a save is attempted with a hotkey string that cannot be parsed
- **THEN** nothing is saved and the problem is reported in the window

### Requirement: Saved settings apply without restart

After a successful save the system SHALL apply the new values immediately:
subsequent agent launches use the saved runtime, subsequent probe rounds use
the saved probe tuning, and the popup opens via the saved hotkey. The
update-check toggle takes effect at the next application start.

#### Scenario: Runtime applies to the next launch

- **WHEN** the user saves a runtime change and then launches an agent
- **THEN** the terminal opens in the saved runtime

#### Scenario: Probe tuning applies to the next round

- **WHEN** the user saves probe tuning changes and a probe round runs
- **THEN** the round uses the saved TTL and timeout

#### Scenario: Update toggle defers to the next start

- **WHEN** the user disables check-on-startup and saves
- **THEN** the current session keeps running and the next application start
  performs no update check

### Requirement: Render custom window chrome

The settings window SHALL render without system caption chrome: it SHALL show
its own title bar with the title «Settings» and a close control, and the
user SHALL be able to move the window by dragging the title bar. Activating
the close control SHALL close the window under the same rules as any other
close action (unsaved edits are discarded, no confirmation).

#### Scenario: Title bar contents

- **WHEN** the settings window opens
- **THEN** it shows a title bar with the title «Settings» and a close
  control, and no system caption bar

#### Scenario: Move by the title bar

- **WHEN** the user drags the title bar
- **THEN** the window moves with the pointer

#### Scenario: Close control closes the window

- **WHEN** the user activates the title-bar close control with unsaved edits
- **THEN** the window closes without confirmation and stored settings remain
  unchanged

### Requirement: Close discards unsaved edits

The window SHALL close on the user's close action without a confirmation
dialog. Unsaved edits are discarded and stored settings remain unchanged.

#### Scenario: Close with unsaved edits

- **WHEN** the user closes the window with unsaved edits
- **THEN** the window closes, the application keeps running, and the stored
  settings are unchanged
