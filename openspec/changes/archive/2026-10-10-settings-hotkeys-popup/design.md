## Context

`LaunchWindow.ShowOnPointerMonitor` calls `WindowPositioner.PlaceOnPointerMonitor`, which centres the window in the work area of the pointer's monitor (with a height cap and a layout fallback). The window hides on deactivation unless `IsPinned` or a modal prompt is open, and Escape closes an open menu first, then hides. `PinLaunchWindow` already persists. Nothing chooses the position.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Add a `PopupScreenPosition` preference with four options and apply it whenever the window is shown.
- Keep dismiss/pinning behaviour exactly as today.

**Non-Goals:**
- A dismiss-on-blur preference; remembered drag coordinates.

## Decisions

### Value type with tokens, stored like the runtime
A `PopupScreenPosition` enum (`Center`, `TopCenter`, `CursorRelative`, `PrimaryScreen`) with canonical tokens (`center`, `topCenter`, `cursorRelative`, `primary`) and a parser that defaults to `Center`. `AppPreferences` holds the enum; `PreferencesDocument` stores the token, mirroring `RuntimeKind`. No schema migration; unknown tokens parse to the default.

### Per-position placement in `WindowPositioner`
Generalize `PlaceOnPointerMonitor` into `Place(window, position)` computing the target work area and anchor:
- `Center` — pointer monitor work area, centred (today's behaviour).
- `TopCenter` — pointer monitor work area, horizontally centred, a small top margin.
- `CursorRelative` — near the pointer with a small offset, clamped into the pointer monitor work area.
- `PrimaryScreen` — the primary monitor work area, centred (resolve the primary monitor via `MonitorFromPoint(0,0, MONITOR_DEFAULTTOPRIMARY)`).

The native-coordinate move and the layout fallback stay, so mixed-DPI monitors remain correct.

### The window reads the preference when shown
`LaunchWindow` receives `IPreferencesStore` and reads `PopupScreenPosition` in `ShowOnPointerMonitor`, then calls `WindowPositioner.Place(this, position)`. The `IStartupWindow.ShowOnPointerMonitor` contract and all show sources are unchanged; dismiss/pinning code is untouched.

### Hotkeys page selector
`SettingsHotkeysPage` gains a segmented "Popup screen position" selector bound to the view model, reusing `SegmentGroup`.

## Risks / Trade-offs

- [Primary-monitor resolution edge cases] → fall back to `SystemParameters.WorkArea` when the native query fails, as today.
- [`LaunchWindow` gains a dependency] → it is resolved by DI (`ServiceRegistration`); update its construction site and any tests.
- [Cursor-relative clamping] → clamp the computed left/top into the work area so the window is never off-screen.

## Migration Plan

No schema change. The preference defaults to `Center` when absent.

## Open Questions

- Exact top margin and cursor offset constants — settle during implementation against the mockup; does not change the specs or task breakdown.
