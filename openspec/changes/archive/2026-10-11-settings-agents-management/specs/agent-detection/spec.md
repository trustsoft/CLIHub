## ADDED Requirements

### Requirement: On-demand detection re-evaluation

The system SHALL re-evaluate an agent's host installation state on demand, bypassing the cached result, and report the outcome.

#### Scenario: Manual probe bypasses the cache
- **WHEN** a manual probe is requested for an agent
- **THEN** the host installation state is recomputed from the file system rather than read from the cache

#### Scenario: Probe reports installed
- **WHEN** a manual probe finds a declared system path present
- **THEN** the agent is reported as installed on the host

#### Scenario: Probe reports not installed
- **WHEN** a manual probe finds none of the declared system paths present
- **THEN** the agent is reported as not installed on the host

#### Scenario: Probe requires no project
- **WHEN** a manual probe is requested and no project is selected
- **THEN** the host installation state is still evaluated
