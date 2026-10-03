# agent-availability-display Specification

## Purpose

Visually de-emphasizes agents that are not available in the currently selected project so that relevant agents stand out, without hiding or disabling them.

## Requirements

### Requirement: Hide agents not installed on the host

The system SHALL exclude agents that are not installed on the host from the agent list, before any project-availability dimming or filtering is applied.

#### Scenario: Agent without system markers
- **WHEN** none of an agent's declared system paths exist on the host
- **THEN** that agent is not listed

#### Scenario: Agent with a system marker
- **WHEN** at least one of an agent's declared system paths exists on the host
- **THEN** that agent is eligible for the agent list and is subject to the existing availability display rules

#### Scenario: Filter applies regardless of the project filter
- **WHEN** the host-installed check excludes an agent
- **THEN** the agent stays excluded whether or not the project-availability filter preference is enabled and whether or not a project is selected

#### Scenario: All agents uninstalled
- **WHEN** no agent is installed on the host
- **THEN** the agent list is empty

#### Scenario: Refresh re-evaluates installation
- **WHEN** the user triggers a refresh after installing or removing an agent on the host
- **THEN** the agent list reflects the current installation state

### Requirement: Dim unavailable agents

The system SHALL display agents that are not available in the current project with reduced emphasis.

#### Scenario: Agent unavailable in the project
- **WHEN** a project is selected and an agent is not available in it
- **THEN** that agent's row is shown dimmed

#### Scenario: Agent available in the project
- **WHEN** a project is selected and an agent is available in it
- **THEN** that agent's row is shown at normal emphasis

#### Scenario: No project selected
- **WHEN** no project is selected
- **THEN** all agent rows are shown at normal emphasis

### Requirement: Emphasis updates with the project

The system SHALL recompute emphasis when the current project changes.

#### Scenario: Switching project updates emphasis
- **WHEN** the current project changes
- **THEN** agent emphasis is recomputed for the new project

### Requirement: Dimming is presentation-only

The system SHALL keep dimmed agents selectable and actionable.

#### Scenario: Dimmed agent remains usable
- **WHEN** an agent is dimmed
- **THEN** it can still be selected and its commands can still be invoked

### Requirement: Hide unavailable agents (optional filter)

The system SHALL support an optional mode that hides agents not available in the current project.

#### Scenario: Filter enabled with a project selected
- **WHEN** the filter is enabled and a project is selected
- **THEN** only agents available in that project are listed

#### Scenario: Filter disabled
- **WHEN** the filter is disabled and a project is selected
- **THEN** all agents are listed, with unavailable ones dimmed

#### Scenario: Filter enabled with no project selected
- **WHEN** the filter is enabled and no project is selected
- **THEN** all agents are listed

#### Scenario: Filter precedes dimming
- **WHEN** the filter is enabled
- **THEN** unavailable agents are hidden rather than dimmed

### Requirement: Filter preference persists

The system SHALL persist the filter preference across sessions.

#### Scenario: Preference restored
- **WHEN** the application restarts
- **THEN** the filter reflects the previously saved preference

#### Scenario: Default is off
- **WHEN** no preference has been saved
- **THEN** the filter defaults to disabled (dimming behavior)
