## Why

The launch window always centres on the monitor that contains the pointer. Users who work across several monitors or prefer a Raycast-style top-centre palette cannot choose where it appears. The agreed design adds a "Popup screen position" preference while keeping the existing dismiss-on-focus-loss and pinning behaviour unchanged.

## What Changes

- Add a `PopupScreenPosition` preference with the options **Center of active monitor** (default), **Top-center of active monitor**, **Cursor-relative**, and **Primary screen**.
- The launch window is positioned according to the preference every time it is shown (tray, hotkey, second-instance activation, startup); the default preserves today's pointer-monitor centring.
- The Hotkeys & Launchers page gains the "Popup screen position" selector.
- Dismiss-on-focus-loss and Escape behaviour are unchanged: the window hides unless pinned (or a modal prompt is open); Escape closes an open menu first, then hides.
- Additive preference with a safe default; no schema migration.

## Capabilities

### New Capabilities
<!-- Requirements land in the existing app-lifecycle and preferences-ui capabilities. -->

### Modified Capabilities
- `app-lifecycle`: the launch window is placed according to the configured popup screen position instead of always the pointer monitor.
- `preferences-ui`: the Hotkeys & Launchers page offers the popup screen position selector.
- `configuration-snapshot`: the popup screen position defaults safely for missing or unknown values.

## Impact

- Core: `AppPreferences`, `AppConfigDocument`, `ConfigurationSnapshot.ClonePreferences`, a `PopupScreenPosition` value type and token parser.
- App: `WindowPositioner` (per-position placement), `LaunchWindow` (reads the preference when shown), `SettingsViewModel`/`SettingsApplicationService`, `SettingsHotkeysPage`.
- Tests: settings tests; placement smoke on multi-monitor hardware.

## Non-goals

- A dismiss-on-focus-loss preference; drag-to-position with remembered coordinates; direct-run / focus-terminal hotkeys.
