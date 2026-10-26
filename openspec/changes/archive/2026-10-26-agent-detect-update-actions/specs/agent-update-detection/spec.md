## ADDED Requirements

### Requirement: On-demand update availability check

The system SHALL support a forced update-availability check, requested from the management UI, that bypasses the per-agent cached result and its time-to-live by invalidating only the checked agents' cached results before checking. The forced check SHALL be driven from the call site: the availability service's own caching, coalescing, and degrade-to-`Unknown` behavior SHALL be unchanged. A forced check SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site; the check itself resolves the installed version through the version service as any availability check does. The forced check SHALL skip agents that are not installed on the host, SHALL clear their advisory update indicators, and SHALL assign each checked agent's fresh result to its card.

#### Scenario: Forced check bypasses the cache

- **WHEN** a forced update check runs for an agent whose availability result is cached and within the time-to-live
- **THEN** that agent's cached result is invalidated and a fresh availability check runs

#### Scenario: Forced check invalidates only checked agents

- **WHEN** a forced update check runs for some agents
- **THEN** only those agents' cached availability results are invalidated and other agents' cached results remain valid within their time-to-live

#### Scenario: Forced check performs no detection or version command at the call site

- **WHEN** a forced update check runs
- **THEN** no host-installation detection runs for it and no version command is issued at the call site
- **AND** each check resolves the installed version through the version service as any availability check does

#### Scenario: Forced check gates on host installation

- **WHEN** a forced update check runs and an agent is not installed on the host
- **THEN** no availability check runs for that agent and its declared update source is not requested
- **AND** any previously shown advisory update indicator for its card is cleared

#### Scenario: Forced check assigns the fresh result

- **WHEN** a forced update check completes for an installed agent
- **THEN** the card's update availability reflects the fresh result

#### Scenario: Availability service behavior is unchanged

- **WHEN** a forced update check is implemented
- **THEN** the availability service's caching, per-agent invalidation, in-flight coalescing, and degrade-to-`Unknown` paths are unchanged, and the force is achieved by invalidating before checking

## MODIFIED Requirements

### Requirement: Update checks are gated on host installation

The system SHALL NOT perform an update-availability check for an agent that is not installed on the host, and SHALL NOT request the agent's declared update source for such an agent. The gate applies to every caller — the load-time population pass, installation probes, and forced update checks alike. The gate is the caller's: the availability service's own behavior — including its degrade-to-`Unknown` path when an installed agent's version cannot be probed or parsed — is unchanged and remains the safety net for installed agents.

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

#### Scenario: A forced check skips a not-installed agent

- **WHEN** a forced update check runs and a card's agent is not installed on the host
- **THEN** no availability check runs for that agent and its declared update source is not requested
- **AND** any previously shown advisory update indicator for its card is cleared
