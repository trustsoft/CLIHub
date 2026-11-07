## 1. ViewModel: new properties and interface additions

- [x] 1.1 In `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs`, add the following declarations:
  - `int MissingFolderCount { get; }` — promotes the VM-only property to the interface for contract hygiene (XAML depends on it; see design.md D3/open question 2); XML doc: "Number of projects whose folder no longer exists."
  - `string MissingFolderChip { get; }` — XML doc: "The compact chip label shown inline with the project count, e.g. '1 missing'; empty string when no folder is missing."
  - `string BadgeTooltipText { get; }` — XML doc: "The shared tooltip for the chip and icon badge; combines the folder-health summary with a description; empty string when no folder is missing."
  - Also update the existing `FolderStatusSummary` XML doc from "The folder-health summary shown as the PROJECTS card's alert line" to "The folder-health summary text, e.g. '1 folder missing'; used as part of the badge tooltip; empty string when no folder is missing."
- [x] 1.2 In `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`, add the three property implementations directly below the existing `FolderStatusSummary` block (lines 77-83):
  - `MissingFolderChip`: `0 => string.Empty; 1 => "1 missing"; var count => $"{count} missing"`
  - `BadgeTooltipText`: `HasMissingFolders ? $"{FolderStatusSummary} · folders that no longer exist" : string.Empty`
  - Ensure the existing `MissingFolderCount` property carries `/// <inheritdoc />` now that the property is declared on the interface
- [x] 1.3 In `RaiseSummaryChanges` (line 300), add `OnPropertyChanged` calls for `MissingFolderChip` and `BadgeTooltipText` (alongside the existing five)
- [x] 1.4 Gate: `dotnet build CLIHub.sln` must exit 0 warnings/0 errors

## 2. ViewModel tests

- [x] 2.1 In `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`, add a test class block (or extend the existing fixture) with tests following the `MethodOrScenario_Condition_ExpectedResult` naming pattern:
  - `MissingFolderChip_WithOneMissingFolder_ReturnsCompactSingular` — asserts `"1 missing"`
  - `MissingFolderChip_WithMultipleMissingFolders_ReturnsCompactPlural` — asserts `"2 missing"` for 2 missing
  - `MissingFolderChip_WithNoMissingFolders_ReturnsEmpty` — asserts `string.Empty`
  - `BadgeTooltipText_WithOneMissingFolder_ReturnsCombinedString` — asserts `"1 folder missing · folders that no longer exist"`
  - `BadgeTooltipText_WithNoMissingFolders_ReturnsEmpty` — asserts `string.Empty`
  - Use the same `CreateSection` / `Project` / `MissingPath` / `ExistingFolder` helpers already established at lines 77-82 of the file
- [x] 2.2 Gate: `dotnet test CLIHub.sln` must pass all tests (expect the prior count + 5 new)

## 3. SummaryCard control surface

- [x] 3.1 In `src/CLIHub/Controls/SummaryCard.cs`, remove the three old DP registrations and CLR wrappers for `Alert` (lines 89-90), `AlertToolTip` (lines 95-96), and `IsAlertVisible` (lines 101-102), and their CLR properties (lines 175-201); update the class-level XML doc to remove the reference to "an optional danger-toned alert line" and replace it with "an optional danger inline chip and icon badge"
- [x] 3.2 Add four new DP registrations following the exact pattern of `DetailToolTipProperty` (lines 83-84) immediately after the removed block:
  - `BadgeTextProperty`: `typeof(string)`, default `default(string)`, XML doc: "The compact danger chip label shown inline with the value, e.g. '1 missing'; hidden when null or empty"
  - `BadgeTooltipProperty`: `typeof(string)`, default `default(string)`, XML doc: "Tooltip for both the inline chip and the icon count badge; one source of truth for the status description"
  - `IsBadgeVisibleProperty`: `typeof(bool)`, default `false`, XML doc: "Master visibility flag; when false both the chip and the icon count badge are hidden"
  - `BadgeCountProperty`: `typeof(string)`, default `default(string)`, XML doc: "Count label for the icon block overlay badge, e.g. '1'; hidden when null or empty"
- [x] 3.3 Add the four CLR wrappers in the same order, keeping the `get`/`set` pattern of the existing properties
- [x] 3.4 Gate: `dotnet build CLIHub.sln` must exit 0 warnings/0 errors

## 4. SummaryCard template

