# plugin-installation Specification

## Purpose
Allows users to install, disable, enable, and manage AI agent CLI plugin descriptors from local package files without waiting for a CLIHub application release.

## Requirements

### Requirement: Inspect plugin package before installation

The system SHALL provide a pre-installation inspection capability that validates a package and returns metadata without writing files.

#### Scenario: Valid package inspection

- **WHEN** the user inspects a valid `.clihub-plugin.zip` file
- **THEN** the preview includes plugin id, name, version, publisher, homepage, all declared commands (each with command kind, executable, and arguments), and an empty error list
- **AND** IsValid is true

#### Scenario: Invalid package inspection

- **WHEN** the user inspects an invalid `.clihub-plugin.zip` file (missing plugin.json, extra files, zip-slip paths, etc.)
- **THEN** the preview includes validation errors describing what failed
- **AND** IsValid is false

#### Scenario: Package replaces existing installed plugin

- **WHEN** the user inspects a package whose plugin id matches an existing installed plugin
- **THEN** ReplacesExistingPlugin is true
- **AND** ExistingPluginVersion contains the currently installed version from the state file (or null if version is unknown)

#### Scenario: Inspection does not write files

- **WHEN** the user inspects a package
- **THEN** no files are written to the plugins directory or staging directory
- **AND** the state file is not modified

### Requirement: Install plugin from local package

The system SHALL accept a local `.clihub-plugin.zip` file, validate it, stage it, atomically install it, update state, and reload the catalog.

#### Scenario: Valid package installs successfully

- **WHEN** a valid `.clihub-plugin.zip` is installed with replaceExisting=true
- **THEN** the descriptor and logo are extracted to `<plugins>/<id>/`
- **AND** the state file records origin = "installed", contentHash, logoHash, and installedVersion
- **AND** the catalog is reloaded
- **AND** Success is true

#### Scenario: Duplicate id rejected without replacement consent

- **WHEN** a package with a duplicate installed plugin id is installed with replaceExisting=false
- **THEN** the installation is rejected with an error
- **AND** no files are written
- **AND** Success is false

#### Scenario: Existing plugin replaced when consent given

- **WHEN** a package with a duplicate installed plugin id is installed with replaceExisting=true
- **THEN** the existing plugin directory is backed up to `.backup-<guid>`
- **AND** the new plugin files replace the existing ones
- **AND** the backup is deleted on success

#### Scenario: Built-in id rejected for installed plugin

- **WHEN** a package declares a plugin id that matches any built-in plugin embedded in the current CLIHub build
- **THEN** the installation is rejected with an error indicating the id is reserved
- **AND** no files are written

**Note:** Built-in IDs are defined as the set of IDs embedded in the current build (enumerated from the same embedded resources used by `PluginSeeder`), not a hardcoded list. This ensures future built-in plugins are automatically reserved without code changes.

### Requirement: Package validation and security

The system SHALL enforce structural and security validation on plugin packages.

#### Scenario: Archive contains exactly one top-level directory

- **WHEN** a package is inspected
- **THEN** it is rejected if it contains zero or multiple top-level entries
- **AND** the error describes the structural violation

#### Scenario: Plugin id matches directory name

- **WHEN** a package is extracted and the descriptor is read
- **THEN** it is rejected if `id` in `plugin.json` does not equal the top-level directory name
- **AND** the error describes the id mismatch

#### Scenario: Only descriptor and logo permitted

- **WHEN** a package is inspected
- **THEN** it is rejected if it contains any files other than `<id>/plugin.json` and `<id>/logo.png`
- **AND** the error lists the disallowed files

#### Scenario: Zip-slip paths rejected

- **WHEN** a package contains an entry with `..` in its path or an absolute path
- **THEN** it is rejected during inspection
- **AND** the error describes the zip-slip attempt

#### Scenario: Symlinks rejected

- **WHEN** a package contains a symlink entry
- **THEN** it is rejected during inspection

#### Scenario: Archive size limit enforced

- **WHEN** a package exceeds 10 MB
- **THEN** it is rejected during inspection
- **AND** the error includes the archive size

#### Scenario: File count limit enforced

- **WHEN** a package contains more than 100 entries
- **THEN** it is rejected during inspection

### Requirement: Atomic installation with rollback

The system SHALL stage extracted files in a temporary directory and atomically move them into place, with rollback on failure.

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

### Requirement: Plugin lifecycle state tracking

The system SHALL record plugin origin, content hash, logo hash, installed version, and disabled flag in a separate state file.

#### Scenario: State file updated on install

- **WHEN** a plugin is installed
- **THEN** the state file is updated with an entry for the plugin id
- **AND** origin is set to "installed"
- **AND** contentHash is set to the SHA-256 hash of the installed `plugin.json` content
- **AND** logoHash is set to the SHA-256 hash of the installed `logo.png` content (or null if no logo)
- **AND** installedVersion is set to the descriptor's `version` field (or null if absent)
- **AND** disabled is set to false

#### Scenario: State persists across restarts

- **WHEN** CLIHub restarts
- **THEN** the state file is read and installed plugins retain their origin, hash, version, and disabled state

### Requirement: Disable and enable plugins

The system SHALL allow users to disable plugins (preventing load and launch) and re-enable them later, without deleting files.

#### Scenario: Disable an enabled plugin

- **WHEN** a user disables an enabled plugin
- **THEN** the state file is updated with `disabled: true` for that plugin id
- **AND** the catalog is reloaded
- **AND** the plugin is removed from GetAllPlugins()
- **AND** the plugin is included in GetDisabledPlugins()

#### Scenario: Enable a disabled plugin

