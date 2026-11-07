# configuration-snapshot Specification

## Purpose

Provides an isolated configuration boundary so callers can read and update application state without mutating a shared in-memory graph implicitly, while preserving the established config.json persistence contract.

## Requirements

### Requirement: Configuration reads are mutation-isolated

The configuration boundary SHALL return a snapshot whose projects, preferences, and nested mutable values are detached from service-owned state.

#### Scenario: Mutating a returned snapshot does not change stored state
- **GIVEN** the configuration contains a project and preferences
- **WHEN** a caller mutates its local snapshot or any nested project/preferences value without submitting an update
- **THEN** a subsequent configuration read returns the original stored values

### Requirement: Configuration updates are explicit

The configuration repository SHALL persist changes only after a complete update transaction and SHALL apply each transaction to the latest committed snapshot.

#### Scenario: Explicit update persists changed state
- **GIVEN** a caller has read a configuration snapshot
- **WHEN** the caller submits a complete update operation
- **THEN** the updated projects, preferences, and current project are persisted

#### Scenario: Failed store update does not persist partial state
- **GIVEN** an owning store has stored preferences or project state
- **WHEN** an explicit update callback throws before completion
- **THEN** the repository propagates the error and the previous stored state remains unchanged

#### Scenario: Concurrent updates preserve both completed changes
- **GIVEN** the repository contains a configuration snapshot
- **WHEN** concurrent update operations change different configuration fields
- **THEN** each completed update is applied to the latest committed snapshot and neither update is lost

### Requirement: Stores use the snapshot boundary

The preferences and project state stores SHALL expose read operations and explicit update operations that work on detached owned values, and SHALL persist the owned section without requiring callers to pair mutable `Load` and `Save` calls.

#### Scenario: Store update preserves unrelated state
- **GIVEN** the configuration contains both preferences and project state
- **WHEN** a store updates only its owned portion through its explicit update operation
- **THEN** the updated portion is persisted and unrelated portions retain their previous values

#### Scenario: Read values are detached from the store
- **GIVEN** a caller reads preferences or project state
- **WHEN** the caller mutates the returned value without invoking an explicit update operation
- **THEN** a subsequent read returns the previously stored value

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

### Requirement: Update preferences default safely

The configuration boundary SHALL default the update preferences when they are absent from a stored document, and SHALL round-trip them: `autoDownloadUpdates` default `true` and `notifyStableUpdates` default `true`.

#### Scenario: Absent fields use defaults

- **GIVEN** a stored configuration has no update-preference fields
- **WHEN** the configuration is loaded
- **THEN** auto-download and stable notifications are enabled

#### Scenario: Update preferences round-trip

- **GIVEN** the update preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value

### Requirement: Log preferences default safely

The configuration boundary SHALL default the log preferences when they are absent from a stored document and SHALL round-trip them: `logRetentionDays` default `7` and `logFileSizeLimitMb` default `0`.

#### Scenario: Absent fields use defaults
- **GIVEN** a stored configuration has no log retention or size-limit fields
- **WHEN** the configuration is loaded
- **THEN** the retention is 7 days and the size limit is unlimited

#### Scenario: Log preferences round-trip
- **GIVEN** the log retention and size-limit preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value

### Requirement: Popup screen position defaults safely

The configuration boundary SHALL parse the stored popup screen position token and default to "Active monitor — centered" for missing or unrecognized values, without preventing the application from starting.

#### Scenario: Missing or unknown token uses the default
- **GIVEN** a stored configuration has no popup screen position or an unrecognized value
- **WHEN** the configuration is loaded
- **THEN** "Active monitor — centered" is used and the application starts normally

#### Scenario: Known token round-trips
- **GIVEN** a stored popup screen position token
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same position is used

### Requirement: Theme preference defaults safely

The configuration boundary SHALL parse the stored theme token and default to "Dark" for missing or unrecognized values, and SHALL round-trip it.

#### Scenario: Missing or unknown token uses dark
- **GIVEN** a stored configuration has no theme token or an unrecognized value
- **WHEN** the configuration is loaded
- **THEN** the dark theme is used and the application starts normally

#### Scenario: Theme token round-trips
- **GIVEN** a stored theme token
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same theme is used

### Requirement: Accent color defaults safely

The configuration boundary SHALL default the accent color to the Sky value when the stored value is missing or is not a valid color, and SHALL round-trip a valid value.

#### Scenario: Missing or invalid value uses the default
- **GIVEN** a stored configuration has no accent or an unparsable value
- **WHEN** the configuration is loaded
- **THEN** the Sky accent is used and the application starts normally

#### Scenario: Accent round-trips
- **GIVEN** a valid stored accent
- **WHEN** the configuration is persisted and loaded again
- **THEN** the same accent is used

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
