# agent-version Specification (Delta)

## MODIFIED Requirements

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
