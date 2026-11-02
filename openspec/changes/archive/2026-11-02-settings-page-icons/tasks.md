# Tasks: Settings page icons

## Stage 1: Verify applied code state

- [x] 1.1 Confirm `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:54,60,66,73,88` shows the five catalog entries with the updated icon constants: `KeyboardShortcut`, `OpenInNewWindow`, `CheckList`, `SyncFolder`, `Process`
- [x] 1.2 Confirm `src/CLIHub/Themes/IconGlyphs.cs:21,25,29,37,49` defines the five constants with their Unicode escape sequences: `KeyboardShortcut = "\uEDA7"`, `Process = "\uE9F3"`, `OpenInNewWindow = "\uE8A7"`, `SyncFolder = "\uE8F7"`, `CheckList = "\uE9D5"`
- [x] 1.3 Confirm `src/CLIHub/Views/SettingsWindow.xaml:367,430` binds to `Icon` and `SelectedPage.Icon` without any glyph literals, so the XAML requires no edit

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 2: Spec delta

- [x] 2.1 Write `openspec/changes/settings-page-icons/specs/preferences-ui/spec.md`: MODIFIED "Agent Detection page content" with all original scenarios preserved in their original order, changing only the "Icon and description" scenario's THEN line from `IconGlyphs.Diagnostic` to `IconGlyphs.Process`

Gates:

```powershell
openspec validate --all
```

## Verification

- [x] Build passes: `dotnet build CLIHub.sln`
- [x] Tests pass: `dotnet test CLIHub.sln`
- [x] OpenSpec validation passes: `openspec validate --all`
- [x] Smoke: the Settings window sidebar shows all five changed pages with their new icons (Global Hotkey: keyboard with a pressed shortcut key; Launch Window: window with an outward arrow; Projects: checklist; Project Scanning: folder with sync arrows; Agent Detection: circular process arrows); selecting any of these pages shows the same icon in the page header beside its title
