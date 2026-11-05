# plugin-seeding Specification

## Purpose

Populates the plugins folder with built-in agent descriptors so a fresh install has a useful agent list immediately, and refreshes unmodified built-in descriptors and logos on application updates while preserving user modifications and disabled state.

## Requirements

### Requirement: Seed built-in descriptors on first run

The system SHALL maintain built-in descriptors using three-hash refresh logic that allows application updates to correct descriptors while preserving user modifications.

#### Scenario: Empty plugins folder

- **WHEN** the application starts and the plugins folder contains no plugin subdirectories and no state entries exist
- **THEN** a descriptor is written for each built-in agent under `<plugins>\<agent-id>\plugin.json`
- **AND** each descriptor is recorded in the plugin state file with origin = "builtin", `contentHash` equal to the embedded descriptor hash (`embeddedHash`), and `logoHash` equal to the embedded logo hash

#### Scenario: Existing plugins present

- **WHEN** the plugins folder already contains plugin subdirectories
- **THEN** the seeding policy evaluates each built-in descriptor individually using the three-hash decision table
- **AND** installed plugins (origin = "installed") are never overwritten
- **AND** unmodified built-in descriptors with differing hashes are refreshed (app update scenario)
- **AND** modified built-in descriptors (hash mismatch) are preserved

#### Scenario: Plugins folder missing

- **WHEN** the plugins folder does not exist at startup
- **THEN** it is created and populated with the built-in descriptors
- **AND** each descriptor is recorded in the plugin state file with origin = "builtin"

#### Scenario: Existing plugin directory matches embedded content

- **WHEN** the plugins folder contains a plugin directory with no state entry
- **AND** the on-disk `plugin.json` hash (`diskHash`) equals the embedded descriptor hash (`embeddedHash`)
- **THEN** the descriptor is adopted as a built-in
- **AND** the state file records origin = "builtin" with `contentHash` equal to `diskHash`

#### Scenario: Existing plugin directory differs from embedded content

- **WHEN** the plugins folder contains a plugin directory with no state entry
- **AND** the on-disk `plugin.json` hash (`diskHash`) does not equal the embedded descriptor hash (`embeddedHash`)
- **THEN** the descriptor is preserved as user content
- **AND** the state file records origin = "unknown" with `contentHash` equal to `diskHash`
- **AND** a warning is logged indicating the descriptor was not recognized as built-in

#### Scenario: Deleted built-in re-seeded

- **WHEN** a built-in plugin directory is deleted (state entry exists with origin = "builtin")
- **AND** the plugin is not marked disabled in the state file
- **THEN** the descriptor and logo are re-written on the next startup
- **AND** the state file is updated with the current embedded hashes (`embeddedHash`, `embeddedLogoHash`)

#### Scenario: Unmodified built-in refreshed on app update

- **WHEN** a built-in plugin directory exists (state entry origin = "builtin")
- **AND** the on-disk `plugin.json` hash (`diskHash`) equals the stored hash (`storedHash`) from the state file (unmodified by user)
- **AND** the embedded descriptor hash (`embeddedHash`) differs from `storedHash` (app update scenario)
- **THEN** the descriptor is overwritten with the embedded version
- **AND** the state file is updated with `contentHash` equal to the new `embeddedHash`

#### Scenario: No-op when already current

- **WHEN** a built-in plugin directory exists (state entry origin = "builtin")
- **AND** `diskHash` equals `storedHash` and `storedHash` equals `embeddedHash`
- **THEN** no files are written
- **AND** the state file is not changed

#### Scenario: User-modified built-in preserved on app update

- **WHEN** a built-in plugin directory exists (state entry origin = "builtin")
- **AND** the on-disk `plugin.json` hash (`diskHash`) does not equal the stored hash (`storedHash`) from the state file (user modified)
- **THEN** the descriptor is preserved and not overwritten
- **AND** a warning is logged indicating the descriptor was manually edited
- **AND** the state file `storedHash` is NOT updated (preserves user modification marker)

#### Scenario: Disabled built-in not re-seeded

- **WHEN** a built-in plugin is marked `disabled: true` in the state file
- **THEN** seeding does not write or overwrite `plugin.json` or `logo.png` files for that plugin
- **AND** the disabled state is preserved

#### Scenario: Installed plugin never touched by seeding

- **WHEN** a plugin state entry has origin = "installed"
- **THEN** seeding never writes or overwrites that plugin's files regardless of hash values
- **AND** the state entry is not modified

#### Scenario: Unknown-origin plugin adopted as built-in

- **WHEN** a plugin state entry has origin = "unknown"
- **AND** the on-disk `plugin.json` hash (`diskHash`) equals the embedded descriptor hash (`embeddedHash`)
- **THEN** the state entry is updated with origin = "builtin" and `contentHash` equal to `embeddedHash`
- **AND** no files are written (disk content is already correct)

#### Scenario: Deleted unknown-origin plugin re-seeded as built-in

