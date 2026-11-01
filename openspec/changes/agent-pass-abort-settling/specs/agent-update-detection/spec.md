# agent-update-detection (partial): explicit failure summary after an aborted forced check

## MODIFIED Requirements

### Requirement: On-demand update availability check

The system SHALL support a forced update-availability check, requested from the management UI, that bypasses the per-agent cached result and its time-to-live by invalidating only the checked agents' cached results before checking. The forced check SHALL be driven from the call site: the availability service's own caching, coalescing, and degrade-to-`Unknown` behavior SHALL be unchanged. A forced check SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site; the check itself resolves the installed version through the version service as any availability check does. The forced check SHALL skip agents that are not installed on the host, SHALL clear their advisory update indicators, and SHALL assign each checked agent's fresh result to its card. When the forced check is aborted by a thrown exception from an agent's check, the system SHALL report an explicit failure summary rather than leaving the summary blank or absent, SHALL still make the check available again afterward, and SHALL leave each already-checked agent's fresh result assigned to its card.

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

#### Scenario: An aborted forced check reports an explicit failure summary

- **WHEN** a forced update check throws while checking one agent
- **THEN** the check-summary line reports an explicit failure rather than being left blank or absent
- **AND** the failure summary is never a valid "agents checked" summary string, so it cannot be read as "no updates found"
- **AND** the forced check becomes available to run again afterward
- **AND** any agent whose fresh result was already assigned to its card before the failure keeps that fresh result
