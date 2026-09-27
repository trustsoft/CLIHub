# agent-availability-display Specification

## Purpose

Visually de-emphasizes agents that are not available in the currently selected project so that relevant agents stand out, without hiding or disabling them.

## Requirements

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
