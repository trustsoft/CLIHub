## ADDED Requirements

### Requirement: Render custom window chrome

The settings window SHALL render without system caption chrome: it SHALL show
its own title bar with the title «Настройки» and a close control, and the
user SHALL be able to move the window by dragging the title bar. Activating
the close control SHALL close the window under the same rules as any other
close action (unsaved edits are discarded, no confirmation).

#### Scenario: Title bar contents

- **WHEN** the settings window opens
- **THEN** it shows a title bar with the title «Настройки» and a close
  control, and no system caption bar

#### Scenario: Move by the title bar

- **WHEN** the user drags the title bar
- **THEN** the window moves with the pointer

#### Scenario: Close control closes the window

- **WHEN** the user activates the title-bar close control with unsaved edits
- **THEN** the window closes without confirmation and stored settings remain
  unchanged
