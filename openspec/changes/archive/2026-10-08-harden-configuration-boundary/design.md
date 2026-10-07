## Context

`ConfigurationRepository` currently caches an `AppConfig`, clones it into `ConfigurationSnapshot` for callbacks, then converts it back before serialization. `AppConfigDocument.From` assigns domain lists and preference objects directly to the JSON DTO. The writer already uses generation checks, a debounced worker, and temp-file replacement.

## Decisions

### Snapshot owns mutable repository state

The repository stores one `ConfigurationSnapshot` as its cached state. `Read` returns a detached clone, `Update` mutates a detached working snapshot, and the working snapshot becomes the cached state only after the callback succeeds. Serialization always starts from the committed snapshot.

### Explicit persistence DTO mapping

`AppConfigDocument` contains persistence-only project and preference DTOs. A dedicated mapper copies every field in both directions. The mapper preserves nullable values, defaults, list ordering, and the existing camelCase flat document shape. Compatibility helpers used by existing serialization tests remain routed through the same mapper.

### Keep writer and migration semantics

Migrations continue to operate on `ConfigurationSnapshot`. Generation checks, pending-write replacement, `Flush`, and temp-file atomic replacement remain unchanged. This keeps the latest valid snapshot winning under concurrent updates and avoids mixing persistence mechanics into mapping code.

## Risks / Trade-offs

- [Risk] A missed field silently disappears on round-trip. -> Add a mapping test covering every `AppPreferences`, `Project`, and root field.
- [Risk] DTO defaults alter legacy documents. -> Keep DTO defaults aligned with current model defaults and retain legacy schema tests.
- [Risk] State publication could occur before a failed callback. -> Commit the working snapshot only after the callback returns successfully.

## Migration Plan

1. Replace direct domain references in the persistence document with explicit DTOs and mapper methods.
2. Change the repository cache and serialization path to use `ConfigurationSnapshot`.
3. Extend mapping, concurrency, Flush, migration, and atomic-write tests.
4. Run build, tests, graphify, validate, and archive the change.
