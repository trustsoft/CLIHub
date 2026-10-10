## MODIFIED Requirements

### Requirement: Startup window visibility

The application SHALL show or hide its main window at startup according to the "Start minimized to tray" preference, for both a manual launch and a launch by Windows.

#### Scenario: Show window on startup
- **WHEN** the application starts and "Start minimized to tray" is disabled
- **THEN** the main window is shown

#### Scenario: Start in the system tray
- **WHEN** the application starts and "Start minimized to tray" is enabled
- **THEN** the main window is not shown and the application runs in the system tray, still reachable from the tray, the hotkey, and a subsequent launch
