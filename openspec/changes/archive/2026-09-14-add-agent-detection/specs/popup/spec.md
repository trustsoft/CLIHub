# popup Delta

## ADDED Requirements

### Requirement: Detection-driven Agents pane

The **Agents** pane SHALL list only host-installed agents, SHALL display
each agent's version when known, and SHALL enable the `run` action only
when the agent is initialized in the selected project. Availability SHALL
be recomputed live when the selected project changes, and the list SHALL
update when a probe round completes.

#### Scenario: Not installed agent hidden

- **WHEN** the host probe finds an agent not installed
- **THEN** it does not appear in the Agents pane

#### Scenario: Version displayed

- **WHEN** an installed agent has a cached or freshly probed version
- **THEN** the version is shown next to the agent's name

#### Scenario: Run disabled for uninitialized project

- **WHEN** the selected project does not contain the agent's detection markers
- **THEN** the agent is listed but its `run` action is disabled

#### Scenario: Popup opens before the probe round completes

- **WHEN** the popup opens while the startup probe round is still running
- **THEN** the Agents pane shows cached results and updates once the round completes
