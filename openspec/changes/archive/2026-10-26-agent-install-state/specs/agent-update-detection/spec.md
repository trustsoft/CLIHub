## ADDED Requirements

### Requirement: Update checks are gated on host installation

The system SHALL NOT perform an update-availability check for an agent that is not installed on the host, and SHALL NOT request the agent's declared update source for such an agent. The gate is the caller's: the availability service's own behavior — including its degrade-to-`Unknown` path when an installed agent's version cannot be probed or parsed — is unchanged and remains the safety net for installed agents.

#### Scenario: No check for a not-installed agent

- **WHEN** agent cards are populated or probed and a card's agent is not installed on the host
- **THEN** no update-availability check runs for that agent
- **AND** its declared update source is not requested

#### Scenario: A probe reporting not installed clears the advisory result

- **WHEN** an on-demand probe reports that an agent is not installed on the host
- **THEN** no availability check runs for it afterwards in the same action
- **AND** any previously shown advisory update indicator for its card is cleared

#### Scenario: Installed agents are still checked

- **WHEN** agent cards are populated or probed and a card's agent is installed on the host
- **THEN** its update availability is checked under the existing rules
