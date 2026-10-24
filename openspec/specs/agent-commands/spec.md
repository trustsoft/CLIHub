# agent-commands Specification

## Purpose

Defines the named command set an AI agent exposes (launch, resume, version, update) and executes those commands in the context of a project directory.

## Requirements

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

### Requirement: Terminal vs captured execution

The system SHALL run interactive commands in the configured runtime and capture the output of the version command. When Git Bash is the configured runtime, the interactive command SHALL start an absolutely resolved `bash.exe` found through a fixed preference order — the `GitForWindows` registry `InstallPath`, a well-known install location, then an operating-system PATH entry outside the Windows directory — and SHALL never use the WSL launcher `bash.exe` in the Windows directory. A PATH hit is accepted as the last-choice source without verifying that it belongs to a Git for Windows installation. When PowerShell 7 is the configured runtime, the interactive command SHALL start a resolved PowerShell 7 `pwsh.exe` in the same way rather than a bare name.

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
- **THEN** the launched executable is the absolute path of the first `bash.exe` found through the preference order — the `GitForWindows` registry `InstallPath`, a well-known install location, or a PATH entry outside the Windows directory, in that order — where a PATH hit is accepted without verifying the Git for Windows distribution
- **AND** the resolution happens at launch time, so a changed installation is reflected on the next launch

#### Scenario: The WSL launcher is never used for Git Bash

- **WHEN** the first `bash.exe` on PATH is the WSL launcher in the Windows directory
- **THEN** that launcher is not used for the Git Bash runtime
- **AND** the launch either starts a `bash.exe` resolved through the preference order or fails as the next scenario describes

#### Scenario: Unresolvable Git Bash fails safely

- **WHEN** the Git Bash runtime is configured but no `bash.exe` can be resolved through the preference order
- **THEN** the launch fails with the existing failed-command result, a clear error naming Git for Windows and the lookup order is logged, and no terminal process starts

#### Scenario: Unresolvable PowerShell 7 fails safely

- **WHEN** the PowerShell 7 runtime is configured but no `pwsh.exe` can be resolved
- **THEN** the launch fails with the existing failed-command result, a clear error naming PowerShell 7 and the lookup order is logged, and no terminal process starts

#### Scenario: PowerShell 7 resolves through any PATH entry

- **WHEN** PowerShell 7 surfaces only through an operating-system PATH entry (for example a Microsoft Store install whose execution alias lives on PATH)
- **THEN** that PATH entry's `pwsh.exe` is resolved and launched
- **AND** unlike Git Bash, PATH entries inside the Windows directory are not excluded, because `pwsh.exe` has no WSL-launcher namesake there

### Requirement: Command result reporting

The system SHALL report the outcome of an executed command.

#### Scenario: Successful execution
- **WHEN** a command completes
- **THEN** a success result is returned, including captured output for `version`

#### Scenario: Failed execution
- **WHEN** a command cannot be started
- **THEN** a failure result is returned with an error message and the failure is logged

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

### Requirement: Runtime command construction is testable by outcome

The system SHALL report a failed command when runtime construction or process startup cannot execute the requested command, and SHALL preserve the existing successful and failed command result contract.

#### Scenario: Command cannot be started after construction
- **WHEN** the selected runtime cannot start the constructed command
- **THEN** the operation returns a failure result and logs the startup failure

#### Scenario: Captured command times out
- **WHEN** a captured command exceeds its configured timeout
- **THEN** the process is terminated and the operation reports a timeout failure

### Requirement: Project-independent agent update

The system SHALL run an agent's `update` command in the user profile directory when the update is requested outside a project context, such as from the Settings window.

#### Scenario: Update without a project
- **WHEN** an agent update is requested and no project is current
- **THEN** the update command runs interactively with the user profile directory as the working directory

#### Scenario: Update with a project context
- **WHEN** an agent update is requested from a surface where a project is current
- **THEN** the update command runs in that project's folder

#### Scenario: Update still respects capabilities
- **WHEN** an agent does not declare an `update` command
- **THEN** the update request is rejected with a message that the agent does not support it
