## Why

`IPluginManager` currently combines plugin catalog responsibilities with a legacy manager name, while agent command, detection, and availability services consume plugin data directly. A dedicated catalog boundary will make plugin discovery and its lifecycle explicit before reload and catalog-management features are added.

## What Changes

- Introduce `IPluginCatalog` as the Core boundary for loading and reading the current plugin set.
- Move the existing descriptor discovery, validation, duplicate handling, logo resolution, and in-memory collection ownership behind a concrete catalog implementation.
- Migrate startup and UI consumers from `IPluginManager` to `IPluginCatalog`.
- Keep `IPluginManager` and its existing methods as a thin compatibility adapter during the transition.
- Preserve all current plugin loading behavior, ordering, validation, and error handling.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is a structural refactor with no intended spec-level behavior change; `.openspec.yaml` explicitly sets `skip_specs: true`.

## Impact

- Core plugin contracts and implementation under `src/CLIHub.Core/Plugins/`.
- Core DI registration and startup initialization.
- WPF tray, launch-window, and main-window consumers that currently depend on `IPluginManager`.
- Existing plugin manager tests and new catalog/adapter composition tests.
