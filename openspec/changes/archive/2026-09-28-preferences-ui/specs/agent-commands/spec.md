## MODIFIED Requirements

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
