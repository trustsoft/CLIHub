# tray-menu Delta

## MODIFIED Requirements

### Requirement: Menu items

The tray menu SHALL contain a settings item that opens the settings window, an
exit item that terminates the application, and SHALL contain an update item
only while an update is ready to apply.

#### Scenario: No update ready

- **WHEN** the user opens the tray menu while no update is ready to apply
- **THEN** the menu contains the settings item and the exit item

#### Scenario: Update becomes ready during the session

- **WHEN** an update becomes ready to apply after the application started, and
  the user opens the tray menu
- **THEN** the menu contains the update item alongside the settings and exit
  items, and activating the update item starts applying the update

#### Scenario: Settings

- **WHEN** the user activates the settings item
- **THEN** the settings window opens

#### Scenario: Exit

- **WHEN** the user activates the exit item
- **THEN** the application terminates
