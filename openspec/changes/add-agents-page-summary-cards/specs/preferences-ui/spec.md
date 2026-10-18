## MODIFIED Requirements

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards defined by the `settings-theme` capability, and a "Default agent" selector, in addition to the existing agents-probe fields. It SHALL show the stored default agent when opened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the summary cards with the supported and installed counts, the "Default agent" selector, and the agents-probe fields

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
