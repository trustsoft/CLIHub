# app-lifecycle delta

## MODIFIED Requirements

### Requirement: Single instance enforcement

The application SHALL ensure only one instance runs at a time using a named mutex.

#### Scenario: First instance starts successfully
- **WHEN** the application starts and no other instance holds the mutex
- **THEN** the application acquires the mutex and continues startup

#### Scenario: Second instance detected
- **WHEN** the application starts and another instance already holds the mutex
- **THEN** the second instance signals the first instance and exits without showing its own tray icon or window

#### Scenario: First instance receives activation signal
- **WHEN** a second instance signals the running instance
- **THEN** the running instance shows and activates its launch window

### Requirement: Startup window visibility

The application SHALL show or hide its launch window at startup according to the "Start minimized to tray" preference, for both a manual launch and a launch by Windows.

#### Scenario: Show window on startup
- **WHEN** the application starts and "Start minimized to tray" is disabled
- **THEN** the launch window is shown

#### Scenario: Start in the system tray
- **WHEN** the application starts and "Start minimized to tray" is enabled
- **THEN** the launch window is not shown and the application runs in the system tray, still reachable from the tray, the hotkey, and a subsequent launch
