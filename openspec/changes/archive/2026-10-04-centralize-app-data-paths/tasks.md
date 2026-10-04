## 1. AppPaths and DirectoryInitializer

- [x] 1.1 Add `static sealed class AppPaths` in `src/CLIHub.Core/Services/` with XML-documented computed properties (`Root`, `LogsDirectory`, `PluginsDirectory`, `CacheDirectory`, `ConfigFile`, `LogosCacheFile`); verify the project compiles
- [x] 1.2 Rework `DirectoryInitializer` to consume `AppPaths` (`GetAppDataRoot()` delegates to `AppPaths.Root`, `EnsureAppDataLayout` creates `AppPaths` directories); verify `DirectoryInitializerTests` still pass

## 2. Consumers

- [x] 2.1 Switch default path construction in `ConfigService`, `PluginManager`, `LogoCacheService`, and `PluginSeeder` to `AppPaths.*`; verify the solution builds and the affected service tests pass unchanged
- [x] 2.2 Use `AppPaths` in `App.ConfigureLogging` (logs directory, config path for `PreferenceReader`) and `LaunchWindowViewModel.OpenDataFolder`; verify the solution builds
- [x] 2.3 Remove the now-redundant `ConfigFilePath` from `IConfigService` (path knowledge lives in `AppPaths`): `ConfigService` keeps the path as a private field, `FakeConfigService` drops the stub; verify no consumer referenced it and the solution builds

## 3. Verification

- [x] 3.1 Run the full test suite (`dotnet test CLIHub.sln`) and confirm all tests pass
- [x] 3.2 Run `dotnet build CLIHub.sln` with no warnings; run the app once and confirm the existing data layout is picked up as before (config, logs, plugins, cache — no new or missing folders)
