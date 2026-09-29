# Spec Delta

## ADDED Requirements

### Requirement: Window chrome

The launch window SHALL be presented without operating-system window chrome, delineated instead by its own border and shape.

#### Scenario: No title bar or frame controls
- **WHEN** the launch window is shown
- **THEN** it shows no OS title bar, no window icon, and no minimize, maximize, or close buttons

#### Scenario: Window outline
- **WHEN** the launch window is shown
- **THEN** its outer edge is drawn with a 1px stroke and its top edge carries a 1px inset highlight, so the window boundary stays visible against dark backgrounds

#### Scenario: Rounded corners
- **WHEN** the launch window is shown on Windows 11
- **THEN** its corners are rounded; on an operating system that does not support the rounding preference, the corners stay square without visual artifacts or errors

#### Scenario: Fixed width and content height
- **WHEN** the panes' content changes
- **THEN** the window height follows its content and its width does not change

#### Scenario: Window cannot be moved or resized by dragging
- **WHEN** the user drags anywhere on the window surface
- **THEN** the window is neither moved nor resized

### Requirement: Pane divider presentation

The system SHALL draw the boundary between the panes as a hairline while keeping the splitter draggable.

#### Scenario: Hairline divider
- **WHEN** the launch window is shown
- **THEN** the Projects pane and the Agents pane are separated by a 1px line

#### Scenario: Dragging from the divider
- **WHEN** the user presses and drags on or just beside the divider
- **THEN** the splitter resizes the two panes as described by the resizable pane split requirement

### Requirement: Actions menu presentation

The system SHALL present each pane's actions menu with an icon on every entry, aligned to the control that opens it.

#### Scenario: Entry icons
- **WHEN** a pane's actions menu is open
- **THEN** every entry shows an icon glyph before its label

#### Scenario: Aligned with the Actions control
- **WHEN** a pane's actions menu opens
- **THEN** the menu's right edge aligns with the right edge of that pane's Actions control

## MODIFIED Requirements

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
- **THEN** the footer exposes actions to add a project, open the application data folder, open Settings, and exit the application

#### Scenario: Open the data folder from the footer
- **WHEN** the user activates the footer's open-data-folder action
- **THEN** the application data folder is opened in the system file browser

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

### Requirement: Project row presentation

The system SHALL present each project as a single row showing its logo, name, path, and favorite state.

#### Scenario: Logo thumbnail
- **WHEN** a project row is shown
- **THEN** the project's resolved logo is shown as a rounded thumbnail on a translucent plate, using the default logo when the project has no logo

#### Scenario: Name and path
- **WHEN** a project row is shown
- **THEN** the project name is shown in a prominent style with the project path beneath it in a secondary, dimmed style

#### Scenario: Long path is middle-ellipsized
- **WHEN** the path display style is middle ellipsis and a project path is wider than the space available in the row
- **THEN** the path is shortened in the middle so that its beginning and end remain visible, and the row does not grow wider than the pane

#### Scenario: Long path is left-trimmed
- **WHEN** the path display style is left trim and a project path is wider than the space available in the row
- **THEN** the path is shortened by dropping its beginning, so the end of the path stays visible, and the row does not grow wider than the pane

#### Scenario: Short path unchanged
- **WHEN** a project path fits the space available in the row
- **THEN** the path is shown unchanged, without an ellipsis

#### Scenario: Short path unchanged
- **WHEN** a project path fits the space available in the row
- **THEN** the path is shown unchanged, without an ellipsis

#### Scenario: Favorite marker
- **WHEN** a project is marked as a favorite
- **THEN** its row shows a favorite marker, and a project that is not a favorite shows no marker

#### Scenario: Selected row indicated
- **WHEN** a project row is selected
- **THEN** the row is highlighted with the accent selection treatment and carries a left accent bar that distinguishes it from unselected rows
