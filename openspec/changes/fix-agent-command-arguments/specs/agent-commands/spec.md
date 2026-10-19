# agent-commands (partial): argument passing through script shims

## MODIFIED Requirements

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

#### Scenario: Arguments reach PATH-resolved commands through shims

- **WHEN** an interactive command carries arguments and the executable is a bare name that PATH resolves to a script shim (for example an npm `.ps1`/`.cmd` shim)
- **THEN** the constructed PowerShell script resolves the name to a native application before invocation
- **AND** the stop-parsing token never reaches the command; the agent receives only the intended arguments

#### Scenario: Command without arguments runs directly

- **WHEN** an interactive command carries no arguments
- **THEN** the constructed PowerShell script invokes the resolved command directly without argument-passing constructs
