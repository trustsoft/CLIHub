## Why

Runtime selection was already represented by `RuntimeKind` in process and Settings contracts, but persisted `DefaultRuntime` remained a string outside the JSON boundary. This allowed application services to parse and emit storage tokens directly.

## What Changed

- Changed `AppPreferences.DefaultRuntime` to typed `RuntimeKind`.
- Kept string tokens only in the persistence DTO mapper and legacy migration boundary.
- Made startup/runtime application consume the enum directly.
- Preserved unknown-token fallback, legacy terminal migration, and JSON compatibility.
- Added tests for canonical tokens, unknown values, and typed DTO round-trips.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal typing refactor; `skip_specs: true` is set because the stored JSON format remains unchanged.

## Impact

- Affects runtime preferences, configuration mapping, migration, startup application, Settings persistence, and tests.
- No changes to runtime command-line behavior or existing `wt`, `cmd`, and `ps` tokens.
