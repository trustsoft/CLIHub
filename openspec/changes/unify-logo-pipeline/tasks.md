# Tasks: unify-logo-pipeline

Implementation happens in a fresh session with no prior context; `design.md` carries the full
rationale and file references. Tasks are checked off as they land.

## 1. Core: logo infrastructure module

- [ ] 1.1 Add `LogoOptions` (record, `string? DefaultLogoPath`) and `LogoCacheKeys` (static class,
      `ProjectPrefix = "project:"`, `PluginPrefix = "plugin:"`) in
      `src/CLIHub.Core/Infrastructure/Persistence/`, with XML docs; build
- [ ] 1.2 Extend `ILogoCacheService.InvalidateAll()` to `InvalidateAll(string? keyPrefix = null)` and
      implement prefix removal in `LogoCacheService` (remove exactly the keys starting with the
      prefix, negative entries included, dirty flag set only when something was removed; no-prefix
      call keeps the current full clear); update `LogoCacheServiceTests`
- [ ] 1.3 Add `AddLogoServices(string? defaultLogoPath)` to
      `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` registering `LogoOptions`,
      `ILogoCacheService → LogoCacheService`, `ProjectLogoResolver`, `PluginLogoResolver` as
      singletons; call it from `AddClIHubCoreServices` (signature unchanged) before
      `AddProjectServices`

## 2. Core: resolvers (policy)

- [ ] 2.1 Make `ProjectLogoResolver` public, take `(ILogoCacheService, LogoOptions)`, build keys from
      `LogoCacheKeys.ProjectPrefix`, and drop the `defaultLogoPath` parameters from `Resolve`,
      `ResolvePath`; keep the candidate filename list and `Remove(projectId)` unchanged
- [ ] 2.2 Extract `PluginLogoResolver` (`public sealed`, `src/CLIHub.Core/Plugins/`): `Resolve(pluginId,
      pluginDirectory)` → `GetOrResolve(PluginPrefix + id, () => Scan(dir)) ?? options.DefaultLogoPath`,
      scanning `logo.png` only (today's behavior); add `PluginLogoResolverTests` for found / not found
      (cached negative) / fallback / second call reuses cache
- [ ] 2.3 Fallback single-source test: with one `LogoOptions.DefaultLogoPath`, a project miss and a
      plugin miss both end at the same default path while null stays cached in `logos.json`

## 3. Core: consumers rewired

- [ ] 3.1 `ProjectService`: ctor takes `ProjectLogoResolver` (not `ILogoCacheService`); remove the
      `DefaultLogoPath` property from the class and `IProjectService`; `RefreshLogos` /
      `GetCurrentProject` / `CreateProject` / `ResolveLogo` call the resolver without a fallback
      argument. Do NOT remove the hidden logo resolution inside `GetAllProjects`/`GetRecentProjects`
      — explicitly deferred (see `design.md`, Decision 5)
- [ ] 3.2 `PluginCatalog`: ctor takes `PluginLogoResolver`; replace the
      `logoCache.GetOrResolve("plugin:...", LoadPluginLogo)` call and delete the private static
      `LoadPluginLogo`
- [ ] 3.3 Slim `AddProjectServices`: drop the `defaultLogoPath` parameter, the `ILogoCacheService`
      registration, and the `DefaultLogoPath = ...` initializer; inject `ProjectLogoResolver` into
      the `ProjectService` factory; update `ServiceCollectionExtensionsTests`
- [ ] 3.4 Update existing tests broken by the API moves (`ProjectServiceTests` default-logo setup,
      `PluginCatalogTests` construction) and run `dotnet test CLIHub.sln`

## 4. App: scoped invalidation and synchronizer cleanup

- [ ] 4.1 `AgentPaneController`: `InvalidateCaches()` → `InvalidateAll(LogoCacheKeys.PluginPrefix)`;
      delete the `DefaultAgentLogoPath` static
- [ ] 4.2 Remove the `defaultLogoPath` parameter from `AgentListSynchronizer.Synchronize` (rows take
      `Plugin.LogoPath` as final) and update the `AgentPaneController` call; update
      `AgentPaneControllerTests` (strict `InvalidateAll(LogoCacheKeys.PluginPrefix)` setups) and any
      synchronizer tests

## 5. App: decoded-image cache and wiring

- [ ] 5.1 Add `src/CLIHub/Imaging/LogoImageSourceCache.cs`: `ConcurrentDictionary<string, ImageSource>`,
      `GetOrLoad(string? path)` returning a frozen `BitmapImage` (`CacheOption.OnLoad`); null/empty,
      missing file, and decode failures return the cached frozen default built from `LogoOptions`;
      register as an app-layer singleton in `ServiceRegistration`, constructed with `LogoOptions`
- [ ] 5.2 Refactor `PathToImageConverter` to take `LogoImageSourceCache` in its constructor and
      delegate; remove the parameterless declarations from `App.xaml` and `MainWindow.xaml`, and at
      application startup assign the DI-constructed converter to the application resources under the
      existing `PathToImage` key so all bindings keep resolving; verify the launch window, agent
      rows, and settings agent cards still render logos
- [ ] 5.3 Rename `src/CLIHub/default-project.png` → `default-logo.png`; update the `Content` include in
      `src/CLIHub/CLIHub.csproj` and the path in `src/CLIHub/Composition/ServiceRegistration.cs`

## 6. App-layer tests

- [ ] 6.1 `LogoImageSourceCacheTests` (use a small real PNG and a non-image `.png` file written to a
      temp folder; STA thread helper if decode requires it, see `SpacedPanelTests`): repeated path
      returns the same frozen instance; missing/null path returns the default instance; decode
      failure returns the default instance
- [ ] 6.2 `PathToImageConverterTests`: delegates to the cache; default image for null/missing input

## 7. Verification

- [ ] 7.1 `dotnet build CLIHub.sln` with no new warnings
- [ ] 7.2 `dotnet test CLIHub.sln` green
- [ ] 7.3 `openspec validate --all` passes
- [ ] 7.4 Manual smoke: app with a project that has no logo shows `default-logo.png`; add `logo.png`
      to the project folder, refresh agents (or reload plugins) — the project logo appears without an
      app restart; delete a resolved logo file — the UI shows the default instead of a blank image;
      `%APPDATA%\CLIHub\cache\logos.json` still contains `project:`/`plugin:` keys and loads on the
      next start
