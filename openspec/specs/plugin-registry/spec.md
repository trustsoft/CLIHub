# plugin-registry Specification

## Purpose
Tracks the local plugin registry document — the single local source of truth for plugin lifecycle state (origin, content hashes, installed version, enabled flag, install timestamp) — including tolerant reading, atomic writing, one-time adoption of the legacy `plugin-state.json`, uniform content-hash coverage, and content verification with reconciliation during catalog scans.

## Requirements

### Requirement: Single local registry document

The system SHALL track plugin lifecycle state — origin, content hashes, installed version, enabled flag, and install timestamp — in one registry document at `%APPDATA%\CLIHub\registry.json`, the only local source of truth for that state. Reads SHALL be tolerant (missing fields defaulted, unknown fields ignored, corrupt files degraded to an empty registry, never throwing) and writes SHALL be atomic.

#### Scenario: Registry records lifecycle state for every managed plugin

- **WHEN** a plugin is installed, seeded as a built-in, or adopted from disk
- **THEN** the registry contains an entry for its id with origin, contentHash, and enabled flag
- **AND** the plugins folder is a derived content store, not a second source of truth

#### Scenario: Missing fields default tolerantly

- **WHEN** a registry entry omits `origin` or `enabled`
- **THEN** origin degrades to "unknown" and enabled defaults to true
- **AND** the load succeeds without throwing

#### Scenario: Unknown fields ignored

- **WHEN** the registry document contains fields not in the schema
- **THEN** those fields are ignored (forward compatibility)
- **AND** parsing succeeds when the required structure is present

#### Scenario: Corrupt registry yields empty state

- **WHEN** `registry.json` exists but cannot be parsed and no legacy `plugin-state.json` exists
- **THEN** the load returns an empty registry and logs a warning
- **AND** no exception is thrown

#### Scenario: Registry written atomically

- **WHEN** the registry is saved
- **THEN** the write lands in a temp file next to the target and is renamed over it
- **AND** an interrupted write cannot leave a truncated registry document

### Requirement: Adopt legacy plugin-state.json

The system SHALL adopt an existing `%APPDATA%\CLIHub\plugin-state.json` into the registry as a one-time transition when `registry.json` is absent, and SHALL stop reading the legacy file once a registry exists.

#### Scenario: Legacy state adopted on first load

- **WHEN** `registry.json` is absent and the legacy `plugin-state.json` exists
- **THEN** legacy entries are mapped into registry entries preserving origin, contentHash, logoHash, and installedVersion
- **AND** a legacy `disabled: true` maps to `enabled: false` and `installedAt` stays null
- **AND** `registry.json` is written once and the legacy file is deleted with an informational log

#### Scenario: Failed adoption keeps legacy file

- **WHEN** the adoption write of `registry.json` fails
- **THEN** the legacy file is kept and the mapped content is served from memory
- **AND** adoption is retried on the next application start

#### Scenario: Legacy ignored once registry exists

- **WHEN** both `registry.json` and the legacy `plugin-state.json` exist
- **THEN** the registry wins and the legacy file is not read
- **AND** the presence of the ignored legacy file is logged once per run

#### Scenario: Corrupt registry recovered from legacy

- **WHEN** `registry.json` exists but cannot be parsed and the legacy `plugin-state.json` exists
- **THEN** the legacy content is adopted into a fresh registry with a warning that the registry was unreadable

### Requirement: Uniform content hash coverage

The system SHALL record a contentHash — the hex-encoded SHA-256 of the raw `plugin.json` file bytes — for every plugin it manages: zip installs, descriptor-only installs, seeded built-ins, and folders adopted from disk.

#### Scenario: Hash recorded for every install kind

- **WHEN** a plugin is installed from a package or from an inline descriptor
- **THEN** the registry entry records the contentHash of the installed `plugin.json` bytes

#### Scenario: Seeded built-ins keep three-hash semantics

- **WHEN** built-in plugins are seeded or refreshed
- **THEN** the seeder's embedded/disk/stored three-hash comparison operates with the stored hash held in the registry
- **AND** unmodified built-ins are refreshed on application updates while user-modified ones are preserved

#### Scenario: Adopted folders record the disk hash

- **WHEN** a plugin folder without a registry entry is adopted during a catalog scan
- **THEN** its registry entry records the contentHash of the descriptor currently on disk

#### Scenario: Stored hash not rewritten by detection

- **WHEN** a descriptor on disk no longer matches the stored contentHash
- **THEN** detection and verification do not update the stored hash
- **AND** the modification remains detectable on every later scan

### Requirement: Verify content and reconcile on catalog load

The system SHALL verify each processed plugin's stored contentHash against the on-disk descriptor at catalog load, flag mismatches as user-modified, and reconcile plugin folders that have no registry entry.

#### Scenario: Modified descriptor flagged as user-modified

- **WHEN** a processed plugin's stored contentHash differs from the on-disk descriptor hash
- **THEN** a warning is logged and the plugin appears in the catalog's user-modified collection with its stored and disk hashes
- **AND** the plugin still loads normally

#### Scenario: Built-in and installed verified uniformly

- **WHEN** the catalog verifies descriptor content
- **THEN** the same comparison applies to every origin (builtin, installed, unknown)
- **AND** disabled plugins are verified in the skip path with mismatches logged

#### Scenario: Matching or unknown hash loads without flag

- **WHEN** the stored contentHash equals the disk hash, or no stored hash exists
- **THEN** the plugin is not flagged as user-modified
- **AND** a null stored hash is treated as unverifiable, not modified

#### Scenario: Folder without entry adopted as unknown

- **WHEN** a plugin folder with a valid descriptor has no registry entry
- **THEN** a registry entry is created with origin "unknown", the disk contentHash, the descriptor's version, and enabled true
- **AND** the registry is saved at most once per scan

#### Scenario: Registry entry with missing folder retained

- **WHEN** a registry entry's plugin folder is absent from disk
- **THEN** the entry is kept with an informational log and is not pruned
- **AND** built-in re-seeding and disabled-state preservation continue to honor the retained entry

#### Scenario: Verification never writes the registry

- **WHEN** a catalog scan completes with nothing to adopt
- **THEN** the registry document is not rewritten
