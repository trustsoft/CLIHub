## Why

The plugin catalog is currently loaded only during startup, so descriptors added or changed while CLIHub is running are invisible until the application restarts. A synchronous manual reload and a change notification provide the lifecycle hook needed by future catalog management without introducing file-system watcher complexity.

## What Changes

- Add a manual reload operation to the plugin catalog.
- Raise a `PluginsChanged` notification after a reload completes and the catalog contains the new snapshot.
- Reuse the existing deterministic discovery, validation, duplicate policy, and logo resolution behavior for reloads.
- Keep initial startup loading behavior unchanged and exclude automatic file watching.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `plugin-seeding`: Define on-demand plugin catalog reload and completion notification behavior.

## Impact

- `IPluginCatalog` and `PluginCatalog` lifecycle API.
- Plugin catalog tests and any future consumers that subscribe to catalog changes.
- No plugin descriptor format, startup ordering, or automatic file-watcher changes.
