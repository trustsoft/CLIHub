## Why

Most update entry points already share the application workflow, but Settings still depends on the narrower checker contract and does not express its participation in the shared lifecycle. Completing that boundary prevents Settings checks from drifting from update state and makes the intended cross-surface behavior explicit.

## What Changes

- Route the Settings update-check command through the shared application update workflow.
- Keep Settings-specific status text, cancellation reporting, and tracked-operation behavior.
- Align the unregistered legacy window with the shared workflow contract, or remove its direct checker dependency if the existing architecture confirms it is not composed.
- Add regression coverage that Settings checks join a check already started by another entry point.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `update-checking`: Require Settings manual checks to participate in the shared update lifecycle and coalesce with checks from other entry points.

## Impact

Affected application update interfaces, Settings ViewModel composition and tests, and the legacy MainWindow reference if it remains in the source tree. Core Velopack ports and persisted configuration remain unchanged.
