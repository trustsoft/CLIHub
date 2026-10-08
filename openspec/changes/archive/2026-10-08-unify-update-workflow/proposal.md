## Why

Startup checks, tray/What's New checks, the launch-window control, and tray/What's New downloads currently implement update orchestration in separate places. A single application workflow will make concurrent checks and shared state consistent while preserving the distinct user-facing apply policies.

## What Changes

- Introduce one application update workflow for checks, downloads, apply/restart, and tray/What's New download-and-restart.
- Coalesce concurrent checks so all entry points observe one active check and one shared checking state.
- Route startup, tray, What's New, and launch-window update actions through the shared workflow.
- Preserve tray/What's New automatic download-and-restart and launch-window's explicit restart action.
- Keep the startup coordinator as a preference/notification adapter; replace the download coordinator and notifier with shared workflow operations and session-wired outcome events.
- Wire the Settings checker port to the shared workflow so it participates in concurrent checks.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `update-checking`: Require cross-surface check coalescing and a shared workflow/state while preserving per-surface apply behavior.

## Impact

Affected WPF application update workflow, startup/session wiring, tray and What's New command handlers, launch-window update control, DI registration, update tests, and architecture documentation. Core update contracts and Velopack integration remain unchanged.
