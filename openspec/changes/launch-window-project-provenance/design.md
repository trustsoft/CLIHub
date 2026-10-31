# Design: launch-window-project-provenance

## Current state (verified in code at HEAD `ccf4285`)

| Concern | Today | File:Line |
|---|---|---|
| ProjectModel provenance | `IsManual` (bool), `SourceMark` ("M"/"D"), `SourceLabel` ("Manual"/"Discovered") — all derived from `Source` enum | `src/CLIHub/ViewModels/Projects/ProjectModel.cs:106-128` |
| Settings page provenance mark | `<TextBlock Style="{StaticResource ProjectProvenanceMark}" AutomationProperties.Name="{Binding SourceLabel}" Text="{Binding SourceMark}" ToolTip="{Binding SourceLabel}"/>` in the project name row | `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:151-155` |
| ProjectProvenanceMark style | Defined locally in `SettingsProjectRegistryPage.xaml` resources at `:10-24`; `FontSize=Sizing.FontSize.Section`, `FontWeight=SemiBold`, `Foreground=TextMutedBrush` (default), with a `DataTrigger` on `CanRemove=True` (manual projects) → `Foreground=AccentSelectionBrush` | `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:10-24` |
| Launch window project row | `DataTemplate` at `:161-230` with logo, text StackPanel (name, path via MultiBinding), favorite star in last grid column; no provenance binding | `src/CLIHub/Views/LaunchWindow.xaml:156-225` |
| Launch window project name | `<TextBlock FontSize="{StaticResource Sizing.FontSize.Name}" Foreground="{DynamicResource TextPrimaryBrush}" Text="{Binding Name}" TextTrimming="CharacterEllipsis"/>` at `:194-197` | `src/CLIHub/Views/LaunchWindow.xaml:194-197` |
| Path trimming | The path TextBlock (`:201-219`) uses a width-aware `PathDisplay` MultiBinding over `TextHost.ActualWidth` — the text StackPanel at `:190-220` occupies Grid.Column="2" (star column) so it stretches to fill available space; the row comments (`:166-173`) note the measured width equals exactly the space available | `src/CLIHub/Views/LaunchWindow.xaml:201-219` |
| Resource organization | `LaunchWindowStyles.xaml` merges `Controls.xaml` only; per-window styles are keyed; the application merges `SettingsStyles.xaml` at app level because Settings pages are UserControls built before window attachment | `docs/architecture/ui.md:12-38` |
| Shared model instances | `ProjectModelCatalog` owns one `ProjectModel` instance per project Id, shared by the launch window and Settings registry; both surfaces display the same instances | `docs/architecture/ui.md:95-106`, archived `introduce-project-model` change design |

Working tree at HEAD is clean except for `.opencode/pipeline.json` (concurrent planner state).

## Decision 1: style location — launch-window-local copy

The `ProjectProvenanceMark` style is currently defined in `SettingsProjectRegistryPage.xaml` resources with these visual values:
- `FontSize=Sizing.FontSize.Section` (shared token)
- `FontWeight=SemiBold`
- `VerticalAlignment=Center`
- `Foreground=TextMutedBrush` (palette brush, dynamic)
- Manual projects (CanRemove=True) → `Foreground=AccentSelectionBrush` (accent with selection alpha, dynamic)

Two paths:

1. **Promote to shared dictionary** — move the style into `Controls.xaml` (or create a new `ProjectStyles.xaml` if more project-specific styles are anticipated). Both surfaces would reference the same keyed style. Benefit: single definition, consistent appearance guaranteed. Cost: touches the Settings page (to remove its local definition) and the shared resource tree used by every window; this also makes change A overlap with `project-path-correction` in `SettingsProjectRegistryPage.xaml`, preventing independent commits.

2. **Keep local, define an equivalent style in `LaunchWindowStyles.xaml`** — copy the style definition with identical visual values into the launch-window resource dictionary, which is already the home of launch-window-specific keyed styles. Benefit: no shared resource change, no edit to the proven Settings page, and change A stays self-contained. Cost: a 15-line token-based duplication; a future style change needs two edits.

**Chosen:** Option 2, a `ProjectProvenanceMark` style in `LaunchWindowStyles.xaml` with the exact visual values above, plus a comment pointing to `SettingsProjectRegistryPage.xaml` as the sibling definition. Rationale: the launch window's resources are already window-scoped (`LaunchWindowStyles.xaml` merges `Controls.xaml`), the Settings page's local style has passed visual review and should not be disturbed for a display-only change, and keeping A disjoint from F lets each finding ship as its own commit. The duplication is small and token-based; it is recorded in `docs/backlog.md` with a trigger to promote the style into `Controls.xaml` when a third surface needs it or when the mark's visual values next change.

## Decision 2: launch-window layout placement

The launch-window project row has this structure (`:163-229`):
- Grid with 4 columns: logo (Auto), fixed gap (Sizing.ProjectRow.TextGap), text (Star), favorite star (Auto).
- The text column (Grid.Column="2") is a StackPanel named `TextHost` containing the name TextBlock and the path TextBlock with its width-aware trimming.
- The favorite star is Grid.Column="3" with left margin 8, vertically centered.

Placement options for the provenance mark:

1. **After the name, inside a horizontal StackPanel** — name and mark in one row inside the text StackPanel. Rejected: a horizontal StackPanel measures its children with unbounded width, so the name TextBlock's `TextTrimming` stops working and a long project name can overflow the row. The Settings page avoids this with a fixed `MaxWidth` (`Sizing.SettingsProjectRow.NameMaxWidth` = 360), but that value does not adapt to the launch window's narrower pane.

