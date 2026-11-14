## MODIFIED Requirements

### Requirement: Plugin lifecycle state tracking

The system SHALL record plugin origin, content hash, logo hash, installed version, enabled flag, and install timestamp in the local plugin registry document (`%APPDATA%\CLIHub\registry.json`), the single local source of truth for plugin lifecycle state.

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

### Requirement: Disable and enable plugins

The system SHALL allow users to disable plugins (preventing load and launch) and re-enable them later, without deleting files.

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

### Requirement: SHA-256 content integrity

The system SHALL compute and store SHA-256 content hashes for `plugin.json` and `logo.png` to detect content drift and user modifications, and SHALL verify the stored contentHash against the descriptor on disk at catalog load.

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
