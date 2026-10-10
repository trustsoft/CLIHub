## ADDED Requirements

### Requirement: Theme preference defaults safely

The configuration boundary SHALL parse the stored theme token and default to "Dark" for missing or unrecognized values, and SHALL round-trip it.

#### Scenario: Missing or unknown token uses dark
- **GIVEN** a stored configuration has no theme token or an unrecognized value
- **WHEN** the configuration is loaded
- **THEN** the dark theme is used and the application starts normally

#### Scenario: Theme token round-trips
- **GIVEN** a stored theme token
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same theme is used
