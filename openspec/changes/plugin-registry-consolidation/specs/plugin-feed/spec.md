## MODIFIED Requirements

### Requirement: Display available plugins with status

The system SHALL display plugins from the feed with their installation status: Available (not installed), Installed (installed version matches feed version), Update Available (feed version is newer), or Different Version Available (feed version differs but is not provably newer).

#### Scenario: Plugin not installed

- **WHEN** a plugin in the feed has no matching entry in `registry.json`
- **THEN** the plugin is displayed with status "Available"
- **AND** an Install button is shown

#### Scenario: Installed plugin matches feed version

- **WHEN** a plugin in the feed has a matching entry in `registry.json` with `installedVersion` equal to the feed `version`
- **THEN** the plugin is displayed with status "Installed"
- **AND** no Install or Update button is shown (or Install button is disabled)

#### Scenario: Installed plugin older than feed version (numeric comparison)

- **WHEN** a plugin in the feed has a matching entry in `registry.json` with `installedVersion` "1.0.0"
- **AND** the feed `version` is "1.2.0"
- **AND** both versions parse as major.minor.patch
- **THEN** the plugin is displayed with status "Update Available"
- **AND** both the installed version and feed version are shown ("Installed: 1.0.0 → Available: 1.2.0")
- **AND** an Update button is shown

#### Scenario: Installed plugin newer than feed version (downgrade case)

- **WHEN** a plugin in the feed has a matching entry in `registry.json` with `installedVersion` "2.0.0"
- **AND** the feed `version` is "1.0.0"
- **AND** both versions parse as major.minor.patch
- **THEN** the plugin is displayed with status "Different Version Available"
- **AND** both versions are shown ("Installed: 2.0.0, Available: 1.0.0")
- **AND** an Install button (not Update button) is shown with warning text "This is a different version (may be older)"

#### Scenario: Installed and feed versions differ but are not comparable (non-numeric)

- **WHEN** a plugin in the feed has a matching entry in `registry.json` with `installedVersion` that does not parse as major.minor.patch
- **AND** the feed `version` also does not parse as major.minor.patch (or either fails to parse)
- **AND** the versions differ as strings
- **THEN** the plugin is displayed with status "Different Version Available"
- **AND** both versions are shown
- **AND** an Install button is shown (not Update button)

#### Scenario: Installed plugin has unknown version

- **WHEN** a plugin in the feed has a matching entry in `registry.json` with `installedVersion` null
- **THEN** the plugin is displayed with status "Update Available"
- **AND** the feed version is shown as available

#### Scenario: Descriptor-only entries displayed like package entries

- **WHEN** the feed contains schema version 2 entries without a `package` alongside entries with one
- **THEN** both kinds of entry render the same card: same statuses from the same version comparison, same Install/Update action, and no visible distinction between zip-backed and descriptor-only entries

### Requirement: Update plugin from feed

The system SHALL install an update from the feed and replace the existing installed plugin if confirmed — for package-backed entries by downloading the updated package and verifying its checksum; for schema version 2 entries without a `package`, directly from the inline descriptor.

#### Scenario: Update from feed succeeds

- **WHEN** a user selects a plugin with "Update Available" status and clicks Update
- **THEN** the package is downloaded and its checksum is verified
- **AND** a confirmation dialog is shown with the new version metadata and all declared commands
- **AND** if the user confirms, the plugin is installed with `replaceExisting=true`
- **AND** the temporary package file is deleted after installation
- **AND** the plugin's `installedVersion` in `registry.json` is updated to the feed version

#### Scenario: Update cancelled by user

- **WHEN** a user selects a plugin with "Update Available" status, clicks Update, and views the confirmation dialog
- **AND** the user clicks Cancel
- **THEN** no installation or replacement occurs
- **AND** the temporary package file is deleted

#### Scenario: Descriptor-only update replaces existing plugin

- **WHEN** a user selects a descriptor-only entry with "Update Available" status and clicks Update, then confirms
- **THEN** the existing plugin is replaced with `replaceExisting=true` through the same backup, atomic-move, and rollback path as a package update
- **AND** the plugin's `installedVersion` in `registry.json` is updated to the installed descriptor's version
