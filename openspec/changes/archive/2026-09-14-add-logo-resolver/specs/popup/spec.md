# popup Delta

## ADDED Requirements

### Requirement: Item identity with logo

The **Projects** pane and the **Agents** pane SHALL display each item's
logo next to its caption. When logo resolution reports no candidate, the
application default logo SHALL be shown.

#### Scenario: Project item logo

- **WHEN** a project has a resolved logo
- **THEN** the Projects pane shows it next to the project name

#### Scenario: Agent item logo

- **WHEN** an agent's plugin folder has a resolved logo
- **THEN** the Agents pane shows it next to the agent name

#### Scenario: Default logo

- **WHEN** logo resolution reports no candidate for an item
- **THEN** the item displays the application default logo
