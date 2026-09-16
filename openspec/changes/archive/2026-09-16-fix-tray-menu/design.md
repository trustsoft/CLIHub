# fix-tray-menu — Design

## Context

See `proposal.md` — Why, for the motivation. Current state that shapes the
approach:

- `App.CreateTrayIcon` builds a WPF `ContextMenu` (two items: a conditionally
  visible "Установить обновление" and "Выход") and assigns it to
  `H.NotifyIcon.TaskbarIcon.ContextMenu` (H.NotifyIcon.Wpf 2.4.1).
- The library shows that menu by setting `Placement = AbsolutePoint` and
  assigning `GetCursorPos()` **physical pixels** to `HorizontalOffset` /
  `VerticalOffset`, which WPF interprets as DIP units.
- The app is WPF + WinForms (`UseWindowsForms` is on, `System.Windows.Forms`
  types are explicitly removed from implicit usings and are fully qualified in
  code, e.g. `System.Windows.Forms.Screen`).
- The process is System-DPI-aware: there is no `app.manifest`, and WPF sets
  process DPI awareness itself.
- The tray icon, tooltip and tray message plumbing (via `TaskbarIcon`) work
  fine and are not part of the problem.

## Goals / Non-Goals

**Goals:**

- Deterministic menu position at the cursor on every open, including the
  first, on any monitor scale.
- Keep the existing menu content and behavior (conditional update item, exit).
- Keep the change local to the tray-menu wiring in the App project.

**Non-Goals:**

- Per-monitor DPI awareness manifest and crisp rendering of `PopupWindow` on
  mixed-DPI setups — separate backlog change.
- Styling/theming the menu, changing tray icon, tooltip, or the hotkey popup.

## Decisions

### Decision 1: Show a WinForms `ContextMenuStrip` instead of the WPF menu

CLIHub shows the menu itself: no WPF `ContextMenu` is assigned to
`TaskbarIcon` at all, and the App handles the tray icon's right-mouse event to
show a `System.Windows.Forms.ContextMenuStrip` at the cursor

Native menus are pixel-based and are measured by WinForms before display, so
neither failure mode of the current implementation can occur: there is no
unmeasured-popup flip race on first open, and no pixel-to-DIP conversion
anywhere in the path.

Alternatives considered:

- **Pre-measure the WPF menu at startup** (`menu.Measure(inf)`): smallest change,
  but it leaves the pixel-as-DIP defect in place. That defect is currently
  masked only because the tray icon sits in a screen corner; it would surface
  as soon as the taskbar is moved or the icon is not in the corner.
- **Show the WPF menu ourselves** (suppress the library menu, measure, clamp
  into the monitor working area, convert pixels to DIPs per monitor): fixes
  both defects while keeping the WPF look, but we own per-monitor DPI math and
  would have to revisit it when a PMv2 manifest is added.
- **Upgrade to H.NotifyIcon 2.5.0-beta** (native popup-menu mode): a beta
  dependency for a small tray utility, and upstream position issues are still
  being worked on.
- **Replace `TaskbarIcon` with WinForms `NotifyIcon` entirely**: swaps working
  tray plumbing for no additional benefit over Decision 1.

Not assigning `ContextMenu` is enough to silence the library menu: its
`ShowContextMenu` returns early when `ContextMenu` is null, so no
`MenuActivation` juggling is needed.

### Decision 2: Force foreground activation for outside-click dismissal

A top-level WinForms drop-down shown from a WPF app closes on outside click
only if it holds foreground activation. The App SHALL call
`SetForegroundWindow` on the shown menu's handle (add the P/Invoke next to the
existing ones in `Interop/NativeMethods`), so clicking anywhere else dismisses
the menu. Dismissal is verified manually on both target machines.

### Decision 3: Show the menu on the WPF UI thread

The menu is shown from the tray event handler on the WPF UI thread (STA), the
same thread that owns the tray message window. WinForms controls are usable
there, and the WPF dispatcher already pumps the Win32 messages a drop-down
needs; no WinForms message loop is introduced.

## Risks / Trade-offs

- **Visual change**: the menu is rendered by the native WinForms renderer
  instead of WPF. Accepted — the menu is two plain text items; consistent with
  the thin-UI boundary.
- **Outside-click dismissal may not work if activation is lost** (another
  window steals foreground) → set foreground on the menu handle at show time
  and verify the outside-click scenario on both machines.
- **The update item appears mid-session and changes the menu height** → native
  menus are measured at show time, so the next open is unaffected. Verify by
  triggering an update-ready state.
- **Implicit-using constraints**: `System.Windows.Forms` / `System.Drawing`
  are removed from implicit usings in `CLIHub.App.csproj` (WPF/WinForms type
  ambiguity), so new types must be fully qualified or explicitly imported, as
  existing code already does.
