## Context

See proposal.md - Why. The Browse Plugins dialog is a modal, owned WPF `Window` shown via `ShowDialog()` from `PluginDialogService.ShowBrowsePluginsDialog()` (line 123-128 in `src/CLIHub/Launching/PluginDialogService.cs`). The Settings window already implements the target drawn-chrome pattern and serves as the reference implementation.

## Verified Current State

**BrowsePluginsDialog.xaml (lines 1-254):**
- Line 1: `Window x:Class="CLIHub.Views.BrowsePluginsDialog"`
- Line 15: `ResizeMode="NoResize"` — prevents user resizing; will change to `CanResize`
- Line 16: `ShowInTaskbar="False"` — kept as-is (user decision)
- Line 17: `WindowStartupLocation="CenterOwner"` — kept as-is (modal dialog convention)
- Line 18: `WindowStyle="None"` — kept (drawn chrome requires this)
- Lines 19-25: `WindowChrome` with `CaptionHeight="0"`, `ResizeBorderThickness="0"` — will change `ResizeBorderThickness` to `6`
- Lines 39-65: Header Grid (row 1) contains app icon, "Browse Plugins" title (line 54), and close button (lines 57-64). No `MouseLeftButtonDown` handler

**BrowsePluginsDialog.xaml.cs (lines 1-29):**
- Lines 16-23: Constructor binds VM, triggers `LoadAsync` on `Loaded`
- Lines 25-28: `OnCloseClick` calls `this.Close()` — kept as-is
- No `OnSourceInitialized`, `OnHeaderMouseDown`, or `OnKeyDown` handlers

**SettingsWindow.xaml (reference implementation):**
- Line 19: `ResizeMode="CanResize"` — enables user resizing
- Line 21: `SourceInitialized="OnSourceInitialized"` — hooks rounded corners and WM_GETMINMAXINFO
- Line 18: `KeyDown="OnKeyDown"` — handles Escape
- Line 28: `WindowChrome ResizeBorderThickness="6"` — 6px resize borders
- Lines 277-318: Header Grid with `MouseLeftButtonDown="OnHeaderMouseDown"` (line 278), maximize/restore button (lines 297-312), and close button (lines 313-317)

**SettingsWindow.xaml.cs (reference implementation):**
- Lines 64-69: `OnSourceInitialized` gets window handle, calls `DwmApi.TryRoundCorners(handle)` (line 67), adds `HwndSource` hook `this.OnWindowMessage` (line 68)
- Lines 78-110: `OnWindowMessage` handles `WM_GETMINMAXINFO` (line 80), clamps maximized position/size to monitor work area (lines 101-104), enforces `MinWidth`/`MinHeight` as minimum tracking size with DPI scaling (lines 105-106)
- Lines 116-131: `OnHeaderMouseDown` filters left button (line 118), calls `this.DragMove()` (line 125), swallows `InvalidOperationException` (line 127-130)
- Line 133-134: `OnMaximizeRestoreClick` toggles `WindowState` between `Maximized` and `Normal`
- Lines 138-147: `OnKeyDown` filters `Key.Escape` (line 140), calls `this.Close()` (line 145), sets `e.Handled = true` (line 146)

**DwmApi.cs (lines 17-26):**
- `TryRoundCorners(IntPtr hwnd)` exists and is reusable — calls `DwmSetWindowAttribute` with `DwmwaWindowCornerPreference=33`, `DwmwcpRound=2`

**User32.cs (interop declarations):**
- `WM_GETMINMAXINFO`, `MonitorFromWindow`, `GetMonitorInfo`, `GetDpiForWindow`, `MINMAXINFO`, `MONITORINFO` already declared (referenced by SettingsWindow)

**PluginDialogService.cs (lines 123-128):**
- `ShowBrowsePluginsDialog()` creates dialog, sets `dialog.Owner = ResolveOwner()` (line 126), calls `dialog.ShowDialog()` (line 127) — modality and ownership kept

## Goals / Non-Goals

**Goals:**
- Drawn chrome with header drag, resize borders (6px), rounded corners, and Escape-to-close
- WM_GETMINMAXINFO hook to enforce DPI-scaled minimum tracking size during user resizing
- Reduce default size to 640x480 (was 800x600) and minimum size to 520x360 (was 600x400)
- Keep `ShowInTaskbar="False"` (modal owned dialog, not a top-level window)
- Keep modality (`ShowDialog()`) and `CenterOwner` startup
- Header contains only the close button (no maximize/restore)

**Non-Goals:**
- Adding maximize/restore or minimize buttons
- Changing the dialog's external contract (entry point, modality, owner)
- Redesigning content

## Decisions

### Decision 1: User scope revision — no maximize/restore button

**Chosen:** After user review of the implemented dialog, the maximize/restore button was removed. The header contains only the close button. The WM_GETMINMAXINFO hook remains to enforce the DPI-scaled minimum tracking size during user resizing, but the maximize work-area clamp is dropped (no maximize path exists).

**Rationale:** User decision after reviewing the implementation. The dialog is resizable by dragging edges, which provides the core benefit (adjusting to see more content). Maximize is not required for a modal plugin-browsing dialog.

**Alternatives considered:** None — user decision is binding.

### Decision 2: Reduce default and minimum size

**Chosen:** Default size reduced to 640x480 (was 800x600), minimum size reduced to 520x360 (was 600x400).

**Rationale:** User decision after reviewing the implementation. The original size was too large for a modal dialog. The new size is adequate for the plugin list while being less intrusive.

**Alternatives considered:** None — user decision is binding.

### Decision 3: Keep ShowInTaskbar="False"

