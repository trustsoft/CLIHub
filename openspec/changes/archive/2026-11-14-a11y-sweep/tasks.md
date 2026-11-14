# Accessibility Sweep Tasks

## Stage 1: DecorativeGlyph Element

- [x] 1.1 Create `src/CLIHub/Controls/DecorativeGlyph.cs` with sealed class deriving from TextBlock, overriding `OnCreateAutomationPeer()` to return null, XML docs on class and override
- [x] 1.2 Build and verify no compilation errors

**Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

## Stage 2: MenuAction Extension

- [x] 2.1 Add `public string? Tooltip { get; init; }` to `src/CLIHub/ViewModels/Shared/MenuAction.cs` with XML doc
- [x] 2.2 Add `public string? AccessibleName { get; init; }` with XML doc
- [x] 2.3 Add `public string EffectiveAccessibleName => string.IsNullOrEmpty(this.AccessibleName) ? this.Label : this.AccessibleName;` with XML doc
- [x] 2.4 Create `tests/CLIHub.Tests/ViewModels/MenuActionTests.cs` with 3 unit tests (null AccessibleName, empty AccessibleName, provided AccessibleName), following test naming pattern `Condition_ExpectedResult`
- [x] 2.5 Build and run tests

**Gate:**
```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~MenuActionTests"
```

## Stage 3: Actions Menu Template Bindings

- [x] 3.1 Add `ToolTip="{Binding Tooltip}"` and `AutomationProperties.Name="{Binding EffectiveAccessibleName}"` to Button in `src/CLIHub/Views/SettingsAgentsPage.xaml` ActionsMenuActionTemplate (lines 59-74)
- [x] 3.2 Add `AutomationProperties.Name="Plugin catalog actions"` to ACTIONS ToggleButton at line 171 in SettingsAgentsPage.xaml
- [x] 3.3 Add `ToolTip="{Binding Tooltip}"` and `AutomationProperties.Name="{Binding EffectiveAccessibleName}"` to Button in `src/CLIHub/Views/LaunchWindow.xaml` ActionsMenuActionTemplate (lines 49-64)
- [x] 3.4 Add the same bindings to ToggleButton in LaunchWindow.xaml ActionsMenuToggleTemplate (lines 66-81)
- [x] 3.5 Build and verify no XAML compilation errors

**Gate:**
```powershell
dotnet build CLIHub.sln
```

## Stage 4: Pattern Chip Accessible Name

- [x] 4.1 Add `AutomationProperties.Name="{Binding StringFormat='Ignored pattern {0}, Tab reveals the remove button'}"` to ChipBorder in `src/CLIHub/Views/SettingsProjectsPage.xaml` at line 226
- [x] 4.2 Build and verify

**Gate:**
```powershell
dotnet build CLIHub.sln
```

## Stage 5: Decorative Glyph Conversion (20 sites)

