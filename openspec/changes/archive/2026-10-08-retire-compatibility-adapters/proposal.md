## Why

The codebase still carries two compatibility boundaries whose production consumers have already moved to canonical contracts: `PluginManager` wraps `IPluginCatalog`, and `IProcessLauncher` aggregates the independent interactive and output runner ports.

## What Changes

- Remove the unused `PluginManager`/`IPluginManager` adapter and registrations.
- Change runtime preference application to depend on `IInteractiveProcessRunner`.
- Keep `IInteractiveProcessRunner` and `IProcessOutputRunner` as the canonical process boundaries.
- Update tests so they exercise canonical interfaces instead of preserving compatibility paths.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal cleanup; `skip_specs: true` is set because supported application behavior is unchanged.

## Impact

- Affects Core plugin composition, process composition, runtime preference adapter, and compatibility tests.
- Plugin catalog behavior, agent execution, output capture, and runtime selection remain unchanged.