- [x] 4.1 In `src/CLIHub/Themes/Common/SettingsStyles.xaml`, in the `SummaryCard` `ControlTemplate`:
  - In the column-0 `StackPanel` (row 0), replace the bare `ValueText` `TextBlock` with a `WrapPanel`
    containing `ValueText` and the chip. Use `WrapPanel` — not a horizontal `StackPanel` — so
    `ValueText` retains its `TextTrimming="CharacterEllipsis"` behaviour on cards with long values
    (a horizontal `StackPanel` would give `ValueText` infinite measure width, stopping trimming):
    ```
    <WrapPanel>
        <TextBlock x:Name="ValueText" ... (all existing attributes unchanged) />
        <Border x:Name="BadgeChip"
                Margin="8,0,0,0"
                Padding="5,1"
                VerticalAlignment="Center"
                BorderBrush="{DynamicResource DangerBrush}"
                BorderThickness="1"
                CornerRadius="{StaticResource Sizing.CornerRadius.Chip}"
                ToolTip="{TemplateBinding BadgeTooltip}"
                Visibility="Collapsed">
            <TextBlock AutomationProperties.HelpText="{TemplateBinding BadgeTooltip}"
                       FontSize="{StaticResource Sizing.FontSize.Chip}"
                       FontWeight="SemiBold"
                       Foreground="{DynamicResource DangerBrush}"
                       Text="{TemplateBinding BadgeText}"/>
        </Border>
    </WrapPanel>
    ```
    Note: only the PROJECTS card combines a chip with a value, and its value is a short integer count.
    If a future card combines a long text value with a chip, the chip may wrap to the next line —
    this is acceptable and is documented in the comment added in task 4.3.
  - Wrap the icon column (column 1) `Border` and the count overlay `Border` in a `Grid` with
    `Width="24"` `Height="24"` to establish a local coordinate context for the badge overhang:
    ```
    <Grid Grid.Column="1"
          Width="24"
          Height="24"
          VerticalAlignment="Top">
        <Border ... (all existing icon Border attributes; remove Width/Height/VerticalAlignment — the outer Grid owns them) .../>
        <Border x:Name="BadgeCountOverlay"
                HorizontalAlignment="Right"
                VerticalAlignment="Top"
                MinWidth="15"
                Height="15"
                Margin="0,-5,-5,0"
                Padding="2,0"
                Background="{DynamicResource DangerBrush}"
                BorderBrush="{DynamicResource PopoverBgBrush}"
                BorderThickness="2"
                CornerRadius="6"
                Grid.ZIndex="1"
                ToolTip="{TemplateBinding BadgeTooltip}"
                Visibility="Collapsed">
            <TextBlock HorizontalAlignment="Center"
                       VerticalAlignment="Center"
                       FontSize="9"
                       FontWeight="Bold"
                       Foreground="{DynamicResource ChromeBgBrush}"
                       Text="{TemplateBinding BadgeCount}"/>
        </Border>
    </Grid>
    ```
  - Remove `AlertText` (lines 409-415) from the row-1 `StackPanel`
- [x] 4.2 In `ControlTemplate.Triggers` (currently lines 419-452): remove the three alert triggers
  (`IsAlertVisible=True`, `Alert=null`, `Alert=""`); add the following triggers in this order,
  exploiting WPF's last-match-wins evaluation to make null/empty always collapse regardless of
  `IsBadgeVisible`:
  1. `<Trigger Property="IsBadgeVisible" Value="True">` — sets `BadgeChip.Visibility=Visible`
     and `BadgeCountOverlay.Visibility=Visible`
  2. `<Trigger Property="BadgeText" Value="{x:Null}">` — sets `BadgeChip.Visibility=Collapsed`
     (fires after trigger 1; wins because it is later in the list)
  3. `<Trigger Property="BadgeText" Value="">` — sets `BadgeChip.Visibility=Collapsed`
  4. `<Trigger Property="BadgeCount" Value="{x:Null}">` — sets `BadgeCountOverlay.Visibility=Collapsed`
  5. `<Trigger Property="BadgeCount" Value="">` — sets `BadgeCountOverlay.Visibility=Collapsed`

  This structure mirrors the existing `Alert`/`Detail` pattern (lines 443-451) and is deterministic:
  `IsBadgeVisible=True` opens both indicators; null/empty text triggers (which fire whenever their
  condition is met, independently of `IsBadgeVisible`) always close the corresponding element.
  If `BadgeText` is null/empty with `IsBadgeVisible=True`, triggers 2/3 win over trigger 1 for
  the chip — the chip stays collapsed. If `BadgeCount` is null/empty with `IsBadgeVisible=True`,
  triggers 4/5 win for the overlay. The call site always supplies both a non-empty `BadgeText` and
  a non-empty `BadgeCount` when `IsBadgeVisible=True`, so both indicators are visible together in
  normal use; the null/empty triggers exist as a safety net consistent with the existing pattern.
