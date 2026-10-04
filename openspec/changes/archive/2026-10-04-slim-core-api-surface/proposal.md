# Proposal: slim-core-api-surface

## Why

The Core interfaces accumulate members nobody calls, duplicate domain logic, and leak implementation details: `IConfigService` still exposes project-domain members that duplicate `IProjectService`, `PluginManager` hands out its internal mutable list and writes placeholder files into user plugin folders, serializer options are declared three times, and six interface members have no production callers. This blurs the interfaces' role as the documentation of the system's intent and widens the surface every future change must respect.

## What Changes

- **`IConfigService` shrinks to storage**: `GetCurrentProject`/`SetCurrentProject` are removed from the interface and both implementations — the current project is owned by `IProjectService` alone (no production callers existed).
- **Dead members removed**: `IPluginManager.GetPluginById`, `IProjectService.TouchProject`, `IProjectService.GetFavorites`, `IReleaseNotesService.GetLatestNote` (interface + implementation; the two `GetFavorites` assertions now check the `IsFavorite` flag, the two `GetLatestNote` assertions use `GetNotes()`).
- **`IProjectService.ResolveLogo` moves off the interface** to an `internal` method on `ProjectService` (tests keep covering candidate priority and the default fallback through `InternalsVisibleTo`).
- **`IProcessLauncher.GetRuntime` moves off the interface** (the concrete method stays for tests).
- **`PluginManager.GetAllPlugins` returns `IReadOnlyList<Plugin>`** instead of the internal mutable list.
- **Placeholder logo provisioning is dropped**: `PluginManager` no longer writes files into plugin folders — a missing `logo.png` resolves to null (negative-cached like projects), and the launch view model falls back to the shipped `default-project.png`. This aligns the code with the existing `plugin-seeding` fallback scenario ("the default logo is used"); the former placeholder was a transparent PNG, so logo-less agents gain a visible default icon.
- **Serializer options deduplicated**: one internal shared camelCase/indented `JsonSerializerOptions` replaces the three copies in `ConfigService`, `PluginManager`, and `LogoCacheService`.

No externally observable behavior changes except the logo-less-agent fallback above, which the existing spec already requires. Specs are skipped (`skip_specs: true`).

## Capabilities

### New Capabilities

(none — no spec-level requirement changes; the agent-logo fallback already exists as a `plugin-seeding` scenario)

### Modified Capabilities

(none)

## Impact

- **Modified interfaces**: `IConfigService`, `IPluginManager`, `IProjectService`, `IProcessLauncher`, `IReleaseNotesService`.
- **Modified implementations**: `ConfigService`, `PluginManager`, `ProjectService`, `ProcessLauncher`, `ReleaseNotesService`, `FakeConfigService`, `LaunchWindowViewModel` (agent logo fallback).
- **New code**: one internal shared serializer-options type.
- **Tests**: `PluginManagerTests`, `ProjectServiceTests`, `ReleaseNotesServiceTests`, `AppConfigSerializationTests` — small adjustments to dropped members and the shared options symbol; no coverage lost.
- Out of scope: `GetRecentProjects` (live: tray menu, spec-backed).
