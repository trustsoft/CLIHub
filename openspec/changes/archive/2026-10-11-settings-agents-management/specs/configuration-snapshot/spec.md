## ADDED Requirements

### Requirement: Default agent preference defaults safely and round-trips

The configuration boundary SHALL persist an optional `defaultAgentId` preference, default it to null when absent, and treat an unrecognized value as no default without preventing the application from starting. Adding this field SHALL NOT change the numeric `schemaVersion`.

#### Scenario: Absent field has no default agent
- **GIVEN** a stored configuration has no `defaultAgentId`
- **WHEN** the configuration is loaded
- **THEN** there is no default agent

#### Scenario: Default agent round-trips
- **GIVEN** a default agent id is stored
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same default agent id is restored
