# Tasks: Settings CLI Agents Page — Plugin Actions Menu

## Stage 1: View model — `PluginActions`

- [x] 1.1 In `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, add `public
      IReadOnlyList<MenuAction> PluginActions { get; }`, initialized in the constructor (after the four
      plugin commands are assigned, around current line 113) from `InstallPluginCommand`,
      `BrowsePluginsCommand`, `OpenPluginsFolderCommand`, `ReloadCatalogCommand`, each paired with its
      existing label and glyph (`IconGlyphs.Add`, `IconGlyphs.Search`, `IconGlyphs.FolderOpen`,
      `IconGlyphs.SyncFolder`), in that exact order; add an XML doc summary. Do not add this member to
      `IAgentManagementSection` (design.md D5).
- [x] 1.2 Build: `dotnet build CLIHub.sln`.

## Stage 2: View model tests

- [x] 2.1 Add `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.PluginActions.cs` as a
      `partial class AgentManagementViewModelTests`, with a test asserting `PluginActions` has exactly
      four entries in order with labels `"Install Plugin…"`, `"Browse Plugins…"`, `"Open Plugins
      Folder"`, `"Reload Catalog"` and glyphs `IconGlyphs.Add`, `IconGlyphs.Search`,
      `IconGlyphs.FolderOpen`, `IconGlyphs.SyncFolder`, and that each entry's `Command` is `Same` as the
      corresponding `AgentManagementViewModel` command property.
- [x] 2.2 Add a second test in the same file asserting the Browse Plugins entry's
      `Command.CanExecute(null)` is `false` when the feed URL preference is disabled (empty string),
      using the existing `AgentManagementViewModelBuilder` and preferences-store setup pattern already
      used by the current `CanBrowsePlugins` tests in `AgentManagementViewModelTests.Plugins.cs` (locate
      and reuse that exact setup).
- [x] 2.3 Build and test: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`. Confirm the two new tests
      pass and no existing test regresses.

## Stage 3: Shared styles — extract to an application-level dictionary

- [x] 3.1 Create `src/CLIHub/Themes/Common/ActionsMenuStyles.xaml`, merging only
      `/Themes/Common/Controls.xaml`, and move the four styles `ActionsPopover`, `ActionsMenuEntryButton`,
      `ActionsMenuEntryToggle`, `PaneActionsToggle` into it verbatim (byte-identical property values) from
      `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml` (currently lines 46-184). Remove those four
      styles from `LaunchWindowStyles.xaml`, leaving every other style in that file untouched.
- [x] 3.2 In `src/CLIHub/Views/LaunchWindow.xaml` (`Window.Resources` / `MergedDictionaries`, currently
      line 31), add a merge of `/Themes/Common/ActionsMenuStyles.xaml` alongside the existing
      `/Themes/Common/LaunchWindowStyles.xaml` merge, so `LaunchWindow` still resolves all four styles.
- [x] 3.3 In `src/CLIHub/Themes/Common/SettingsStyles.xaml` (`MergedDictionaries`, currently lines
      13-25), add a merge of `/Themes/Common/ActionsMenuStyles.xaml` to the existing list (alongside
      `Controls.xaml`, `PickerData.xaml`, `SettingsControls.xaml`), so the four styles become
      application-level and reachable from `SettingsAgentsPage.xaml`.
- [x] 3.4 Build: `dotnet build CLIHub.sln`. Run `dotnet test CLIHub.sln` and confirm
      `ApplicationResourceKeyTests` and `XamlResourceReferenceTests` (in `tests/CLIHub.Tests/Themes/`)
      still pass — they assert no duplicate application-level key and every `StaticResource`/
      `DynamicResource` reference resolves; a failure here means a style or a brush reference was not
      moved correctly.

## Stage 4: Settings page — Actions menu markup and code-behind

- [x] 4.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml` `<UserControl.Resources>`, add the four
      `DataTemplate`s needed for the menu: an action-button template (`Button Click=
      "OnMenuActionInvoked" Command="{Binding Command}" Style="{StaticResource
      ActionsMenuEntryButton}"` with the glyph+label `StackPanel`, mirroring
      `LaunchWindow.xaml:45-60`), a separator template (mirroring `:79-81`), and the template-switching
      `ContentControl` item template (mirroring `:83-99`). A checkable-toggle template is not required
      for this change's four plain command entries (design.md Non-Goals) but may be omitted entirely if
      the template-switching `ContentControl` only needs the separator/default cases.
