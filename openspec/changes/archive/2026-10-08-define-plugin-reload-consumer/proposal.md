## Why

`PluginCatalog` raises `PluginsChanged` after an explicit reload, but no application consumer refreshes the agent pane or invalidates descriptor-related caches. A reload can therefore leave stale agent rows, selections, detection results, versions, or logos visible.

## What Changes

- Make `AgentPaneController` the application consumer of `IPluginCatalog.PluginsChanged`.
- Preserve the last project/filter context and rebuild agent rows after reload.
- Invalidate detection, version, and logo caches before rebuilding.
- Preserve surviving selection identity and clear selection when its plugin disappears.
- Do not add a file watcher in this change.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal reload-consumer change; `skip_specs: true` is set because explicit reload behavior becomes the supported UI path without changing plugin format.

## Impact

- Affects `AgentPaneController` and its ViewModel tests.
- `PluginCatalog` publishing behavior and plugin descriptor format remain unchanged.
