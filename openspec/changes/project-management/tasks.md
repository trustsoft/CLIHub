## 1. Core Interface & Models

- [ ] 1.1 Add `ProjectsChanged` event and method surface to a new `IProjectService` in `src/CLIHub.Core/Interfaces/IProjectService.cs` (GetAllProjects, GetRecentProjects, GetFavorites, GetCurrentProject, AddProject, RemoveProject, SetCurrentProject, ToggleFavorite, TouchProject, ResolveLogo) and verify `CLIHub.Core` compiles
- [ ] 1.2 Confirm `Project` model already carries Id, Name, Path, IsFavorite, LastUsed, LogoPath; add any missing property and verify it compiles

## 2. Path Normalization & Registration

- [ ] 2.1 Add a `NormalizePath` helper using `Path.GetFullPath` with trailing-separator trimming, and verify unit test covers `C:\a\b\` vs `C:\a\b`
- [ ] 2.2 Implement `AddProject(string folderPath)` in `src/CLIHub.Core/Services/ProjectService.cs`: reject non-existent folder, reject duplicate (case-insensitive normalized compare), else create Project with `Guid.NewGuid().ToString("N")` and folder-derived name, persist, raise `ProjectsChanged`, and verify unit tests cover add / duplicate / missing-folder

## 3. Persistence & Current Project

- [ ] 3.1 Back `ProjectService` with `IConfigService` (constructor injection) and verify projects round-trip through a fake `IConfigService`
- [ ] 3.2 Implement `SetCurrentProject`, `GetCurrentProject`, and clear-on-missing logic at load (if `currentProjectId` does not match a registered project, clear it) and verify unit tests cover set, restore, and stale-id clearing
- [ ] 3.3 Implement `TouchProject` (update `LastUsed`) and `GetRecentProjects(int limit)` sorted by `LastUsed` descending and verify unit tests cover ordering and limit
- [ ] 3.4 Implement `ToggleFavorite` and `GetFavorites` and verify unit tests cover toggling on and off
- [ ] 3.5 Implement `RemoveProject` that deletes the entry from config (and clears current if it was current) without touching disk files, and verify unit test asserts the folder is untouched

## 4. Logo Resolution

- [ ] 4.1 Add ordered candidate filename list (`logo.png`, `logo.jpg`, `logo.jpeg`, `logo.svg`, `icon.png`, `icon.jpg`, `favicon.png`) and implement `ResolveLogo(string projectPath)` returning the first existing match, else the default logo path, and verify unit tests cover "found first match", "found later match", and "none found"

## 5. UI Integration

- [ ] 5.1 Register `IProjectService`/`ProjectService` and existing core services in `App.xaml.cs` startup (simple manual wiring for MVP) and verify the app starts without error
- [ ] 5.2 Update the tray context menu in `App.xaml.cs` to show "Current: <name>" (or "No project selected") and a "Recent Projects" submenu that sets the current project on click, then rebuild the menu when `ProjectsChanged` fires, and verify the menu reflects the current project
- [ ] 5.3 Add an "Add Project..." tray menu item using `Microsoft.Win32.OpenFolderDialog` that registers the selected folder and verify the new project appears in the menu
- [ ] 5.4 Update `src/CLIHub/Windows/MainWindow.xaml` to list projects with logo, name, and path and add a selection action, and verify the list renders loaded projects
- [ ] 5.5 Wire MainWindow agent launch to use the current project's folder as the working directory and block launch with a message when no project is selected, and verify launching opens the terminal in the project folder
- [ ] 5.6 Finish the previously deferred tray task: current-project display and recent-projects submenu are now backed by `IProjectService`

## 6. Tests

- [ ] 6.1 Add unit tests for `ProjectService` in `src/CLIHub.Tests/Services/ProjectServiceTests.cs` using a fake `IConfigService` and verify `dotnet test` passes
- [ ] 6.2 Add unit tests for logo resolution using a temporary directory and verify `dotnet test` passes

## 7. Verification

- [ ] 7.1 Build the full solution with 0 warnings/errors
- [ ] 7.2 Manually verify: add a project, select it, launch an agent → Windows Terminal opens in the project folder
- [ ] 7.3 Manually verify: restart the app and confirm the project and current selection persist
- [ ] 7.4 Manually verify: drop a `logo.png` into a project folder and confirm the logo is used; remove it and confirm the default logo is used
