# Design: unify the logo pipeline

## Current pipeline (verified in code)

| Concern | Today | File |
|---|---|---|
| Shared persistent cache | `LogoCacheService` / `ILogoCacheService`: namespaced keys `project:<id>` / `plugin:<id>`, negative caching (null = looked up, not found), loads `%APPDATA%\CLIHub\cache\logos.json` on construction, atomic save on dispose when dirty | `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs` |
| Project policy | `ProjectLogoResolver` (internal): scans `logo.png, logo.jpg, logo.jpeg, logo.svg, icon.png, icon.jpg, favicon.png`; fallback `?? defaultLogoPath` applied at resolver level, default passed in from the caller | `src/CLIHub.Core/Projects/ProjectLogoResolver.cs` |
| Project fallback plumbing | `ProjectService.DefaultLogoPath` settable property (also on `IProjectService`), set from composition; `GetAllProjects`/`GetRecentProjects`/`GetCurrentProject` mutate `project.LogoPath` as a hidden side effect | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Plugin policy | Private static `LoadPluginLogo` in `PluginCatalog`: checks `logo.png` in the plugin directory; result stored raw (nullable) on `Plugin.LogoPath`, no fallback | `src/CLIHub.Core/Plugins/PluginCatalog.cs` |
| Plugin fallback | Applied late and inconsistently: `AgentListSynchronizer` does `?? defaultLogoPath` for launch rows; `AgentManagementViewModel` passes `Plugin.LogoPath` through raw (blank card for logo-less plugins) | `src/CLIHub/ViewModels/Agents/AgentListSynchronizer.cs`, `AgentManagementViewModel.cs` |
| App fallback constant | Private static `AgentPaneController.DefaultAgentLogoPath` → `default-project.png` | `src/CLIHub/ViewModels/Agents/AgentPaneController.cs` |
| Registration | `ILogoCacheService` registered inside `AddProjectServices(defaultLogoPath)` | `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` |
| Invalidation | `AgentPaneController.InvalidateCaches()` → `InvalidateAll()` clears project entries too; only production caller of a full clear | `src/CLIHub/ViewModels/Agents/AgentPaneController.cs` |
| UI decode | `PathToImageConverter`: new `BitmapImage` (`CacheOption.OnLoad` + `Freeze()`) per conversion call, no dedup; null result (blank image) for null/missing paths | `src/CLIHub/Converters/PathToImageConverter.cs` |

## Decision 1: one shared cache stays

No per-domain caches. `LogoCacheService` keeps its `ConcurrentDictionary<string, string?>`, negative
caching, `logos.json` state file, atomic-save-on-dispose, and dirty-flag skip. Nothing about the
stored format changes, so existing installations load their current state without migration.

## Decision 2: prefix-scoped invalidation

`ILogoCacheService.InvalidateAll()` becomes `InvalidateAll(string? keyPrefix = null)`:

- `InvalidateAll()` (no argument) keeps today's full-clear semantics.
- `InvalidateAll(LogoCacheKeys.PluginPrefix)` removes exactly the `plugin:`-prefixed keys,
  including cached negative outcomes, and marks the state dirty only when something was removed.

`LogoCacheKeys` (new small static class in `CLIHub.Core.Infrastructure.Persistence`,
`ProjectPrefix = "project:"`, `PluginPrefix = "plugin:"`) becomes the single home for the key
namespace strings; both resolvers build keys from it, and invalidation callers reference the
constants instead of literal strings.

`AgentPaneController.InvalidateCaches()` switches to `InvalidateAll(LogoCacheKeys.PluginPrefix)`,
so agent refreshes and plugin reloads no longer evict project logo entries. The existing strict
mocks in `AgentPaneControllerTests` that set up `InvalidateAll()` are updated accordingly.

## Decision 3: single fallback source

`LogoOptions` (record with `string? DefaultLogoPath`) is registered once as a singleton in Core and
injected into both resolvers. This deletes:

