## Context

The persisted document is represented by `AppConfigDocument` and currently contains `projects`, `preferences`, and `currentProjectId`. `ConfigService` deserializes this document and serializes it again through the existing atomic/debounced writer. A missing property is currently indistinguishable from an intentionally absent future field.

This change establishes metadata and policy only. The next planned change will introduce migration contracts, so the version handling must identify documents that need migration without embedding migration-specific transformations in ordinary deserialization.

## Goals / Non-Goals

**Goals:**

- Make the persisted schema version explicit and testable.
- Preserve readability of existing config files that have no version field.
- Prevent unknown future documents from being silently interpreted as the current schema or overwritten.
- Keep the existing JSON shape, snapshot API, debounce, flush, and atomic write behavior intact.

**Non-Goals:**

- Implement field migrations or `IConfigMigration`.
- Change the meaning of existing preference fields.
- Split the config file or introduce a second persistence path.
- Add a user-facing migration UI in this change.

## Decisions

- Define `CurrentSchemaVersion = 1`; a missing `schemaVersion` is legacy version `0`.
- Add `schemaVersion` as a top-level camelCase property in `AppConfigDocument`; all new writes use version 1.
- Legacy version 0 documents are accepted by the current reader and loaded using existing defaults/compatibility behavior. They are eligible for normalization on the next ordinary save, which writes version 1.
- A document with a schema version greater than the current version is unsupported. The service logs a warning, loads safe defaults, and does not automatically overwrite the source file; a later migration/upgrade path must explicitly handle it.
- A malformed, negative, or otherwise invalid schema version is treated as unreadable configuration and follows the existing safe-default/error logging path without destructive rewrite.
- Keep `schemaVersion` out of `ConfigurationSnapshot`; it describes the persisted document and belongs to the persistence mapping until the migration runner defines a richer version model.

## Risks / Trade-offs

- **Legacy files are rewritten only after an explicit save** -> This avoids surprising writes during read and lets the migration runner own future transformations.
- **Unknown versions make the app start with defaults** -> Preserve the source file and log clearly so newer data is not destroyed.
- **Version metadata may be omitted by direct document tests** -> Add focused serialization and service-level compatibility tests.

## Migration Plan

1. Add current/legacy version constants and the document property.
2. Update load validation and write mapping while retaining existing persistence behavior.
3. Add tests for version 1 writes, version 0 legacy reads, invalid/unknown versions, and non-destructive handling.
4. Run full build/test and validate/sync the configuration snapshot spec.
5. Rollback consists of removing schema metadata handling; existing files remain readable because the field is additive.

## Open Questions

None.
