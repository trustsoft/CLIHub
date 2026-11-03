# Tasks: Launch Window Settings Page Layout Revision

## Stage 1: XAML Layout Restructure

- [x] 1.1 In `src/CLIHub/Views/SettingsLaunchWindowPage.xaml`, remove the `<controls:SectionHeader Text="ACTIVATION"/>` element at line 11.
- [x] 1.2 Remove the `<controls:SectionHeader Text="POSITION"/>` element at line 37.
- [x] 1.3 Remove the `<controls:SectionHeader Text="PROJECT PATHS"/>` element at line 54.
- [x] 1.4 Replace the three standalone `SettingCard` elements with one `<controls:SettingsGroup Margin="0,18,0,8">` container that wraps all three cards.
- [x] 1.5 Add `Grouped="True"` to each of the three `SettingCard` elements inside the SettingsGroup.
- [x] 1.6 Verify the cards remain in the same order: hotkey (Keyboard icon), position (Location icon), path display (FolderOpen icon).
- [x] 1.7 Build: `dotnet build CLIHub.sln`

## Stage 2: Setting Titles and Hints

- [x] 2.1 Change the hotkey card title (current line 12): `Global hotkey` → `Launcher shortcut`.
- [x] 2.2 Change the hotkey card hint (current line 13): `Press a combination. Esc cancels capture.` → `Global hotkey. Press a combination; Esc cancels capture.`
- [x] 2.3 Change the position card title (current line 38): `Launcher window position` → `Launcher placement`.
- [x] 2.4 Change the position card hint (current line 39): `Active monitor is the one under the pointer.` → `Where the window opens. The active monitor follows the pointer.`
- [x] 2.5 Change the path display card title (current line 55): `Path display` → `Project path shortening`.
- [x] 2.6 Change the path display card hint (current line 56): `Long paths are shortened in project rows.` → `How long paths are shortened in project rows.`
- [x] 2.7 Build: `dotnet build CLIHub.sln`
- [x] 2.8 Run tests: `dotnet test CLIHub.sln`

## Stage 3: Verify No Page Description Change

- [x] 3.1 Confirm `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:52` still reads: `"Global hotkey, launcher position, and path display."` (unchanged).
- [x] 3.2 Build: `dotnet build CLIHub.sln`

## Stage 4: Spec Delta

- [x] 4.1 Verify this change's `specs/preferences-ui/spec.md` contains exactly three MODIFIED requirements: `Path display preference`, `Popup screen position`, and `Settings sections and pages`.
- [x] 4.2 Verify each requirement name and every scenario name are byte-identical to `openspec/specs/preferences-ui/spec.md`.
- [x] 4.3 Verify the deltas remove section-header references (ACTIVATION, POSITION, PROJECT PATHS) and adopt the new control labels (`Launcher shortcut`, `Launcher placement`, `Project path shortening`) while keeping all behavioral clauses unchanged.
- [x] 4.4 Validate: `openspec validate --all`

## Stage 5: Final Verification

- [x] 5.1 Build: `dotnet build CLIHub.sln`
- [x] 5.2 Run tests: `dotnet test CLIHub.sln`
- [x] 5.3 Validate: `openspec validate --all`
- [x] 5.4 Run manual smoke test (user-confirmed): Open CLIHub, navigate to Settings → Launch Window, confirm one grouped card with three rows (hairline separators, no section headers), exact approved titles and hints (`Launcher shortcut`, `Launcher placement`, `Project path shortening`), controls unchanged (HotkeyField with chips, two ComboBoxes), option lists unchanged (position options, path display options), Save/Cancel still work, settings persist and apply correctly. Confirmed by the user on 2026-11-03.
- [x] 5.5 Update graph: `graphify update .`

## Notes

- The manual smoke test (5.4) confirms the grouped-card layout matches the approved mockup and that behavior is unchanged.
- No screenshot update is included; release-preparation owns that.
- The page description in `SettingsNavigation.cs` remains unchanged per the user's explicit rejection.
- The test suite does not pin setting titles or hints, so no test updates are required.
