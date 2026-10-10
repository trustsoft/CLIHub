## ADDED Requirements

### Requirement: Accent color preference

The system SHALL offer an application accent color preference with the presets Sky (`#4CC2FF`), Windows Blue (`#3B82F6`), Emerald (`#10B981`), Purple (`#8B5CF6`), and Amber (`#F59E0B`), and SHALL accept a custom `#RRGGBB` value, defaulting to Sky.

#### Scenario: Default accent
- **WHEN** no accent has been stored
- **THEN** the Sky accent is used

#### Scenario: Preset selected
- **WHEN** the user selects a preset accent
- **THEN** that accent is applied

#### Scenario: Custom accent accepted
- **WHEN** the user enters a valid `#RRGGBB` value
- **THEN** that accent is applied

#### Scenario: Invalid custom value rejected
- **WHEN** the user enters a value that is not a valid color
- **THEN** the value is rejected and an error is shown and the window does not save

### Requirement: Accent applies live

Saving a new accent SHALL update the application accent immediately, without a restart, and SHALL persist the value for later starts.

#### Scenario: Live application on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the accent-bearing controls update immediately and the value is written to `config.json`

#### Scenario: Applied on next start
- **WHEN** the application starts with a stored accent
- **THEN** the accent is applied before the windows render
