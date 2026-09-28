## MODIFIED Requirements

### Requirement: Configurable hotkey combination

The system SHALL allow users to configure the hotkey combination in preferences and apply a change without restarting.

#### Scenario: Default hotkey
- **WHEN** no custom hotkey is configured
- **THEN** `Ctrl+Shift+A` is used as the default

#### Scenario: Custom hotkey setting
- **WHEN** a user sets a custom hotkey in preferences and restarts
- **THEN** the new combination is registered

#### Scenario: Custom hotkey applied without restart
- **WHEN** a user saves a custom hotkey in Settings
- **THEN** the previously registered hotkey is unregistered and the new combination is registered immediately
