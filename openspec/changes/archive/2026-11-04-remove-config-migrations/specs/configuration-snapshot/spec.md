# configuration-snapshot Specification

## Purpose

Provides an isolated configuration boundary so callers can read and update application state without mutating a shared in-memory graph implicitly, while preserving the established config.json persistence contract.

## MODIFIED Requirements

### Requirement: Runtime tokens resolve to the available shells

The configuration boundary SHALL parse only the canonical runtime tokens (`ps`, `ps7`, `cmd`, `bash`, case-insensitive and trimmed) and SHALL resolve any missing, unknown, or no-longer-supported token to the default runtime PowerShell, without preventing the application from starting.

#### Scenario: Known tokens map to their runtime
- **GIVEN** a configuration stores `defaultRuntime` as `ps`, `ps7`, `cmd`, or `bash` (in any case, with or without surrounding whitespace)
- **WHEN** the configuration is loaded
- **THEN** the corresponding runtime is used

#### Scenario: Removed or unknown token resolves to the default
- **GIVEN** a configuration stores `defaultRuntime` as an unrecognized value or is missing the field entirely
- **WHEN** the configuration is loaded
- **THEN** PowerShell is used and the application starts normally

### Requirement: Project scanning preferences default safely and round-trip

The configuration boundary SHALL default the project-scanning preferences when they are absent from a stored document and SHALL round-trip them through the single `config.json` document: `watchedDirectories` default empty, `scanDepth` default `2`, `ignoredPatterns` default the built-in set when absent, `discoveryMode` default `both`, and `watchedDirectorySettings` default empty. Each per-root entry SHALL carry the root path, a scan depth, and a discovery mode; entries for paths that are not in `watchedDirectories` SHALL be ignored. Adding these fields SHALL NOT require a schema version increment.

#### Scenario: Absent fields use defaults
- **GIVEN** a stored configuration has no project-scanning preference fields
- **WHEN** the configuration is loaded
- **THEN** the watched-directories list is empty, the scan depth is 2, the ignored patterns are the built-in set, the discovery mode is `both`, and there are no per-root entries

#### Scenario: Missing or unknown discovery mode uses both
- **GIVEN** a stored configuration has no discovery mode or an unrecognized token
- **WHEN** the configuration is loaded
- **THEN** the discovery mode resolves to `both` and the application starts normally

#### Scenario: Scanning preferences round-trip
- **GIVEN** the project-scanning preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value

#### Scenario: Per-root settings round-trip
- **GIVEN** watched roots have per-root scan depths and discovery modes
- **WHEN** the configuration is persisted and loaded again
- **THEN** each root keeps its own depth and discovery mode

#### Scenario: Root without a stored entry uses the defaults
- **GIVEN** a watched root has no per-root entry
- **WHEN** the configuration is loaded
- **THEN** the root uses the global scan depth and discovery mode

#### Scenario: Explicit empty pattern list is preserved
- **GIVEN** the ignored patterns are set to an explicit empty list
- **WHEN** the configuration is persisted and loaded again
- **THEN** the ignored patterns remain empty rather than reverting to the built-in set

#### Scenario: Out-of-range scan depth is normalized
- **GIVEN** a stored configuration has a scan depth below or above the allowed range
- **WHEN** the configuration is loaded
- **THEN** the built-in default scan depth is used and the application starts normally

#### Scenario: Out-of-range per-root depth is normalized
- **GIVEN** a stored per-root entry has a scan depth below or above the allowed range
- **WHEN** the configuration is loaded
- **THEN** that root's depth is clamped into the allowed range

#### Scenario: Schema version is unchanged
- **GIVEN** a configuration is persisted after project-scanning preferences are added
- **WHEN** the resulting `config.json` is read
- **THEN** it contains no `schemaVersion` metadata and the scanning fields are present

### Requirement: Project provenance defaults safely and round-trips

The configuration boundary SHALL persist each project's provenance (`source`: `manual` or `discovered`) and SHALL treat a missing or unrecognized value as `manual`. Adding this field SHALL NOT require a schema version increment.

