# app-lifecycle Specification (Delta)

## MODIFIED Requirements

### Requirement: Launch window popup shell

The launch window SHALL behave as a popup shell that stays out of the way and can be pinned open. A
show request that arrives after the launch window has closed — which happens during application
shutdown, when windows are closed while the tray icon and activation subscriptions are still live —
SHALL be ignored: the closed window SHALL not be positioned, shown, or acted on, no unhandled
exception SHALL escape the request, and the application SHALL continue its shutdown.

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
- **THEN** it is positioned according to the configured popup screen position, which defaults to centred in the work area of the monitor that contains the pointer at that moment

#### Scenario: Show request after the window has closed is ignored
- **WHEN** the launch window has closed during application shutdown and a show request arrives before the tray icon is disposed, from a tray click, a tray menu command, or a second-instance activation
- **THEN** the request is ignored without positioning or showing the closed window and without loading preferences
- **AND** the deferred placement that was queued before the window closed is also ignored
- **AND** the application completes its shutdown without an unhandled exception
