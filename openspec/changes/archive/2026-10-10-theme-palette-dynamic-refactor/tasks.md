## 1. Enumerate palette keys

- [x] 1.1 List every brush key defined in `LaunchTheme.xaml` and confirm which are palette keys (exclude `Sizing.*`/font/corner keys); record the list in the change for review.

## 2. Convert references

- [x] 2.1 Replace `{StaticResource <paletteKey>}` with `{DynamicResource <paletteKey>}` in `Themes/Controls.xaml`, `Themes/LaunchWindowStyles.xaml`, `Themes/SettingsStyles.xaml`, `Themes/WhatsNewStyles.xaml`, `Views/*.xaml`, and `App.xaml`; verify no `{StaticResource <paletteKey>}` remains.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and verify it succeeds with no unresolved-resource errors.
- [x] 3.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 3.3 Run `openspec validate theme-palette-dynamic-refactor --strict` and verify the change is valid.
- [x] 3.4 Smoke-test: open every window (launch, settings and its pages, What's New, tray menu) and confirm the dark theme renders identically to before.
