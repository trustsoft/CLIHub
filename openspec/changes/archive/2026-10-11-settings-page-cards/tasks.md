## 1. Primitives

- [x] 1.1 Add `SettingCard : ContentControl` in `src/CLIHub/Views/SettingCard.cs` with DPs `Title`, `Hint`, `Icon`, `Badge`, `BadgeTone` (`SettingBadgeTone`), `Placement` (`SettingCardPlacement`), `Grouped`; verify the app builds.
- [x] 1.2 Add the implicit `SettingCard` style to `SettingsStyles.xaml`: rounded dark surface; header with an optional leading icon block, title, optional badge, and hint; a single content host repositioned by `Placement` (right vs stacked); `Grouped` drops the surface. Verify both placements and the icon/badge render.
- [x] 1.3 Add `SettingsGroup : ItemsControl` in `src/CLIHub/Views/SettingsGroup.cs` with an implicit style rendering one rounded bordered surface and hairline separators between rows (none on the first); verify grouped rows render.
- [x] 1.4 Add `SectionHeader : ContentControl` (`Text`, `ShowMarker`) and its implicit style (uppercase muted label + optional accent marker); verify it renders.
- [x] 1.5 Add the glyph constants needed for the setting icons to `IconGlyphs`; verify the app builds.

## 2. General & Startup

- [x] 2.1 Add a page description; render the two startup toggles as a `SettingsGroup` of grouped rows (`Start with Windows`, `Start minimized to tray`) with leading icons and hints; verify they still bind.

## 3. Hotkeys & Quick Launcher

- [x] 3.1 Add a page description; `SectionHeader` `ACTIVATION` with the global-hotkey card (icon, title, hint, hotkey field right) and `SectionHeader` `POPUP` with the popup-position card (stacked segmented selector); verify capture still works.

## 4. Terminal Profiles

- [x] 4.1 Add a page description and the default-runtime card (icon, title, hint, stacked segmented selector); verify the runtime still applies on save.

## 5. Appearance

- [x] 5.1 Add a page description; `SectionHeader` `THEME` with the application-theme card (right-aligned segmented) and the accent-color card (stacked swatches + hex); `SectionHeader` `PATHS` with the path-display card (stacked segmented); verify theme/accent/path controls bind.

## 6. Telemetry & Logs

- [x] 6.1 Add a page description; `SectionHeader` `LOGGING` with the log-level card (stacked segmented) and the retention/size card (stacked inputs); `SectionHeader` `LOG FILE` with the log-file card (stacked path + Copy/Open); verify fields, copy, and open still work.

## 7. Updates

- [x] 7.1 Add a page description; `SectionHeader` `AUTOMATIC` with a `SettingsGroup` of the four toggle rows (icons, right-aligned toggles); `SectionHeader` `MANUAL` with the manual-check card (button + status, stacked); verify toggles and the manual check still work.

## 8. Verification

- [x] 8.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 8.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 8.3 Run `openspec validate settings-page-cards --strict` and verify the change is valid.
- [x] 8.4 Smoke-test: each of the six pages shows a description, section headers (with marker) where applicable, and cards with leading icons; compact controls sit right, wide controls stack; grouped rows share one surface with dividers; every control loads, edits, and saves.
