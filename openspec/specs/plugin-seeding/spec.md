# plugin-seeding Specification

## Purpose

Populates the plugins folder with built-in agent descriptors on first run so a fresh install has a useful agent list immediately, without overwriting user content.

## Requirements

### Requirement: Seed built-in descriptors on first run

The system SHALL populate the plugins folder with the built-in descriptor set when it contains no plugins.

#### Scenario: Empty plugins folder
- **WHEN** the application starts and the plugins folder contains no plugin subdirectories
- **THEN** a descriptor is written for each built-in agent under `<plugins>\<agent-id>\plugin.json`

#### Scenario: Existing plugins present
- **WHEN** the plugins folder already contains at least one plugin subdirectory with a `plugin.json`
- **THEN** no seeding occurs

#### Scenario: Plugins folder missing
- **WHEN** the plugins folder does not exist at startup
- **THEN** it is created and populated with the built-in descriptors

### Requirement: Built-in descriptor coverage

The built-in set SHALL include a descriptor for each supported agent, and each descriptor SHALL expose the interactive and maintenance commands that agent's CLI actually supports.

#### Scenario: Six agents seeded

- **WHEN** seeding runs
- **THEN** descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are available

#### Scenario: Each seeded descriptor is loadable

- **WHEN** a seeded descriptor is loaded by the plugin manager
- **THEN** it is accepted (it defines an id, a name, and a launch command)

#### Scenario: Complete command sets

- **WHEN** the built-in descriptors are inspected
- **THEN** OpenCode and Pi define `launch`, `resume`, `version`, and `update`
- **AND** OpenClaude defines `launch`, `resume`, and `version`
- **AND** GitHub Copilot and Qwen Code define `launch`, `version`, and `update`
- **AND** Cline CLI defines `launch` and `version`

#### Scenario: Commands match the agent CLI surface

- **WHEN** a built-in descriptor defines a command
- **THEN** the command invocation matches the agent CLI's documented interface (for example `copilot --continue` resumes the most recent session; `cline update` checks for and installs updates)

#### Scenario: Unsupported command kinds are omitted

- **WHEN** an agent CLI has no documented invocation for a command kind (for example Cline CLI can only resume by explicit session id and has no resume-most-recent command)
- **THEN** the built-in descriptor omits that command kind instead of offering a command the agent cannot fulfill

### Requirement: Never overwrite user content

The system SHALL NOT overwrite or delete existing plugin files.

#### Scenario: User-modified descriptor preserved
- **WHEN** a plugin subdirectory already exists
- **THEN** its files are left untouched by seeding

#### Scenario: Seeding is idempotent
- **WHEN** seeding runs more than once against a populated folder
- **THEN** the folder contents are unchanged

### Requirement: Seeding failure is non-fatal

The system SHALL continue startup if seeding fails.

#### Scenario: Write failure
- **WHEN** a descriptor cannot be written (for example, a permissions error)
- **THEN** the failure is logged and application startup continues

### Requirement: Seed built-in agent logos

The system SHALL write each built-in agent's logo file alongside its descriptor when seeding.

#### Scenario: Logo written on first run
- **WHEN** the plugins folder is empty and seeding runs
- **THEN** a `logo.png` is written into each built-in agent's folder in addition to `plugin.json`

#### Scenario: Logo reflects in the UI
- **WHEN** a seeded agent is displayed
- **THEN** its logo is shown instead of the default placeholder

#### Scenario: Logo never overwrites user file
- **WHEN** a `logo.png` already exists in an agent folder
- **THEN** it is left untouched

#### Scenario: Missing logo falls back
- **WHEN** an agent has no bundled or user logo
- **THEN** the default logo is used

### Requirement: Load plugins deterministically

The system SHALL process plugin directories in ascending ordinal, case-insensitive order by directory path, using ordinal comparison as the tie-breaker.

#### Scenario: Plugin directories are discovered in file-system order
- **WHEN** multiple plugin directories exist in an order that differs from the required directory-path order
- **THEN** plugins are loaded and reported in the required ascending order

### Requirement: Resolve duplicate plugin IDs deterministically

The system SHALL keep the first valid plugin encountered in the deterministic directory order and SHALL skip later valid plugins with the same ID.

#### Scenario: Duplicate IDs have different directory paths
- **WHEN** valid plugin descriptors in multiple directories declare the same plugin ID
- **THEN** only the descriptor from the first directory in deterministic order is loaded

#### Scenario: Duplicate ID is skipped
- **WHEN** a valid plugin is skipped because its ID was already loaded
- **THEN** the warning identifies the duplicate ID, the skipped directory, and the directory of the loaded plugin

### Requirement: Reload the plugin catalog on demand

The system SHALL provide a synchronous manual operation that rescans the plugin directory and replaces the current catalog snapshot using the same discovery, validation, deterministic ordering, duplicate-ID, and logo-resolution rules as initial loading.

#### Scenario: New plugin is available after reload
- **WHEN** a valid plugin descriptor is added after initial loading and the manual reload operation is invoked
- **THEN** the plugin is present in the catalog after the operation completes

#### Scenario: Removed plugin is absent after reload
- **WHEN** a previously loaded plugin descriptor is removed and the manual reload operation is invoked
- **THEN** the plugin is absent from the catalog after the operation completes

### Requirement: Notify consumers after catalog reload

The system SHALL notify subscribed consumers after a manual reload completes and the catalog exposes the resulting snapshot.

#### Scenario: Reload notification is raised after replacement
- **WHEN** a manual reload operation completes
- **THEN** exactly one catalog-changed notification is raised after the new plugin snapshot is available

#### Scenario: Initial load does not create a duplicate reload notification
- **WHEN** the catalog performs its initial startup load
- **THEN** no manual-reload notification is raised solely because of the initial load

### Requirement: Detached plugin reads

The plugin catalog SHALL return detached plugin copies from its read methods. A returned copy SHALL clone nested command and detection state, so that mutating a returned plugin — including its commands or detection markers — SHALL NOT change the loaded catalog or any later read.

#### Scenario: Reads return detached copies

- **WHEN** the plugin catalog returns plugins from a read method
- **THEN** mutating a returned plugin changes neither the loaded catalog nor any later read

#### Scenario: Nested command and detection state is cloned

- **WHEN** a caller mutates a returned plugin's command or detection state
- **THEN** the catalog's internal instances keep their own state unchanged
