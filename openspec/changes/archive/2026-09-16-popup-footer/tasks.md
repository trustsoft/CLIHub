## 1. ViewModel and wiring

- [x] 1.1 Add `string appVersion` constructor parameter, `AppVersion` property, `OpenDataFolderCommand` (injected `Action openDataFolder`), `ExitCommand`, and `ExitRequested` event to `PopupViewModel`; verify `dotnet build CLIHub.sln` succeeds
- [x] 1.2 In `App.OnStartup`, resolve the version string (Velopack runtime version, fallback to entry-assembly `AssemblyInformationalVersion` per design D2), wire `openDataFolder` to `_processRunner.StartDetached("explorer.exe", paths.ConfigDirectory, ...)` (design D4), and subscribe `ExitRequested` to `Shutdown()` (design D5); verify `dotnet build CLIHub.sln` succeeds

## 2. Footer UI

- [x] 2.1 Rebuild the footer in `PopupWindow.xaml` as a three-zone Grid per design D1: brand + version pill bound to `AppVersion` (left), `StatusText` centered with `CharacterEllipsis` (center), three glyph buttons from Segoe MDL2 Assets (right) bound to `OpenDataFolderCommand` / disabled settings / `ExitCommand` (design D3, D6); verify visually via `dotnet run --project src/CLIHub.App` (run in background): version pill shows, status appears in center, folder opens `%AppData%\CLIHub`, exit terminates the app
- [x] 2.2 Verify disabled state: settings icon is visible but inert on click; verify popup hide-on-focus-loss still works with the new footer

## 3. Verification and docs

- [x] 3.1 Run `dotnet test CLIHub.sln` and confirm the full suite passes (no Core behavior changes expected)
- [x] 3.2 Update the implementation snapshot in `docs/ui.md` ("Футер реализован частично" line) to reflect the implemented footer
