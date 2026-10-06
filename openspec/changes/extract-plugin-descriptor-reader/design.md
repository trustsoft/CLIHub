## Context

`PluginManager.LoadPlugins` currently combines directory iteration, `plugin.json` existence checks, file reads, `JsonSerializer.Deserialize<Plugin>`, validation, duplicate handling, and logo cache resolution. The next planned plugin changes will separate validation and catalog policy, so descriptor acquisition should become an independent seam first.

## Goals / Non-Goals

**Goals:**

- Give descriptor acquisition one narrow, injectable contract.
- Preserve current per-directory failure handling and deserialization options.
- Keep plugin directory paths available to the manager for validation, provenance, and logo lookup.
- Make malformed/missing descriptor behavior directly testable.

**Non-Goals:**

- Move validation rules or duplicate policy; those belong to later changes.
- Sort directories or change plugin precedence.
- Add plugin origin metadata or a catalog.
- Change `IPluginManager` or plugin JSON shape.

## Decisions

- Add `IPluginDescriptorReader.ReadAll(string pluginsRoot)` and a `PluginDescriptorReadResult` carrying the plugin directory, nullable deserialized plugin, and nullable per-descriptor exception.
- The reader creates a missing plugins root and returns an empty result, enumerates child directories in the existing filesystem order, skips directories without `plugin.json`, and reads/deserializes each present descriptor with `CoreJson.Options`.
- The reader captures per-descriptor read/deserialization exceptions in the result while allowing root directory enumeration/creation failures to propagate as they do today. A JSON `null` descriptor is represented as a result with a null plugin and no error, preserving current silent skip behavior.
- `PluginManager` consumes results in order, logs captured errors using the existing directory-aware warning, skips null descriptors, then retains responsibility for validation, duplicate detection, `PluginDirectory` assignment, and logo cache resolution.
- Keep validation, duplicate detection, `PluginDirectory` assignment, and logo cache resolution in `PluginManager`.
- Register the reader as a singleton in `AddPluginServices`; inject it into `PluginManager` without changing external plugin manager consumers.

## Risks / Trade-offs

- **Reader swallows too much context** -> Return the original exception and directory in each result so `PluginManager` can preserve the existing warning and continue loading other plugin directories.
- **Serialization options drift** -> Reuse `CoreJson.Options` and add a JSON round-trip test.
- **Constructor changes affect tests/DI** -> Update direct test construction and verify Core composition resolves the manager.

## Migration Plan

1. Add the reader contract and implementation with focused tests.
2. Inject it into `PluginManager` and remove direct descriptor read/deserialization code.
3. Run existing PluginManager/PluginSeeder tests plus full build/test.
4. Rollback consists of restoring direct descriptor reading and removing the reader registration/type.

## Open Questions

None.
