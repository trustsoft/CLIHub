## MODIFIED Requirements

### Requirement: Global hotkey registration

The system SHALL register a global keyboard shortcut (Ctrl+Shift+Space by default) using the Windows hotkey API.

#### Scenario: Hotkey registration at startup
- **WHEN** the application starts
- **THEN** the configured hotkey is registered with Windows

#### Scenario: Hotkey conflict handling
- **WHEN** the hotkey is already registered by another application
- **THEN** registration fails gracefully, the failure is logged, and the application continues running without a hotkey

### Requirement: Configurable hotkey combination

The system SHALL allow users to configure the hotkey combination in preferences and apply a change without restarting.

#### Scenario: Default hotkey
- **WHEN** no custom hotkey is configured
- **THEN** `Ctrl+Shift+Space` is used as the default

#### Scenario: Custom hotkey setting
- **WHEN** a user sets a custom hotkey in preferences and restarts
- **THEN** the new combination is registered

#### Scenario: Custom hotkey applied without restart
- **WHEN** a user saves a custom hotkey in Settings
- **THEN** the previously registered hotkey is unregistered and the new combination is registered immediately

### Requirement: Hotkey modifier validation

The system SHALL validate that a hotkey combination includes at least one modifier key.

#### Scenario: Modifier key requirement
- **WHEN** a hotkey is configured
- **THEN** it must include at least one of Ctrl, Shift, Alt, or Win

#### Scenario: Invalid hotkey falls back to default
- **WHEN** a hotkey without modifiers (or an unparsable value) is configured
- **THEN** the value is rejected, a warning is logged, and the default `Ctrl+Shift+Space` is used