- `IProjectService.DefaultLogoPath` / `ProjectService.DefaultLogoPath` (property), and
- `AgentPaneController.DefaultAgentLogoPath` (private static), along with the `defaultLogoPath`
  parameter of `AgentListSynchronizer.Synchronize` (rows receive final paths; no consumer-side
  `??` remains).

The asset rename `default-project.png` → `default-logo.png` is included: it touches only
`src/CLIHub/CLIHub.csproj` (Content include), the composition call site
(`src/CLIHub/Composition/ServiceRegistration.cs`), and the file itself; no test or descriptor
references the old name.

## Decision 4: policy/mechanics separation and fallback placement

Mechanics = the cache (Decision 1). Policy = per-domain resolvers:

- `ProjectLogoResolver` (Core.Projects) becomes `public sealed`, takes `(ILogoCacheService,
  LogoOptions)`, and drops its `defaultLogoPath` parameters (`Resolve(Project)`,
  `ResolvePath(string)`, `Remove(projectId)` unchanged in spirit).
- `PluginLogoResolver` (new, `public sealed` in Core.Plugins) mirrors it:
  `Resolve(pluginId, pluginDirectory)` → `logoCache.GetOrResolve(LogoCacheKeys.PluginPrefix +
  pluginId, () => Scan(pluginDirectory)) ?? options.DefaultLogoPath`, scanning only `logo.png`
  (today's behavior). `PluginCatalog` stops holding `ILogoCacheService` and its private static
  `LoadPluginLogo`; it holds the resolver.

**Fallback placement: resolver level, in exactly one place per chain.** Rationale:

- The negative outcome must stay cached as null (so a later-added logo is picked up after
  invalidation), which means the fallback has to be applied *after* the cache lookup — the
  resolver's return path. That is where the project chain already applies it
  (`ProjectLogoResolver.Resolve` ends in `?? defaultLogoPath`), so choosing resolver level
  preserves existing project behavior byte-for-byte, including what `ConfigurationSnapshot`
  persists into `config.json` (`project.LogoPath` already contains the default when nothing was
  found).
- Consumer-level placement is what makes the plugin chain inconsistent today: `AgentListSynchronizer`
  re-applies `?? default` per row while `AgentManagementViewModel` forgets it. With resolver-level
  fallback, `Plugin.LogoPath` is a final path for every consumer, the management card gains the
  default-logo fallback `agent-ui` already requires, and the synchronizer loses its parameter.
- Moving the project fallback to consumer level would either push `LogoOptions` into UI-facing
  consumers or force the default into cached values (wrong: it would freeze the fallback as the
  resolution result and break negative-caching refresh).

Note the plugin side has no persisted-path equivalent (`Plugin.LogoPath` is runtime-only; only
projects' `LogoPath` reaches `config.json`), so defaulting `Plugin.LogoPath` at the resolver has no
persistence impact.

## Decision 5: registration home

`ServiceCollectionExtensions` gains a private `AddLogoServices(string? defaultLogoPath)` called from
`AddClIHubCoreServices` before the domain modules; it registers `LogoOptions`, `ILogoCacheService →
LogoCacheService`, `ProjectLogoResolver`, and `PluginLogoResolver` (all singletons).

- `AddProjectServices` loses the `defaultLogoPath` parameter, the `ILogoCacheService` registration,
  and the `DefaultLogoPath = ...` initializer; its `ProjectService` factory injects the public
  `ProjectLogoResolver`.
- `ProjectService` takes `ProjectLogoResolver` instead of `ILogoCacheService` and drops the
  `DefaultLogoPath` property (`ResolveProjectLogo` → `logoResolver.Resolve(project)`,
  `ResolveLogo` → `logoResolver.ResolvePath(path)`).
- `AddPluginServices` is unchanged in shape; `PluginCatalog` now resolves `PluginLogoResolver`.

**Explicitly deferred:** removing the hidden per-call logo mutation (`RefreshLogos` inside
`GetAllProjects`/`GetRecentProjects`, and the `GetCurrentProject` resolution) is *not* part of this
change. It cannot be done without the ProjectModel/enrichment refactor: UI consumers read
`Project.LogoPath` straight off the returned models, and a fresh process's first read is the only
thing that populates those paths (the persisted `config.json` copies are stale snapshots). This
change only relocates the dependency (`ProjectService` → resolver) and the relocation of the call
site itself belongs to the future enrichment/ProjectModel change.

## Decision 6: app-layer decoded-image cache

New `LogoImageSourceCache` in the app project (`src/CLIHub/Imaging/LogoImageSourceCache.cs`):

- `ConcurrentDictionary<string, ImageSource>` keyed by the resolved path.
- Loads `BitmapImage` with `CacheOption.OnLoad`, then `Freeze()` — the frozen instance is shared by
  every `Image`/`ImageBrush` bound to that path. Memory becomes O(distinct logos) with one decode
  per logo instead of one decoded buffer (W×H×4 bytes) per `Image` control; WPF gives no
  cross-instance decode dedup, so the sharing must be explicit.
- `GetOrLoad(string? path)`: null/whitespace path, missing file, or decode failure returns the
  default logo's frozen `ImageSource` (loaded through the same path and cached once). The default is
  taken from `LogoOptions.DefaultLogoPath`, so the app layer consumes the same single fallback
  source as Core. This also fixes the stale-path case: a resolved logo deleted after resolution
  currently renders blank; it now renders the default.
- Registered as an app-layer singleton in `ServiceRegistration`, constructed with `LogoOptions`
  from the Core provider.

**Interim delivery as a value converter.** `PathToImageConverter` keeps its `PathToImage` resource
key and one-way semantics but is constructed with the cache (its parameterless XAML declarations in
`App.xaml` and `MainWindow.xaml` are removed, and application startup assigns the DI-constructed
converter to the application resource dictionary under the same key; `MainWindow.xaml`'s
window-level declaration goes away so bindings resolve the shared app-level one). Existing
`{Binding LogoPath, Converter={StaticResource PathToImage}}` bindings in `LaunchWindow.xaml`,
`MainWindow.xaml`, and `SettingsAgentsPage.xaml` are untouched.

The future ProjectModel change may expose `ImageSource` (or a view model wrapper) directly and
delete the converter; noted as direction only.

`DecodePixelWidth` downscaling for small row icons is an optional follow-up (memory saver for large
logos rendered at ~24px) — not a requirement of this change.

## Spec deltas

- `logo-cache` — MODIFIED *Cache invalidation on demand*: scoped prefix invalidation added; agent
  refresh clears only `plugin:` entries; full clear preserved.
- `project-management` — MODIFIED *Project logo detection*: an agent-pane refresh preserves project
  logo entries (replaces the "manual refresh clears project logos" scenario).
- `logo-display` — ADDED capability: one frozen decoded image per distinct resolved path, shared
  across views; default image for null/missing/undecodable paths.

## Alternatives considered

- **Split per-domain caches** — rejected: duplicates persistence and negative-caching machinery for
  no behavioral gain; the namespacing already separates domains.
- **Consumer-level fallback for both domains** — rejected: this is the current plugin-chain shape
  and it demonstrably leaks (`AgentManagementViewModel` blank cards); it forces every current and
  future consumer to remember the `??`.
- **Fold the default into cached values** — rejected: the cached value would become non-null on
  "not found", defeating negative caching and re-resolution after invalidation.
- **Static shared `ImageSource` cache inside the converter** — rejected: hides global state the
  composition root cannot replace in tests; the DI-registered cache assigned at startup keeps the
  converter thin and testable (`new PathToImageConverter(cache)`).
- **Keep the `default-project.png` name** — viable, but the rename is two reference updates plus a
  file move with zero test/descriptor impact, and the new name no longer claims to be
  project-specific.
