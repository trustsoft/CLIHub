## ADDED Requirements

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
