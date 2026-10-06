## Context

The current `PluginManager` owns directory scanning, descriptor reading and validation, duplicate resolution, logo caching, and the loaded plugin collection. `IPluginManager` exposes only `LoadPlugins` and `GetAllPlugins`, but its name suggests broader behavior than the catalog it actually provides. Startup and UI code depend on that legacy contract directly.

## Goals / Non-Goals

**Goals:**

- Establish one explicit `IPluginCatalog` boundary for plugin discovery and read access.
- Keep plugin state owned by one catalog instance in production DI.
- Preserve `IPluginManager` as a small adapter so existing callers and staged migration remain safe.
- Migrate all in-repository consumers to the catalog contract.
- Keep the next reload change able to extend the catalog without reintroducing manager concerns.

**Non-Goals:**

- Adding reload, file watching, change notifications, or origin metadata.
- Changing plugin descriptor JSON, validation rules, duplicate policy, or directory ordering.
- Splitting agent command, detection, version, or availability services in this change.
- Updating broad architecture documentation; that belongs to the planned documentation change.

## Decisions

- Introduce `IPluginCatalog` with the current read/load operations expressed as catalog responsibilities. Use a concrete `PluginCatalog` for the existing implementation and collection state.
- Keep `IPluginManager` as a compatibility adapter that delegates to the catalog. This allows a staged migration and preserves the old contract without keeping duplicate plugin state.
- Register one `PluginCatalog` singleton and expose it through both `IPluginCatalog` and the compatibility adapter in DI. The adapter must delegate to that same instance.
- Migrate `PluginInitializationService`, `TrayMenuBuilder`, `LaunchWindowViewModel`, and `MainWindow` to `IPluginCatalog`; update tests and fakes accordingly.
- Keep the catalog API synchronous because the existing discovery and descriptor loading flow is synchronous. Reload and notification semantics are intentionally deferred to the next change.

## Risks / Trade-offs

- [Risk] A compatibility adapter can hide accidental continued use of the legacy contract. -> Mitigation: migrate all in-repository production consumers and cover adapter delegation with tests.
- [Risk] DI registration could create separate catalog instances and divergent plugin lists. -> Mitigation: register the concrete singleton once and resolve both interfaces from it.
- [Risk] Constructor changes can affect direct tests and composition roots. -> Mitigation: update all repository call sites and run Core/UI solution tests before archiving.
