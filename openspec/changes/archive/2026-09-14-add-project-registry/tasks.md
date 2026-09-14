## 1. Safe persistence (app-config)

- [x] 1.1 Add `Move(source, destination, overwrite)` to `IFileSystem` and implement it in `PhysicalFileSystem`; verify Core compiles and a unit test moves a file over an existing target
- [x] 1.2 Add `[JsonExtensionData]` to `Config`; verify a unit test that loads JSON with an unknown top-level field, saves, and finds the field still present
- [x] 1.3 Implement `ConfigStore.Save`: write to a temp file in the config directory then `Move` over the target, and back up an unparseable `config.json` to `config.json.bak` first; verify unit tests for missing file, unknown-field survival, corrupt-to-bak, and no partial file

## 2. Project registry (project-registry)

- [x] 2.1 Implement `ProjectRegistry.Add(path)`: validate (path exists and is a directory, no case-insensitive duplicate path, non-empty name), assign a new `Guid` id, name from the folder, append, then `Save`, returning a result with success/project/error; verify unit tests for valid add, nonexistent path, duplicate path, and default name
- [x] 2.2 Implement `ProjectRegistry.Remove(id)`: remove the entry and `Save`; verify a unit test that the entry disappears from the persisted config and the folder on disk is untouched
- [x] 2.3 Implement `ProjectRegistry.Load()` over an existing `Config`/`ConfigStore`; verify a unit test that loaded projects are exposed and a removed id is not found

## 3. Popup UI

- [x] 3.1 Replace the synthetic fallback with an empty state and add an `Actions` menu (Add/Remove) to the Projects pane header; verify the empty state renders when the registry is empty
- [x] 3.2 Wire **Add** to `Microsoft.Win32.OpenFolderDialog` -> `ProjectRegistry.Add` -> add the project to the `ObservableCollection` and select it; verify manually that a picked folder appears selected
- [x] 3.3 Wire **Remove** with a confirmation dialog -> `ProjectRegistry.Remove`, and disable Remove when no project is selected; verify manually
- [x] 3.4 Ensure the popup does not hide while the folder picker is open (guard the `Deactivated` handler); verify manually that the popup stays open through Add

## 4. Composition and persistence wiring

- [x] 4.1 In `App.xaml.cs` load `Config` via `ConfigStore`, create `ProjectRegistry`, and pass it to `PopupViewModel`; verify the app starts and project list comes from the registry

## 5. Verification

- [x] 5.1 Run `dotnet test` and `openspec validate add-project-registry`; verify both pass
- [x] 5.2 Manual end-to-end: add a project, confirm it is written to `%AppData%\CLIHub\config.json`, restart the app, confirm the project persists, launch an agent in it, then remove it; verify the folder on disk is untouched
