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

The system SHALL clear cached detection results on demand, so the next detection re-checks the file system.

#### Scenario: Invalidation clears cached results
- **WHEN** the detection cache is invalidated
- **THEN** the next detection re-checks the file system instead of returning a cached result

#### Scenario: Manual refresh re-evaluates availability
- **WHEN** the user triggers a refresh in the window
- **THEN** cached detection results are cleared so agent availability is re-evaluated
