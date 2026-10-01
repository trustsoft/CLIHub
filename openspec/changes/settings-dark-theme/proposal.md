# Proposal

## Why

The Settings window is the last light-styled surface in the app: hardcoded light colors (`#2C3E50`, `#ECF0F1`, `#3498DB`) clash with the dark launch window and What's New chrome it opens above. The approved mockup defines its dark look using the palette the app already has, so the change is a restyle — no new preferences, no section changes, and almost no new colors.

## What Changes

- Rebuild the Settings window on the shared dark theme, following the mockup: dark surfaces, an uppercase section-label rhythm with hint lines under fields, and a dark footer with the CLIHub brand, version chip, an accent **Save**, and a neutral **Cancel**.
- Draw the window chrome like the What's New window: no OS title bar, a "Settings" header with a close button, header drag, rounded corners, Escape closes; the window stays resizable and owned by the launch window.
- Replace the runtime and path-display combo boxes with segmented controls (`cmd` / `ps` / `wt`, and the two path-display options), selected segment highlighted with the accent; selection behavior and saved preferences are unchanged.
- Present the hotkey capture field in the dark input style, showing the captured combination as key chips (Ctrl, Alt, Space…) with the capture hint below; capture behavior is unchanged.
- Style the probe TTL/timeout fields, checkboxes, the "Check for updates" button, and the update-status line (accent-toned) in the dark theme; all five sections from the mockup's language stay, including Startup and Appearance which the mockup does not draw.
- Reuse the existing theme palette; the only allowed addition is a single error tone for validation messages if the existing brushes cannot carry it.
- All Settings behavior (sections, save/discard, hotkey capture, probe fields, update check) keeps its current semantics.

## Capabilities

### New Capabilities

- `settings-theme`: the dark visual theme of the Settings window — drawn chrome, section layout, segmented selectors, dark inputs, themed footer, and the update-status presentation, built by reusing the existing launch-window palette.

### Modified Capabilities

- `launch-window-theme`: remove the "Theme scope" requirement, which limits the dark theme to the launch window and requires the Settings window to keep its light styling — the Settings window now has its own dark theme.

## Impact

- **Modified:** `src/CLIHub/Windows/SettingsWindow.xaml(.cs)` (layout, drawn chrome, segmented selectors, chips), `src/CLIHub/ViewModels/SettingsViewModel.cs` (a computed key-chip collection for the hotkey field; no behavior change), new `src/CLIHub/Themes/SettingsStyles.xaml` (window-scoped styles), possibly one new brush token in `Themes/LaunchTheme.xaml`, `docs/architecture.md`, `docs/repo-structure.md`, `docs/vision.md`.
- **Tests:** no new unit tests (XAML styling); hotkey-chip presentation may get a small view-model test if the chips come from a testable formatter; existing tests must keep passing.
- **Dependencies:** none added.
- **Not included:** new settings or preference keys; changes to save/discard or capture semantics; dark theming for windows other than Settings; new palette entries beyond the single error tone.
