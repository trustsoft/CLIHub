## MODIFIED Requirements

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
