# agent-detection Specification

## Purpose

Determines whether an AI agent is installed on the host and whether it is used within a given project, using folder and file markers declared by the plugin.

## Requirements

### Requirement: System installation detection

The system SHALL determine whether an agent is installed on the host using the plugin's declared system paths.

#### Scenario: Marker present
- **WHEN** any declared system path exists as a file or directory
- **THEN** the agent is reported as installed in the system

#### Scenario: No marker present
- **WHEN** none of the declared system paths exist
- **THEN** the agent is reported as not installed in the system

#### Scenario: Environment variables in paths
- **WHEN** a declared system path contains environment variables (for example `%USERPROFILE%`)
- **THEN** the variables are expanded before checking existence

### Requirement: Project availability detection

The system SHALL determine whether an agent is used within a project using the plugin's declared project indicators.

#### Scenario: Indicator present in project
- **WHEN** any declared project indicator exists in the project folder
- **THEN** the agent is reported as available in that project

#### Scenario: No indicator present
- **WHEN** none of the declared project indicators exist in the project folder
- **THEN** the agent is reported as not available in that project

#### Scenario: Missing project folder
- **WHEN** the project folder does not exist
- **THEN** the agent is reported as not available and no exception is raised

### Requirement: Detection requires no execution

The system SHALL perform detection using only file-system checks.

#### Scenario: Detection does not run the agent
- **WHEN** detection is evaluated for an agent
- **THEN** no agent process is started

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

### Requirement: On-demand detection re-evaluation

The system SHALL re-evaluate an agent's host installation state on demand, bypassing the cached
result, and report the outcome. When a multi-card detection pass is aborted — a probe throws, or
the pass is abandoned because the owning view is disposed mid-pass — the system SHALL settle every
card consistently: a card the pass had already applied a fresh probe result to SHALL keep that
fresh result (installation state, version, and update-availability indicator together), and a card
the pass had not yet resolved SHALL be restored to its complete pre-pass state (installation state,
version, and update-availability indicator together), so no card is left announcing an in-progress
probe that will never complete and no card is left with an installation state that disagrees with
its shown version or update indicator.

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

#### Scenario: An aborted pass keeps already-settled cards' fresh results
- **WHEN** a multi-card detection pass has already applied a fresh probe result to one card and a
  later card's probe then throws
- **THEN** the already-settled card keeps its fresh installation state and version exactly as the
  probe reported them
- **AND** its update-availability indicator is consistent with that state — preserved when the
  agent is installed and cleared when it is not, as a probe result always settles it
- **AND** it is not rolled back to its pre-pass state

#### Scenario: An aborted pass restores every unresolved card completely
- **WHEN** a multi-card detection pass's probe throws before a card's result has been applied
- **THEN** every card the pass had not yet resolved is restored to its complete pre-pass
  installation state, version, and update-availability indicator together
- **AND** no restored card is left showing a version or update indicator inconsistent with its
  restored installation state

#### Scenario: A pass abandoned by disposal settles every unresolved card
- **WHEN** a multi-card detection pass is abandoned because the owning view is disposed before every
  card has been probed
- **THEN** every card the pass had not yet resolved is restored to its complete pre-pass state exactly
  as it would be for a thrown probe
- **AND** no card is left showing the in-progress "Detecting" state
