## ADDED Requirements

### Requirement: Version subtitle only for resolved versions

The agent-row version subtitle SHALL be rendered only when a version was resolved for the agent. An unresolved version SHALL render no subtitle and no placeholder text such as `unknown` or `…`, and SHALL NOT render an empty version prefix (for example `Version: `).

#### Scenario: Resolved version shown as the subtitle

- **WHEN** an agent row is shown and the agent's version resolved to `1.2.3`
- **THEN** the row's version subtitle reads `1.2.3`

#### Scenario: Unresolved version renders no subtitle

- **WHEN** an agent row is shown and the agent's version is unresolved or unavailable
- **THEN** the row renders no version subtitle and no placeholder text for it

#### Scenario: No empty version prefix

- **WHEN** an agent row is shown and the agent's version is unresolved
- **THEN** the row does not render a version label with an empty value (for example `Version: `)

#### Scenario: Subtitle appears when a version resolves

- **WHEN** a row was shown without a version subtitle and version population resolves the agent's version
- **THEN** the subtitle appears with the resolved version without replacing the row
