## ADDED Requirements

### Requirement: Start with Windows

The application SHALL let the user enable or disable launching automatically when Windows starts, using the current user's Run registration.

#### Scenario: Enable autostart
- **WHEN** the user enables "Start with Windows" and saves
- **THEN** a per-user Run entry for the current executable is created so CLIHub starts at Windows sign-in

#### Scenario: Disable autostart
- **WHEN** the user disables "Start with Windows" and saves
- **THEN** the per-user Run entry is removed

#### Scenario: Registration reflects current state
- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" toggle reflects whether the Run entry currently exists

#### Scenario: Registration refreshed on startup
- **WHEN** the application starts with "Start with Windows" enabled
- **THEN** the Run entry is updated to point at the current executable path

### Requirement: Startup window visibility

The application SHALL show or hide its main window at startup according to a preference, for both a manual launch and a launch by Windows.

#### Scenario: Show window on startup
- **WHEN** the application starts and "Show window on startup" is enabled
- **THEN** the main window is shown

#### Scenario: Start in the system tray
- **WHEN** the application starts and "Show window on startup" is disabled
- **THEN** the main window is not shown and the application runs in the system tray, still reachable from the tray, the hotkey, and a subsequent launch
