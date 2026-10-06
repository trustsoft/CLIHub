## Why

After descriptor reading was extracted, `PluginManager` still owns plugin validation rules and turns them directly into log messages. A dedicated validator with structured results will make validation independently testable and give the later catalog/policy changes a stable boundary without changing which plugins are accepted.

## What Changes

- Add `IPluginDescriptorValidator` and a structured `PluginValidationResult`.
- Move required ID, required name, and required launch command checks out of `PluginManager`.
- Preserve the current validation rules, error text, skip behavior, and manager logging.
- Register the validator in Core DI and inject it into `PluginManager`.
- Add focused validator tests and preserve existing PluginManager/Seeder behavior.

## Capabilities

### New Capabilities

None. This is a structural refactor with no spec-level behavior change.

### Modified Capabilities

None.

## Impact

- Affected Core plugin loading: new validator contract/implementation, `PluginManager`, DI registration, and tests.
- Plugin JSON shape, accepted/rejected descriptors, duplicate policy, directory ordering, logging, and logo cache behavior remain unchanged.
- The structured result is internal Core API and does not alter public `IPluginManager`.
