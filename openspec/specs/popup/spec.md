# popup Specification

## Purpose

Provides the popup surface: a window that on hotkey shows projects and the agents
of the selected project, positions itself at the cursor, and lives as a reusable
instance.

## Requirements

### Requirement: Application host in the tray

The system SHALL keep the application running through a system tray icon with an
"Exit" item.

#### Scenario: Application start

- **WHEN** the application starts
- **THEN** a CLIHub icon appears in the system tray

#### Scenario: Exit

- **WHEN** the user selects "Exit" in the tray icon menu
- **THEN** the application terminates

### Requirement: Reusable window instance (warm singleton)

The system SHALL create the popup window once and reuse it. Hiding the window
SHALL call Hide, not Close.

#### Scenario: Reopening

- **WHEN** the popup was hidden and is invoked again
- **THEN** the same window instance is shown without being recreated

#### Scenario: Hiding preserves the window

- **WHEN** the popup is closed
- **THEN** the window is hidden and stays ready to be shown

### Requirement: Show at cursor on hotkey

The system SHALL show the popup in hotkey mode on the screen where the cursor is
located and give it focus.

#### Scenario: Show at cursor

- **WHEN** the hotkey fires
- **THEN** the popup is shown on the screen with the cursor and receives focus

### Requirement: Keep popup within the working area

The system SHALL position the popup so that it stays entirely within the working
area (WorkingArea) of the screen with the cursor, shifting it when space is short.

#### Scenario: Not enough room at the cursor

- **WHEN** the popup does not fit next to the cursor
- **THEN** the popup is repositioned and stays entirely within the WorkingArea

### Requirement: Correct coordinates across DPI

The system SHALL convert on-screen pixel coordinates to DIP when positioning the
window.

#### Scenario: Scaled screen

- **WHEN** the screen has a scale other than 100%
- **THEN** the popup is positioned at the cursor without offset

### Requirement: Close on Esc and focus loss

The system SHALL hide the popup when Esc is pressed and when it loses focus.

#### Scenario: Esc

- **WHEN** the popup is visible and the user presses Esc
- **THEN** the popup is hidden

#### Scenario: Focus loss

- **WHEN** the popup is visible and stops being the active window
- **THEN** the popup is hidden

### Requirement: Projects pane actions

The **Projects** pane SHALL offer an `Actions` menu with **Add** (choose a folder)
and **Remove** (remove the selected project, after confirmation).

#### Scenario: Add from the Projects pane

- **WHEN** the user chooses Add and picks a folder
- **THEN** the project appears in the list and becomes selected

#### Scenario: Remove from the Projects pane

- **WHEN** the user chooses Remove for the selected project and confirms
- **THEN** the project disappears from the list

#### Scenario: Remove without a selection

- **WHEN** no project is selected
- **THEN** the Remove action is unavailable

### Requirement: Empty Projects state

When no projects are registered, the system SHALL show an empty state that prompts
to add a project instead of a synthetic fallback entry.

#### Scenario: No projects

- **WHEN** the registry is empty
- **THEN** the Projects pane shows an empty state instead of a synthetic current-directory entry

### Requirement: Detection-driven Agents pane

The **Agents** pane SHALL list only host-installed agents, SHALL display
each agent's version when known, and SHALL enable the `run` action only
when the agent is initialized in the selected project. Availability SHALL
be recomputed live when the selected project changes, and the list SHALL
update when a probe round completes.

#### Scenario: Not installed agent hidden

- **WHEN** the host probe finds an agent not installed
- **THEN** it does not appear in the Agents pane

#### Scenario: Version displayed

- **WHEN** an installed agent has a cached or freshly probed version
- **THEN** the version is shown next to the agent's name

#### Scenario: Run disabled for uninitialized project

- **WHEN** the selected project does not contain the agent's detection markers
- **THEN** the agent is listed but its `run` action is disabled

#### Scenario: Popup opens before the probe round completes

- **WHEN** the popup opens while the startup probe round is still running
- **THEN** the Agents pane shows cached results and updates once the round completes

### Requirement: Footer with version and system actions

The popup footer SHALL display the application brand and version, and SHALL
offer three actions: open the data folder, settings, and exit. The status
text SHALL be shown in the footer center, between the brand/version area and
the action area.

#### Scenario: Version displayed

- **WHEN** the popup opens
- **THEN** the footer shows the brand and the application version

#### Scenario: Version fallback when unpackaged

- **WHEN** the application runs without an installed (Velopack) version
- **THEN** the footer shows the assembly informational version instead

#### Scenario: Open data folder

- **WHEN** the user activates the folder action
- **THEN** the stable application data folder (`%AppData%\CLIHub`) opens in
  the system file manager, not the installation directory

#### Scenario: Settings action unavailable

- **WHEN** no settings surface exists
- **THEN** the settings action is visible in the footer but unavailable

#### Scenario: Exit from the footer

- **WHEN** the user activates the exit action
- **THEN** the application terminates

#### Scenario: Status text in the footer center

- **WHEN** a status message is set (project added or removed, launch result)
- **THEN** the message appears in the footer center between the
  brand/version area and the action area

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
