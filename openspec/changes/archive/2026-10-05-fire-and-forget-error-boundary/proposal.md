## Why

Several UI commands intentionally start asynchronous work without awaiting it because the command interface is synchronous. The version population path now has an exception-safe boundary, but agent commands, update actions, and settings actions still rely on raw fire-and-forget calls. An unexpected exception can therefore become unobserved or surface as an application-level failure without a consistent user status.

## What Changes

- Introduce one reusable async fire-and-forget boundary for UI commands.
- Log unexpected exceptions with the relevant operation context.
- Report a user-safe failure message through the existing status/message mechanism where a host provides one.
- Route launch-window agent commands, update-control actions, and other applicable UI async actions through the boundary.
- Preserve successful command behavior and existing expected service-level failure results.
- Add focused tests for exception capture and status reporting.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal reliability refactor; expected user-facing command results remain unchanged.

## Impact

- Affects WPF ViewModels and application-level async command wiring.
- Does not change Core service contracts or persisted data.
- Improves failure reporting for unexpected exceptions from already-started background operations.
