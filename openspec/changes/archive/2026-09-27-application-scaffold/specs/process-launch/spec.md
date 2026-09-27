## Purpose

Spawns and manages external CLI tool processes with proper working directory context, environment variables, and Windows Terminal integration.

## ADDED Requirements

### Requirement: Agent process spawning

The system SHALL launch AI agent CLI tools as separate processes.

#### Scenario: Launch agent in project context
- **WHEN** an agent is launched for a project
- **THEN** a new process is started with the project path as working directory

#### Scenario: Command arguments injection
- **WHEN** an agent has configured arguments
- **THEN** the arguments are passed to the launched process

### Requirement: Windows Terminal integration

The system SHALL spawn agent processes in Windows Terminal sessions.

#### Scenario: Launch in Windows Terminal
- **WHEN** an agent is launched
- **THEN** the process is spawned via wt.exe with the agent command

#### Scenario: Terminal working directory
- **WHEN** an agent is launched via Windows Terminal
- **THEN** the -d flag sets the working directory to the project path

### Requirement: Process error handling

The system SHALL handle launch failures gracefully.

#### Scenario: Command not found
- **WHEN** an agent executable does not exist
- **THEN** an error is logged and the user is notified

#### Scenario: Permission denied
- **WHEN** launching an agent fails due to permissions
- **THEN** an error is logged with diagnostic information

### Requirement: Configurable terminal executable

The system SHALL support custom terminal executables beyond wt.exe.

#### Scenario: Use default Windows Terminal
- **WHEN** no custom terminal is configured
- **THEN** wt.exe is used by default

#### Scenario: Use custom terminal
- **WHEN** a custom terminal executable is configured in preferences
- **THEN** that executable is used for launching agents

### Requirement: Process launch logging

The system SHALL log agent launch events for diagnostics.

#### Scenario: Log successful launch
- **WHEN** an agent is launched successfully
- **THEN** the agent name, project path, and command are logged

#### Scenario: Log launch failure
- **WHEN** an agent launch fails
- **THEN** the error, agent name, and attempted command are logged with stack trace
