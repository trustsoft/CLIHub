## ADDED Requirements

### Requirement: Agent card content

An agent's management card SHALL present its declared identity and state: display name and logo, host installation status, installed version, plugin folder path, declared command set, and declared detection markers.

#### Scenario: Identity shown
- **WHEN** an agent card is shown
- **THEN** it shows the agent's display name and logo (falling back to the default logo)

#### Scenario: Installation status shown
- **WHEN** an agent card is shown
- **THEN** it indicates whether the agent is installed on the host

#### Scenario: Installed version shown
- **WHEN** an agent card is shown
- **THEN** it shows the installed version, or an "unknown" placeholder when the version is not available

#### Scenario: Plugin path shown
- **WHEN** an agent card is shown
- **THEN** it shows the folder the agent's `plugin.json` was loaded from

#### Scenario: Command set shown
- **WHEN** an agent card is shown
- **THEN** it lists which named commands the agent declares (launch, resume, version, update)

#### Scenario: Detection markers shown
- **WHEN** an agent card is shown
- **THEN** it lists the agent's declared host system paths and project indicators

#### Scenario: No detection markers
- **WHEN** an agent declares no detection markers
- **THEN** the card indicates that none are declared without showing an error

### Requirement: Agent card action availability

The card SHALL present actions appropriate to the agent's declared capabilities and current state.

#### Scenario: Test always available
- **WHEN** an agent card is shown
- **THEN** "Test" is available

#### Scenario: Update requires the update command
- **WHEN** an agent does not declare an `update` command
- **THEN** "Update" is unavailable on its card

#### Scenario: Update blocked by a running agent
- **WHEN** a matching agent process is running
- **THEN** "Update" is unavailable and the card explains that a running agent blocks it

#### Scenario: Edit Config requires a plugin file
- **WHEN** the agent's `plugin.json` path is known
- **THEN** "Edit Config" is available; otherwise it is unavailable
