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

The system SHALL prevent a version result from an obsolete agent-list population pass from being applied to a newer list state. An obsolete pass — one superseded by a newer list refresh or abandoned when the surface that started it goes away — SHALL be cancelled so that it starts no further version lookups or availability checks for that list; a version command already in flight MAY complete and cache its result, but that result SHALL NOT be applied to the abandoned surface. An unexpected population-pass failure SHALL be logged and SHALL leave the currently displayed list state unchanged.

#### Scenario: Refresh supersedes a population pass

- **WHEN** the agent list is refreshed while version probes from an earlier pass are still running
- **THEN** results from the earlier pass are ignored for items no longer belonging to the current pass

#### Scenario: Current population completes

- **WHEN** a version probe from the current population pass completes
- **THEN** its result updates the corresponding current agent item

#### Scenario: Surface exit cancels its population pass

- **WHEN** the surface showing the agent list is closed while its population pass is still running
- **THEN** the pass is cancelled and starts no further version lookups or availability checks
- **AND** a version command already in flight may complete and cache its result without updating the closed surface

#### Scenario: Population pass failure is logged

- **WHEN** a population pass fails with an unexpected error
- **THEN** the failure is logged through the application logger
- **AND** the list keeps its current state, and results of other agents in the pass are still applied

### Requirement: On-demand version re-probe

The system SHALL re-probe an agent's version on demand, bypassing the cached value and its time-to-live, and report the freshly probed result. The re-probe SHALL invalidate only the requested agent's cached version; cached versions of other agents SHALL remain valid and SHALL be reused without running their commands.

#### Scenario: Manual re-probe bypasses the cache
- **WHEN** a manual version probe is requested for an agent whose version is cached
- **THEN** the version command runs again instead of returning the cached value

#### Scenario: Fresh version reported
- **WHEN** a manual re-probe succeeds
- **THEN** the freshly probed version is reported

#### Scenario: Unknown version reported
- **WHEN** a manual re-probe cannot produce a version
- **THEN** the version is reported as unknown

#### Scenario: Re-probe has no project requirement
- **WHEN** a manual version probe is requested and no project is selected
- **THEN** the version is still probed

#### Scenario: Re-probe invalidates only the requested agent
- **WHEN** a manual version probe is requested for an agent
- **THEN** the requested agent's cached version is invalidated and its version command runs again
- **AND** cached versions of other agents remain valid

#### Scenario: Unrelated agents stay cache-served
- **WHEN** versions of other agents were resolved within their time-to-live and one agent is re-probed
- **THEN** the other agents' version commands do not run again for their next lookup
