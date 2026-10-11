## 1. Model and icons

- [x] 1.1 Add the missing page-icon glyph constants to `IconGlyphs`; verify the app builds.
- [x] 1.2 Extend the `SettingsPage` record with an `Icon` field and supply a glyph for each page in `AllPages`, renaming the hotkeys title to "Hotkeys & Quick Launcher"; verify the view-model tests build.

## 2. Sidebar rendering

- [x] 2.1 Update the sidebar item template in `SettingsWindow.xaml` to show the leading icon in a fixed-width column before the title; verify it renders in the dark theme.
- [x] 2.2 Confirm the existing `SidebarItem` styling keeps the fixed icon column and title aligned across items (no style change was needed); verify labels never wrap oddly.

## 3. Page header

- [x] 3.1 Update the `SettingsHotkeysPage.xaml` page header text to "Hotkeys & Quick Launcher"; verify the page header matches the sidebar label.

## 4. Verification

- [x] 4.1 Update/extend navigation tests to assert each page carries an icon and the hotkeys title is "Hotkeys & Quick Launcher"; verify tests pass.
- [x] 4.2 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 4.3 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 4.4 Run `openspec validate settings-sidebar-icons --strict` and verify the change is valid.
- [x] 4.5 Smoke-test: every sidebar page shows a leading icon; the hotkeys page reads "Hotkeys & Quick Launcher" in the sidebar and its header; navigation and the active highlight are unchanged.
