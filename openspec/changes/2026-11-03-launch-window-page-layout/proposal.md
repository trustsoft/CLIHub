# Proposal: Launch Window Settings Page Layout Revision

## Why

The Settings → Launch Window page has three single-setting sections (ACTIVATION, POSITION, PROJECT PATHS), each with its own section header and one SettingCard. This creates unnecessary visual weight and spacing for a page with only three controls. The user reviewed a mockup (`launch-window-settings-mockup.html`) that removes the section headers, consolidates the three settings into one grouped SettingsGroup surface with hairline separators (the pattern already used by the Updates page), and renames the settings so each label stands on its own without needing section context. The user explicitly approved the proposed layout and copy for implementation, and explicitly rejected changing the page description in `SettingsNavigation.cs`.

This change applies only approved layout and copy edits. No behavior, control types, option lists, configuration keys, validation, persistence, or settings functionality changes are included.

## What Changes

### XAML (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml`)

1. Remove all three `<controls:SectionHeader>` elements (lines 11, 37, 54).
2. Replace the three standalone `SettingCard` elements with one `<controls:SettingsGroup Margin="0,18,0,8">` container holding three `Grouped="True"` `SettingCard` elements in the same order. The first group starts 18 px below the page-header divider (mirroring the archived `2026-11-03-theme-accent-copy` pattern where section-header removal moves the 18 px top margin to the first group).
3. Hotkey card (current line 12): Title `Global hotkey` → `Launcher shortcut`, Hint `Press a combination. Esc cancels capture.` → `Global hotkey. Press a combination; Esc cancels capture.`
4. Position card (current line 38): Title `Launcher window position` → `Launcher placement`, Hint `Active monitor is the one under the pointer.` → `Where the window opens. The active monitor follows the pointer.`
5. Path display card (current line 55): Title `Path display` → `Project path shortening`, Hint `Long paths are shortened in project rows.` → `How long paths are shortened in project rows.`
6. Keep the HotkeyField control structure and bindings (lines 15-34), the position ComboBox (lines 41-51), and the path display ComboBox (lines 58-68) unchanged.

### Page Description (NO CHANGE)

The page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:52` remains EXACTLY as-is: `Global hotkey, launcher position, and path display.` The user explicitly rejected changing it.

### Explicit Non-Changes

- Icons (`Keyboard`, `Location`, `FolderOpen`) are unchanged.
- Control types, bindings, option lists, placeholders, validation, and all behavior are unchanged.
- The page description in `SettingsNavigation.cs` is unchanged (user-rejected).
- `PopupPositionOption` and `PathDisplayOption` names in the option lists are unchanged.
- Configuration keys, defaults, persistence, and the Save/Cancel flow are unchanged.
- `Save` / `Cancel`, footer, sidebar, all other pages, and all behavior are unchanged.
- No README screenshot update is included in this change (release-preparation owns screenshot refresh).

## Impact

- The page reads as one visual unit instead of three isolated single-setting sections.
- Setting titles are self-describing without section headers (verified by the approved mockup).
- Vertical rhythm is preserved: the SettingsGroup starts 18 px below the divider (the same spacing the first section header had).
- The grouped-card pattern is already established on the Updates page (`src/CLIHub/Views/SettingsUpdatesPage.xaml`), so no new UI pattern is introduced.
- The `preferences-ui` spec requires MODIFIED requirements for `Path display preference`, `Popup screen position`, and `Settings sections and pages` to reflect the new single-group layout and the new setting titles/hints, and to remove section-header language from scenarios while keeping every original requirement name and scenario name byte-for-byte.

## Dependencies

None. This change is independent and has no blocking or dependent changes.
