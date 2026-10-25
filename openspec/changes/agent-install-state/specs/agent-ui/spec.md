## ADDED Requirements

### Requirement: Default agent selector reflects installation state

The default-agent selector SHALL present each agent option with display text derived from the agent's installation state, and SHALL disable only options for agents not installed on the host. Every other option, including one whose installation state is being detected, SHALL remain selectable, and no option SHALL carry version placeholder text.

#### Scenario: Installed agent with a resolved version

- **WHEN** the selector lists an installed agent whose version resolved to `2.0.32`
- **THEN** the option reads `OpenClaude (2.0.32)` and is selectable

#### Scenario: Installed agent without a resolved version

- **WHEN** the selector lists an installed agent whose version did not resolve
- **THEN** the option reads the agent's name alone, with no parenthesized suffix

#### Scenario: Not-installed agent marked and disabled

- **WHEN** the selector lists an agent that is not installed on the host
- **THEN** the option reads `{Name} (Not installed)` with a capitalized suffix, deliberately different from the default-runtime selector's lowercase `(not installed)`, whose own style stays unchanged
- **AND** the option is disabled and cannot be selected

#### Scenario: Detecting option stays selectable

- **WHEN** the selector lists an agent whose installation state is being re-detected
- **THEN** the option reads the agent's name alone and remains selectable

#### Scenario: Selector text carries no version placeholders

- **WHEN** an agent's version is unresolved or unavailable
- **THEN** the selector option renders no version placeholder text such as `unknown` for it

## MODIFIED Requirements

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed with a resolved version, an "Installed" state when it is installed without one, a "Not installed" state when it is missing, or a "Detecting…" state while the card's installation state is being probed, and the declared command set listed after a "Supported commands" label. The state chip text SHALL be derived from the card's installation state, never from version placeholder text; the "Not installed" state SHALL keep its danger styling, and the "Detecting…" state SHALL use the chip's default styling. The card SHALL NOT present the plugin folder path or the declared detection markers. When an update is known to be available, the card SHALL also show an advisory update indicator next to the state chip with the installed and latest versions. When availability is unknown or the agent is up to date, the card SHALL NOT show an update indicator, and a probe that reports the agent not installed on the host SHALL clear any update indicator the card was showing. The update indicator SHALL NOT alter the Update action's availability.

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

#### Scenario: Detecting state shown

- **WHEN** a detection or test probe is running for a card
- **THEN** the state chip shows "Detecting…" for the duration of the probe
- **AND** the chip keeps the default chip styling rather than the danger styling of the "Not installed" state

#### Scenario: Detecting never persists after a failed probe

- **WHEN** a probe for a card throws or is abandoned
- **THEN** the card does not stay in the "Detecting…" state and returns to its previous installation state

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

#### Scenario: Update indicator cleared when a probe reports not installed

- **WHEN** a card previously showed an update indicator and a later probe reports the agent not installed on the host
- **THEN** the card shows no update indicator

#### Scenario: Update indicator is advisory

- **WHEN** the update indicator is shown
- **THEN** the Update action remains available under its existing rules
- **AND** the indicator does not enable, disable, or block the Update action
