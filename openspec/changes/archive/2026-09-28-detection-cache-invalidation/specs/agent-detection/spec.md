## ADDED Requirements

### Requirement: Detection cache invalidation

The system SHALL clear cached detection results on demand, so the next detection re-checks the file system.

#### Scenario: Invalidation clears cached results
- **WHEN** the detection cache is invalidated
- **THEN** the next detection re-checks the file system instead of returning a cached result

#### Scenario: Manual refresh re-evaluates availability
- **WHEN** the user triggers a refresh in the window
- **THEN** cached detection results are cleared so agent availability is re-evaluated
