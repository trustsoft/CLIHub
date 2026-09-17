# Restyle Settings Window

## Why

The settings window is still the default light WPF look, while the approved
UI mockup (`CLIHub 2 UI\settings\settings-mockup.html`, rendered to
`docs\ui\settings.png`) defines a dark chrome-frame visual language shared
with the popup. The window must match the mockup so both surfaces read as one
product.

## What Changes

- Replace the system window chrome with a custom dark frame: a 36 px title
  bar (title «Настройки» + a close button), dark top/bottom strips
  (`rgba(0,0,0,0.14)` tint) with hairline dividers, rounded window corners.
- Restyle the content to the mockup:
  - section headers become uppercase micro-labels;
  - runtime becomes a segmented control (cmd / ps / wt) instead of a combo box;
  - the hotkey capture field shows captured modifiers as key chips;
  - probe TTL/timeout become labeled numeric fields;
  - the update section gets a styled checkbox, a ghost «Проверить обновления»
    button, and a status line with the version accented.
- Move the action buttons into the footer bar: brand «CLIHub» + version chip
  on the left; «Сохранить» (accent) and «Отмена» (ghost) on the right.
- Extract the shared visual vocabulary (dark palette, section label, field,
  ghost/accent buttons, footer) into application-level style resources so the
  popup can adopt the same language later.
- No behavior change: loading, editing, validation, save, apply-on-save,
  hotkey capture semantics, and close-discards-edits all stay as specified.
  The custom close button and Alt+F4 both close without confirmation.

## Capabilities

### New Capabilities

- *(none)*

### Modified Capabilities

- `settings-window`: add a requirement for the custom window chrome — the
  window renders its own title bar with a close control and can be moved by
  dragging the title bar; the close control discards unsaved edits like any
  other close action.

## Impact

- `src/CLIHub.App/Views/SettingsWindow.xaml` — full visual rework;
  `SettingsWindow.xaml.cs` — title-bar drag support, close button wiring.
- `src/CLIHub.App/App.xaml` — new shared style resources (palette, buttons,
  labels, footer).
- `src/CLIHub.App/Views/HotkeyCaptureBox.cs` — visual template only; capture
  logic unchanged.
- `docs/ui.md` — implementation snapshot updated after the change lands.
- No Core changes; UI-only, verified manually per repo convention.
