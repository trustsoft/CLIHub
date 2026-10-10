## MODIFIED Requirements

### Requirement: Terminal vs captured execution

The system SHALL run interactive commands in the configured runtime and capture the output of the version command.

#### Scenario: Interactive command opens a terminal
- **WHEN** `launch`, `resume`, `init`, or `update` is executed
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

### Requirement: Command arguments preserve boundaries

The system SHALL preserve executable and argument boundaries when running plugin commands through Command Prompt, PowerShell, PowerShell 7, Git Bash, or captured command execution.

#### Scenario: Executable path contains spaces
- **WHEN** a plugin command uses an executable path containing spaces
- **THEN** the intended executable is invoked as one executable value

#### Scenario: Arguments contain spaces and quotes
- **WHEN** a plugin command contains arguments with spaces or embedded quotes
- **THEN** the invoked command receives the intended argument values without truncation or unintended splitting

#### Scenario: Arguments contain shell metacharacters
- **WHEN** a plugin command contains shell metacharacters such as `&`, `|`, `^`, or `%`
- **THEN** those characters are passed according to the selected runtime's command rules and do not silently change the command structure
