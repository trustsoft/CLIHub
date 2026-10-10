## ADDED Requirements

### Requirement: Accent color defaults safely

The configuration boundary SHALL default the accent color to the Sky value when the stored value is missing or is not a valid color, and SHALL round-trip a valid value.

#### Scenario: Missing or invalid value uses the default
- **GIVEN** a stored configuration has no accent or an unparsable value
- **WHEN** the configuration is loaded
- **THEN** the Sky accent is used and the application starts normally

#### Scenario: Accent round-trips
- **GIVEN** a valid stored accent
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same accent is used