**Chosen:** `ShowInTaskbar="False"` remains unchanged.

**Rationale:** The dialog is modal and owned by the launch window; it does not need a taskbar entry. Settings has `ShowInTaskbar="True"` because it is a non-modal, independent top-level window. The distinction is intentional: modal owned dialogs stay out of the taskbar, top-level modeless windows appear in it.

**Alternatives considered:** None — scope decision is binding.

### Decision 4: OnHeaderMouseDown swallows InvalidOperationException

**Chosen:** `OnHeaderMouseDown` filters left button, calls `this.DragMove()` in a try-catch, swallows `InvalidOperationException`.

**Rationale:** Matches Settings (lines 116-131). `DragMove()` throws `InvalidOperationException` if the button is released before the move starts (rapid click on the header). Swallowing the exception is the correct WPF pattern.

**Alternatives considered:**
- No exception handling: Rejected — would crash on rapid header clicks
- Checking mouse state before calling `DragMove()`: Rejected — race condition; the exception is the correct signal

### Decision 5: Escape-to-close via KeyDown handler

**Chosen:** Add `KeyDown="OnKeyDown"` to the Window, filter `Key.Escape`, call `this.Close()`, set `e.Handled = true`.

**Rationale:** Matches Settings (lines 138-147). No text input exists in the dialog (only buttons and a ScrollViewer with ItemsControl), so Escape is unambiguous. Provides a standard dialog affordance.

**Alternatives considered:**
- No Escape handling: Rejected — user requested parity with Settings
- Close button default: Rejected — WPF default buttons apply to Enter, not Escape; `KeyDown` is the correct mechanism

### Decision 6: WM_GETMINMAXINFO hook for minimum tracking size only

**Chosen:** `OnWindowMessage` handles `WM_GETMINMAXINFO`, sets `ptMinTrackSize.X = (int)Math.Ceiling(this.MinWidth * scale)` and `.Y = (int)Math.Ceiling(this.MinHeight * scale)`, where `scale = dpi / 96.0`. The maximize work-area clamp (`ptMaxPosition`, `ptMaxSize`) is omitted because the dialog exposes no maximize control.

**Rationale:** WPF's `MinWidth`/`MinHeight` prevent programmatic resizing but do not enforce a minimum tracking size during user dragging. The hook ensures the user cannot drag the window below 520x360 at any DPI scale. The maximize clamp is unnecessary for the supported affordances because the dialog has no maximize button.

**Alternatives considered:**
- No hook: Rejected — user could drag below MinWidth/MinHeight
- Full Settings hook including maximize clamp: Rejected — no maximize control exists; the clamp would serve only the unsupported keyboard path noted in Risks

## Risks / Trade-offs

**[Risk] Resizable modal dialog without maximize may surprise users** → Mitigation: The dialog can be resized by dragging edges/corners, which is the primary benefit. Maximize is less expected in a modal dialog than in a top-level window. The Settings window has maximize, but it is non-modal and taskbar-visible; Browse Plugins is modal and taskbar-hidden, so the pattern divergence is justified.

**[Risk] ShowInTaskbar="False" with ResizeMode="CanResize"** → Mitigation: The dialog is owned and modal; the owner appears in the taskbar. Clicking the owner's taskbar entry brings both windows forward. This is the standard WPF owned-dialog behavior.

**[Trade-off] Smaller default size (640x480 vs 800x600)** → Benefit: less intrusive for a modal dialog. Cost: may require resizing or scrolling to see long plugin descriptions. Acceptable — the dialog is user-resizable, and the smaller size is better for most cases.

**[Risk] Keyboard-maximized dialog may overlap taskbar** → `ResizeMode="CanResize"` allows system maximize via Win+Up or WM_SYSCOMMAND even without a button. Because the maximize work-area clamp was removed (no maximize path intended), a keyboard-maximized dialog may overlap the taskbar. Out of scope for this change, but should be re-checked in manual smoke.

## Alternatives Considered

**Alternative 1: Keep ResizeMode="NoResize"**

**Rejected.** The user explicitly requested resizing capability. Without resizing, users with long plugin lists or descriptions cannot adjust the dialog to see more content.

**Alternative 2: Resizing with maximize/restore button**

**Rejected.** User decision after reviewing the implementation. The dialog is resizable by dragging edges, which provides the core benefit. Maximize is not required for a modal plugin-browsing dialog.

**Alternative 3: Keep original size (800x600, MinWidth 600, MinHeight 400)**

**Rejected.** User decision after reviewing the implementation. The original size was too large for a modal dialog. The new size (640x480, minimum 520x360) is less intrusive while remaining adequate for the plugin list.

**Alternative 4: Skip WM_GETMINMAXINFO hook**

**Rejected.** Without the hook, WPF's `MinWidth`/`MinHeight` do not enforce a minimum tracking size during user dragging — the window can be resized below the minimum at certain DPI scales or by dragging corners. The hook ensures the minimum size is always respected.

## Open Questions

- **Should the dialog remember its last size/position across sessions?** Out of scope for this change (Settings does not persist state either; it uses CenterScreen, Browse Plugins uses CenterOwner). Could be added later as a general window-state persistence feature.

- **Should the dialog's size persist across opens within the same session?** Out of scope — the dialog is disposed on close and recreated on open. Persisting state would require the ViewModel or PluginDialogService to track it. Deferred.

- **Should a tooltip explain why there is no maximize button?** Out of scope — tooltips on the absence of a feature are unusual. The Settings window omits the minimize button with no tooltip explanation; this follows the same pattern. (Note: The original plan mentioned a backlog entry for Settings tooltips; that entry now applies to Settings only, not to Browse Plugins.)
