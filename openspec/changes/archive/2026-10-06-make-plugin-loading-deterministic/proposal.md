## Why

Plugin discovery currently depends on the order returned by the file system, so duplicate plugin IDs can produce different winners and logs between runs. Defining one stable loading policy makes startup behavior reproducible and makes duplicate-plugin diagnostics actionable.

## What Changes

- Sort plugin directories using a stable, platform-independent comparison before loading them.
- Define the first directory in that order as the winner when multiple descriptors use the same plugin ID.
- Log the accepted and skipped directories when a duplicate ID is encountered.
- Add focused coverage for ordering, duplicate selection, and deterministic diagnostics.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `plugin-seeding`: Define deterministic plugin directory processing and duplicate-ID selection during plugin loading.

## Impact

- `PluginManager` loading order, duplicate handling, and related tests.
- The existing plugin-seeding behavior contract and its OpenSpec delta.
- No public API, descriptor format, or plugin seeding data changes.
