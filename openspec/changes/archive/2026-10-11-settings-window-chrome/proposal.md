## Why

The Settings window's drawn header is a bare "Settings" title with only a close button, while the target design (`ui/mockups/settings-target.html`) presents a native-looking Windows 11 Fluent title bar. Bringing the title bar to the target is the first, low-risk step toward the target look and does not touch the settings themselves.

## What Changes

- Replace the current drawn header with a **title bar** that shows the application icon and the "CLIHub - Settings" title, followed by drawn **maximize/restore** and **close** buttons.
- **Maximize** expands the window; the button then restores it (with the standard restore glyph).
- Preserve the existing behaviors: header drag, drawn dark chrome with rounded corners, Escape closing, resizing, and the Save/Cancel footer.
- The footer, the save model, and the page content are **not** changed by this change.
- The title bar intentionally omits the version pill and the minimize button.

## Capabilities

### New Capabilities
<!-- None: this change restyles the existing Settings window chrome. -->

### Modified Capabilities
- `settings-theme`: the Settings window chrome gains a title bar with the application identity and maximize/restore/close controls.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml`, `src/CLIHub/Views/SettingsWindow.xaml.cs`
- `src/CLIHub/Themes/SettingsStyles.xaml` (title-bar button styles)
- `src/CLIHub/Themes/IconGlyphs.cs` (minimize/maximize/restore glyphs)
- `tests/CLIHub.Tests` (title-bar/chrome tests)
- No `config.json` change; no `CLIHub.Core` change; footer and Save/Cancel unchanged.

## Non-goals

- The status footer, per-page "Apply Changes", the search field, cards, badges, and the profile card (later phases).
- Mica and other Windows 11 material effects.
