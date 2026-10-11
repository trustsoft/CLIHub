## ADDED Requirements

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, a supported/installed metrics line, and a "Default agent" selector, in addition to the existing agents-probe fields. It SHALL show the stored default agent when opened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the supported and installed counts, the "Default agent" selector, and the agents-probe fields

#### Scenario: One card per agent
- **WHEN** the plugin catalog contains agents
- **THEN** the page shows one card for each loaded agent

#### Scenario: Metrics shown
- **WHEN** the page opens
- **THEN** the supported count is the number of loaded agents and the installed count is the number installed on the host

#### Scenario: Stored default agent loaded
- **WHEN** a default agent has been saved
- **THEN** the "Default agent" selector shows it selected

#### Scenario: No stored default agent
- **WHEN** no default agent preference has been stored
- **THEN** the selector offers a "None" option and no agent is selected by default

### Requirement: Default agent persists and applies

Saving the "Default agent" selector SHALL persist the choice and take effect when the launch window is next shown.

#### Scenario: Persist on save
- **WHEN** the user selects an agent as the default and chooses "Save"
- **THEN** the value is written to `config.json` as `defaultAgentId`

#### Scenario: Cleared default
- **WHEN** the user selects "None" and chooses "Save"
- **THEN** no default agent is stored

#### Scenario: Applied without restart
- **WHEN** a default agent is saved
- **THEN** the launch window preselects it the next time it is shown, without restarting the application

#### Scenario: Removed default agent falls back
- **WHEN** `defaultAgentId` references an agent that is no longer loaded
- **THEN** no agent is preselected and the application starts normally

#### Scenario: Discard on cancel
- **WHEN** the user changes the default agent and chooses "Cancel"
- **THEN** the stored value is unchanged and reopening Settings shows the stored value

### Requirement: Agent card actions

Each agent card SHALL offer "Test", "Update", and "Edit Config" actions, and SHALL report the outcome of the actions it runs.

#### Scenario: Test action
- **WHEN** the user chooses "Test" on an agent card
- **THEN** the agent's installation status and version are re-evaluated and the result is shown on the card

#### Scenario: Update action
- **WHEN** the user chooses "Update" on an agent card and no matching agent process is running
- **THEN** the agent's update command runs interactively in the user profile directory

#### Scenario: Update blocked while running
- **WHEN** the user chooses "Update" and a matching agent process is running
- **THEN** the update does not start and the card reports that it is blocked

#### Scenario: Edit Config action
- **WHEN** the user chooses "Edit Config" on an agent card
- **THEN** the agent's `plugin.json` is opened with the operating-system default handler

#### Scenario: Action availability reflects capability
- **WHEN** an agent does not declare an update command
- **THEN** its "Update" action is unavailable
