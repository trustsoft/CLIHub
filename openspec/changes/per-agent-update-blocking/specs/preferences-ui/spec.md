# preferences-ui (partial): per-agent update blocking

## MODIFIED Requirements

### Requirement: Agent card actions

Each agent card SHALL offer "Test", "Update", and "Edit Config" actions, and SHALL report the outcome of the actions it runs.

#### Scenario: Test action

- **WHEN** the user chooses "Test" on an agent card
- **THEN** the agent's installation status and version are re-evaluated and the result is shown on the card

#### Scenario: Update action

- **WHEN** the user chooses "Update" on an agent card and no process matching that agent is running
- **THEN** the agent's update command runs interactively in the user profile directory

#### Scenario: Update blocked while running

- **WHEN** the user chooses "Update" and a process matching that agent is running
- **THEN** the update does not start and the card reports that it is blocked

#### Scenario: Other running agents do not block the update

- **WHEN** the user chooses "Update" and only processes of other agents are running
- **THEN** the update starts normally

#### Scenario: Edit Config action

- **WHEN** the user chooses "Edit Config" on an agent card
- **THEN** the agent's `plugin.json` is opened with the operating-system default handler

#### Scenario: Action availability reflects capability

- **WHEN** an agent does not declare an update command
- **THEN** its "Update" action is unavailable