- [x] 4.3 Update the comment above the style (lines 344-351) to reflect the new indicator design: remove the reference to "alert line" and record that the chip sits inline with the value and the count badge overlays the icon block's top-right corner
- [x] 4.4 Gate: `dotnet build CLIHub.sln` must exit 0 warnings/0 errors

## 5. Call site and accessibility binding

- [x] 5.1 In `src/CLIHub/Views/SettingsWindow.xaml`, in the `RegistrySummaryCards` `DataTemplate` (lines 188-208), update the PROJECTS `SummaryCard` element:
  - Remove `Alert="{Binding ProjectRegistry.FolderStatusSummary}"` (line 190)
  - Remove `AlertToolTip="Folders that no longer exist"` (line 191)
  - Remove `IsAlertVisible="{Binding ProjectRegistry.HasMissingFolders}"` (line 194)
  - Add `BadgeText="{Binding ProjectRegistry.MissingFolderChip}"`
  - Add `BadgeTooltip="{Binding ProjectRegistry.BadgeTooltipText}"`
  - Add `BadgeCount="{Binding ProjectRegistry.MissingFolderCount, StringFormat={}{0}}"`
  - Add `IsBadgeVisible="{Binding ProjectRegistry.HasMissingFolders}"`
  - Do NOT add `AutomationProperties.Name`; accessibility is handled via `AutomationProperties.HelpText`
    on the chip's inner `TextBlock` in the template (see task 4.1)
- [x] 5.2 Gate: `dotnet build CLIHub.sln` must exit 0 warnings/0 errors; `dotnet test CLIHub.sln` must pass all tests

## 6. Repository hygiene and spec validation

- [x] 6.1 Run `openspec validate --all` and confirm it passes (0 errors); record the output
- [x] 6.2 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it exits clean
- [x] 6.3 Run `graphify update .` to refresh the knowledge graph with the control, template, and VM changes
- [x] 6.4 Confirm `git status --short` shows only: the five changed source files (`SummaryCard.cs`, `SettingsStyles.xaml`, `SettingsWindow.xaml`, `IProjectRegistrySection.cs`, `ProjectRegistryViewModel.cs`), the two changed test files (`ProjectRegistryViewModelTests.cs`, `SummaryCardTests.cs`), and this change's directory; the pre-existing `D opencode.json` and the untracked mockup `mockup-projects-summary-card.html` are untouched and out of scope

## 7. Manual smoke (blocks commit/archive)

- [x] 7.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → Projects while the current session has at least one missing project folder (the repo itself satisfies this: `CLIHub` project folder is known missing). Verify:
  - The PROJECTS card has exactly three visual rows: label `PROJECTS`, value row with `14` (or your count) and chip `1 missing` in danger style, and detail `In the launch window`; no fourth line appears
  - The chip is positioned immediately after the value number with an ~8 DIP gap; the value row height matches the other cards' value row
  - The icon block shows the `Code` glyph with a small danger rounded square `1` badge at its top-right corner, overhanging the 24×24 block
  - Hovering the chip shows the tooltip `"1 folder missing · folders that no longer exist"` (or the pluralized form if multiple)
  - Hovering the icon block badge shows the same tooltip text
  - The three cards (PROJECTS, MANUAL, DISCOVERED) remain equal in height — no card is taller than the others
- [x] 7.2 Remove the missing-folder condition (re-add the project or temporarily point it to a valid path) and verify: the chip disappears, the icon badge disappears, the card returns to its plain three-line form, card heights remain equal
- [ ] 7.3 Verify accessibility with Narrator (Win+Enter) or Inspect.exe / Accessibility Insights:
  navigate to the PROJECTS card's chip element in the automation tree; confirm its `Name` reads
  `"1 missing"` (the chip text) and its `HelpText` reads the full tooltip string; report the
  actual result honestly — if the `HelpText` is not surfaced by Narrator in the running app,
  record that as an open finding rather than marking the task passed.
  **Not independently verified in this session:** the visual smoke was confirmed, but Narrator/Inspect
  was not run. `AutomationProperties.HelpText` is set on the chip `TextBlock` per design D7; verify
  before closing this as an accessibility follow-up.
- [x] 7.4 Record the smoke result in this tasks file; report to the user and wait for explicit `git commit` and `openspec archive` approval before proceeding.
  Visual smoke confirmed by the user on 2026-11-08; after the badge-shape tweak (18×18 rounded square,
  radius 6) the visual smoke was re-confirmed.
