## MODIFIED Requirements

### Requirement: Pane headers with actions

The system SHALL give each pane a header that names the pane and exposes that pane's actions through a menu.

#### Scenario: Headers name the panes
- **WHEN** the main window is shown
- **THEN** the Projects pane header names the projects pane and the AI Agents pane header names the agents pane

#### Scenario: Actions menu opens
- **WHEN** the user activates a pane's Actions control
- **THEN** a menu of that pane's actions is shown

#### Scenario: Projects actions offered
- **WHEN** the Projects actions menu is open
- **THEN** it offers adding a project, removing the selected project, toggling the favorite state of the selected project, and refreshing the project list

#### Scenario: Agents actions offered
- **WHEN** the Agents actions menu is open
- **THEN** it offers launch, resume, update, and version for the selected agent, refreshing agents, and toggling the availability filter

#### Scenario: Filter state shown in the menu
- **WHEN** the Agents actions menu is open
- **THEN** the availability filter entry reflects whether the filter is currently enabled

#### Scenario: Row-dependent action without a selection
- **WHEN** no row is selected in a pane
- **THEN** that pane's actions which need a selected row are unavailable and no command runs
