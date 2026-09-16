# fix-tray-menu

## Why

The tray context menu opens at a wrong position on the first open: it hangs down
over the taskbar instead of flipping up. Root cause is in how H.NotifyIcon.Wpf
2.4.1 shows a WPF `ContextMenu`: the menu is not measured before the first
`IsOpen = true`, so WPF cannot compute the screen-edge flip; from the second
open the layout is cached and the position is correct.

The same library code also assigns `GetCursorPos()` physical pixels to
`HorizontalOffset`/`VerticalOffset`, which are DIP units. At 100% scaling the
error is zero; at 150% it is currently masked only because the tray icon sits
in the screen corner. CLIHub runs on machines with 150% and mixed-DPI setups,
so menu placement must be deterministic regardless of scaling and open count.

## What Changes

- Stop using the WPF `ContextMenu` managed by H.NotifyIcon
  (`MenuActivation` off) and show a WinForms `ContextMenuStrip` from CLIHub on
  tray right-click instead. Native menus are pixel-based: no measure race, no
  pixel-vs-DIP conversion, correct behavior on every monitor DPI.
- Menu items are unchanged: "Установить обновление" (visible only when an
  update is ready to apply) and "Выход".
- The menu closes on click outside / focus loss, as before.

Visual change: the menu renders with the native WinForms renderer instead of
the WPF one.

## Capabilities

### New Capabilities

- `tray-menu`: behavior of the system-tray context menu — when it opens, where
  it is placed, how it closes, and which items it contains.

### Modified Capabilities

None. The tray menu behavior was previously unspecified; no existing spec
touches it.

## Impact

- `src/CLIHub.App/App.xaml.cs` (`CreateTrayIcon`): menu construction, event
  wiring, showing at cursor; `SetForegroundWindow` interop for outside-click
  dismissal.
- No dependency changes: H.NotifyIcon.Wpf stays for the icon, tooltip and
  tray events.
- Manual verification on both target PCs (150% single-monitor; mixed-DPI
  multi-monitor), including first open after start, repeated opens, and the
  "Установить обновление" item appearing mid-session.
