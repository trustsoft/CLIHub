# Tasks: Launch Window Settings Page Copy Revision

## Stage 1: Page description and XAML hints

- [x] 1.1 Update the page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` line 52 from "The global hotkey, where the launch window appears, and how project paths are shortened in it." to "Global hotkey, launcher position, and path display."
- [x] 1.2 Update the global hotkey hint in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` line 13 from "Press a combination in the field. Esc cancels capture." to "Press a combination. Esc cancels capture."
- [x] 1.3 Update the launcher window position hint in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` line 39 from "Where the launch window appears when it is shown." to "Active monitor is the one under the pointer."
- [x] 1.4 Update the path display hint in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` line 56 from "How long project paths are shortened in the launch window." to "Long paths are shortened in project rows."
- [x] 1.5 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 1.6 Run `dotnet test CLIHub.sln` and verify all tests pass.

## Stage 2: Code-behind error strings

- [x] 2.1 Update the no-modifier error in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` line 54 from "Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win)." to "Add a modifier: Ctrl, Shift, Alt, or Win."
- [x] 2.2 Update the unsupported-key error in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` line 61 from "Unsupported key. Use a letter, digit, F1-F24, or a named key (Space, Enter, Tab, Escape, arrows, and so on) with a modifier." to "Unsupported key. Use a letter, digit, F1–F24, or a named key (Space, Enter, Tab, arrows), plus a modifier." (Note: use en dash U+2013 between F1 and F24.)
- [x] 2.3 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 2.4 Run `dotnet test CLIHub.sln` and verify all tests pass.

## Stage 3: Final gates

- [x] 3.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 3.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 3.3 Run `openspec validate --all` and verify it reports 0 errors.
- [x] 3.4 Run `graphify update .` to refresh the knowledge graph.

## Stage 4: Manual smoke test (user-confirmed)

- [x] 4.1 Launch CLIHub and open Settings → Launch Window.
- [x] 4.2 Verify the page description under the title reads "Global hotkey, launcher position, and path display."
- [x] 4.3 Verify the three card hints:
  - Global hotkey: "Press a combination. Esc cancels capture."
  - Launcher window position: "Active monitor is the one under the pointer."
  - Path display: "Long paths are shortened in project rows."
- [x] 4.4 Focus the hotkey capture field and press a key without a modifier (e.g., just "A"). Verify the error message reads "Add a modifier: Ctrl, Shift, Alt, or Win."
- [x] 4.5 Press an unsupported key with a modifier (e.g., Ctrl+PrintScreen). Verify the error message reads "Unsupported key. Use a letter, digit, F1–F24, or a named key (Space, Enter, Tab, arrows), plus a modifier." with an en dash between F1 and F24.

**Note:** This stage is not automatable and requires user confirmation that the UI shows the expected strings.
