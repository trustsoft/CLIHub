# Proposal: Settings CLI Agents Page — Plugin Actions Menu

## Why

The CLI Agents page header (`src/CLIHub/Views/SettingsAgentsPage.xaml:99-205`) packs six buttons into
one row: "Detect", "Check updates", "Install Plugin…", "Browse Plugins…", "Open Plugins Folder", and
"Reload Catalog". The last four manage the plugin catalog rather than individual agents, and the row is
visually crowded compared with the Launch window, which already solves the same "several related
actions in a header" problem with one data-driven "ACTIONS" dropdown
(`src/CLIHub/Views/LaunchWindow.xaml:126-153`, `:282-309`). The user asked to combine the four
plugin-catalog buttons into an Actions menu matching that existing pattern, leaving Detect and Check
updates as standalone header actions because they act on agents, not the plugin catalog.

## What Changes

- Replace the four header `Button` elements for "Install Plugin…", "Browse Plugins…", "Open Plugins
  Folder", and "Reload Catalog" (`SettingsAgentsPage.xaml:144-203`) with one `ToggleButton` labelled
  "ACTIONS" plus an in-tree `Popup` menu, built the same way as the Launch window's pane Actions menus:
  `ToggleButton` (`PaneActionsToggle` style) + `Popup` (`Placement="Bottom"`, `StaysOpen="False"`,
  `AllowsTransparency="True"`, `PopupAnimation="Fade"`, `VerticalOffset="-1"`) hosting a `Border`
  (popover style) with an `ItemsControl` bound to an `IReadOnlyList<MenuAction>`.
- "Detect" and "Check updates" remain unchanged, standalone header buttons immediately to the left of
  the new Actions toggle.
- Add a `PluginActions` property to `AgentManagementViewModel`, built once from the four existing
  commands (`InstallPluginCommand`, `BrowsePluginsCommand`, `OpenPluginsFolderCommand`,
  `ReloadCatalogCommand`) and their current glyphs and labels, mirroring
  `LaunchWindowActionBuilder.Build` (`src/CLIHub/ViewModels/Launch/LaunchWindowActionBuilder.cs:52-71`).
  No new abstraction is introduced; the existing `MenuAction` type
  (`src/CLIHub/ViewModels/Menu/MenuAction.cs`) is reused as-is.
- Move the four reusable Actions-menu styles (`ActionsPopover`, `ActionsMenuEntryButton`,
  `ActionsMenuEntryToggle`, `PaneActionsToggle`) out of the window-scoped
  `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml` into a new application-level dictionary merged by
  `src/CLIHub/Themes/Common/SettingsStyles.xaml`, so both `LaunchWindow.xaml` and
  `SettingsAgentsPage.xaml` can reach them. See `design.md` for the exact file and merge-order decision
  and the verification that this does not change `LaunchWindow`'s rendered output.
- Add settings-page-local item templates (action button, separator, template-switching
  `ContentControl`) and code-behind handlers (`OnActionsPopupOpened` for right-alignment,
  `OnActionsMenuPreviewKeyDown`/window `OnKeyDown` equivalent for Escape, a click handler that closes
  the popup) to `SettingsAgentsPage.xaml` / `SettingsAgentsPage.xaml.cs`, following the Launch window's
  rationale that an item template wiring a code-behind `Click` handler must live in a file with
  code-behind, not in a shared styles dictionary.
- Browse Plugins keeps its existing disabled state when the feed URL is configured off
  (`CanBrowsePlugins`, `AgentManagementViewModel.cs:1125`): the menu entry's `IsEnabled` follows
  `Command.CanExecute`, identical to how `Button.IsEnabled` already worked.
- No command, service, or `CLIHub.Core` change. No change to Install Plugin's dialog flow, Browse
  Plugins' feed behavior, Open Plugins Folder's target, or Reload Catalog's rescan behavior.

## Non-Goals

- Changing which plugin actions exist, their commands, their enabled logic, their labels, or their
  icons.
- Touching "Detect" or "Check updates" — they stay standalone `SectionHeaderChipButton` buttons.
- Converting any other Settings page header, footer, or dialog control to the Actions-menu pattern.
- Adding checkable/toggle entries, separators, or submenus to the new menu (the four actions render as
  plain command entries, matching the brief).
- Changing the AGENTS section header layout beyond consolidating the four buttons into one toggle.
- Refreshing README screenshots (release-preparation owns that per `AGENTS.md`).

## Impact

- `src/CLIHub/Views/SettingsAgentsPage.xaml` — header markup and new item templates/resources.
- `src/CLIHub/Views/SettingsAgentsPage.xaml.cs` — new code-behind handlers (currently a one-line
  `InitializeComponent` class).
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — new `PluginActions` property.
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — decide and document whether
  `PluginActions` is added to the interface (see `design.md`).
- `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml` — four styles removed (relocated).
- `src/CLIHub/Themes/Common/SettingsStyles.xaml` (or a new dictionary it merges) — four styles added.
- `src/CLIHub/Views/LaunchWindow.xaml` — no visual or behavioral change; only the resource dictionary
  that defines the shared styles it already references changes.
- `openspec/specs/preferences-ui/spec.md` — MODIFIED "CLI Agents page management" requirement: adds a
  sentence and new scenario(s) describing the grouped "Actions" menu; every existing scenario name and
  body is preserved unchanged.
- Tests: a new `AgentManagementViewModelTests` test (or a new partial file) asserting `PluginActions`
  exposes the four expected labels, glyphs, and command instances in order, and that the Browse Plugins
  entry's command reflects the existing `CanBrowsePlugins` gate. No XAML/UI automated test (the
  repository has none for layout).

## Dependencies

None. This change is independent and has no blocking or dependent OpenSpec changes.