2. **Two-column Grid in the name row** — name in a `*` column, mark in an `Auto` column inside the text StackPanel. The name receives the remaining width and keeps its adaptive `CharacterEllipsis` trimming; the mark keeps its constant width and sits at the trailing edge of the text column, immediately before the favorite-star column. The path TextBlock and its `TextHost.ActualWidth` multibinding stay unchanged below.

3. **Grid.Column="3" before the favorite star** — provenance mark in the last column, with the star pushed further right. Clean separation, no text-layout impact. Rejected as unnecessary once the two-column Grid keeps the mark in the name row; also splits the row's trailing metadata into two groups.

**Chosen:** Two-column Grid (option 2). Rationale: it preserves the name's adaptive trimming (the pre-existing behavior), keeps the path trimming untouched, and places the mark in the name row as a trailing badge. The mark is one letter with modest font size and an 8 DIP left margin; its constant width reduces the name's available width by ~16 DIP, which the `*` column absorbs.

Implementation: replace the standalone name TextBlock in the text StackPanel with a `Grid` containing `ColumnDefinition Width="*"` (name TextBlock, unchanged bindings, `Grid.Column="0"`) and `ColumnDefinition Width="Auto"` (provenance mark TextBlock, `Grid.Column="1"`, `Margin="8,0,0,0"`, bindings from the Settings page: `Style=ProjectProvenanceMark`, `Text=SourceMark`, `ToolTip=SourceLabel`, `AutomationProperties.Name=SourceLabel`). Do not change the path TextBlock or the `TextHost` name.

## Decision 3: accessibility and tooltips

The Settings page bindings are:
```xaml
<TextBlock Style="{StaticResource ProjectProvenanceMark}"
           AutomationProperties.Name="{Binding SourceLabel}"
           Text="{Binding SourceMark}"
           ToolTip="{Binding SourceLabel}"/>
```

The same pattern applies to the launch window: `AutomationProperties.Name` exposes the full label ("Manual"/"Discovered") to screen readers, and `ToolTip` shows it on hover. No deviation from the Settings page is needed.

## Decision 4: test strategy

The data path for `ProjectModel.SourceMark` and `SourceLabel` is unit-tested:
- `tests/CLIHub.Tests/ViewModels/Projects/ProjectModelCatalogTests.cs:93-111` — "EnrichAndUpdate_Sets model properties from enriched data" scenario covers all `ProjectModel` fields including `Source`, which drives `IsManual`, `CanRemove`, `SourceMark`, and `SourceLabel`.

The XAML binding itself (TextBlock visibility, text, and tooltip at runtime) is not unit-testable in this repository — WPF view tests would require a running dispatcher and real XAML instantiation, which the test projects do not support.

**Test plan:** manual smoke only. The focused regression scenario is:
1. Start the application with at least one manual project and one discovered project (or use the Projects page to add/remove as needed).
2. Open the launch window and verify each project row shows the correct provenance mark ("M" for manual, "D" for discovered) after the project name.
3. Hover over the mark and verify the tooltip displays "Manual" or "Discovered".
4. Use a screen reader or accessibility inspector to confirm the mark's accessible name is the full label.
5. Verify the path trimming still works correctly (resize the window or drag the pane splitter; the path should re-trim without overlapping the favorite star or mark).

If the launch-window builder (`tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`) is extended to produce `ProjectModel` instances with mixed provenance, that builder may serve a future focused view-model test, but no new unit test is added as part of this change — the data is already tested, and the XAML binding is covered by smoke.

## Alternatives considered

- **Do not show provenance in the launch window** — rejected: the brief explicitly requests it, and provenance is stable context that helps users distinguish manual vs discovered projects when selecting.
- **Show full "Manual"/"Discovered" label instead of compact mark** — rejected: the launch window is width-constrained and uses a compact layout; a full word would consume too much horizontal space and push the path trimming into an awkward state. The compact mark + tooltip is the established pattern from the Settings page.
- **Place mark in Grid.Column="3" before the favorite star** — considered: clean separation, no text-layout coupling. Rejected because provenance complements project identity (the name), so grouping them is more semantically correct. The favorite star is an action/state indicator and already occupies the trailing position; inserting provenance there would split visual groups.
- **Add an Icon/glyph instead of text mark** — rejected: no icon font glyph naturally represents "manual" vs "discovered", and introducing imagery for a simple state would add visual complexity. The text mark "M"/"D" is compact, unambiguous, and matches the Settings page.
- **Style duplication (keep local in both pages)** — rejected: duplication risks divergence; if the style changes, both pages must be updated. Promoting to shared `Controls.xaml` is one small keyed style and prevents future inconsistency.

## Open questions for the implementation session

1. Exact left margin on the mark TextBlock inside the horizontal StackPanel wrapping name + mark — 8 DIP matches the Settings page spacing; confirm it reads well in the compact launch-window row during smoke.
2. Visual verification during smoke: does the mark's color (muted for discovered, accent-selection for manual) provide sufficient contrast in both light and dark themes? The Settings page already passed visual review with these colors, so consistency should be safe, but confirm in the launch-window context.

## Spec deltas

`openspec/specs/project-management/spec.md` — Requirement "Project provenance" (`:147-169`):
- MODIFIED requirement preserving all five original scenario names.
- One added scenario, `#### Scenario: Launch window shows provenance mark`: WHEN a project is
  displayed in the launch window, THEN its compact provenance mark ("M"/"D") is shown after the
  project name, the full label ("Manual"/"Discovered") is the mark's tooltip and accessible name,
  and the mark uses the shared `ProjectProvenanceMark` style for consistency with the Settings page.

The drafted delta is `openspec/changes/launch-window-project-provenance/specs/project-management/spec.md`.
No scenario is added to "Enriched project display models": `ProjectModel.SourceMark`/`SourceLabel`
already exist and are unit-tested; this change only renders them on a second surface.
