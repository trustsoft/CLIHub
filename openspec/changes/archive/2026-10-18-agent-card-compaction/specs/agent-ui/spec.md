## REMOVED Requirements

### Requirement: Agent card content

**Reason**: The card no longer presents the plugin folder path or the declared detection markers; that reference information stays in the agent's `plugin.json` descriptor, which remains openable through the card's "Edit Config" action. The requirement is replaced by "Agent card displayed content" with the reduced card content.

## ADDED Requirements

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed or a "Not installed" state when it is missing, and the declared command set listed after a "Supported commands" label. The card SHALL NOT present the plugin folder path or the declared detection markers.

#### Scenario: Identity shown

- **WHEN** an agent card is shown
- **THEN** it shows the agent's display name and logo (falling back to the default logo)

#### Scenario: Installation status shown

- **WHEN** an agent card is shown
- **THEN** the state chip next to the agent's name indicates whether the agent is installed on the host
- **AND** when the agent is not installed, the chip shows a danger-styled "Not installed" state

#### Scenario: Installed version shown

- **WHEN** an installed agent's card is shown and the installed version is available
- **THEN** the state chip next to the agent's name shows the installed version
- **AND** when the version is not available, the chip shows an "Installed" state instead

#### Scenario: Command set shown

- **WHEN** an agent card is shown
- **THEN** it lists which named commands the agent declares (launch, resume, version, update)
