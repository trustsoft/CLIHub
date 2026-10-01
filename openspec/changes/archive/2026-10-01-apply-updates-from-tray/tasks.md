# Tasks

## 1. Core: download and apply operations

- [x] 1.1 Add the download result model to `src/CLIHub.Core/Models/` (status: `Downloaded` with version, `NoUpdate`, `NotInstalled`, `Failed`, `AlreadyDownloading`) and verify `dotnet build CLIHub.sln` passes
- [x] 1.2 Extend `IUpdateService` with `DownloadUpdateAsync` and `ApplyDownloadedUpdateAndRestart`, implement both in `UpdateService` per design.md (reuse the check, keep the `UpdateInfo`, guard with an `IsDownloading` flag, log each outcome) and verify the solution builds
- [x] 1.3 Add unit tests to `tests/CLIHub.Tests/Services/UpdateServiceTests.cs` using the `TestVelopackLocator` + `SimpleFileSource` seam: no update → `NoUpdate`, not installed → `NotInstalled`, second call while downloading → `AlreadyDownloading`, and never throws; verify with `dotnet test CLIHub.sln`

## 2. Tray surface

- [x] 2.1 Make `TrayIconController` consult the update state when building the menu: show "Download update and restart" only when an update is available, switch it to a disabled "Downloading update…" while running, and show nothing without an update or on non-managed installs; verify the solution builds and the menu states by running the app
- [x] 2.2 Wire the flow in `App.xaml.cs`: invoke the download from the menu item, forward state changes to the tray, show balloons on completion ("restarting") and failure, and call `ApplyDownloadedUpdateAndRestart` after a successful download; verify by exercising the menu and reading `%APPDATA%\CLIHub\logs\`

## 3. What's New window

- [x] 3.1 Add the install action and download state to `WhatsNewViewModel`, calling the same `IUpdateService` methods as the tray; verify the solution builds and the view model behavior via unit tests where practical
- [x] 3.2 Add the install button to `WhatsNewWindow.xaml` bound to the view model state (visible only when an update is available or downloading, disabled while downloading); verify manually by opening the window with and without an available update

## 4. Documentation

- [x] 4.1 Update `docs/vision.md` (move "Downloading and applying updates from the tray" from remaining directions to delivered), `README.md` (Features / Using it), and `docs/architecture.md` (update flow); verify the links and wording match the implemented behavior

## 5. Final verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` and confirm all tests pass
- [x] 5.2 Manually verify the end-to-end flow against a real or locally packaged Velopack release: update found → menu item appears → download state shows → completion balloon → app restarts into the new version; confirm the failure path (e.g. source unreachable) logs the failure and restores the action
