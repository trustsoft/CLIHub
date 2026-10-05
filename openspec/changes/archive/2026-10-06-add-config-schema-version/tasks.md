## 1. Add Schema Metadata

- [x] 1.1 Define current schema version 1, legacy version 0, and the persisted `schemaVersion` field in the configuration document mapping.
- [x] 1.2 Update configuration writes to include the current version without changing existing property names or atomic persistence.

## 2. Define Compatibility Handling

- [x] 2.1 Accept missing `schemaVersion` as legacy configuration and preserve current defaults/compatibility behavior.
- [x] 2.2 Detect unknown future and invalid versions, log the condition, load safe defaults, and avoid overwriting the source document.
- [x] 2.3 Add serialization and service tests for current, legacy, unknown, and invalid versions.

## 3. Verify Preserved Behavior

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.3 Run `openspec validate add-config-schema-version` and verify the delta spec.
