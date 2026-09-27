## Purpose

Defines the named command set an AI agent exposes (launch, resume, version, update, init) and executes those commands in the context of a project directory.

## ADDED Requirements

### Requirement: Named agent command set

A plugin SHALL describe its commands as a named set rather than an ordered list.

#### Scenario: Commands are named
- **WHEN** a plugin defines commands
- **THEN** each command is identified by a kind: `launch`, `resume`, `version`, `update`, or `init`

#### Scenario: Launch command required
- **WHEN** a plugin is loaded and does not define a `launch` command
- **THEN** the plugin is rejected with a logged warning

#### Scenario: Optional commands
- **WHEN** a plugin omits `resume`, `version`, `update`, or `init`
- **THEN** the plugin loads successfully and only the defined commands are offered

### Requirement: Execute a command in a project

The system SHALL execute an agent command using the current project's folder as the working directory.

#### Scenario: Launch in project
- **WHEN** a user launches an agent for the current project
- **THEN** the launch command runs with the project folder as the working directory

#### Scenario: No project selected
- **WHEN** a command is requested and no project is current
- **THEN** the request is rejected with a message asking the user to select a project

#### Scenario: Missing command for the requested kind
- **WHEN** a user requests a command kind the plugin does not define
- **THEN** the request is rejected with a message that the agent does not support it

### Requirement: Terminal vs captured execution

The system SHALL run interactive commands in Windows Terminal and capture the output of the version command.

#### Scenario: Interactive command opens a terminal
- **WHEN** `launch`, `resume`, `init`, or `update` is executed
- **THEN** it opens in Windows Terminal in the project folder

#### Scenario: Version is captured
- **WHEN** the `version` command is executed
- **THEN** the executable runs without an interactive terminal and its standard output is returned

#### Scenario: Version command failure
- **WHEN** the `version` command cannot be started or exits non-zero
- **THEN** the failure is reported and no version text is returned

### Requirement: Command result reporting

The system SHALL report the outcome of an executed command.

#### Scenario: Successful execution
- **WHEN** a command completes
- **THEN** a success result is returned, including captured output for `version`

#### Scenario: Failed execution
- **WHEN** a command cannot be started
- **THEN** a failure result is returned with an error message and the failure is logged
