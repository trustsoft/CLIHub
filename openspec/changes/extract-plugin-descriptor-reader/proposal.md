## Why

`PluginManager` currently owns plugin directory discovery, file reads, JSON deserialization, validation, duplicate handling, and logo resolution. Separating descriptor reading now will make the loader easier to test and gives the following validator/catalog changes a narrow input boundary without changing plugin behavior.

## What Changes

- Add an `IPluginDescriptorReader` abstraction for discovering plugin directories and reading/deserializing their `plugin.json` descriptors.
- Move descriptor discovery, file reads, and JSON deserialization out of `PluginManager`.
- Return descriptor directory, parsed plugin, and any per-descriptor read error so the manager can preserve its current warning and skip behavior.
- Preserve the current behavior for missing files, malformed JSON, null descriptors, and per-directory load failures.
- Register the reader in Core DI and inject it into `PluginManager`.
- Add focused reader tests and preserve all existing PluginManager tests and behavior.

## Capabilities

### New Capabilities

None. This is a structural refactor with no spec-level behavior change.

### Modified Capabilities

None.

## Impact

- Affected Core plugin loading: `PluginManager`, a new descriptor reader contract/implementation, DI registration, and plugin tests.
- JSON format, plugin validation rules, duplicate policy, logo cache behavior, and public `IPluginManager` API remain unchanged.
- The reader remains independent of WPF and can be unit tested using temporary plugin directories.
