# agent-ui Specification (Delta)

## MODIFIED Requirements

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed with a resolved version, an "Installed" state when it is installed without one, a "Not installed" state when it is missing, or a "Detecting" state with an animated braille marker while the card's installation state is being probed, and the declared command set listed after a "Supported commands" label. While a probe runs, the chip SHALL render the text "Detecting" (no trailing ellipsis) alongside an animated braille glyph marker that cycles frames to indicate work in progress. The state chip text SHALL be derived from the card's installation state, never from version placeholder text; the "Not installed" state SHALL keep its danger styling, and the "Detecting" state SHALL use the chip's default styling. The card SHALL NOT present the plugin folder path or the declared detection markers, and SHALL NOT render a separate origin badge. The agent name's tooltip SHALL present the recorded origin (one of "Built-in", "Installed", or "Unknown") as a labelled line, followed by the descriptor's version, publisher, and homepage as labelled lines when declared. When an update is known to be available, the card SHALL render the update chip in the state chip's position (immediately after the name) with the installed and latest versions, and SHALL NOT render the state chip while the update chip is shown. When availability is unknown or the agent is up to date, the card SHALL show the state chip in that position and SHALL NOT show an update indicator, and a probe that reports the agent not installed on the host SHALL clear any update indicator the card was showing and SHALL show the state chip again. The update indicator and Update action enablement both derive from the card's `UpdateAvailability` result; when availability reports `UpdateAvailable`, the indicator is shown and Update becomes available (subject to declaration and running-agent gating).

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
- **THEN** the state chip shows "Detecting" (no trailing ellipsis) alongside an animated braille glyph marker for the duration of the probe
- **AND** the chip keeps the default chip styling rather than the danger styling of the "Not installed" state
- **AND** the braille marker cycles through its frames to indicate that work is in progress

#### Scenario: Detecting never persists after a failed probe

- **WHEN** a probe for a card throws or is abandoned
- **THEN** the card does not stay in the "Detecting" state and returns to its previous installation state

#### Scenario: Command set shown

- **WHEN** an agent card is shown
- **THEN** it lists which named commands the agent declares (launch, resume, version, update)

#### Scenario: Update available indicator shown

- **WHEN** an installed agent's card is shown and availability is `UpdateAvailable` with installed version `2.0.32` and latest version `2.0.33`
- **THEN** the card shows `2.0.32 → 2.0.33` in place of the state chip, immediately after the name
- **AND** the state chip is not shown

#### Scenario: No indicator when availability is unknown

- **WHEN** availability is `Unknown`
- **THEN** the card shows no update indicator and no placeholder text for it
- **AND** the state chip is shown in its usual position

#### Scenario: No indicator when up to date

- **WHEN** availability is `UpToDate`
- **THEN** the card shows no update indicator
- **AND** the state chip is shown in its usual position

#### Scenario: No indicator for a missing installation

- **WHEN** the agent is not installed on the host
- **THEN** the card shows no update indicator
- **AND** the state chip is shown with its danger-styled "Not installed" state

#### Scenario: Update indicator cleared when a probe reports not installed

- **WHEN** a card previously showed an update indicator and a later probe reports the agent not installed on the host
- **THEN** the card shows no update indicator
- **AND** the state chip is shown again, with its danger-styled "Not installed" state

#### Scenario: Update indicator is advisory

- **WHEN** availability reports `UpdateAvailable`
- **THEN** the update indicator is shown
- **AND** the Update action becomes available (subject to the agent declaring an update command and no matching agent process running)
- **WHEN** availability is `Unknown` or `UpToDate`
- **THEN** no update indicator is shown
- **AND** the Update action is unavailable (regardless of declaration or running-agent state)

#### Scenario: State chip returns when no update remains

- **WHEN** a card is showing the update chip in place of the state chip and a later check settles availability to `UpToDate` or `Unknown`
- **THEN** the update chip is no longer shown
- **AND** the state chip is shown again, in its usual position immediately after the name, reflecting the card's current installation state

#### Scenario: Agent identity shown in name tooltip

- **WHEN** an agent card's name is hovered
- **THEN** the tooltip shows the recorded origin as "Origin: Built-in", "Origin: Installed", or "Origin: Unknown"
- **AND** when the descriptor declares a version, publisher, or homepage, each is shown as an additional labelled line ("Version: …", "Publisher: …", "Homepage: …")
- **AND** no separate origin badge is rendered on the card

#### Scenario: Braille marker animates during probe

- **WHEN** a probe is running for a card
- **THEN** the state chip shows "Detecting" with a braille glyph marker that cycles through braille frames at a visible speed (approximately 80ms per frame)
- **AND** the glyph marker occupies a fixed-width slot so that cycling frames do not shift the chip's layout

#### Scenario: Braille marker disappears when probe completes

- **WHEN** a probe completes and the card settles to an installation state (Installed or NotInstalled)
- **THEN** the braille marker is no longer shown
- **AND** the state chip shows the detected version or "Not installed" without any marker or ellipsis