- **WHEN** a user enables a disabled plugin
- **THEN** the state file is updated with `disabled: false` for that plugin id
- **AND** the catalog is reloaded
- **AND** the plugin is included in GetAllPlugins()
- **AND** the plugin is removed from GetDisabledPlugins()

#### Scenario: Disabled plugin files not deleted

- **WHEN** a plugin is disabled
- **THEN** the plugin directory and its files remain on disk
- **AND** the plugin can be re-enabled without re-installation

### Requirement: Plugin origin display

The system SHALL display the origin of each plugin in the CLI Agents UI.

#### Scenario: Built-in badge displayed

- **WHEN** a plugin has origin = "builtin" in the state file
- **THEN** the UI displays a "Built-in" badge on the plugin card

#### Scenario: Installed badge displayed

- **WHEN** a plugin has origin = "installed" in the state file
- **THEN** the UI displays an "Installed" badge on the plugin card

#### Scenario: Unknown badge displayed

- **WHEN** a plugin has origin = "unknown" in the state file (no recognized origin)
- **THEN** the UI displays an "Unknown" badge (neutral styling) on the plugin card

#### Scenario: Origin tooltip shows metadata

- **WHEN** a user hovers over an origin badge
- **THEN** a tooltip displays the plugin's version, publisher, and homepage (if available)

### Requirement: Open plugins folder

The system SHALL provide a command to open the plugins directory in Windows Explorer.

#### Scenario: Plugins folder opened

- **WHEN** the user clicks "Open Plugins Folder"
- **THEN** Windows Explorer opens at `%APPDATA%\CLIHub\plugins\`

### Requirement: Manual catalog reload

The system SHALL provide a command to reload the plugin catalog without restarting the application.

#### Scenario: Catalog reloaded on demand

- **WHEN** the user clicks "Reload Catalog"
- **THEN** the catalog rescans the plugins directory
- **AND** the UI is updated with any newly detected, modified, or removed plugins

### Requirement: Installation confirmation dialog

The system SHALL show a confirmation dialog after package inspection and before installation, displaying plugin metadata and every declared command with its exact executable and arguments.

#### Scenario: Confirmation dialog shown for valid package

- **WHEN** a user selects a valid `.clihub-plugin.zip` file
- **THEN** a confirmation dialog is displayed
- **AND** the dialog shows plugin name, version, publisher, and homepage
- **AND** the dialog lists every declared command (launch, resume, version, update) with its exact executable and arguments
- **AND** if the package replaces an existing installed plugin, the dialog indicates the current installed version

#### Scenario: Confirmation lists every declared command

- **WHEN** the confirmation dialog displays a plugin with multiple command kinds
- **THEN** each declared command kind (launch, resume, version, update) is shown with its exact executable and arguments
- **AND** undeclared command kinds are omitted from the list

**Security rationale:** All command kinds execute arbitrary binaries specified in the descriptor. A package could hide its payload in `resume`, `version`, or `update` while showing a harmless `launch` command. Listing every declared command ensures the user can inspect all execution paths before approving installation.

#### Scenario: Error dialog shown for invalid package

- **WHEN** a user selects an invalid `.clihub-plugin.zip` file
- **THEN** an error dialog is displayed
- **AND** the dialog lists the validation errors
- **AND** no confirmation or installation occurs

#### Scenario: Installation proceeds only after confirmation

- **WHEN** a user confirms the installation dialog
- **THEN** the Install method is called with replaceExisting=true if replacing, false otherwise
- **AND** installation proceeds

#### Scenario: Installation cancelled

- **WHEN** a user cancels the confirmation dialog
- **THEN** no installation occurs
- **AND** no files are written

### Requirement: Descriptor metadata fields

The system SHALL support optional `version`, `publisher`, and `homepage` fields in plugin descriptors without requiring a schema version change.

#### Scenario: Optional fields populated

- **WHEN** a descriptor includes `version`, `publisher`, or `homepage`
- **THEN** those values are read and available in the plugin model

#### Scenario: Optional fields absent

- **WHEN** a descriptor omits `version`, `publisher`, or `homepage`
- **THEN** the plugin is accepted and those properties are null

### Requirement: SHA-256 content integrity

The system SHALL compute and store SHA-256 content hashes for `plugin.json` and `logo.png` to detect content drift and user modifications.

#### Scenario: Hash computed on install

- **WHEN** a plugin is installed
- **THEN** the contentHash is computed from the raw `plugin.json` file bytes (UTF-8) and hex-encoded
- **AND** the logoHash is computed from the raw `logo.png` file bytes and hex-encoded (or null if logo absent)

#### Scenario: Hash comparison detects modification

- **WHEN** a plugin file on disk is modified after installation
- **AND** the catalog or seeding policy computes its hash
- **THEN** the hash does not match the stored hash in the state file
- **AND** the file is recognized as user-modified

#### Scenario: Hash semantics clarified

- **WHEN** the state file stores contentHash or logoHash
- **THEN** it indicates **content drift and user modification detection** relative to what CLIHub last wrote
- **AND** it does NOT indicate cryptographic authenticity or tamper-evidence against motivated attackers
- **AND** the user is informed that hashes detect changes, not unauthorized access

### Requirement: Disabled plugins rendered in separate UI section

The system SHALL display disabled plugins in a separate "Disabled" section of the CLI Agents page with an Enable action.

#### Scenario: Disabled section shown when plugins are disabled

- **WHEN** one or more plugins are disabled
- **THEN** the CLI Agents page displays a "Disabled" section below the active plugins list

#### Scenario: Disabled plugin cards show Enable button

- **WHEN** a disabled plugin is rendered in the Disabled section
- **THEN** the card shows the plugin id, origin badge, version (if known), and an "Enable" button

#### Scenario: No Disabled section when all plugins enabled

- **WHEN** all plugins are enabled
- **THEN** the Disabled section is not displayed
