## Context

`PluginManager` currently validates a deserialized `Plugin` inline: ID and name must be non-empty, and `Commands.Launch` must exist. It logs a joined error string with the plugin directory and skips invalid descriptors. The next plugin change will define deterministic loading policy, so validation should be isolated before policy moves.

## Goals / Non-Goals

**Goals:**

- Make descriptor validation an injectable, independently testable Core service.
- Return structured validation status and all validation errors.
- Preserve exact current acceptance rules and manager-facing warning text.
- Keep directory path/context out of descriptor model validation unless needed for result metadata.

**Non-Goals:**

- Add new validation rules or change required fields.
- Move duplicate ID policy, directory ordering, or plugin origin handling.
- Change plugin JSON deserialization or `IPluginManager`.

## Decisions

- Add `IPluginDescriptorValidator.Validate(Plugin plugin)` returning `PluginValidationResult` with `IsValid` and an immutable/read-only error collection.
- Implement the current three rules: non-empty `Id`, non-empty `Name`, and non-null `Commands.Launch`.
- `PluginManager` invokes the validator after the reader returns a plugin. When invalid, it logs the existing `Invalid plugin in {Directory}: {Errors}` warning by joining structured error messages, then skips the plugin.
- Register the validator as a singleton in `AddPluginServices` and inject it into `PluginManager`, retaining optional defaults for direct construction tests if consistent with the reader pattern.
- Keep the validator independent of filesystem and logging so unit tests can assert individual and combined errors without log capture.

## Risks / Trade-offs

- **Error order changes** -> Preserve the existing rule order: ID, Name, launch command.
- **Manager and validator duplicate checks** -> Remove inline checks completely; manager only maps result to existing logging and skip behavior.
- **Future validator evolution changes acceptance** -> Keep this change limited to existing rules and add regression tests for all three.

## Migration Plan

1. Add validation result, contract, implementation, and focused tests.
2. Inject validator into `PluginManager` and remove inline validation rules.
3. Register through Core DI and run existing plugin/seeder tests.
4. Run full Core/solution build and test, then archive the structural change.
5. Rollback consists of restoring inline checks and removing validator wiring/types.

## Open Questions

None.
