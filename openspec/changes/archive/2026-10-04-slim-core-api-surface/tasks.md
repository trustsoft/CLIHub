## 1. Interface slimming

- [x] 1.1 Remove `GetCurrentProject`/`SetCurrentProject` from `IConfigService`, `ConfigService`, and `FakeConfigService`; verify the solution builds (no production callers exist)
- [x] 1.2 Remove `GetPluginById` from `IPluginManager`/`PluginManager` and `TouchProject`/`GetFavorites` from `IProjectService`/`ProjectService`; rework the two `ToggleFavorite` test assertions to check `IsFavorite`; verify `dotnet build` and `dotnet test`
- [x] 1.3 Move `ResolveLogo` off `IProjectService` to an `internal` method on `ProjectService`; verify the three `ResolveLogo` tests still pass via `InternalsVisibleTo`
- [x] 1.4 Remove `GetLatestNote` from `IReleaseNotesService`/`ReleaseNotesService`; rework its two test assertions to `GetNotes()`; remove `GetRuntime` from `IProcessLauncher` (keep the concrete method for `ProcessLauncherTests`); verify `dotnet test`

## 2. Collection and serializer hygiene

- [x] 2.1 Change `PluginManager.GetAllPlugins` to return `IReadOnlyList<Plugin>`; verify all consumers build unchanged and `PluginManagerTests` pass
- [x] 2.2 Introduce one internal shared camelCase/indented `JsonSerializerOptions` type; replace the copies in `ConfigService`, `PluginManager`, and `LogoCacheService`; update `AppConfigSerializationTests` to the shared symbol; verify `dotnet test`

## 3. Logo fallback

- [x] 3.1 Replace `LoadPluginLogo` with a null-returning scan (`logo.png` or null), delete `CreatePlaceholderLogo`, and keep the logo-cache keying; verify `PluginManagerTests` pass
- [x] 3.2 Fall back to the shipped `default-project.png` in `LaunchWindowViewModel` when an agent's `LogoPath` is null; verify the solution builds

## 4. Verification

- [x] 4.1 Run the full test suite (`dotnet test CLIHub.sln`) and confirm all tests pass
- [x] 4.2 Run `dotnet build CLIHub.sln` with no warnings; manually verify the launch window renders agents with logos, and a user plugin folder without `logo.png` shows the default icon with no file written into the folder
