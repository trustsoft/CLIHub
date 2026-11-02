# Tasks: Selection by border color only

## Stage 1: Selection marker

- [x] 1.1 In `src/CLIHub/Themes/ThemePickerData.xaml`, drop the `SelectionRing` border from `ThemeOptionItem` and paint `Card.BorderBrush` with `AccentBrush` on `IsSelected` and `IsKeyboardFocused`, keeping the hover stroke below them in trigger order
- [x] 1.2 In `src/CLIHub/Views/SettingsAppearancePage.xaml`, remove the theme preview check badge and the accent dot check together with their selection triggers, leaving the custom swatch glyph visible when selected

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 2: Spec delta

- [x] 2.1 Write `openspec/changes/selection-border-indicator/specs/preferences-ui/spec.md` with MODIFIED "Application theme selector" and "Accent color selector" requirements, preserving the original scenario names and replacing the ring/check marker with the accent-colored border

Gates:

```powershell
openspec validate --all
```

## Verification

- [x] Build passes: `dotnet build CLIHub.sln`
- [x] Tests pass: `dotnet test CLIHub.sln`
- [x] OpenSpec validation passes: `openspec validate --all`
- [x] Smoke: the selected theme and accent cards show only an accent-colored border (no ring, no check badge, no check in the dot); hovering an unselected card raises its border; the custom accent entry still shows its glyph when selected
