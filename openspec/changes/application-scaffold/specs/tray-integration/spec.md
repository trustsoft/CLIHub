## Purpose

Provides system tray icon presence, context menu interaction, and coordination between tray icon and application windows for seamless user experience.

## ADDED Requirements

### Requirement: System tray icon presence

The application SHALL display an icon in the Windows system tray.

#### Scenario: Tray icon on startup
- **WHEN** the application starts
- **THEN** a tray icon appears in the system notification area

#### Scenario: Tray icon tooltip
- **WHEN** the user hovers over the tray icon
- **THEN** "CLIHub" tooltip is displayed

### Requirement: Tray icon context menu

The system SHALL provide a context menu on tray icon interaction.

#### Scenario: Right-click shows menu
- **WHEN** the user right-clicks the tray icon
- **THEN** a context menu appears with available actions

#### Scenario: Left-click shows Launch Window
- **WHEN** the user left-clicks the tray icon
- **THEN** the Launch Window is shown and brought to front

### Requirement: Context menu structure

The system SHALL include project selection and agent launch options in the menu.

#### Scenario: Menu shows current project
- **WHEN** a project is selected
- **THEN** the context menu displays "Current: [ProjectName]"

#### Scenario: Menu shows available agents
- **WHEN** a project is selected
- **THEN** loaded plugins appear as menu items for launching

#### Scenario: Menu shows recent projects
- **WHEN** the context menu opens
- **THEN** a "Recent Projects" submenu lists recently accessed projects

### Requirement: Window show/hide coordination

The system SHALL coordinate tray icon clicks with Launch Window visibility.

#### Scenario: Show hidden window
- **WHEN** the user clicks the tray icon and Launch Window is hidden
- **THEN** the Launch Window is shown and activated

#### Scenario: Minimize to tray
- **WHEN** the Launch Window is closed
- **THEN** the window hides but the application continues running in the tray

### Requirement: Settings access from tray

The system SHALL provide access to settings from the tray context menu.

#### Scenario: Open settings from menu
- **WHEN** the user clicks "Settings" in the tray context menu
- **THEN** the Settings Window opens

### Requirement: Application exit from tray

The system SHALL allow exiting the application from the tray context menu.

#### Scenario: Exit from menu
- **WHEN** the user clicks "Exit" in the tray context menu
- **THEN** the application performs graceful shutdown and terminates
