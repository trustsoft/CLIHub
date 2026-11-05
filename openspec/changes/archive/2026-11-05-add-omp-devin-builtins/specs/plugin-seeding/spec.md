## MODIFIED Requirements

### Requirement: Built-in agent descriptors

The built-in set SHALL include a descriptor for each supported agent, and each descriptor SHALL
expose the interactive and maintenance commands that agent's CLI actually supports.

#### Scenario: Built-in agents seeded

- **WHEN** seeding runs
- **THEN** descriptors for OpenCode, Pi, OMP, Qwen Code, and Devin are available

#### Scenario: Each seeded descriptor is loadable

- **WHEN** a seeded descriptor is loaded by the plugin manager
- **THEN** it is accepted (it defines an id, a name, and a launch command)

#### Scenario: Complete command sets

- **WHEN** the built-in descriptors are inspected
- **THEN** OpenCode, Pi, OMP, Qwen Code, and Devin each define `launch`, `resume`, `version`, and `update`

#### Scenario: Commands match the agent CLI surface

- **WHEN** a built-in descriptor defines a command
- **THEN** the command invocation matches the agent CLI's documented interface (for example `copilot --continue` resumes the most recent session)

#### Scenario: Unsupported command kinds are omitted

- **WHEN** an agent CLI has no documented invocation for a command kind (for example an agent that can only resume by explicit session id and has no resume-most-recent command)
- **THEN** the built-in descriptor omits that command kind instead of offering a command the agent cannot fulfill
