## ADDED Requirements

### Requirement: Detection result caching

The system SHALL cache detection results and reuse them until a configurable time-to-live (TTL) elapses.

#### Scenario: Cache hit within TTL
- **WHEN** a detection result was computed within the configured TTL
- **THEN** a subsequent detection returns the cached result without re-checking the file system

#### Scenario: Cache expires
- **WHEN** the configured TTL has elapsed since a result was cached
- **THEN** the next detection re-checks the file system and refreshes the cached result

#### Scenario: Default TTL
- **WHEN** no TTL preference is configured
- **THEN** a built-in default TTL is used
