# agent-detection Specification

## Purpose

Detects which agents are installed on the host and initialized in a project
folder, so the agent list and action availability reflect reality instead of
the static bundle contents.

## Requirements

### Requirement: Host probe via version action

The system SHALL determine whether an agent is installed on the host by
running the agent's `version` action command as a hidden probe with a
timeout: exit code 0 means installed; a non-zero exit code or a timeout
means not installed. The system SHALL capture stdout and report the first
line, trimmed, as the agent's version.

#### Scenario: Probe succeeds

- **WHEN** an agent's `version` command exits with code 0 and prints `1.2.3`
- **THEN** the agent is host-installed with version `1.2.3`

#### Scenario: Probe fails

- **WHEN** the `version` command exits with a non-zero code or exceeds the timeout
- **THEN** the agent is reported as not installed and has no version

### Requirement: PATH fallback when no version action

The system SHALL, when a manifest defines no `version` action, treat the
agent as installed when the executable of its `run` action command is
found on PATH. Such an agent SHALL have no version.

#### Scenario: Run executable on PATH

- **WHEN** a manifest has no `version` action and its `run` executable is on PATH
- **THEN** the agent is host-installed with no version

#### Scenario: Run executable missing

- **WHEN** a manifest has no `version` action and its `run` executable is not on PATH
- **THEN** the agent is not installed

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

### Requirement: Project initialization detection

The system SHALL determine whether an agent is initialized in a project by
checking the manifest's project detection paths relative to the project
folder: the agent is initialized when at least one of the paths exists
(any-of). The check SHALL run live on each project selection without
caching. When a manifest declares no detection paths, the agent SHALL be
treated as initialized in every project.

#### Scenario: Any marker present

- **WHEN** detection paths are `.claude` and `CLAUDE.md`, and the project folder contains `CLAUDE.md`
- **THEN** the agent is initialized in that project

#### Scenario: No markers present

- **WHEN** none of the detection paths exist in the project folder
- **THEN** the agent is not initialized in that project

#### Scenario: No detection paths declared

- **WHEN** a manifest declares no project detection paths
- **THEN** the agent is treated as initialized in every project

### Requirement: Derived action availability

The system SHALL derive action availability from host and project state:
`run` and `resume` require host-installed and project-initialized; `init`
requires host-installed and not project-initialized; `update` requires
host-installed; `version` is informational and not an action.

#### Scenario: Installed and initialized

- **WHEN** an agent is host-installed and initialized in the selected project
- **THEN** its `run` and `resume` actions are available and `init` is not

#### Scenario: Installed but not initialized

- **WHEN** an agent is host-installed and not initialized in the selected project
- **THEN** its `init` action is available and `run`/`resume` are not

#### Scenario: Not installed

- **WHEN** an agent is not installed on the host
- **THEN** none of its actions are available
