# Spec Delta

## REMOVED Requirements

### Requirement: Consistent pane alignment

**Reason**: The per-pane bottom action rows it aligned no longer exist — the Projects pane's "Add Project..." action and the Agents pane's command row moved into the pane headers' Actions menus and the window footer.

**Migration**: Replaced by the "Pane and footer alignment" requirement, which aligns the pane headers with each other and the pane bodies with the footer.

## ADDED Requirements

### Requirement: Pane and footer alignment

The system SHALL align the two panes so their layout is visually consistent, aligning the pane headers with each other and the pane bodies with the footer.

#### Scenario: Pane headers aligned
- **WHEN** the main window is shown
- **THEN** the Projects pane header and the AI Agents pane header share the same vertical position and height

#### Scenario: Pane bodies end at the footer
- **WHEN** the main window is shown
- **THEN** the project list and the agent list end at the same vertical position, directly above the footer

#### Scenario: Uniform outer margins
- **WHEN** the main window is shown
- **THEN** both panes use the same outer margin and padding on all sides

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
- **THEN** it offers launch, resume, initialize, update, and version for the selected agent, refreshing agents, and toggling the availability filter

#### Scenario: Filter state shown in the menu
- **WHEN** the Agents actions menu is open
- **THEN** the availability filter entry reflects whether the filter is currently enabled

#### Scenario: Row-dependent action without a selection
- **WHEN** no row is selected in a pane
- **THEN** that pane's actions which need a selected row are unavailable and no command runs

### Requirement: Project row presentation

The system SHALL present each project as a single row showing its logo, name, path, and favorite state.

#### Scenario: Logo thumbnail
- **WHEN** a project row is shown
- **THEN** the project's resolved logo is shown as a rounded thumbnail, using the default logo when the project has no logo

#### Scenario: Name and path
- **WHEN** a project row is shown
- **THEN** the project name is shown in a prominent style with the project path beneath it in a secondary, dimmed style

#### Scenario: Long path is middle-ellipsized
- **WHEN** a project path is wider than the space available in the row
- **THEN** the path is shortened in the middle so that its beginning and end remain visible, and the row does not grow wider than the pane

#### Scenario: Favorite marker
- **WHEN** a project is marked as a favorite
- **THEN** its row shows a favorite marker, and a project that is not a favorite shows no marker

#### Scenario: Selected row indicated
- **WHEN** a project row is selected
- **THEN** the row is highlighted and carries a left accent bar that distinguishes it from unselected rows

### Requirement: Agent row presentation and inline actions

The system SHALL present each agent as a single row with its logo, name, version, and inline launch and resume actions.

#### Scenario: Agent identity and version
- **WHEN** an agent row is shown
- **THEN** the agent's logo and name are shown, with the agent's version displayed beneath the name

#### Scenario: Version still resolving
- **WHEN** an agent's version has not been resolved yet
- **THEN** the row shows a placeholder and updates in place when the version arrives, without reordering the list or clearing the selection

#### Scenario: Inline launch
- **WHEN** the user activates a row's Launch action
- **THEN** the agent's launch command runs in the current project

#### Scenario: Inline resume
- **WHEN** the user activates a row's Resume action
- **THEN** the agent's resume command runs in the current project

#### Scenario: Inline actions apply to their own row
- **WHEN** the user activates an inline action on a row that is not the selected row
- **THEN** the action applies to that row's agent

#### Scenario: Inline action not supported by the agent
- **WHEN** an agent does not define the command for an activated inline action
- **THEN** that action is unavailable for the agent and no command runs

### Requirement: Window footer

The system SHALL provide a footer that shows the application identity and version, keeps the update-check control in the left part of the footer immediately to the right of the version, and exposes the window-level actions.

#### Scenario: Footer identity and version
- **WHEN** the main window is shown
- **THEN** the left part of the footer shows the application name and the current application version, and the update-check control sits immediately to the right of the version

#### Scenario: Update check from the footer
- **WHEN** the user activates the footer's update-check action
- **THEN** an update check runs immediately and its outcome is reported in the footer

#### Scenario: Footer actions
- **WHEN** the main window is shown
- **THEN** the footer exposes actions to add a project, open Settings, and exit the application

#### Scenario: Add a project from the footer
- **WHEN** the user activates the footer's add-project action
- **THEN** a folder picker opens and the selected folder is registered and made the current project

#### Scenario: Open Settings from the footer
- **WHEN** the user activates the footer's settings action
- **THEN** the Settings window opens, or the existing Settings window is brought to the front

#### Scenario: Exit from the footer
- **WHEN** the user activates the footer's exit action
- **THEN** the application exits and its tray icon is removed

#### Scenario: Transient status messages
- **WHEN** an action reports status or a failure
- **THEN** the message is shown in the footer, is replaced by the next message, and leaves the footer's actions in place

### Requirement: Scrolling lists

The system SHALL keep long project and agent lists usable within the window.

#### Scenario: Long project list scrolls
- **WHEN** the registered projects do not fit in the Projects pane
- **THEN** the project list scrolls while the pane header and the footer stay in place

#### Scenario: Long agent list scrolls
- **WHEN** the available agents do not fit in the Agents pane
- **THEN** the agent list scrolls while the pane header and the footer stay in place