- [x] 5.1 Replace TextBlock with DecorativeGlyph (preserving all attributes) in `src/CLIHub/Themes/Common/SettingsStyles.xaml` at lines 228-233 (SettingCard icon)
- [x] 5.2 Replace TextBlock with DecorativeGlyph at SettingsStyles.xaml lines 418-423 (SummaryCard icon)
- [x] 5.3 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsWindow.xaml` at line 104 (status chip glyph)
- [x] 5.4 Replace TextBlock with DecorativeGlyph at SettingsWindow.xaml lines 297-302 (sidebar page icon)
- [x] 5.5 Replace TextBlock with DecorativeGlyph at SettingsWindow.xaml lines 359-365 (page-header emblem)
- [x] 5.6 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` at lines 23-26 (Reset button glyph)
- [x] 5.7 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsAppearancePage.xaml` at lines 103-109 (custom theme swatch)
- [x] 5.8 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsProjectsPage.xaml` at lines 49-54 (watched-folder emblem)
- [x] 5.9 Replace TextBlock with DecorativeGlyph at SettingsProjectsPage.xaml lines 133-138 (add-folder input glyph)
- [x] 5.10 Replace TextBlock with DecorativeGlyph at SettingsProjectsPage.xaml lines 283-287 (add-pattern input glyph)
- [x] 5.11 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` at lines 55-60 (add-project input glyph)
- [x] 5.12 Replace TextBlock with DecorativeGlyph at SettingsProjectRegistryPage.xaml lines 114-119 (project emblem)
- [x] 5.13 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Themes/Common/SettingsControls.xaml` at lines 277-282 (checkmark in toggle)
- [x] 5.14 Replace TextBlock with DecorativeGlyph at SettingsControls.xaml lines 415-420 (ComboBox chevron)
- [x] 5.15 Replace TextBlock with DecorativeGlyph at SettingsControls.xaml lines 552-559 (menu chevron)
- [x] 5.16 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/SettingsAgentsPage.xaml` at lines 66-72 (Actions button glyph)
- [x] 5.17 Replace TextBlock with DecorativeGlyph at SettingsAgentsPage.xaml lines 144-147 (Detect button glyph)
- [x] 5.18 Replace TextBlock with DecorativeGlyph at SettingsAgentsPage.xaml lines 160-163 (Check updates button glyph)
- [x] 5.19 Replace TextBlock with DecorativeGlyph in `src/CLIHub/Views/LaunchWindow.xaml` at lines 54-60 (Actions button glyph)
- [x] 5.20 Replace TextBlock with DecorativeGlyph at LaunchWindow.xaml lines 71-77 (Actions toggle glyph)
- [x] 5.21 Build and verify no visual regressions

**Gate:**
```powershell
dotnet build CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
```
(Verify Settings and Launch windows render correctly; no missing glyphs, misaligned text, or style changes)

## Stage 6: SMOKE (Manual Screen Reader Testing)

**IMPORTANT:** This stage is user-confirmed. Commit and archive wait for smoke result.

- [x] 6.1 Launch CLIHub and open Settings
- [x] 6.2 Run Windows Narrator or Accessibility Insights: verify decorative glyphs (page-header icon, sidebar page glyphs, SettingCard icons, status chip glyph, input-field decorations, checkmarks, chevrons) are NOT announced or visible in the UIA control view
- [x] 6.3 Verify non-decorative glyphs (icon-only buttons with ToolTips) are still announced
- [x] 6.4 Navigate to CLI Agents page, Tab to ACTIONS toggle: verify announced name is "Plugin catalog actions"
- [x] 6.5 Open the ACTIONS menu: verify menu entries are announced with meaningful names (fallback to Label or explicit AccessibleName)
- [x] 6.6 Navigate to Projects and Paths page, Tab to an ignored-pattern chip: verify announced name includes "Ignored pattern <name>, Tab reveals the remove button"
- [x] 6.7 Press Tab from the chip: verify remove button is announced
- [x] 6.8 Launch the Launch window, verify Actions menus have accessible names and tooltips
- [x] 6.9 Verify Launch window footer buttons (icon-only) still announce correctly via ToolTip

**Smoke result:** (awaiting user confirmation before commit/archive)

## Stage 7: OpenSpec Update

- [x] 7.1 Run `graphify update .` to refresh the graph after code changes
- [x] 7.2 Verify OpenSpec validation passes

**Gate:**
```powershell
graphify update .
openspec validate a11y-sweep
openspec validate --all
```

## Testing Notes

- **Unit-testable:** `MenuAction.EffectiveAccessibleName` property is tested in `tests/CLIHub.Tests/ViewModels/MenuActionTests.cs` (3 cases: null, empty, provided AccessibleName).
- **Manual smoke required:** XAML automation peer behavior (DecorativeGlyph returning null from OnCreateAutomationPeer, AutomationProperties.Name bindings) requires Windows Narrator or Accessibility Insights inspection. Verify decorative glyphs are excluded from the UIA tree and menu/chip accessible names are announced correctly.
- **Visual verification:** After glyph conversion (Stage 5), launch CLIHub and verify Settings and Launch windows render identically (no missing glyphs, no layout shifts).
