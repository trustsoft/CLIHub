# agent-launch Specification

## Purpose

Launches an agent in the selected project folder in a terminal of the required
runtime, sparing the user from typing commands and opening a terminal in the right
directory.

## Requirements

### Requirement: Runtime resolution

The system SHALL choose the runtime by priority: per-action override from the
manifest, then the global runtime from `settings.json`, then the built-in
default.

#### Scenario: Action override set

- **WHEN** the launched action specifies a runtime
- **THEN** that runtime is used

#### Scenario: No override

- **WHEN** the action does not specify a runtime and config has a global value
- **THEN** the global runtime is used

#### Scenario: Neither override nor global value

- **WHEN** neither the action nor config specifies a runtime
- **THEN** the built-in application default is used

### Requirement: Terminal command form per runtime

The system SHALL open the terminal with one of these commands:
`cmd` — `cmd.exe /k "<command>"`; `ps` — `powershell.exe -NoExit -Command "<command>"`;
`wt` — `wt.exe -d "<cwd>" cmd /k "<command>"`. The working directory SHALL be the
project folder. The terminal window SHALL stay open after the command completes.

#### Scenario: Launch in cmd

- **WHEN** the `cmd` runtime is chosen
- **THEN** a cmd window opens in the project folder and stays open after the command

#### Scenario: Launch in ps

- **WHEN** the `ps` runtime is chosen
- **THEN** a PowerShell window opens in the project folder and stays open after the command

#### Scenario: Launch in wt

- **WHEN** the `wt` runtime is chosen and Windows Terminal is available
- **THEN** a Windows Terminal window opens in the project folder with the command executed

### Requirement: Fallback when Windows Terminal is missing

The system SHALL, when `wt` is unavailable, launch through `ps` or `cmd` and report
a warning.

#### Scenario: wt unavailable

- **WHEN** the `wt` runtime is chosen but Windows Terminal is not installed
- **THEN** the agent launches through the fallback runtime and the application reports a warning

### Requirement: Asynchronous launch without blocking

The system SHALL launch the terminal without waiting for the process to exit and
without blocking the UI.

#### Scenario: Launch does not block the UI

- **WHEN** the user launches an agent
- **THEN** the terminal starts and the popup remains responsive
