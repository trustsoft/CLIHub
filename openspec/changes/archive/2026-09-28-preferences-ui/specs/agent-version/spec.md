## MODIFIED Requirements

### Requirement: Version caching

The system SHALL cache retrieved versions per agent and reuse them until the cache is invalidated or a configurable time-to-live (TTL) elapses.

#### Scenario: Repeated lookup uses cache
- **WHEN** a version has already been retrieved for an agent within the configured TTL
- **THEN** a subsequent lookup returns the cached value without running the command again

#### Scenario: Cache invalidation
- **WHEN** the cache is invalidated
- **THEN** the next lookup runs the version command again

#### Scenario: Cache expires
- **WHEN** the configured TTL has elapsed since a version was cached
- **THEN** the next lookup runs the version command again

## ADDED Requirements

### Requirement: Probe timeout

The system SHALL bound each version probe with a configurable timeout.

#### Scenario: Default timeout
- **WHEN** no probe timeout preference is configured
- **THEN** a built-in default timeout is used

#### Scenario: Slow probe times out
- **WHEN** a version command does not finish within the configured timeout
- **THEN** the process is terminated and the version is reported as unknown
