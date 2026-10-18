## MODIFIED Requirements

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards defined by the `settings-theme` capability, and a "Default agent" selector, in addition to the existing agents-probe fields. It SHALL show the stored default agent when opened. The "Default agent" selector SHALL offer every loaded agent plus a "None" option, SHALL mark agents that are not installed on the host as disabled entries that cannot be selected, and SHALL refresh the option availability when agent detection runs. The page SHALL offer an agent detection action above the agent cards that re-probes every supported agent and refreshes the agent cards, the availability summary, and the option availability.

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

#### Scenario: Uninstalled agents are not selectable
- **WHEN** the "Default agent" dropdown is opened
- **THEN** agents that are not installed on the host are shown dimmed and disabled
- **AND** they cannot be selected, while "None" and installed agents remain selectable

#### Scenario: Detection refreshes option availability
- **WHEN** an agent detection pass completes while the page is open, whether from a single-agent test or the detect action above the agent cards
- **THEN** the option availability reflects the fresh installed states without reopening the window

#### Scenario: Detect action re-probes every supported agent
- **WHEN** the user invokes the detect action above the agent cards
- **THEN** every supported agent is re-probed against the host
- **AND** the agent cards, the availability summary, and the option availability refresh without reopening the window
- **AND** the action is unavailable while a detection pass is already running
