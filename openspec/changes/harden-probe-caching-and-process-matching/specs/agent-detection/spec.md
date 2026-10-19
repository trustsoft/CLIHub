# agent-detection (partial): scoped detection-cache invalidation

## MODIFIED Requirements

### Requirement: Detection cache invalidation

The system SHALL clear cached detection results on demand, either for all agents or for a single agent, so the next detection for the affected agents re-checks the file system. A single-agent clear SHALL remove both the system and project results cached for that agent and SHALL preserve the results cached for every other agent.

#### Scenario: Invalidation clears cached results
- **WHEN** the detection cache is invalidated
- **THEN** the next detection re-checks the file system instead of returning a cached result

#### Scenario: Manual refresh re-evaluates availability
- **WHEN** the user triggers a refresh in the window
- **THEN** cached detection results are cleared so agent availability is re-evaluated

#### Scenario: Single-agent invalidation re-checks that agent
- **WHEN** the cached detection results of one agent are invalidated for an on-demand probe
- **THEN** the next system and project detection for that agent re-checks the file system instead of returning a cached result

#### Scenario: Single-agent invalidation preserves other agents
- **WHEN** the cached detection results of one agent are invalidated
- **THEN** cached system and project results for every other agent remain valid and are returned without re-checking the file system
