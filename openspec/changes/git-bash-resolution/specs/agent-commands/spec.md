# agent-commands (partial): Git Bash and PowerShell 7 runtime resolution

## MODIFIED Requirements

### Requirement: Terminal vs captured execution

The system SHALL run interactive commands in the configured runtime and capture the output of the version command. When Git Bash is the configured runtime, the interactive command SHALL start a Git for Windows `bash.exe` and never an arbitrary `bash.exe` resolved from the operating system path. When PowerShell 7 is the configured runtime, the interactive command SHALL start a resolved PowerShell 7 `pwsh.exe` in the same way rather than a bare name.

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

#### Scenario: Git Bash resolves to Git for Windows

- **WHEN** the configured runtime is Git Bash and an interactive command is launched
- **THEN** the launched executable is the absolute path of a Git for Windows `bash.exe`, resolved from the `GitForWindows` registry `InstallPath`, a well-known install location, or a PATH entry outside the Windows directory, in that order
- **AND** the resolution happens at launch time, so a changed installation is reflected on the next launch

#### Scenario: The WSL launcher is never used for Git Bash

- **WHEN** the first `bash.exe` on PATH is the WSL launcher in the Windows directory
- **THEN** that launcher is not used for the Git Bash runtime
- **AND** the launch either starts a resolved Git for Windows `bash.exe` or fails as the next scenario describes

#### Scenario: Unresolvable Git Bash fails safely

- **WHEN** the Git Bash runtime is configured but no Git for Windows `bash.exe` can be resolved
- **THEN** the launch fails with the existing failed-command result, a clear error naming Git for Windows and the lookup order is logged, and no terminal process starts

#### Scenario: Unresolvable PowerShell 7 fails safely

- **WHEN** the PowerShell 7 runtime is configured but no `pwsh.exe` can be resolved
- **THEN** the launch fails with the existing failed-command result, a clear error naming PowerShell 7 and the lookup order is logged, and no terminal process starts
