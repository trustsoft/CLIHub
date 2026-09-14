## MODIFIED Requirements

### Requirement: Probe cache with TTL

The system SHALL persist per-agent probe results in `agents.json`
(`hostInstalled`, `version`, `lastProbed`). Cached entries younger than the
configured TTL SHALL be reused without re-probing; stale or missing entries
SHALL be probed again. Probe settings (`ttlMinutes`, `timeoutSeconds`) SHALL
come from the `probe` section of `settings.json`, falling back to built-in
defaults when the section is missing or contains non-positive values.

#### Scenario: Fresh cache skips probe

- **WHEN** a cache entry's `lastProbed` is newer than the TTL allows
- **THEN** the cached result is used and no probe process is started for that agent

#### Scenario: Stale cache re-probes

- **WHEN** a cache entry's `lastProbed` is older than the TTL allows
- **THEN** the agent is probed again and the entry is updated

#### Scenario: Missing or invalid probe settings

- **WHEN** the `probe` section is absent or contains non-positive values
- **THEN** built-in defaults are used for TTL and timeout

### Requirement: Lazy background probe round at startup

The system SHALL run one probe round at application startup in the
background without blocking the UI. The round SHALL write `agents.json`
exactly once, after all agents have been checked, not once per agent.

#### Scenario: UI stays responsive

- **WHEN** the application starts and probes are still running
- **THEN** the popup opens and reacts to input without waiting for the round

#### Scenario: Single write per round

- **WHEN** the startup round finishes for all agents
- **THEN** `agents.json` is written once with all updated probe results
