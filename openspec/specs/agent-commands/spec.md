# agent-commands Specification

## Purpose

Defines the named command set an AI agent exposes (launch, resume, version, update, init) and executes those commands in the context of a project directory.

## Requirements

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

The system SHALL run interactive commands in the configured runtime and capture the output of the version command.

#### Scenario: Interactive command opens a terminal
- **WHEN** `launch`, `resume`, `init`, or `update` is executed
- **THEN** it opens in the configured runtime in the project folder

#### Scenario: Default runtime
- **WHEN** no runtime preference is configured
- **THEN** Windows Terminal is used

#### Scenario: Alternative runtime
- **WHEN** the configured runtime is Command Prompt (`cmd`) or PowerShell (`ps`)
- **THEN** the interactive command opens in that runtime in the project folder

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

### Requirement: Command arguments preserve boundaries

The system SHALL preserve executable and argument boundaries when running plugin commands through Windows Terminal, Command Prompt, PowerShell, or captured command execution.

#### Scenario: Executable path contains spaces
- **WHEN** a plugin command uses an executable path containing spaces
- **THEN** the intended executable is invoked as one executable value

#### Scenario: Arguments contain spaces and quotes
- **WHEN** a plugin command contains arguments with spaces or embedded quotes
- **THEN** the invoked command receives the intended argument values without truncation or unintended splitting

#### Scenario: Arguments contain shell metacharacters
- **WHEN** a plugin command contains shell metacharacters such as `&`, `|`, `^`, or `%`
- **THEN** those characters are passed according to the selected runtime's command rules and do not silently change the command structure

### Requirement: Runtime command construction is testable by outcome

The system SHALL report a failed command when runtime construction or process startup cannot execute the requested command, and SHALL preserve the existing successful and failed command result contract.

#### Scenario: Command cannot be started after construction
- **WHEN** the selected runtime cannot start the constructed command
- **THEN** the operation returns a failure result and logs the startup failure

#### Scenario: Captured command times out
- **WHEN** a captured command exceeds its configured timeout
- **THEN** the process is terminated and the operation reports a timeout failure
