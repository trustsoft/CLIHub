## 1. Page naming

- [x] 1.1 Rename the "Hotkeys & Quick Launcher" section header in `src/CLIHub/Views/SettingsHotkeysPage.xaml` from "POPUP" to "LAUNCHER POSITIONING"
- [x] 1.2 Rename the card title from "Popup screen position" to "Launcher window position", keeping the existing hint
- [x] 1.3 Rename the `PopupPositionOptions` display labels in `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` to the "<monitor> — <placement>" template: "Center of active monitor" becomes "Active monitor — centered", "Top-center" becomes "Active monitor — top-center", and "Primary screen" becomes "Primary monitor — centered"; leave "Cursor-relative" unchanged
- [x] 1.4 Leave the enum members, tokens, and placement logic untouched, and verify the selector still resolves the stored position through `Load()` and saves the same token
- [x] 1.5 Verify no other user-facing surface (XAML, docs, README) repeats the old section header, card title, or option labels

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`
- [x] 2.2 Run `openspec validate --all` and confirm the change validates without errors