- [x] 4.2 Replace the four `Button` elements for "Install Plugin…", "Browse Plugins…", "Open Plugins
      Folder", "Reload Catalog" (currently `SettingsAgentsPage.xaml:144-203`) with one `ToggleButton
      x:Name="PluginActionsButton" Content="ACTIONS" Style="{StaticResource PaneActionsToggle}"` plus a
      `Popup x:Name="PluginActionsPopup"` bound the same way as `LaunchWindow.xaml:130-153`
      (`IsOpen` two-way to the toggle's `IsChecked`, `Opened="OnActionsPopupOpened"`,
      `Placement="Bottom"`, `PlacementTarget={Binding ElementName=PluginActionsButton}`,
      `PopupAnimation="Fade"`, `StaysOpen="False"`, `VerticalOffset="-1"`), containing a `Grid
      PreviewKeyDown="OnActionsMenuPreviewKeyDown"` with the `Border Style="{StaticResource
      ActionsPopover}"` hosting an `ItemsControl ItemsSource="{Binding AgentsState.PluginActions}"
      ItemTemplate="{StaticResource <the template-switching key from 4.1>}"`, plus the masking
      `Rectangle` (mirroring `LaunchWindow.xaml:144-151`). Keep "Detect" and "Check updates" exactly as
      they are, immediately to the left of the new toggle.
- [x] 4.3 In `src/CLIHub/Views/SettingsAgentsPage.xaml.cs`, add: `OnActionsPopupOpened` (measure the
      popup child, right-align by setting `HorizontalOffset`, mirroring
      `LaunchWindow.xaml.cs:254-265`), `OnActionsMenuPreviewKeyDown` (Escape closes
      `PluginActionsPopup`, mirroring `:140-147`), and `OnMenuActionInvoked` (closes
      `PluginActionsPopup` after a click, mirroring `:271`). A multi-popup `CloseActionsMenus()` helper
      is not needed here (design.md D4) — close the single popup directly in each handler.
- [x] 4.4 Build: `dotnet build CLIHub.sln`.

## Stage 5: Spec delta

- [x] 5.1 Verify `openspec/changes/settings-agents-actions-menu/specs/preferences-ui/spec.md` (already
      authored in this change) contains exactly one MODIFIED requirement, "CLI Agents page management",
      with every existing requirement sentence and every existing scenario name and body preserved
      byte-for-byte, plus one added sentence and new scenario(s) describing the grouped Actions menu. Do
      not rewrite the delta; only confirm it still matches the implemented behavior (menu label, which
      four actions it contains, and that Detect/Check updates stay standalone) and adjust the new
      scenario wording if the implementation session changed any of those specifics from design.md's
      assumptions (see design.md Open Questions #1-2).
- [x] 5.2 Validate: `openspec validate --all`.

## Stage 6: Final verification

- [x] 6.1 Build: `dotnet build CLIHub.sln`.
- [x] 6.2 Test: `dotnet test CLIHub.sln`. Confirm no regression and the two new tests from Stage 2 pass.
- [x] 6.3 Validate: `openspec validate --all`.
- [x] 6.4 Manual smoke test (requires running the app; report the result to the user before archiving):
      open Settings → CLI Agents, confirm "Detect" and "Check updates" remain standalone buttons, click
      the new "ACTIONS" toggle and confirm its popup lists "Install Plugin…", "Browse Plugins…", "Open
      Plugins Folder", "Reload Catalog" in that order with their existing icons; confirm the popup's
      right edge aligns with the toggle; confirm clicking an entry runs its action and closes the popup;
      confirm Escape closes the popup without closing the Settings window; confirm Browse Plugins
      renders disabled/dimmed when the plugin feed preference is set to the disabled empty string; open
      the Launch window and confirm its Projects/Agents Actions menus render and behave exactly as
      before (no regression from the style relocation).
- [x] 6.5 Update graph: `graphify update .`.

## Notes

- This change does not touch `CLIHub.Core`, any command's behavior, or the plugin feed/install/reload
  logic — only how the four existing commands are presented.
- No README screenshot update is included; release-preparation owns that per `AGENTS.md`.
- Do not run `openspec archive` or `git commit` as part of these tasks; implementation/verification
  stops at 6.5, per the repository's commit/archive approval policy.
