## Context

`PluginCatalog` already owns the complete synchronous scan and in-memory plugin collection. Startup calls `LoadPlugins` once, and there is currently no supported way to rescan the directory or notify future consumers that the collection changed.

## Goals / Non-Goals

**Goals:**

- Add an explicit synchronous reload operation to the existing catalog boundary.
- Keep initial startup load behavior and compatibility adapter behavior unchanged.
- Raise one notification after a completed manual reload, after the new snapshot is available.
- Reuse the existing scan rules rather than creating a second reload implementation.

**Non-Goals:**

- File-system watchers, polling, debounce, or background reload scheduling.
- Adding a reload button or other UI trigger in this change.
- Changing plugin descriptor format, validation, ordering, duplicate selection, or logo cache semantics.
- Adding plugin origin metadata.

## Decisions

- Add `ReloadPlugins()` and a `PluginsChanged` event to `IPluginCatalog`. Keep `LoadPlugins()` as the startup operation and do not raise the event from it.
- Extract the existing scan into one private path used by both initial load and reload. This prevents reload from drifting from startup behavior.
- Raise `PluginsChanged` once, after the synchronous scan has completed and the catalog collection has been replaced. Per-plugin errors remain handled by the existing warning-and-continue behavior.
- Keep notification payload-free (`EventHandler`) because consumers can query `GetAllPlugins()` for the current snapshot and no incremental diff contract is needed yet.
- Do not add reload members to `IPluginManager`; the compatibility adapter remains limited to its legacy contract while new lifecycle behavior belongs to `IPluginCatalog`.

## Risks / Trade-offs

- [Risk] A synchronous reload can briefly block the caller while descriptors are read. -> Mitigation: preserve the existing synchronous API and defer background scheduling to a future change if real workloads require it.
- [Risk] Subscribers may observe a notification without knowing which plugins changed. -> Mitigation: publish only after replacement and require consumers to query the complete current snapshot; incremental diffs are out of scope.
