## MODIFIED Requirements

### Requirement: Disable and enable plugins

The system SHALL allow users to disable plugins (preventing load and launch) and re-enable them later, without deleting files. A failed registry write during disable or enable SHALL surface the failure to the user and present the persisted truth.

#### Scenario: Disable an enabled plugin

- **WHEN** a user disables an enabled plugin
- **THEN** the registry is updated with `enabled: false` for that plugin id
- **AND** the catalog is reloaded
- **AND** the plugin is removed from GetAllPlugins()
- **AND** the plugin is included in GetDisabledPlugins()

#### Scenario: Enable a disabled plugin

- **WHEN** a user enables a disabled plugin
- **THEN** the registry is updated with `enabled: true` for that plugin id
- **AND** the catalog is reloaded
- **AND** the plugin is included in GetAllPlugins()
- **AND** the plugin is removed from GetDisabledPlugins()

#### Scenario: Disabled plugin files not deleted

- **WHEN** a plugin is disabled
- **THEN** the plugin directory and its files remain on disk
- **AND** the plugin can be re-enabled without re-installation

#### Scenario: Failed save after disable surfaces message

- **WHEN** a user disables an enabled plugin and the registry write fails
- **THEN** the card displays "Could not save the plugin state"
- **AND** the catalog is reloaded to present the persisted (pre-attempt) state
- **AND** the plugin remains enabled on the next restart

#### Scenario: Failed save after enable surfaces message

- **WHEN** a user enables a disabled plugin and the registry write fails
- **THEN** the card displays "Could not save the plugin state"
- **AND** the catalog is reloaded to present the persisted (pre-attempt) state
- **AND** the plugin remains disabled on the next restart

### Requirement: Plugin lifecycle state tracking

The system SHALL record plugin origin, content hash, logo hash, installed version, enabled flag, and install timestamp in the local plugin registry document (`%APPDATA%\CLIHub\registry.json`), the single local source of truth for plugin lifecycle state. A state-write failure after a committed install SHALL add a warning, not a failure.

#### Scenario: State file updated on install

- **WHEN** a plugin is installed
- **THEN** the registry is updated with an entry for the plugin id
- **AND** origin is set to "installed"
- **AND** contentHash is set to the SHA-256 hash of the installed `plugin.json` content
- **AND** logoHash is set to the SHA-256 hash of the installed `logo.png` content (or null if no logo)
- **AND** installedVersion is set to the descriptor's `version` field (or null if absent)
- **AND** enabled is set to true
- **AND** installedAt is set to the UTC install time

#### Scenario: State persists across restarts

- **WHEN** CLIHub restarts
- **THEN** the registry is read and installed plugins retain their origin, hash, version, enabled state, and install timestamp

#### Scenario: State write failure after install adds warning

- **WHEN** a plugin is installed successfully (files committed, backup deleted) and the registry write fails
- **THEN** the install result reports `Success = true` with a warning that the plugin state could not be saved
- **AND** the warning states the plugin will appear as Unknown origin on the next catalog scan
- **AND** the files remain installed and functional

### Requirement: Atomic installation with rollback

The system SHALL stage extracted files in a temporary directory and atomically move them into place, with rollback on failure. A state-write failure after the atomic move SHALL be a warning, not a rollback trigger.

#### Scenario: Staging directory used

- **WHEN** a package is installed
- **THEN** files are extracted to `<plugins>/.staging-<guid>/`
- **AND** the staging directory is created only for the duration of installation

#### Scenario: Atomic move to target

- **WHEN** staging completes successfully
- **THEN** the staging directory is renamed to `<plugins>/<id>/` in a single filesystem operation
- **AND** the staging directory no longer exists after success

#### Scenario: Backup created for replacement

- **WHEN** an existing plugin directory is replaced
- **THEN** it is first renamed to `<id>.backup-<guid>` before the new plugin is moved into place

#### Scenario: Rollback on failure

- **WHEN** installation fails after staging (validation error, descriptor invalid, etc.)
- **THEN** the staging directory is deleted
- **AND** if a backup was created, it is restored to `<plugins>/<id>/`
- **AND** the state file is not modified

#### Scenario: Backup deleted on success

- **WHEN** installation succeeds
- **THEN** any backup directory is deleted

#### Scenario: State write failure after move is a warning not a rollback

- **WHEN** the atomic move succeeds and the backup is deleted, but the subsequent registry write fails
- **THEN** the install is not rolled back (files stay in place)
- **AND** the install result carries a warning, not a failure
- **AND** the backup is not restored (it was already deleted before the state write)

### Requirement: SHA-256 content integrity

The system SHALL compute and store SHA-256 content hashes for `plugin.json` and `logo.png` to detect content drift and user modifications, and SHALL verify the stored contentHash against the descriptor on disk at catalog load. An install or update SHALL replace the descriptor and the stored hash, clearing any user-modified flag for that plugin.

#### Scenario: Hash computed on install

- **WHEN** a plugin is installed
- **THEN** the contentHash is computed from the raw `plugin.json` file bytes (UTF-8) and hex-encoded
- **AND** the logoHash is computed from the raw `logo.png` file bytes and hex-encoded (or null if logo absent)

#### Scenario: Hash comparison detects modification

- **WHEN** a plugin file on disk is modified after installation
- **AND** the catalog loads and computes its hash
- **THEN** the hash does not match the stored hash in the registry
- **AND** the plugin is recognized as user-modified (logged and exposed through the catalog's user-modified collection)

#### Scenario: Hash semantics clarified

- **WHEN** the registry stores contentHash or logoHash
- **THEN** it indicates **content drift and user modification detection** relative to what CLIHub last wrote
- **AND** it does NOT indicate cryptographic authenticity or tamper-evidence against motivated attackers
- **AND** the user is informed that hashes detect changes, not unauthorized access

#### Scenario: Local modification cleared by install

- **WHEN** a plugin descriptor was locally modified (stored hash and disk hash differ)
- **AND** the plugin is installed or updated from a package or descriptor-only source
- **THEN** the installed descriptor and its content hash replace the modified ones
- **AND** the plugin is no longer reported as user-modified on the next catalog scan
- **AND** the user-modified flag is a diagnostic of drift since the last CLIHub write, not a protection mechanism