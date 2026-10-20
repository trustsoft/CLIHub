# agent-update-detection Specification

## Purpose
Determines whether a newer version of an agent exists by comparing the probed installed version
with the latest version published by the agent's descriptor-declared distribution channel, and
reports it as advisory information. Distinct from `update-checking`, which covers CLIHub's own
Velopack updates, and from `agent-availability-display`, which dims agents unavailable in a
project.

## Requirements

### Requirement: Descriptor-declared update source

The plugin descriptor MAY declare an optional `updateCheck` block naming the agent's distribution
source. The system SHALL support the `npm` source kind, which names a package, and the
`githubReleases` source kind, which names an `owner/repository`. A descriptor without the block
SHALL result in no availability check for that agent. A block with an unrecognized `source` SHALL
be ignored, the agent SHALL be treated as having no update source, and the system SHALL log a
warning.

#### Scenario: npm source declared

- **WHEN** a descriptor declares `{ "source": "npm", "package": "@opencode/cli" }`
- **THEN** the latest version is read from the npm registry entry for that package

#### Scenario: GitHub releases source declared

- **WHEN** a descriptor declares `{ "source": "githubReleases", "repo": "github/copilot-cli" }`
- **THEN** the latest version is read from that repository's latest release

#### Scenario: No updateCheck block

- **WHEN** a descriptor does not declare an `updateCheck` block
- **THEN** no availability check is performed and no availability is reported for that agent

#### Scenario: Unknown source kind is ignored

- **WHEN** a descriptor declares an `updateCheck` block with an unrecognized `source`
- **THEN** the block is ignored, the agent is treated as having no update source, availability is unknown, and a warning is logged

#### Scenario: Malformed known source is ignored

- **WHEN** an `npm` block has no `package`, or a `githubReleases` block has no `repo`
- **THEN** availability is unknown, no source request is made, and a warning is logged

### Requirement: Installed and latest version comparison

The system SHALL determine availability by extracting a numeric `major.minor.patch` triple from the
installed version and from the version reported by the declared source, and comparing the triples
numerically. The system SHALL report `UpdateAvailable` when the latest triple is greater than the
installed triple, `UpToDate` when it is equal or lower, and `Unknown` when either side cannot be
parsed. Pre-release and build suffixes SHALL be ignored.

#### Scenario: Update available

- **WHEN** the installed version is `2.0.32` and the source reports `2.0.33`
- **THEN** availability reports `UpdateAvailable` with both versions

#### Scenario: Up to date

- **WHEN** the installed version equals the reported latest version
- **THEN** availability reports `UpToDate`

#### Scenario: Installed version is newer

- **WHEN** the installed version is `1.0.100` and the reported latest version is `1.0.99`
- **THEN** availability reports `UpToDate`

#### Scenario: Numeric, not lexical, comparison

- **WHEN** the installed version is `1.2.3` and the reported latest version is `1.10.0`
- **THEN** availability reports `UpdateAvailable`

#### Scenario: Version text with surrounding content

- **WHEN** the installed version text is `opencode v2.0.32`, `0.30.0 (OpenClaude)`, or `1.0.100`
- **THEN** the extracted triple is `2.0.32`, `0.30.0`, or `1.0.100` respectively

#### Scenario: Unparseable version

- **WHEN** the installed or latest value does not contain a numeric triple
- **THEN** availability reports `Unknown`

### Requirement: Detection failures degrade to unknown

The system SHALL report `Unknown` instead of throwing when the installed version is unavailable,
the source cannot be reached, the response is not successful, the response body is malformed, the
expected version field is missing, or the request times out.

#### Scenario: Source unreachable

- **WHEN** the request to the declared source fails with a network error or timeout
- **THEN** availability reports `Unknown` and no exception escapes the check

#### Scenario: Non-success response

- **WHEN** the source responds with a non-success status
- **THEN** availability reports `Unknown` and the failure is logged

#### Scenario: Malformed response

- **WHEN** the response body is not valid JSON or lacks the expected version field
- **THEN** availability reports `Unknown` and no exception escapes the check

#### Scenario: Installed version unavailable

- **WHEN** the installed version cannot be probed or parsed
- **THEN** availability reports `Unknown` and no source request is made

### Requirement: Update availability is advisory

The availability result SHALL NOT gate, disable, enable, or otherwise alter the Update action. The
Update action SHALL remain governed only by its existing rules: the agent declares an update
command and no matching agent process is running.

#### Scenario: Available update does not change the action

- **WHEN** availability reports `UpdateAvailable`
- **THEN** the Update action's availability is unchanged

#### Scenario: Unknown availability does not change the action

- **WHEN** availability reports `Unknown` because the check failed
- **THEN** the Update action remains available under its existing rules

#### Scenario: No update does not disable the action

- **WHEN** availability reports `UpToDate`
- **THEN** the Update action remains available under its existing rules

### Requirement: Availability results are cached per agent

The system SHALL cache each agent's availability result and reuse it until the configured agent
probe time-to-live elapses. Cached results SHALL be invalidated per agent, leaving other agents'
cached results and already-running checks unaffected. Concurrent checks for the same agent SHALL
share one in-flight request. An `Unknown` result SHALL also be cached so a failing source is not
requested again within the time-to-live.

#### Scenario: Cached within the probe interval

- **WHEN** availability for an agent was checked within the configured probe interval
- **THEN** the cached result is reused without a new source request

#### Scenario: Refreshed after the interval

- **WHEN** the cached result is older than the configured probe interval
- **THEN** a new availability check is performed

#### Scenario: Scoped invalidation

- **WHEN** one agent's cached availability is invalidated
- **THEN** only that agent is checked again, and other agents' cached results remain valid

#### Scenario: Concurrent checks are coalesced

- **WHEN** multiple callers request availability for the same uncached agent concurrently
- **THEN** one source request runs and all callers receive the same result

#### Scenario: Unknown is cached

- **WHEN** a check fails and reports `Unknown`
- **THEN** the `Unknown` result is reused within the probe interval without a new source request

### Requirement: Descriptor update state survives catalog reads

The update-source block SHALL survive the plugin catalog's detached-read contract: reading plugins
SHALL return an update block equal to the loaded descriptor's block, and mutating the block on a
returned plugin SHALL NOT affect later reads.

#### Scenario: Round trip through the catalog

- **WHEN** a descriptor with an `updateCheck` block is loaded and read through the catalog
- **THEN** the returned plugin carries the same source and package or repository

#### Scenario: Mutation of a returned plugin

- **WHEN** a caller replaces the update block on a plugin returned by the catalog
- **THEN** a later catalog read still returns the loaded descriptor's block
