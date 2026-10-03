## 1. Action model and palette

- [x] 1.1 Add `MenuAction` (`ObservableObject`) with `Label`, `Glyph`, `Command`, `IsCheckable`, `IsChecked`, `IsSeparator` in `src/CLIHub/ViewModels/`; confirm `dotnet build CLIHub.sln` succeeds and the type carries XML docs per the repo convention
- [x] 1.2 Keep the palette's existing `DangerBrush` for other windows and add no icon-color brushes, because every actions-menu glyph uses the neutral `TextMutedBrush`
- [x] 1.3 Add the `Sizing.ActionsMenu.MinWidth` floor token to `src/CLIHub/Themes/Sizing.xaml`; confirm `dotnet build CLIHub.sln` succeeds

## 2. View model wiring

- [x] 2.1 Build `ProjectsActions` and `AgentsActions` in the `LaunchWindowViewModel` constructor from the existing commands and `IconGlyphs`, including separators, and expose them as `IReadOnlyList<MenuAction>`; confirm the window still lists the same actions as before
- [x] 2.2 Wire the availability filter entry's `IsChecked` to `ShowOnlyProjectAgents` in both directions using the existing `_suppressFilterChange` guard; verify toggling the entry flips the filter and re-toggling the filter updates the entry

## 3. Popup and styles

- [x] 3.1 Add the popover style (rounded `PopoverBgBrush` card with shadow, content-sized with a `Sizing.ActionsMenu.MinWidth` floor) and the shared entry button/toggle style with hover, checked, and disabled triggers to `LaunchWindowStyles.xaml`, including the open Actions control's merged-with-the-menu state
- [x] 3.2 Add the `ItemsControl` item template with the template-switching `ContentControl` (separator hairline, checkable toggle, default command button) in `LaunchWindow.xaml` (it wires the window's click handler), and draw every glyph in the neutral `TextMutedBrush`
- [x] 3.3 Replace the two `Button` + `ContextMenu` blocks in `LaunchWindow.xaml` with `ToggleButton` Actions controls and in-tree `Popup`s bound two-way to the toggles, right-aligned by measuring the popup when it opens; confirm the build succeeds

## 4. Cleanup

- [x] 4.1 Remove `OnOpenProjectsMenu`, `OnOpenAgentsMenu`, and `OpenMenuAlignedRight` from `LaunchWindow.xaml.cs`; add the shared entry-click handler that closes the owning popup and the popup-opened handler that measures and right-aligns it
- [x] 4.2 Remove `DarkContextMenu`, `DarkMenuItem`, `MenuIcon`, and `MenuSeparator` from `LaunchWindowStyles.xaml`; confirm no remaining references via a repo search and that `dotnet build CLIHub.sln` succeeds

## 5. Verification

- [x] 5.1 Run the app and, for both panes, confirm: entries show neutral glyphs, the menu's right edge aligns with the Actions control, hovering highlights an entry, an action with no selection is disabled, and invoking an entry or clicking outside closes the menu
- [x] 5.2 Confirm the availability filter entry shows a checked state that matches the current filter, and that opening/closing the menu never hides the launch window
- [x] 5.3 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` to confirm no build or test regressions

## 6. Keyboard

- [x] 6.1 Make Escape close an open actions menu before hiding the window, covering both focus on the Actions control and focus inside the popup; verify on the running app that the first Escape closes the menu and the second hides the window

## 7. Visual polish

- [x] 7.1 Keep the Actions label and chevron from moving when the menu opens (compensate the removed bottom border with padding), and verify by measuring the label's text baseline is identical closed vs. open and matches the pane's section label
- [x] 7.2 Give the open menu surface the control's pressed tone (`PopoverOpenBrush`) so pressing the control previews the menu fill; verify the card background pixel equals the pressed chip interior pixel