- **WHEN** a plugin state entry has origin = "unknown"
- **AND** the plugin directory is missing (no user content remains)
- **THEN** the embedded descriptor and logo are written
- **AND** the state entry is updated with origin = "builtin", `contentHash` equal to `embeddedHash`, and `logoHash` equal to `embeddedLogoHash`

### Requirement: Never overwrite user content

The system SHALL NOT overwrite or delete plugin files that differ from the content it last wrote. Unmodified built-in content MAY be refreshed on application update to correct errors or add features. Disabled plugins SHALL NOT have their files rewritten.

#### Scenario: User-modified descriptor preserved

- **WHEN** a plugin directory contains a descriptor with a content hash (`diskHash`) that differs from the stored hash (`storedHash`) in the state file
- **THEN** the descriptor is left untouched by seeding
- **AND** a warning is logged indicating manual modification was detected

#### Scenario: Seeding is idempotent

- **WHEN** seeding runs more than once against a populated folder with unmodified descriptors
- **THEN** the folder contents are unchanged after the second run

### Requirement: Seeding failure is non-fatal

The system SHALL continue startup if seeding fails.

#### Scenario: Write failure
- **WHEN** a descriptor cannot be written (for example, a permissions error)
- **THEN** the failure is logged and application startup continues

### Requirement: Seed built-in agent logos

The system SHALL write each built-in agent's logo file alongside its descriptor when seeding.

#### Scenario: Logo written on first run
- **WHEN** the plugins folder is empty and seeding runs
- **THEN** a `logo.png` is written into each built-in agent's folder in addition to `plugin.json`

#### Scenario: Logo reflects in the UI
- **WHEN** a seeded agent is displayed
- **THEN** its logo is shown instead of the default placeholder

#### Scenario: Logo never overwrites user file
- **WHEN** a `logo.png` already exists in an agent folder
- **THEN** it is left untouched

#### Scenario: Missing logo falls back
- **WHEN** an agent has no bundled or user logo
- **THEN** the default logo is used

### Requirement: Load plugins deterministically

The system SHALL process plugin directories in ascending ordinal, case-insensitive order by directory path, using ordinal comparison as the tie-breaker.

#### Scenario: Plugin directories are discovered in file-system order
- **WHEN** multiple plugin directories exist in an order that differs from the required directory-path order
- **THEN** plugins are loaded and reported in the required ascending order

### Requirement: Resolve duplicate plugin IDs deterministically

The system SHALL keep the first valid plugin encountered in the deterministic directory order and SHALL skip later valid plugins with the same ID.

#### Scenario: Duplicate IDs have different directory paths
- **WHEN** valid plugin descriptors in multiple directories declare the same plugin ID
- **THEN** only the descriptor from the first directory in deterministic order is loaded

#### Scenario: Duplicate ID is skipped
- **WHEN** a valid plugin is skipped because its ID was already loaded
- **THEN** the warning identifies the duplicate ID, the skipped directory, and the directory of the loaded plugin

### Requirement: Reload the plugin catalog on demand

The system SHALL provide a synchronous manual operation that rescans the plugin directory and replaces the current catalog snapshot using the same discovery, validation, deterministic ordering, duplicate-ID, and logo-resolution rules as initial loading.

#### Scenario: New plugin is available after reload
- **WHEN** a valid plugin descriptor is added after initial loading and the manual reload operation is invoked
- **THEN** the plugin is present in the catalog after the operation completes

#### Scenario: Removed plugin is absent after reload
- **WHEN** a previously loaded plugin descriptor is removed and the manual reload operation is invoked
- **THEN** the plugin is absent from the catalog after the operation completes

### Requirement: Notify consumers after catalog reload

The system SHALL notify subscribed consumers after a manual reload completes and the catalog exposes the resulting snapshot.

#### Scenario: Reload notification is raised after replacement
- **WHEN** a manual reload operation completes
- **THEN** exactly one catalog-changed notification is raised after the new plugin snapshot is available

#### Scenario: Initial load does not create a duplicate reload notification
- **WHEN** the catalog performs its initial startup load
- **THEN** no manual-reload notification is raised solely because of the initial load

### Requirement: Detached plugin reads

The plugin catalog SHALL return detached plugin copies from its read methods. A returned copy SHALL clone nested command and detection state, so that mutating a returned plugin — including its commands or detection markers — SHALL NOT change the loaded catalog or any later read.

#### Scenario: Reads return detached copies

- **WHEN** the plugin catalog returns plugins from a read method
- **THEN** mutating a returned plugin changes neither the loaded catalog nor any later read

#### Scenario: Nested command and detection state is cloned

- **WHEN** a caller mutates a returned plugin's command or detection state
- **THEN** the catalog's internal instances keep their own state unchanged

### Requirement: Handle unsupported schema versions

The system SHALL reject descriptors with unsupported schema versions and expose the validation error so the UI can display disabled entries with reasons.

#### Scenario: Schema version absent defaults to 1

- **WHEN** a descriptor omits the schemaVersion field
- **THEN** it is treated as version 1 and loaded if other validation passes

#### Scenario: Unsupported schema version is rejected

