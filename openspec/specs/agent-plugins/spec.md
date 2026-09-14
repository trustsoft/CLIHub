# agent-plugins Specification

## Purpose

Builds the list of available agents from built-in plugin folders with a manifest,
so agents can be described declaratively without changing application code.

## Requirements

### Requirement: Load plugins from folder

The system SHALL load agents from the built-in `plugins/agents/<folder>/` folder,
where the `agent.json` manifest sits next to the agent assets.

#### Scenario: Valid plugin

- **WHEN** a valid `agent.json` is present in the folder
- **THEN** the agent appears in the agent list

#### Scenario: Broken or missing manifest

- **WHEN** `agent.json` is missing or cannot be parsed
- **THEN** the folder is skipped, the application keeps running and reports a warning

### Requirement: Agent manifest contract

The manifest SHALL contain `schemaVersion` (int), `id` and `name`. The `id` field
SHALL be treated as the authoritative agent identifier, while the folder name is
only a convention. Unknown fields SHALL be ignored.

#### Scenario: Unknown fields

- **WHEN** the manifest contains fields not present in the contract
- **THEN** they are ignored and the agent loads

#### Scenario: Unsupported schema version

- **WHEN** the manifest's `schemaVersion` is not supported by the application
- **THEN** the plugin is skipped with a warning

### Requirement: Agent actions

The manifest SHALL support a fixed set of five optional actions: `run`, `resume`,
`version`, `update`, `init`. Terminal actions (`run`, `resume`, `update`, `init`)
SHALL be defined as a shell-agnostic command string — executable and arguments.

#### Scenario: Agent with a run action

- **WHEN** the manifest describes a `run` action
- **THEN** the `run` action is available for the agent

### Requirement: Per-action runtime override

The manifest SHALL allow defining an optional runtime for an individual action.

#### Scenario: Action runtime specified

- **WHEN** a runtime is specified in a manifest action
- **THEN** it is used instead of the global value from config
