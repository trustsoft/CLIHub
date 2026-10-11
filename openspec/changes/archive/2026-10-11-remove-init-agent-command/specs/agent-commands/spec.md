## MODIFIED Requirements

### Requirement: Named agent command set

A plugin SHALL describe its commands as a named set rather than an ordered list.

#### Scenario: Commands are named
- **WHEN** a plugin defines commands
- **THEN** each command is identified by a kind: `launch`, `resume`, `version`, or `update`

#### Scenario: Launch command required
- **WHEN** a plugin is loaded and does not define a `launch` command
- **THEN** the plugin is rejected with a logged warning

#### Scenario: Optional commands
- **WHEN** a plugin omits `resume`, `version`, or `update`
- **THEN** the plugin loads successfully and only the defined commands are offered

#### Scenario: Unknown command kind is ignored
- **WHEN** a plugin descriptor contains a command kind that is not recognized (for example a legacy `init`)
- **THEN** the extra field is ignored and the plugin still loads with its recognized commands

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
