## ADDED Requirements

### Requirement: Log preferences default safely

The configuration boundary SHALL default the log preferences when they are absent from a stored document and SHALL round-trip them: `logRetentionDays` default `7` and `logFileSizeLimitMb` default `0`.

#### Scenario: Absent fields use defaults
- **GIVEN** a stored configuration has no log retention or size-limit fields
- **WHEN** the configuration is loaded
- **THEN** the retention is 7 days and the size limit is unlimited

#### Scenario: Log preferences round-trip
- **GIVEN** the log retention and size-limit preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value
