## 1. Option data

- [x] 1.1 Add `Description` to `AppThemeOption` and rename/re-describe the three theme options
- [x] 1.2 Keep loading and saving keyed by `AppTheme`, unchanged

## 2. Picker layout

- [x] 2.1 Add the `ThemeOptionGroup`/`ThemeOptionItem` styles and fixed preview brushes to `SettingsStyles.xaml`
- [x] 2.2 Replace the segmented theme selector with the preview-card list on the Appearance page
- [x] 2.3 Show the accent ring and check badge on the selected card

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
- [x] 3.2 Check the Appearance page visually at the default and minimum window widths
