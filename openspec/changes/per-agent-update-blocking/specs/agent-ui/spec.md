# agent-ui (partial): per-agent update blocking

## MODIFIED Requirements

### Requirement: Agent card action availability

The card SHALL present actions appropriate to the agent's declared capabilities and current state.

#### Scenario: Test always available

- **WHEN** an agent card is shown
- **THEN** "Test" is available

#### Scenario: Update requires the update command

- **WHEN** an agent does not declare an `update` command
- **THEN** "Update" is unavailable on its card

#### Scenario: Update blocked by a running agent

- **WHEN** a process matching the agent being updated is running
- **THEN** "Update" is unavailable and the card explains that the running agent blocks it

#### Scenario: Other running agents do not block an update

- **WHEN** processes of other agents are running and the agent being updated has no running processes
- **THEN** "Update" is available

#### Scenario: Edit Config requires a plugin file

- **WHEN** the agent's `plugin.json` path is known
- **THEN** "Edit Config" is available; otherwise it is unavailable
