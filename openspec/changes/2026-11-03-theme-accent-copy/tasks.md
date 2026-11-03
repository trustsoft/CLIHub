# Tasks: Theme & Accent Copy Revision

## Stage 1: XAML and Navigation Copy

- [x] 1.1 Remove `<controls:SectionHeader Text="THEME"/>` from `src/CLIHub/Views/SettingsAppearancePage.xaml:10`.
- [x] 1.2 Set the theme `SettingCard` (line 11) to `Margin="0,18,0,8"` to preserve vertical rhythm.
- [x] 1.3 Change theme card title (line 11): `Application theme` → `Theme`.
- [x] 1.4 Change theme card hint (line 12): `Select the visual palette and luminosity mode. System Theme follows Windows.` → `System follows the Windows light/dark mode.`
- [x] 1.5 Change accent card hint (line 72): `Applied to active states, focus rings, toggles, and primary actions.` → `Used for selection, focus, and emphasis.`
- [x] 1.6 Change custom accent TextBox tooltip (line 158): `Custom accent color as #RRGGBB` → `Hex color, e.g. #4CC2FF`.
- [x] 1.7 Change page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:93`: `Theme and accent color.` → `How CLIHub looks.`
- [x] 1.8 Build: `dotnet build CLIHub.sln`

## Stage 2: Theme Descriptors and Accessible Names

- [x] 2.1 In `src/CLIHub/Themes/ThemeDescriptors.cs`, change theme names (lines 48, 55, 62, 69): `Dark Theme` → `Dark`, `Light Theme` → `Light`, `Deep Blue Theme` → `Deep Blue`, `System Theme` → `System`.
- [x] 2.2 Change theme descriptions (lines 49, 56, 63, 70): `Fluent Dark (default)` → `Default palette`, `High contrast daylight profile` → `Bright, high-contrast palette`, `Blue-tinted dark chrome` → `Dark with a blue tint`, `Follow Windows personalization state` → `Follows the Windows light/dark mode`.
- [x] 2.3 In `src/CLIHub/ViewModels/Settings/SettingsOptions.cs`, add an `AccessibleName` property to `AppThemeOption` after line 68: `public string AccessibleName => $"{this.Name} theme";` with XML doc `/// <summary> The accessible name combining the theme name with "theme" for screen readers. </summary>`.
- [x] 2.4 In `src/CLIHub/Themes/ThemePickerData.xaml:38`, change the binding from `Value="{Binding Name}"` to `Value="{Binding AccessibleName}"`.
- [x] 2.5 Build: `dotnet build CLIHub.sln`
- [x] 2.6 Run tests: `dotnet test CLIHub.sln`

## Stage 3: Accent Custom Entry and Accessible Name

- [x] 3.1 In `src/CLIHub/ViewModels/Settings/AccentSelection.cs:12`, change the custom entry: `new("Custom...", "Transparent", "RGB / Hex", true)` → `new("Custom", "Transparent", "Your own color", true)`.
- [x] 3.2 In `src/CLIHub/ViewModels/Settings/SettingsOptions.cs:82`, update the `AccessibleName` property to special-case the custom entry: `public string AccessibleName => this.IsCustom ? "Custom accent color" : $"{this.Name} {this.Hint}";`.
- [x] 3.3 Build: `dotnet build CLIHub.sln`
- [x] 3.4 Run tests: `dotnet test CLIHub.sln`

## Stage 4: Spec Delta

- [x] 4.1 Verify this change's `specs/preferences-ui/spec.md`: exactly one MODIFIED `Application theme selector` requirement; the requirement name and every scenario name are byte-identical to `openspec/specs/preferences-ui/spec.md`; only the selector label, the theme names, the descriptions, and the accessible-name sentence differ. The delta is already authored — do not rewrite it.
- [x] 4.2 Validate: `openspec validate --all`

## Stage 5: Final Verification

- [x] 5.1 Build: `dotnet build CLIHub.sln`
- [x] 5.2 Run tests: `dotnet test CLIHub.sln`
- [x] 5.3 Validate: `openspec validate --all`
- [ ] 5.4 Run manual smoke test: Open CLIHub, navigate to Settings → Theme & Accent, verify copy matches the approved mockup, verify accessible names via Narrator or Accessibility Insights (theme options read as "Dark theme", "Light theme", etc.; custom accent entry reads as "Custom accent color"), verify selection and save behavior unchanged.
- [x] 5.5 Update graph: `graphify update .`

## Notes

- The manual smoke test (5.4) includes accessibility verification with a screen reader or inspection tool to confirm the `AccessibleName` bindings are correct.
- No screenshot update is included; release-preparation owns that.
- The test suite already asserts `Active: Sky` and `Active: Custom`, which remain unchanged, so no test updates are required.
