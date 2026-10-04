# agent-version Specification

## Purpose

Retrieves each agent's version via its own version command and presents it in the agent list without blocking the UI or requiring a selected project.

## Requirements

### Requirement: Version lookup

The system SHALL retrieve an agent's version by running its declared `version` command and capturing the output.

#### Scenario: Version command defined
- **WHEN** an agent declares a `version` command
- **THEN** the command is executed and the version number found in its output is returned (for example `1.0.88` from `GitHub Copilot CLI 1.0.88.`)

#### Scenario: No version number in output
- **WHEN** the output contains no version-like number
- **THEN** the first non-empty line of the output is returned as-is

#### Scenario: No version command
- **WHEN** an agent does not declare a `version` command
- **THEN** no process is started and the version is reported as unknown

#### Scenario: Version command fails
- **WHEN** the version command cannot start or exits non-zero
- **THEN** the version is reported as unknown and the failure is logged

### Requirement: Project-independent lookup

The system SHALL retrieve versions without requiring a selected project.

#### Scenario: No current project
- **WHEN** no project is selected and versions are requested
- **THEN** versions are still retrieved

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

### Requirement: Probe timeout

The system SHALL bound each version probe with a configurable timeout.

#### Scenario: Default timeout
- **WHEN** no probe timeout preference is configured
- **THEN** a built-in default timeout is used

#### Scenario: Slow probe times out
- **WHEN** a version command does not finish within the configured timeout
- **THEN** the process is terminated and the version is reported as unknown

### Requirement: Non-blocking presentation

The system SHALL render the agent list before versions resolve and update it as versions become available.

#### Scenario: List appears immediately
- **WHEN** the agent list is shown
- **THEN** agents are displayed before their versions have been retrieved

#### Scenario: Versions fill in
- **WHEN** version retrieval completes for an agent
- **THEN** the displayed version for that agent is updated

### Requirement: Unknown version display

The system SHALL display a clear placeholder when a version is not available.

#### Scenario: Unknown shown
- **WHEN** an agent's version is unknown
- **THEN** the list shows a placeholder (for example, `unknown`) for that agent

### Requirement: Concurrent version probes are coalesced

The system SHALL ensure that concurrent requests for the same agent version share one in-flight probe rather than starting duplicate processes.

#### Scenario: Concurrent cache miss
- **WHEN** multiple callers request the same uncached agent version concurrently
- **THEN** one version command runs and all callers receive the same completed result

#### Scenario: Different agents probe independently
- **WHEN** callers request versions for different agents concurrently
- **THEN** each agent may run its own version command without being blocked by another agent's probe

### Requirement: Stale version population does not update the current list

The system SHALL prevent a version result from an obsolete agent-list population pass from being applied to a newer list state.

#### Scenario: Refresh supersedes a population pass
- **WHEN** the agent list is refreshed while version probes from an earlier pass are still running
- **THEN** results from the earlier pass are ignored for items no longer belonging to the current pass

#### Scenario: Current population completes
- **WHEN** a version probe from the current population pass completes
- **THEN** its result updates the corresponding current agent item