- **WHEN** a descriptor declares schemaVersion with an integer value greater than 1
- **THEN** it is marked invalid with error "Unsupported schema version X; maximum supported is 1"
- **AND** the entry is retained in the invalid-plugins collection

#### Scenario: Invalid schema version type is rejected

- **WHEN** schemaVersion is present but not an integer (string, null, float, or other non-integer type)
- **THEN** the descriptor is marked invalid with error "Schema version must be an integer"
- **AND** the entry is retained in the invalid-plugins collection

#### Scenario: Zero or negative schema version is rejected

- **WHEN** schemaVersion is an integer less than or equal to zero
- **THEN** the descriptor is marked invalid with error "Schema version must be positive"
- **AND** the entry is retained in the invalid-plugins collection

### Requirement: Expose invalid plugin entries

The catalog SHALL retain invalid plugin entries with their validation errors so the UI can display them as disabled with diagnostic information.

#### Scenario: Invalid entries are accessible

- **WHEN** the catalog loads and encounters invalid descriptors
- **THEN** those entries are available through GetInvalidPlugins()
- **AND** each entry includes the plugin directory and the validation or read error

#### Scenario: Valid plugins remain unchanged

- **WHEN** the catalog exposes invalid entries
- **THEN** GetAllPlugins() continues to return only valid plugins
- **AND** existing consumers see no behavior change

#### Scenario: Reload replaces invalid entries

- **WHEN** the catalog is reloaded
- **THEN** the previous invalid-entries snapshot is replaced with the new scan result
- **AND** fixed descriptors move from invalid to valid, broken ones move from valid to invalid

### Requirement: Refresh built-in logos using three-hash logic

The system SHALL refresh built-in `logo.png` files using the same three-hash logic as descriptors, allowing logo updates to propagate on application updates while preserving user-modified logos.

#### Scenario: Logo refresh follows descriptor logic

- **WHEN** built-in seeding processes a plugin
- **THEN** the logo file is evaluated using `embeddedLogoHash`, `storedLogoHash`, and `diskLogoHash` with the same rules as descriptor refresh:
  - Unmodified logo + different embedded hash → overwrite (app update)
  - Modified logo (diskLogoHash != storedLogoHash) → preserve, log warning
  - Missing logo → write embedded logo
  - Disabled plugin → do not write logo

#### Scenario: Logo hash stored in state file

- **WHEN** a built-in plugin is seeded or refreshed
- **THEN** the `logoHash` field in the state entry is updated with the SHA-256 hash of the logo file content (or `null` if no logo)

#### Scenario: User-modified logo preserved on app update

- **WHEN** a built-in plugin has a `logo.png` file whose hash differs from the `storedLogoHash` in the state file
- **AND** the embedded logo hash (`embeddedLogoHash`) differs from `storedLogoHash` (app update scenario)
- **THEN** the on-disk logo is preserved (user modification)
- **AND** a warning is logged indicating the logo was manually modified

### Requirement: Skip disabled plugins during load

The system SHALL not load plugins marked as disabled in the plugin state file, but SHALL expose them separately for UI rendering.

#### Scenario: Disabled plugin skipped by GetAllPlugins

- **WHEN** the plugin catalog loads and a plugin is marked `disabled: true` in the state file
- **THEN** the plugin is not included in the result of `GetAllPlugins()`
- **AND** the plugin is not launchable

#### Scenario: Disabled plugin returned by GetDisabledPlugins

- **WHEN** the plugin catalog loads and a plugin is marked `disabled: true` in the state file
- **THEN** the plugin is included in the result of `GetDisabledPlugins()` with its id, directory, origin, and version

#### Scenario: Enabled plugin loads normally

- **WHEN** a plugin is marked `disabled: false` or has no state entry
- **THEN** the plugin loads normally if valid and appears in `GetAllPlugins()`

### Requirement: Built-in agent descriptors

The built-in set SHALL include a descriptor for each supported agent, and each descriptor SHALL
expose the interactive and maintenance commands that agent's CLI actually supports.

#### Scenario: Built-in agents seeded

- **WHEN** seeding runs
- **THEN** descriptors for OpenCode, Pi, and Qwen Code are available

#### Scenario: Each seeded descriptor is loadable

- **WHEN** a seeded descriptor is loaded by the plugin manager
- **THEN** it is accepted (it defines an id, a name, and a launch command)

#### Scenario: Complete command sets

- **WHEN** the built-in descriptors are inspected
- **THEN** OpenCode, Pi, and Qwen Code each define `launch`, `resume`, `version`, and `update`

#### Scenario: Commands match the agent CLI surface

- **WHEN** a built-in descriptor defines a command
- **THEN** the command invocation matches the agent CLI's documented interface (for example `copilot --continue` resumes the most recent session)

#### Scenario: Unsupported command kinds are omitted

- **WHEN** an agent CLI has no documented invocation for a command kind (for example an agent that can only resume by explicit session id and has no resume-most-recent command)
- **THEN** the built-in descriptor omits that command kind instead of offering a command the agent cannot fulfill
