# Unify the logo pipeline: one shared cache, one fallback source, scoped invalidation

## Why

Logo handling grew by accretion and now shows four structural smells:

- **Two fallback sources for one asset.** `ProjectService.DefaultLogoPath` (settable property, fed
  from composition in `AddProjectServices`) and the private `AgentPaneController.DefaultAgentLogoPath`
  static both hardcode `AppContext.BaseDirectory\default-project.png`. Because the plugin chain applies
  its fallback late (in `AgentListSynchronizer`), the agent management card passes `Plugin.LogoPath`
  through raw and renders blank for logo-less plugins, even though `agent-ui` requires the default-logo
  fallback there.
- **Over-broad invalidation.** `AgentPaneController.InvalidateCaches()` calls
  `ILogoCacheService.InvalidateAll()`, so every agent refresh (including every plugin reload) also
  deletes all `project:` cache entries and forces project folders to be rescanned.
- **Misplaced registration.** `ILogoCacheService` is registered inside `AddProjectServices`
  (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`) although its main consumer is
  `PluginCatalog`, and `ProjectService` depends on the raw cache instead of a domain resolver.
- **Hidden side effects and repeated decodes.** `ProjectService.GetAllProjects`/`GetRecentProjects`
  mutate `project.LogoPath` as a side effect of read methods, and the UI's `PathToImageConverter`
  creates a new `BitmapImage` per binding evaluation — one decoded pixel buffer per `Image` control —
  and returns null (blank) when a resolved path no longer exists on disk.

## What Changes

- **Shared cache stays.** `LogoCacheService` keeps namespaced `project:`/`plugin:` keys, negative
  caching, and the `logos.json` file format. Only its invalidation grows: `InvalidateAll(string?
  keyPrefix)` clears exactly the keys with that prefix; the parameterless call keeps full-clear
  semantics. `AgentPaneController.InvalidateCaches()` switches to the scoped form with the `plugin:`
  prefix.
- **Single fallback source.** A `LogoOptions` record (`DefaultLogoPath`) registered once in Core
  replaces `ProjectService.DefaultLogoPath` and `AgentPaneController.DefaultAgentLogoPath`.
- **Policy/mechanics separation.** `ProjectLogoResolver` (Core.Projects, becomes public) and a new
  symmetric `PluginLogoResolver` (Core.Plugins, extracted from `PluginCatalog`'s private static
  `LoadPluginLogo`) own domain policy; each applies the `?? DefaultLogoPath` fallback exactly once,
  at resolver level.
- **Registration home.** `AddLogoServices(defaultLogoPath)` registers the cache, options, and both
  resolvers; `AddProjectServices` loses the logo registrations and the `defaultLogoPath` parameter.
  `ProjectService` depends on the public `ProjectLogoResolver` instead of `ILogoCacheService`.
- **App-layer decoded-image cache.** New `LogoImageSourceCache` (frozen shared `ImageSource` per
  resolved path) delivered behind the existing `PathToImage` binding key; null, missing, and
  undecodable paths return the frozen default-logo image instead of null.
- **Asset rename.** `default-project.png` → `default-logo.png` (cosmetic; no test or descriptor
  references).

## Impact

- Affected specs: `logo-cache` (modified: scoped invalidation), `project-management` (modified:
  project logo entries survive agent refresh), new `logo-display` capability.
- Affected code (future): `CLIHub.Core` persistence/options/resolvers/composition, `PluginCatalog`,
  `ProjectService`, `AgentPaneController`, `AgentListSynchronizer`, `src/CLIHub` converter,
  composition, and asset wiring.
- Non-goals: Project/ProjectModel/enricher refactor (separate future change; this change only
  reorganizes logo plumbing and creates the seams); changes to `config.json` or the `logos.json`
  cache format (no migration); changes to the plugin descriptor format (plugins remain JSON
  descriptors); `DecodePixelWidth` downscaling of row icons (optional follow-up); exposing
  `ImageSource` on a future ProjectModel (direction only, not this change).
