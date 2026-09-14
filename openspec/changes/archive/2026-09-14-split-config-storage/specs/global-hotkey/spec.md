## MODIFIED Requirements

### Requirement: Register global hotkey

The system SHALL register a global hotkey on application startup. The combination
comes from `settings.json`, defaulting to `Ctrl+Alt+Space`.

#### Scenario: Successful registration

- **WHEN** the application starts and the combination is not taken by another application
- **THEN** the hotkey is registered and fires while the application runs

#### Scenario: Combination already taken

- **WHEN** the combination is already registered by another application
- **THEN** registration does not happen, the application reports the problem and keeps running without the hotkey
