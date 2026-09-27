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

The built-in set SHALL include a descriptor for each supported agent.

#### Scenario: Six agents seeded
- **WHEN** seeding runs
- **THEN** descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are available

#### Scenario: Each seeded descriptor is loadable
- **WHEN** a seeded descriptor is loaded by the plugin manager
- **THEN** it is accepted (it defines an id, a name, and a launch command)

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
