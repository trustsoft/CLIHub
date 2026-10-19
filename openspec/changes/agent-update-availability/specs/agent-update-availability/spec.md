# agent-update-availability (new capability): update availability checking

## ADDED Requirements

### Requirement: Descriptor-declared update source

The plugin descriptor MAY declare an `updateCheck` block naming the agent's distribution source. The system SHALL support the `npm` source kind (npm package name) and the `githubReleases` source kind (owner/repository). Descriptors without the block, and blocks with unknown source kinds, SHALL result in no availability checking for that agent.

#### Scenario: npm source declared

- **WHEN** a descriptor declares `{ "source": "npm", "package": "@opencode/cli" }`
- **THEN** availability is checked against the npm registry's latest version of that package

#### Scenario: GitHub releases source declared

- **WHEN** a descriptor declares `{ "source": "githubReleases", "repo": "QwenLM/qwen-code" }`
- **THEN** availability is checked against the latest release of that repository

#### Scenario: No updateCheck block

- **WHEN** a descriptor does not declare an `updateCheck` block
- **THEN** no availability checking is performed for that agent and no availability is shown

#### Scenario: Unknown source kind is ignored

- **WHEN** a descriptor declares an `updateCheck` block with an unrecognized `source`
- **THEN** the block is ignored and the agent is treated as having no update source

### Requirement: Availability from version comparison

The system SHALL determine availability by comparing the agent's installed version (from the version probe) against the latest version reported by the declared source, and SHALL report one of: up to date, update available (with both versions), or unknown.

#### Scenario: Update available

- **WHEN** the latest published version differs from the installed version by version comparison
- **THEN** availability reports an update with the installed and latest versions

#### Scenario: Up to date

- **WHEN** the latest published version equals the installed version
- **THEN** availability reports the agent as up to date

#### Scenario: Unknown when versions are unavailable

- **WHEN** the installed version cannot be probed, the source cannot be reached, or either value cannot be parsed as a version
- **THEN** availability reports unknown

### Requirement: Availability is advisory

The availability result SHALL NOT gate or alter the Update action's availability or behavior. Network, registry, and parsing failures SHALL degrade to unknown without affecting the Update action.

#### Scenario: Offline check leaves the action intact

- **WHEN** the availability check cannot reach the declared source
- **THEN** availability reports unknown
- **AND** the Update action remains available under its existing rules

### Requirement: Availability results are cached

The system SHALL cache availability results and reuse them within the same probe cadence as version probes, so distribution sources are not requested more often than the configured probe interval.

#### Scenario: Cached within the probe interval

- **WHEN** availability was checked within the configured probe interval
- **THEN** the cached result is reused without a new source request

#### Scenario: Refreshed after the interval

- **WHEN** the cached result is older than the configured probe interval
- **THEN** a new availability check is performed

## Purpose

Determines whether a real update exists for an agent by comparing its installed version against the latest version published by the agent's declared distribution source, as advisory information for the user. Distinct from `update-checking`, which covers CLIHub's own Velopack updates.
