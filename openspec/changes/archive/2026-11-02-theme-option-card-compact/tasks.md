# Tasks: Compact theme option cards

## Stage 1: Template

- [x] 1.1 In `src/CLIHub/Views/SettingsAppearancePage.xaml`, set the theme preview border to `Height="48"` and the name margin to `2,4,0,0`, replace the two-row grid with a `StackPanel` root, and remove the visible description text while binding it to `ToolTip` and `AutomationProperties.HelpText`
- [x] 1.2 Align the fixed preview swatches in `src/CLIHub/Themes/ThemePickerData.xaml` with each palette's main background (`#26262C` Dark, `#F3F3F5` Light, `#232833` Blue) and use those values as the System gradient endpoints
- [x] 1.3 Rename the blue palette's picker name to "Deep Blue Theme" in `src/CLIHub/Themes/ThemeDescriptors.cs` and in the change's spec delta, leaving the stored token and enum member unchanged
- [x] 1.4 Rename the system palette's picker name to "System Theme" (and the page hint mention) in `ThemeDescriptors.cs` and `SettingsAppearancePage.xaml`, update the change's spec delta, and refresh `docs/project-context.md`

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 2: Spec delta

- [x] 2.1 Update `openspec/changes/theme-option-card-compact/specs/preferences-ui/spec.md`: MODIFIED "Application theme selector" with the original scenario names preserved, stating that cards show the preview and name and expose the description as tooltip and accessible help text

Gates:

```powershell
openspec validate --all
```

## Verification

- [x] Build passes: `dotnet build CLIHub.sln`
- [x] Tests pass: `dotnet test CLIHub.sln`
- [x] OpenSpec validation passes: `openspec validate --all`
- [x] Smoke: the Theme & Accent page shows four shorter cards named "Dark Theme", "Light Theme", "Deep Blue Theme", and "System Theme" with preview and name only; each preview shows its palette's main background; hovering a card shows its palette description; the stored theme is still selected and marked
