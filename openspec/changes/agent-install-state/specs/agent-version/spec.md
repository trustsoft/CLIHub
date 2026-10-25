## MODIFIED Requirements

### Requirement: Version lookup

The system SHALL retrieve an agent's version by running its declared `version` command and capturing the output. The system SHALL NOT run the version command for an agent that detection reports as not installed on the host; such an agent has no version to retrieve and its version stays unresolved (no value).

#### Scenario: Version command defined
- **WHEN** an agent declares a `version` command
- **THEN** the command is executed and the version number found in its output is returned (for example `1.0.88` from `GitHub Copilot CLI 1.0.88.`)

#### Scenario: No version number in output
- **WHEN** the output contains no version-like number
- **THEN** the first non-empty line of the output is returned as-is

#### Scenario: No version command
- **WHEN** an agent does not declare a `version` command
- **THEN** no process is started and the version is reported as unknown

#### Scenario: Version command fails
- **WHEN** the version command cannot start or exits non-zero
- **THEN** the version is reported as unknown and the failure is logged

#### Scenario: No version command for a not-installed agent
- **WHEN** an agent is not installed on the host and its version is requested or probed
- **THEN** no version command process is started
- **AND** the version stays unresolved (no value) rather than being reported through a placeholder

### Requirement: Unknown version display

The system SHALL NOT display placeholder text for a version that is not available. An unresolved version SHALL render no version text at all — no `unknown` placeholder and no pending marker such as `…` — in any agent list surface.

<!-- Deliberate contract change: this requirement previously REQUIRED a placeholder ("the list shows
     a placeholder (for example, `unknown`)"). The scenario name "Unknown shown" is preserved because
     `openspec validate` refuses a MODIFIED block that drops or renames an existing scenario; its body
     is inverted accordingly. The name now reads as "the unknown state is shown (by the absence of any
     version text)". See design.md Decision 9 and Open Question 1. -->

#### Scenario: Unknown shown
- **WHEN** an agent's version is unknown or still unresolved
- **THEN** no placeholder text (for example `unknown` or `…`) is rendered for that agent
- **AND** the list entry shows no version text rather than a marker
