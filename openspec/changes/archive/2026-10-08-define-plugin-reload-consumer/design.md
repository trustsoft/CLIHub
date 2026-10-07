## Context

`AgentPaneController` already owns the agent collection, selected row, filtering, cache invalidation, and asynchronous version population. `LaunchWindowViewModel` delegates agent refreshes to it, making the controller the narrowest real consumer for plugin snapshot changes.

## Decision

Subscribe to `IPluginCatalog.PluginsChanged` in `AgentPaneController`. Store the last `currentProjectPath` and `showOnlyProjectAgents` values whenever `Refresh` runs. On reload, clear logo, version, and detection caches, then call `Refresh` with the stored context. Existing `AgentListSynchronizer` preserves the selected row by plugin ID when available and returns null when it is gone.

The controller remains subscribed for its lifetime; it is a singleton alongside the catalog. No file watcher or catalog polling is introduced.

## Verification

- Reload with a surviving plugin preserves selection and refreshes rows.
- Reload removing the selected plugin clears selection safely.
- Cache invalidation is verified before reload composition.
- Existing agent refresh/version cancellation tests remain unchanged.
