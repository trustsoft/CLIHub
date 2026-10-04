## ADDED Requirements

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
