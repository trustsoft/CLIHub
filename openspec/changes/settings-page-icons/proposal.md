# Proposal: Settings page icons

## Why

The user approved a presentational change that swaps five Settings navigation/page-emblem icons to
better match the specific purpose of each page. The code is already applied in the working tree and
verified (build 0/0, 1344 tests passed, openspec 29/0). This proposal exists to record the change
and reconcile the one durable spec that pins a glyph: the Agent Detection page sidebar item scenario
currently specifies the Diagnostic icon, but the approved UI shows the Process icon.

## What changes

- Five Settings page catalog entries in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`
  swap their icon constants:
  - `hotkeys`: `IconGlyphs.Keyboard` → `IconGlyphs.KeyboardShortcut`
  - `launch-window`: `IconGlyphs.Equalizer` → `IconGlyphs.OpenInNewWindow`
  - `projects-registry`: `IconGlyphs.BulletedList` → `IconGlyphs.CheckList`
  - `projects`: `IconGlyphs.FolderOpen` → `IconGlyphs.SyncFolder`
  - `agent-detection`: `IconGlyphs.Diagnostic` → `IconGlyphs.Process`
- Five new Segoe MDL2 glyph constants added to `src/CLIHub/Themes/IconGlyphs.cs`:
  `KeyboardShortcut`, `Process`, `SyncFolder`, `CheckList`, `OpenInNewWindow`.
- `openspec/specs/preferences-ui/spec.md`: the "Agent Detection page content" requirement, scenario
  "Icon and description", updates from the Diagnostic icon to the Process icon (`IconGlyphs.Process`).

## Impact

- **Source files**:
  - `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:54,60,66,73,88` — catalog entries updated.
  - `src/CLIHub/Themes/IconGlyphs.cs:21,25,29,37,49` — five constants added.
  - `src/CLIHub/Views/SettingsWindow.xaml:367,430` — unchanged; they bind to `Icon` and
    `SelectedPage.Icon`, so no XAML edit was required.
- **Spec**: `preferences-ui` — the Agent Detection sidebar item scenario updates from Diagnostic to
  Process.
- **Configuration and behavior**: none; the icon swaps are purely presentational and affect only the
  Segoe MDL2 glyph rendered in the sidebar and page header. No configuration schema, persistence,
  navigation logic, or page content changes.
- **Tests**: none; no test asserts the specific icon glyph values.

## Explicit non-goals

- Swapping any other page icon; the five changed pages are exhaustive.
- Changing the XAML icon bindings or the sidebar/header layout.
- Adding configuration for icon themes or glyph customization.
- Changing page behavior, content, or configuration schema.

## Dependencies

None.
