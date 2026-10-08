## Purpose

Coordinates application shell creation (tray icon controller and launch window) and wires event handlers between update workflow, single-instance guard, and UI components, separating shell orchestration from session lifecycle management.

## ADDED Requirements

### Requirement: Shell creation and wiring

The shell coordinator SHALL create the application startup UI (tray and launch window) and SHALL wire update-state, update-request, update-outcome, and second-instance activation events to their handlers through the provided dispatcher.

#### Scenario: Shell coordinator creates startup UI

- **WHEN** the session starts the shell coordinator
- **THEN** the coordinator creates the tray icon controller and launch window through the provided factories
- **AND** returns the created startup UI to the session

#### Scenario: Update state changes trigger menu refresh

- **WHEN** the update workflow raises an update-state-changed event
- **THEN** the shell coordinator invokes the startup UI's menu refresh through the dispatcher
- **AND** the tray menu reflects the current update availability

#### Scenario: Tray requests update download

- **WHEN** the startup UI raises an update-download-requested event
- **THEN** the shell coordinator starts the update download through the operation lifetime
- **AND** the update workflow handles the download and apply

#### Scenario: Update request source triggers download

- **WHEN** the update request source raises an update-requested event
- **THEN** the shell coordinator starts the update download through the operation lifetime
- **AND** the update workflow handles the download and apply

#### Scenario: Second instance activates first instance

- **WHEN** the single-instance guard raises an activation-requested event
- **THEN** the shell coordinator shows and activates the launch window through the dispatcher
- **AND** the first instance becomes visible to the user

#### Scenario: Update downloaded notification

- **WHEN** the update workflow raises an update-downloaded event with a version
- **THEN** the shell coordinator invokes the startup UI's update-downloaded notification through the dispatcher
- **AND** the user sees a notification that the update will be applied on restart

#### Scenario: Update download failed notification

- **WHEN** the update workflow raises an update-download-failed event with a version
- **THEN** the shell coordinator invokes the startup UI's update-failed notification through the dispatcher
- **AND** the user sees a notification that the update download failed

### Requirement: Shell coordinator disposal

The shell coordinator SHALL unsubscribe from all event sources and SHALL dispose the startup UI when the coordinator is disposed.

#### Scenario: Disposal removes event subscriptions

- **WHEN** the shell coordinator is disposed
- **THEN** it removes subscriptions from update workflow, startup UI, update request source, and single-instance guard
- **AND** later events do not invoke disposed handlers

#### Scenario: Disposal cleans up startup UI

- **WHEN** the shell coordinator is disposed and the startup UI is disposable
- **THEN** the coordinator disposes the startup UI
- **AND** the tray icon and launch window resources are released

### Requirement: Shell coordinator operates through dispatcher

The shell coordinator SHALL invoke all UI notifications and actions through the provided dispatcher to preserve WPF thread-affinity requirements.

#### Scenario: Menu refresh uses dispatcher

- **WHEN** an update-state-changed event arrives on a background thread
- **THEN** the shell coordinator dispatches the menu refresh to the UI thread
- **AND** the tray menu updates without cross-thread exceptions

#### Scenario: Activation uses dispatcher

- **WHEN** a second-instance activation arrives
- **THEN** the shell coordinator dispatches the window-show action to the UI thread
- **AND** the launch window activates without cross-thread exceptions

#### Scenario: Update notifications use dispatcher

- **WHEN** update-downloaded or update-failed events arrive on a background thread
- **THEN** the shell coordinator dispatches notifications to the UI thread
- **AND** toast notifications display without cross-thread exceptions
