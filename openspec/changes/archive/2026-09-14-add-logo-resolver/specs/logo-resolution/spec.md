# logo-resolution Delta

## Purpose

Resolves the visual identity of agents and projects from a single priority
chain evaluated against a root directory, so every surface renders logos the
same way.

## ADDED Requirements

### Requirement: Unified resolution chain

The system SHALL resolve a logo for a root directory by checking, flat in
that directory and in this order: `logo.png`, `logo.ico`, `icon.png`,
`icon.ico`, `favicon.ico`. The first existing file SHALL win. When none
exists, the system SHALL report that no candidate was found.

#### Scenario: logo wins over icon

- **WHEN** a root contains both `logo.png` and `icon.png`
- **THEN** `logo.png` is resolved

#### Scenario: png precedes ico within a name

- **WHEN** a root contains both `logo.ico` and no `logo.png`
- **THEN** `logo.ico` is resolved

#### Scenario: fallback to favicon

- **WHEN** a root contains only `favicon.ico`
- **THEN** `favicon.ico` is resolved

#### Scenario: nothing found

- **WHEN** a root contains none of the chain files
- **THEN** resolution reports no candidate

### Requirement: Resolution roots

The system SHALL evaluate the chain with the plugin folder as the root for
an agent, and with the project folder as the root for a project.

#### Scenario: Agent logo from plugin assets

- **WHEN** an agent's plugin folder contains `logo.png`
- **THEN** that file is resolved as the agent's logo

#### Scenario: Project logo from project folder

- **WHEN** a project folder contains `icon.png` and no logo files
- **THEN** `icon.png` is resolved as the project's logo

### Requirement: Project override precedence

The system SHALL, when a project stores a `logo` path, prefer it over the
auto chain: a relative path SHALL resolve against the project folder, an
absolute path SHALL be used as-is. When the stored path does not point to
an existing file, the system SHALL silently fall back to the auto chain.

#### Scenario: Absolute override wins

- **WHEN** a project stores `logo` as an absolute path to an existing image
- **THEN** that file is resolved, regardless of chain files in the folder

#### Scenario: Relative override resolves against the project folder

- **WHEN** a project stores `logo` as `assets/brand.png` and that file exists in the project folder
- **THEN** `<project>\assets\brand.png` is resolved

#### Scenario: Broken override falls back

- **WHEN** a project stores `logo` pointing to a missing file while the folder contains `logo.png`
- **THEN** the auto chain resolves `logo.png`
