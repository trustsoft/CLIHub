## Why

Application startup currently combines ordered initialization with long-lived event wiring, making subscription ownership and shutdown cleanup difficult to verify. The tray and What's New surfaces also lack the manual update-check action already available in other parts of the application.

## What Changes

- Move application-level update and activation subscriptions into an explicitly disposable first-instance session.
- Preserve startup order and route session-created UI through the existing startup boundary.
- Add manual update-check actions to the tray and What's New window, reflecting check progress and shared update availability.
- Keep the existing update download-and-restart workflow unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-lifecycle`: Define deterministic ownership and cleanup for first-instance application event subscriptions.
- `update-checking`: Specify manual checks from tray and What's New and their visible in-progress/available state.

## Impact

Affected application startup/session orchestration, tray actions and menu state, the What's New ViewModel and view, lifecycle/update tests, and architecture documentation. No Core update contracts or external dependencies change.
