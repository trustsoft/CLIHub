## ADDED Requirements

### Requirement: Popup screen position defaults safely

The configuration boundary SHALL parse the stored popup screen position token and default to "Center of active monitor" for missing or unrecognized values, without preventing the application from starting.

#### Scenario: Missing or unknown token uses the default
- **GIVEN** a stored configuration has no popup screen position or an unrecognized value
- **WHEN** the configuration is loaded
- **THEN** "Center of active monitor" is used and the application starts normally

#### Scenario: Known token round-trips
- **GIVEN** a stored popup screen position token
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same position is used
