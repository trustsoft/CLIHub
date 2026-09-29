# Tasks

## 1. Core: middle-ellipsis path formatting

- [x] 1.1 Add a platform-independent middle-ellipsis formatter to `src/CLIHub.Core` that keeps the beginning and end of a text within a maximum length, and verify `dotnet build CLIHub.sln` succeeds
- [x] 1.2 Add xUnit tests in `tests/CLIHub.Tests` covering a path shorter than the budget, a long path with separators (both ends preserved), a single long segment without a separator, and a budget too small to keep both ends, and verify `dotnet test CLIHub.sln` passes

## 2. LaunchWindow scaffold and switchover

- [x] 2.1 Copy `src/CLIHub/Windows/MainWindow.xaml` and `MainWindow.xaml.cs` to `LaunchWindow.xaml` and `LaunchWindow.xaml.cs` with the class renamed, leaving `MainWindow` untouched, and verify `dotnet build CLIHub.sln` succeeds with both windows present
- [x] 2.2 Move every application reference to `LaunchWindow` — the DI registration, the `GlobalHotkeyService` target, `TrayIconController`'s field, constructor parameter, and show/toggle members, and the startup-visibility path in `App.xaml.cs` — and verify that nothing in `App.xaml.cs`, `ServiceRegistration.cs`, `TrayIconController.cs`, or `GlobalHotkeyService.cs` still refers to `MainWindow`
- [x] 2.3 Verify `MainWindow` is retained unmodified and unwired: the solution builds with it present, the application starts and works, and `MainWindow` is only declared, never constructed by application code
- [x] 2.4 Verify parity with the window it replaced: selecting a project sets the current project, the agent list and versions populate, launch/resume/init/update/version run in the current project, the availability filter toggles and persists, add and remove project work including the removal confirmation, closing hides to tray, the global hotkey toggles the window, and startup visibility follows the preference; also verify that activating the window from the tray, the hotkey, or a repeated application launch shows and activates the existing instance instead of opening a second window
- [x] 2.5 Update `docs/architecture.md` to present `LaunchWindow` as the application window and `MainWindow` as retained but not wired, and verify every file name it names exists in the repository

## 3. LaunchWindow view model

- [x] 3.1 Add `LaunchWindowViewModel` under `src/CLIHub/ViewModels/` owning the project list, agent list, selected project and agent, availability filter state, and all commands, bind the window to it, and remove that behavior from `LaunchWindow.xaml.cs`; verify the parity check from 2.4 still passes
- [x] 3.2 Move `AgentItem` into `src/CLIHub/ViewModels/` (namespace `CLIHub.ViewModels`) and expose whether the agent defines launch and resume; add the `using` directive the retained `MainWindow.xaml.cs` needs to keep compiling, verify `dotnet build CLIHub.sln` succeeds, and verify with a seeded plugin that omits `resume` that Resume is unavailable and invokes nothing
- [x] 3.3 Reflect command availability in `CanExecute` so actions needing a project or an agent are disabled, and verify no action path produces an error dialog when nothing is selected
- [x] 3.4 Extract the Settings window instance from `TrayIconController` into a shared launcher used by both the tray menu and the window, and verify that invoking Settings from the tray and then from the window activates the already open window and brings it to the front instead of creating a second instance

## 4. Dark theme resources

- [x] 4.1 Create `src/CLIHub/Themes/LaunchTheme.xaml` defining the dark palette (window and row surfaces, separator, primary/secondary/dimmed text, accent, hover, selection, disabled) and implicit styles for the controls the window uses, and verify `dotnet build CLIHub.sln` succeeds
- [x] 4.2 Merge the dictionary from `src/CLIHub/App.xaml` and verify the application starts with no XAML parse or missing-resource error
- [x] 4.3 Verify interaction-state contrast on the dark surfaces for resting, hovered, selected, dimmed, and keyboard-focused states, and confirm primary, secondary, and dimmed text remain readable

## 5. Launch window structure

- [x] 5.1 Add pane headers that name each pane (`PROJECTS`, `AGENTS`) with an `Actions` menu each, and replace the status bar and the bottom command row with a footer holding the application name, the version pill, the update-check button immediately to the right of that pill, and the add-project, Settings, and Exit buttons; verify the headers, the pane bodies, and the footer align as described in the `main-window-layout` delta
- [x] 5.2 Add scrolling to both lists and verify with more projects and agents than fit that the lists scroll while the pane headers and the footer stay in place
- [x] 5.3 Populate the Projects menu (Add Project..., Remove Project, Toggle Favorite, Refresh) and the Agents menu (Launch, Resume, Init, Update, Version, Refresh, and a checkable availability filter) from the existing commands, verify each entry performs its action, that the filter entry shows its current state and survives a restart, and that favorite toggling is persisted and reflected in the list
- [x] 5.4 Verify the footer's update-check button runs a check and reports its outcome in the footer, and confirm the outcome text replaces the previous message without moving the footer's controls
- [x] 5.5 Update `ui/mockups/README.md` so the launch window structure is no longer listed as upcoming, and verify the described layout matches the delivered window

## 6. Row presentation

- [x] 6.1 Render project rows with a rounded logo thumbnail, a prominent name, a dimmed path beneath it, a favorite marker only for favorites, and a highlighted selected row carrying a left accent bar; verify each of these with a favorite and a non-favorite project
- [x] 6.2 Wire the Core formatter through a WPF converter so long paths are middle-ellipsized to the available row width, and verify with the sample paths from the mockup that both the beginning and the end of each path stay visible and no row overflows the pane
- [x] 6.3 Render agent rows with logo, name, and version beneath the name, plus inline Launch and Resume actions, and verify with an unselected row that each inline action applies to its own row and that unavailable agents stay dimmed and usable
- [x] 6.4 Update `docs/repo-structure.md` for the new `Themes/` and `ViewModels/` files, the new converters, the window swap, and the retained `MainWindow`, and verify every documented path exists

## 7. Integration verification

- [x] 7.1 Walk the full `ui/mockups/popup-split.png` checklist against the running application and verify every element present in the mockup is present or explicitly deferred by the design
- [x] 7.2 Run the regression pass and verify the tray icon and menu, the global hotkey toggle, single-instance enforcement, hide-to-tray on close, the startup-window-visibility preference, and the update notification all still work
- [x] 7.3 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` and verify both succeed, including the code-style enforcement in the build
- [x] 7.4 Move the launch-window direction to Delivered in `docs/vision.md` with a note on the theme and the view model, and verify `openspec validate "launch-window-ui"` passes
