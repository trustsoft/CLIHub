# agent-commands (partial): launch working directory

## MODIFIED Requirements

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

#### Scenario: Working directory is applied by the operating system

- **WHEN** an interactive command (`launch` or `resume`) is started for the current project
- **THEN** the terminal process is started with the project folder as its operating-system working directory before any shell command or profile runs
- **AND** the shell command line itself is not responsible for changing the directory

#### Scenario: Working directory applies to every supported runtime

- **WHEN** the configured runtime is PowerShell, PowerShell 7, Command Prompt, or Git Bash
- **THEN** the interactive command starts with the project folder as its operating-system working directory

#### Scenario: Project path contains spaces

- **WHEN** the project folder path contains spaces
- **THEN** the terminal process starts in that exact folder

## MODIFIED Requirements

### Requirement: Terminal vs captured execution

The system SHALL run interactive commands in the configured runtime and capture the output of the version command.

#### Scenario: Interactive command opens a terminal

- **WHEN** `launch`, `resume`, or `update` is executed
- **THEN** it opens in the configured runtime in the project folder

#### Scenario: Default runtime

- **WHEN** no runtime preference is configured
- **THEN** PowerShell is used

#### Scenario: Alternative runtime

- **WHEN** the configured runtime is Command Prompt (`cmd`), PowerShell 7 (`ps7`), or Git Bash (`bash`)
- **THEN** the interactive command opens in that runtime in the project folder

#### Scenario: Version is captured

- **WHEN** the `version` command is executed
- **THEN** the executable runs without an interactive terminal and its standard output is returned

#### Scenario: Version command failure

- **WHEN** the `version` command cannot be started or exits non-zero
- **THEN** the failure is reported and no version text is returned

#### Scenario: PowerShell script is a single call

- **WHEN** an interactive command is built for PowerShell or PowerShell 7
- **THEN** the encoded script contains exactly one call operator followed by the quoted executable invocation
- **AND** the script is not rejected by the runtime as a parse error
