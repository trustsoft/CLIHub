# agent-seeding (partial): built-in command coverage

## MODIFIED Requirements

### Requirement: Built-in descriptor coverage

The built-in set SHALL include a descriptor for each supported agent, and each descriptor SHALL expose the interactive and maintenance commands that agent's CLI actually supports.

#### Scenario: Six agents seeded

- **WHEN** seeding runs
- **THEN** descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are available

#### Scenario: Each seeded descriptor is loadable

- **WHEN** a seeded descriptor is loaded by the plugin manager
- **THEN** it is accepted (it defines an id, a name, and a launch command)

#### Scenario: Complete command sets

- **WHEN** the built-in descriptors are inspected
- **THEN** OpenCode and Pi define `launch`, `resume`, `version`, and `update`
- **AND** OpenClaude defines `launch`, `resume`, and `version`
- **AND** GitHub Copilot and Qwen Code define `launch`, `version`, and `update`
- **AND** Cline CLI defines `launch` and `version`

#### Scenario: Commands match the agent CLI surface

- **WHEN** a built-in descriptor defines a command
- **THEN** the command invocation matches the agent CLI's documented interface (for example `copilot --continue` resumes the most recent session; `cline update` checks for and installs updates)

#### Scenario: Unsupported command kinds are omitted

- **WHEN** an agent CLI has no documented invocation for a command kind (for example Cline CLI can only resume by explicit session id and has no resume-most-recent command)
- **THEN** the built-in descriptor omits that command kind instead of offering a command the agent cannot fulfill
