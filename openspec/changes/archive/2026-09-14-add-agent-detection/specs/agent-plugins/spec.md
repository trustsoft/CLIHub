# agent-plugins Delta

## ADDED Requirements

### Requirement: Project detection markers

The manifest SHALL support an optional `detect` section with a `project`
list of relative paths (files or directories) that mark a project folder
as initialized for the agent. Unknown fields inside `detect` SHALL be
ignored.

#### Scenario: Detection markers declared

- **WHEN** `agent.json` contains `detect.project` with paths `[".claude", "CLAUDE.md"]`
- **THEN** the agent loads and exposes these paths for project detection

#### Scenario: No detect section

- **WHEN** `agent.json` contains no `detect` section
- **THEN** the agent loads normally and declares no detection paths