#### Scenario: Missing provenance is manual
- **GIVEN** a stored project has no provenance field
- **WHEN** the configuration is loaded
- **THEN** the project is treated as manually registered

#### Scenario: Provenance round-trips
- **GIVEN** a project is marked discovered
- **WHEN** the configuration is persisted and loaded again
- **THEN** the project is still marked discovered

### Requirement: Default agent preference defaults safely and round-trips

The configuration boundary SHALL persist an optional `defaultAgentId` preference, default it to null when absent, and treat an unrecognized value as no default without preventing the application from starting. Adding this field SHALL NOT require a schema version increment.

#### Scenario: Absent field has no default agent
- **GIVEN** a stored configuration has no `defaultAgentId`
- **WHEN** the configuration is loaded
- **THEN** there is no default agent

#### Scenario: Default agent round-trips
- **GIVEN** a default agent id is stored
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same default agent id is restored

## ADDED Requirements

### Requirement: Snapshot persistence preserves the document without schema migration

The configuration repository SHALL be the sole coordination boundary for snapshots, debounced persistence, atomic replacement, and synchronous flush of the existing single `config.json` document, while preserving its JSON property names and defaults. Unknown properties SHALL be ignored on read, missing properties SHALL default, and the repository SHALL NOT reject or rewrite a document because of removed metadata.

#### Scenario: Snapshot round-trip preserves configuration data
- **GIVEN** a snapshot containing projects, preferences, and a current project ID
- **WHEN** it is persisted, flushed, and loaded by a new repository instance
- **THEN** all values round-trip without changing the established JSON document shape

#### Scenario: Document with unknown properties reads successfully
- **GIVEN** a configuration document contains properties that are not recognized by the current schema (e.g., `schemaVersion`, `terminalExecutable`, `showWindowOnStartup`)
- **WHEN** the configuration is loaded
- **THEN** unknown properties are ignored, known current fields load their values, and the application starts normally

#### Scenario: Document with missing current fields uses defaults
- **GIVEN** a configuration document is missing current preference fields (e.g., `defaultRuntime`, `startMinimized`)
- **WHEN** the configuration is loaded
- **THEN** missing fields receive their default values and the application starts normally

#### Scenario: Old document is not rewritten at load
- **GIVEN** a configuration document contains unknown properties or is missing current fields
- **WHEN** the configuration is loaded
- **THEN** the source document is not modified until the next explicit save operation

#### Scenario: Flush persists all completed updates
- **GIVEN** one or more repository updates have completed
- **WHEN** the repository's synchronous flush operation returns
- **THEN** all updates completed before the flush began are durable in the single configuration document

#### Scenario: Repository retains atomic single-file persistence
- **GIVEN** configuration updates are pending
- **WHEN** the repository writes them to disk
- **THEN** it preserves the established JSON property shape and uses debounced atomic replacement of the single `config.json` file

## REMOVED Requirements

### Requirement: Snapshot persistence preserves the existing document

**Reason**: Replaced by "Snapshot persistence preserves the document without schema migration", which drops the schema-version and migration scenarios while keeping the snapshot, flush, and atomic-write contract.

### Requirement: Configuration schema versions are explicit and compatible

**Reason**: The configuration boundary no longer tracks schema versions. Unknown properties are ignored, missing properties default, and the repository does not distinguish old from new documents.

### Requirement: Configuration migrations are ordered and explicit

**Reason**: The migration layer is removed. Documents load as-is with unknown properties ignored and missing properties defaulted.

### Requirement: Migration failures are non-destructive

**Reason**: No migration path remains in the configuration boundary.

### Requirement: Startup visibility preference is migrated to start-minimized

**Reason**: The migration layer is removed. Old documents are read as-is; the removed `showWindowOnStartup` field is ignored and `startMinimized` defaults to `true` when missing.

### Requirement: Legacy terminal executable maps to the available shells

**Reason**: The migration layer is removed. Old documents are read as-is; the removed `terminalExecutable` field is ignored and `defaultRuntime` defaults to PowerShell when missing.
