# agent-ui delta

## MODIFIED Requirements

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed or a "Not installed" state when it is missing, and the declared command set listed after a "Supported commands" label. The card SHALL NOT present the plugin folder path or the declared detection markers. When an update is known to be available, the card SHALL also show an advisory update indicator next to the state chip with the installed and latest versions. When availability is unknown or the agent is up to date, the card SHALL NOT show an update indicator. The update indicator SHALL NOT alter the Update action's availability.

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

#### Scenario: Update available indicator shown

- **WHEN** an installed agent's card is shown and availability is `UpdateAvailable` with installed version `2.0.32` and latest version `2.0.33`
- **THEN** the card shows `2.0.32 → 2.0.33` next to the state chip

#### Scenario: No indicator when availability is unknown

- **WHEN** availability is `Unknown`
- **THEN** the card shows no update indicator and no placeholder text for it

#### Scenario: No indicator when up to date

- **WHEN** availability is `UpToDate`
- **THEN** the card shows no update indicator

#### Scenario: No indicator for a missing installation

- **WHEN** the agent is not installed on the host
- **THEN** the card shows no update indicator

#### Scenario: Update indicator is advisory

- **WHEN** the update indicator is shown
- **THEN** the Update action remains available under its existing rules
- **AND** the indicator does not enable, disable, or block the Update action
