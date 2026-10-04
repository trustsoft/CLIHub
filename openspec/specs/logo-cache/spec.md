# logo-cache Specification

## Purpose

Caches project and agent logo resolution results in a persistent, key-based store, so logo lookups avoid repeated file-system scans and survive application restarts.

## Requirements

### Requirement: Key-based logo lookup

The system SHALL resolve a project's or agent's logo by a unique key: the project ID for projects and the plugin ID for agents.

#### Scenario: Project key
- **WHEN** a project logo is resolved
- **THEN** the cache entry is addressed by the project's unique ID

#### Scenario: Agent key
- **WHEN** an agent logo is resolved
- **THEN** the cache entry is addressed by the agent's plugin ID

### Requirement: Cache-first resolution

The system SHALL return a cached logo for a key without touching the file system, and SHALL resolve on the file system only when the key is not cached, storing the resolved value back into the cache.

#### Scenario: Cache hit
- **WHEN** a logo is requested for a key that has a cached value
- **THEN** the cached value is returned and no file-system scan is performed

#### Scenario: Cache miss resolves and stores
- **WHEN** a logo is requested for a key that has no cached value
- **THEN** the logo is resolved from the file system and the resolved value is stored in the cache

### Requirement: Negative result caching

The system SHALL cache the outcome of a resolution even when no logo file is found.

#### Scenario: No logo found is cached
- **WHEN** a logo resolution scans the file system and finds no logo
- **THEN** the negative outcome is cached for that key and later lookups for the same key do not rescan

### Requirement: Cache persistence across restarts

The system SHALL load the logo cache from a state file under the application data folder at startup and SHALL save the cache state back to that file at application shutdown.

#### Scenario: State loaded at startup
- **WHEN** the application starts and a cache state file exists from a previous run
- **THEN** cached entries are loaded and logo lookups reuse them without rescanning

#### Scenario: State saved at shutdown
- **WHEN** the application exits
- **THEN** the current cache state, including entries resolved during the run, is written to the state file

#### Scenario: Updates apply on top of loaded state
- **WHEN** a logo is resolved during a run for a key that was loaded from the state file
- **THEN** the freshly resolved value replaces the loaded value in the cache

#### Scenario: First run without state file
- **WHEN** the application starts and no cache state file exists
- **THEN** the cache starts empty and resolution proceeds normally

### Requirement: Safe cache file handling

The system SHALL write the cache state file atomically, skip rewriting it when nothing changed, and continue normally when the state file is missing or unreadable.

#### Scenario: Unchanged state is not rewritten
- **WHEN** the application exits and no cache entry changed since the last save
- **THEN** the state file is not rewritten

#### Scenario: Unreadable state file
- **WHEN** the cache state file exists but cannot be read or parsed
- **THEN** the application starts with an empty cache and continues normally

#### Scenario: Atomic write
- **WHEN** the cache state is saved
- **THEN** the file is written through a temporary file and moved into place so an interrupted save cannot corrupt the previous state

### Requirement: Cache entry removal

The system SHALL remove a project's logo cache entry when that project is removed, so the entry is no longer present when the cache state is saved.

#### Scenario: Entry removed with the project
- **WHEN** a project is removed
- **THEN** its logo cache entry is deleted, and a subsequent lookup for that key re-resolves instead of returning the stale value

#### Scenario: Removed entry is not persisted
- **WHEN** a project is removed and the application later saves the cache state
- **THEN** the state file does not contain an entry for the removed project

### Requirement: Cache invalidation on demand

The system SHALL clear cached logo entries on demand so the next lookup re-resolves from the file system and updates the cache.

#### Scenario: Manual refresh re-resolves
- **WHEN** the user triggers a refresh in the launch window
- **THEN** cached logo entries are cleared and subsequent lookups re-resolve and update the cache

#### Scenario: Invalidation is non-fatal to resolution
- **WHEN** a re-resolve after invalidation finds no logo
- **THEN** the negative outcome is cached again and the default logo is used for display
