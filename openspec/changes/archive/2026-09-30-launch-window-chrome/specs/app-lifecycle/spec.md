# Spec Delta

## ADDED Requirements

### Requirement: Launch window popup shell

The launch window SHALL behave as a popup shell that stays out of the way and can be pinned open.

#### Scenario: No taskbar entry
- **WHEN** the launch window is shown
- **THEN** it does not appear as a taskbar button

#### Scenario: Always on top
- **WHEN** the launch window is shown while other applications are open
- **THEN** it is displayed above non-topmost windows

#### Scenario: Hides when focus is lost
- **WHEN** the launch window is visible, is not pinned, and the user activates another application
- **THEN** the launch window hides instead of staying behind that application

#### Scenario: Pinned window stays visible
- **WHEN** the launch window is pinned and the user activates another application
- **THEN** the launch window stays visible

#### Scenario: Escape hides the window
- **WHEN** the launch window is visible and the user presses Escape
- **THEN** the window hides and the application keeps running

#### Scenario: Pin control in the footer
- **WHEN** the launch window is shown
- **THEN** the footer exposes a Pin control whose state reflects whether the window is pinned

#### Scenario: Pin state persists
- **WHEN** the user pins (or unpins) the window and the application restarts
- **THEN** the window starts in the stored pinned or unpinned state

#### Scenario: Modal dialogs do not hide the window
- **WHEN** a folder picker or a confirmation dialog opened from the launch window is active
- **THEN** losing focus to that dialog does not hide the launch window

#### Scenario: Positioned on the pointer's monitor
- **WHEN** the launch window is shown from the tray, the global hotkey, a second-instance activation, or startup
- **THEN** it is centered in the work area of the monitor that contains the pointer at that moment
