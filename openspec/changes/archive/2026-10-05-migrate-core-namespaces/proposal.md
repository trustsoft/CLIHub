## Why

The Core project is now organized into subsystem folders, but most implementations and interfaces still use the broad `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` namespaces. This weakens discoverability and hides ownership: a type in `Projects/` appears to belong to a shared service bucket, and consumers can import broad namespaces instead of depending on the subsystem that owns the contract.

The folder structure and the namespace structure should describe the same architecture before more Core functionality is added.

## What Changes

- Migrate Core implementation and contract namespaces from the broad `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` buckets to the subsystem namespaces that own them.
- Keep models, hotkeys, logging, formatting, composition, and embedded seed resources in their existing namespace families unless the design identifies a concrete ownership conflict.
- Update all Core, WPF, and test consumers to use the new namespaces and remove obsolete broad namespace imports.
- Keep the public method signatures, DI registrations, service lifetimes, persistence format, and runtime behavior unchanged.
- **BREAKING**: source consumers that reference the old public Core namespaces must update their using directives and fully qualified type names.
- Update architecture and repository-structure documentation to describe the namespace ownership map and the migration rule.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a structural refactor with no observable capability or requirement change; the change uses `skip_specs: true`.

## Impact

- Affects namespace declarations and using directives throughout `src/CLIHub.Core`, `src/CLIHub`, and `tests/CLIHub.Tests`.
- Affects public source-level Core API names, while preserving public members and runtime contracts.
- Requires focused compilation and test verification after each subsystem migration, followed by full solution build, tests, and OpenSpec validation.
- Does not change persisted configuration, plugin descriptors, Windows integration behavior, process command construction, update behavior, or UI behavior.
