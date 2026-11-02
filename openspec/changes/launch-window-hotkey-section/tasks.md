## 1. Prepend hotkey section to Launch Window XAML

- [x] 1.1 Copy the `ACTIVATION` section header and hotkey capture card (lines 10-34 from `src/CLIHub/Views/SettingsHotkeysPage.xaml`) into `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` as the first children of the StackPanel, above the existing `POSITION` section header, and verify the file compiles

## 2. Move code-behind handlers

- [x] 2.1 Add `using System.Windows.Input;`, `using CLIHub.Core.Hotkeys;`, and `using CLIHub.ViewModels;` to `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` (after the namespace declaration, in configured order)
- [x] 2.2 Copy the three handler methods (`HotkeyField_MouseLeftButtonDown`, `HotkeyField_PreviewKeyDown`) and two private static helpers (`IsModifierKey`, `ToHotkeyModifiers`) — lines 19-100 from `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs` — into the `SettingsLaunchWindowPage` class body, preserving all comments, logic, and signatures
- [x] 2.3 Update the class XML doc (line 6 in the current file) to "Settings page for the launch window: hotkey capture, launcher positioning, and path display."
- [x] 2.4 Build `src/CLIHub/CLIHub.csproj` with 0 errors and verify the handlers compile

## 3. Delete the old Global Hotkey page

- [x] 3.1 Delete `src/CLIHub/Views/SettingsHotkeysPage.xaml`
- [x] 3.2 Delete `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs`
- [x] 3.3 Remove lines 49-54 (the `hotkeys` entry) from the `AllPagesData` array in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`
- [x] 3.4 Update the `launch-window` description (line 58 in `SettingsNavigation.cs`) to "The global hotkey, where the launch window appears, and how project paths are shortened in it."
- [x] 3.5 Remove line 505 (`<views:SettingsHotkeysPage Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=hotkeys}"/>`) from `src/CLIHub/Views/SettingsWindow.xaml`
- [x] 3.6 Build `src/CLIHub/CLIHub.csproj` with 0 errors and verify the solution compiles

## 4. Update tests

- [x] 4.1 In `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs`, change the page count assertion (line 26) from `Assert.Equal(11, ...)` to `Assert.Equal(10, ...)`
- [x] 4.2 Remove `"hotkeys",` from the key order assertion (line 29), leaving `"general", "launch-window", "projects-registry", ...`
- [x] 4.3 Remove the `Catalog_EveryPageHasAnIcon_AndHotkeysTitleMatchesTarget` test method's hotkeys title assertion line (line 39: `Assert.Equal("Global Hotkey", viewModel.Pages.Single(page => page.Key == "hotkeys").Title);`)
- [x] 4.4 Remove the `("hotkeys", null, null, null),` tuple row (line 75) from the `Catalog_PageSlotKeys_AreSetOnlyForThePageSpecificSlots` test's expected array
- [x] 4.5 Build `tests/CLIHub.Tests/CLIHub.Tests.csproj` with 0 errors and verify the test file compiles

## 5. Verification

- [x] 5.1 Build the full solution with `dotnet build CLIHub.sln` and verify 0 warnings/0 errors
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 5.3 Manually verify: open the Settings window; confirm the sidebar shows ten pages (no "Global Hotkey" entry); select "Launch Window"; confirm it shows three sections in order: `ACTIVATION` (with the hotkey capture field), `POSITION` (launcher window position selector), `PROJECT PATHS` (path display selector); confirm the hotkey field captures combinations, validates (rejects bare keys, accepts modifier+key), and updates the captured value; confirm Escape cancels capture; confirm switching to another page and back preserves the hotkey value; confirm Save persists the hotkey and Cancel discards changes
- [x] 5.4 Run `openspec validate --all` and verify it passes

> Manual verification notes: Open Settings → sidebar has ten pages, "Global Hotkey" absent, "Launch Window" present and first under "General" → select "Launch Window" → page shows `ACTIVATION` section first (hotkey capture field), then `POSITION`, then `PROJECT PATHS` → click hotkey field, press `Ctrl+Shift+X` → field shows the combination → press a bare `A` → error shown, previous value kept → press `Escape` during capture → capture cancelled → switch to "General & Startup" and back → hotkey value preserved → change hotkey to `Ctrl+Alt+F12`, Save → config.json updated → Cancel without saving → changes discarded.
