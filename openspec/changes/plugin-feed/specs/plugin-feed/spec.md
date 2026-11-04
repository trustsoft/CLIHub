# plugin-feed Specification

## Purpose

Allows users to browse, download, and install AI agent CLI plugin descriptors from a curated remote feed served over HTTPS, with update detection for installed plugins and integrity verification via SHA-256 checksums.

## ADDED Requirements

### Requirement: Fetch and parse feed index

The system SHALL fetch a JSON feed index from a configured HTTPS URL, validate its schema, and return available plugins with metadata.

#### Scenario: Valid index fetched successfully

- **WHEN** the feed URL is reachable and returns a valid `index.json` with `schemaVersion: 1` and a `plugins` array
- **THEN** the feed result includes a parsed `PluginFeedIndex` with the list of plugins
- **AND** Success is true

#### Scenario: Index schema version unsupported

- **WHEN** the feed index has `schemaVersion` greater than 1
- **THEN** the feed result includes an error "Unsupported feed schema version X"
- **AND** Success is false

#### Scenario: Index missing required field

- **WHEN** the feed index is missing `schemaVersion` or `plugins` array
- **THEN** the feed result includes an error describing the missing field
- **AND** Success is false

#### Scenario: Index contains unknown fields

- **WHEN** the feed index or plugin entries contain fields not in the schema
- **THEN** those fields are ignored (forward compatibility)
- **AND** parsing succeeds if all required fields are present

#### Scenario: Index entry with invalid fields skipped

- **WHEN** a feed index entry is missing required fields (e.g., `id`, `name`, `packageUrl`, `sha256`)
- **THEN** that entry is skipped with a logged warning
- **AND** the rest of the index entries are loaded successfully
- **AND** the feed result Success is true if the index itself is valid

#### Scenario: Duplicate plugin IDs in index

- **WHEN** the feed index contains multiple entries with the same plugin `id`
- **THEN** the first entry is kept
- **AND** later entries with the same `id` are skipped with a logged warning

#### Scenario: Index exceeds size limit

- **WHEN** the feed index response is larger than 2 MB
- **THEN** the feed result includes an error "Feed index too large"
- **AND** Success is false

#### Scenario: HTTP URL rejected

- **WHEN** the configured feed URL has `http://` scheme (not HTTPS)
- **THEN** the request is rejected before sending
- **AND** the feed result includes an error "Feed URL must use HTTPS"
- **AND** Success is false

#### Scenario: Redirect to HTTP rejected

- **WHEN** an HTTPS feed URL redirects to an `http://` URL
- **THEN** the request is rejected after detecting the redirect
- **AND** the feed result includes an error "Redirect to non-HTTPS URL rejected"
- **AND** Success is false

#### Scenario: Network timeout

- **WHEN** the feed fetch exceeds the configured agent probe timeout
- **THEN** the feed result includes an error "Feed request timed out"
- **AND** Success is false

#### Scenario: Network unavailable

- **WHEN** the network is unavailable or the feed URL is unreachable
- **THEN** the feed result includes an error "Could not load plugin feed"
- **AND** Success is false
- **AND** no exception is thrown

### Requirement: Download package and verify checksum

The system SHALL download a plugin package to a temporary file, compute its SHA-256 checksum, compare with the expected checksum from the feed index, and return the temp file path on match or delete the file and return an error on mismatch.

#### Scenario: Package downloaded and checksum matches

- **WHEN** a package is downloaded from `packageUrl` and its SHA-256 hash matches the `sha256` field from the feed index
- **THEN** the download result includes the temporary file path
- **AND** Success is true

#### Scenario: Checksum mismatch

- **WHEN** a package is downloaded and its SHA-256 hash does not match the expected checksum
- **THEN** the temporary file is deleted
- **AND** the download result includes an error "Package checksum mismatch. The downloaded file may be corrupted or tampered with."
- **AND** Success is false

#### Scenario: Package exceeds size limit

- **WHEN** a package download exceeds 10 MB
- **THEN** the download is aborted
- **AND** the download result includes an error "Package too large"
- **AND** Success is false

#### Scenario: Package URL is HTTP

- **WHEN** the `packageUrl` from the feed index has `http://` scheme (not HTTPS)
- **THEN** the download is rejected before sending the request
- **AND** the download result includes an error "Package URL must use HTTPS"
- **AND** Success is false

#### Scenario: Temporary file cleanup on failure

- **WHEN** a package download fails for any reason (network error, checksum mismatch, exception)
- **THEN** the temporary file is deleted if it was created
- **AND** no temp file is left on disk

### Requirement: Display available plugins with status

The system SHALL display plugins from the feed with their installation status: Available (not installed), Installed (installed version matches feed version), Update Available (feed version is newer), or Different Version Available (feed version differs but is not provably newer).

#### Scenario: Plugin not installed

- **WHEN** a plugin in the feed has no matching entry in `plugin-state.json`
- **THEN** the plugin is displayed with status "Available"
- **AND** an Install button is shown

#### Scenario: Installed plugin matches feed version

- **WHEN** a plugin in the feed has a matching entry in `plugin-state.json` with `installedVersion` equal to the feed `version`
- **THEN** the plugin is displayed with status "Installed"
- **AND** no Install or Update button is shown (or Install button is disabled)

#### Scenario: Installed plugin older than feed version (numeric comparison)

- **WHEN** a plugin in the feed has a matching entry in `plugin-state.json` with `installedVersion` "1.0.0"
- **AND** the feed `version` is "1.2.0"
- **AND** both versions parse as major.minor.patch
- **THEN** the plugin is displayed with status "Update Available"
- **AND** both the installed version and feed version are shown ("Installed: 1.0.0 → Available: 1.2.0")
- **AND** an Update button is shown

#### Scenario: Installed plugin newer than feed version (downgrade case)

- **WHEN** a plugin in the feed has a matching entry in `plugin-state.json` with `installedVersion` "2.0.0"
- **AND** the feed `version` is "1.0.0"
- **AND** both versions parse as major.minor.patch
- **THEN** the plugin is displayed with status "Different Version Available"
- **AND** both versions are shown ("Installed: 2.0.0, Available: 1.0.0")
- **AND** an Install button (not Update button) is shown with warning text "This is a different version (may be older)"

#### Scenario: Installed and feed versions differ but are not comparable (non-numeric)

- **WHEN** a plugin in the feed has a matching entry in `plugin-state.json` with `installedVersion` that does not parse as major.minor.patch
- **AND** the feed `version` also does not parse as major.minor.patch (or either fails to parse)
- **AND** the versions differ as strings
- **THEN** the plugin is displayed with status "Different Version Available"
- **AND** both versions are shown
- **AND** an Install button is shown (not Update button)

#### Scenario: Installed plugin has unknown version

- **WHEN** a plugin in the feed has a matching entry in `plugin-state.json` with `installedVersion` null
- **THEN** the plugin is displayed with status "Update Available"
- **AND** the feed version is shown as available

### Requirement: Install plugin from feed

The system SHALL download a plugin package from the feed, verify its checksum, show a confirmation dialog with all declared commands, and install the plugin if confirmed.

#### Scenario: Install from feed succeeds

- **WHEN** a user selects an available plugin and clicks Install
- **THEN** the package is downloaded and its checksum is verified
- **AND** a confirmation dialog is shown with plugin name, version, publisher, homepage, and all declared commands (launch, resume, version, update) with their executables and arguments
- **AND** if the user confirms, the plugin is installed with origin = "installed"
- **AND** the temporary package file is deleted after installation
- **AND** the plugin appears in the CLI Agents list with "Installed" badge

#### Scenario: Checksum mismatch during install

- **WHEN** a user selects an available plugin and clicks Install
- **AND** the downloaded package checksum does not match the feed `sha256`
- **THEN** an error dialog is shown: "Package checksum mismatch. The downloaded file may be corrupted or tampered with. Installation cancelled."
- **AND** the temporary file is deleted
- **AND** no installation occurs

#### Scenario: Install cancelled by user

- **WHEN** a user selects an available plugin, clicks Install, and views the confirmation dialog
- **AND** the user clicks Cancel
- **THEN** no installation occurs
- **AND** the temporary package file is deleted

### Requirement: Update plugin from feed

The system SHALL download an updated plugin package from the feed, verify its checksum, show a confirmation dialog, and replace the existing installed plugin if confirmed.

#### Scenario: Update from feed succeeds

- **WHEN** a user selects a plugin with "Update Available" status and clicks Update
- **THEN** the package is downloaded and its checksum is verified
- **AND** a confirmation dialog is shown with the new version metadata and all declared commands
- **AND** if the user confirms, the plugin is installed with `replaceExisting=true`
- **AND** the temporary package file is deleted after installation
- **AND** the plugin's `installedVersion` in `plugin-state.json` is updated to the feed version

#### Scenario: Update cancelled by user

- **WHEN** a user selects a plugin with "Update Available" status, clicks Update, and views the confirmation dialog
- **AND** the user clicks Cancel
- **THEN** no installation or replacement occurs
- **AND** the temporary package file is deleted

### Requirement: Minimum app version gating

The system SHALL prevent installation of plugins whose `minAppVersion` is greater than the current CLIHub version, showing a message and disabling the Install button.

#### Scenario: Current CLIHub version satisfies minAppVersion

- **WHEN** a plugin in the feed has `minAppVersion` "1.1.0"
- **AND** the current CLIHub version is "1.2.0"
- **THEN** the plugin can be installed
- **AND** the Install button is enabled

#### Scenario: Current CLIHub version too old

- **WHEN** a plugin in the feed has `minAppVersion` "2.0.0"
- **AND** the current CLIHub version is "1.5.0"
- **THEN** a message is shown: "Requires CLIHub 2.0.0 or newer. Update CLIHub to install this plugin."
- **AND** the Install button is disabled

#### Scenario: Version comparison uses major.minor.patch

- **WHEN** comparing current CLIHub version with `minAppVersion`
- **THEN** the comparison uses numeric major, minor, and patch segments (e.g., "1.10.0" > "1.9.5")
- **AND** major version takes precedence over minor and patch

### Requirement: Refresh feed on demand

The system SHALL provide a Refresh command that re-fetches the feed index and updates the displayed plugin list.

#### Scenario: Refresh updates plugin list

- **WHEN** the user clicks the Refresh button in the Browse Plugins dialog
- **THEN** the feed index is re-fetched from the configured URL
- **AND** the plugin list is updated with the latest feed data
- **AND** status badges (Available/Installed/Update Available/Different Version Available) are recalculated

#### Scenario: Refresh shows new plugins

- **WHEN** the feed index is updated with new plugins since the last fetch
- **AND** the user clicks Refresh
- **THEN** the new plugins appear in the list with status "Available"

#### Scenario: Refresh shows updated versions

- **WHEN** the feed index is updated with new versions of existing plugins since the last fetch
- **AND** the user clicks Refresh
- **THEN** plugins with new feed versions show status "Update Available"

### Requirement: Feed URL configuration

The system SHALL allow users to configure the feed URL via a preference in `config.json`, with a default official URL if not specified.

#### Scenario: Feed URL configuration absent or null

- **WHEN** the `PluginFeedUrl` field in `config.json` is absent or JSON `null`
- **THEN** the default official feed URL is used: `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`

#### Scenario: Custom feed URL used

- **WHEN** the `PluginFeedUrl` field in `config.json` is set to a non-empty custom HTTPS URL
- **THEN** that custom URL is used for feed fetching
- **AND** the custom URL must be HTTPS (enforced by HTTPS-only rule)

#### Scenario: Feed disabled with empty string

- **WHEN** the `PluginFeedUrl` field in `config.json` is set to an explicit empty string `""`
- **THEN** the Browse Plugins button is grayed out or hidden
- **AND** no feed fetching occurs

### Requirement: Offline degradation

The system SHALL handle network failures and unreachable feeds gracefully, showing an error message with a Retry button, and never crashing or throwing unhandled exceptions.

#### Scenario: Offline error message shown

- **WHEN** the network is unavailable or the feed URL is unreachable
- **AND** the user opens the Browse Plugins dialog
- **THEN** an error message is shown: "Could not load plugin feed. Check your network connection."
- **AND** a Retry button is shown

#### Scenario: Retry after offline

- **WHEN** the Browse Plugins dialog shows an offline error
- **AND** the user clicks Retry
- **THEN** the feed fetch is reattempted
- **AND** the plugin list is populated if the network is now available

#### Scenario: No unhandled exceptions

- **WHEN** any network error occurs during feed fetch or package download
- **THEN** the error is caught and returned as a structured error in the result
- **AND** no unhandled exception is thrown

### Requirement: Built-in plugins excluded from feed

The system SHALL not list built-in plugins (origin = "builtin") in the feed, and SHALL reject feed entries whose plugin `id` matches a reserved built-in ID at install time, regardless of the built-in's disabled state.

#### Scenario: Built-in ID in feed rejected during install

- **WHEN** a feed entry has a plugin `id` that matches a built-in plugin embedded in the current CLIHub build
- **AND** the user attempts to install that plugin from the feed
- **THEN** the installation is rejected with an error "Plugin ID is reserved for built-in plugins"
- **AND** no files are written

#### Scenario: Feed lists only non-built-in plugins

- **WHEN** the feed is curated by maintainers
- **THEN** the feed does not include plugin IDs that match any built-in plugin embedded in CLIHub releases (the set defined by `EmbeddedPluginResources.BuiltinPluginIds`)

#### Scenario: Feed entry with built-in ID is omitted from browse list

- **WHEN** the feed index is loaded
- **AND** an entry's `id` matches a reserved built-in ID
- **THEN** that entry is omitted from the Browse Plugins list or marked as unavailable
- **AND** no Install button is shown for that entry

### Requirement: Confirmation dialog shows all declared commands

The system SHALL display every declared command (launch, resume, version, update) with its exact executable and arguments in the confirmation dialog before installation, so users can inspect all execution paths.

#### Scenario: All commands shown in confirmation

- **WHEN** a user is shown the confirmation dialog for a plugin installation from the feed
- **THEN** the dialog lists every declared command kind (launch, resume, version, update) present in the plugin descriptor
- **AND** for each command, the exact executable and arguments are shown
- **AND** undeclared command kinds are omitted from the list

**Security rationale:** All command kinds execute arbitrary binaries specified in the descriptor. A package could hide its payload in `resume`, `version`, or `update` while showing a harmless `launch` command. Listing every declared command ensures the user can inspect all execution paths before approving installation.

### Requirement: Temporary file cleanup

The system SHALL delete temporary package files after installation completes (success or failure) or if an exception occurs during the workflow.

#### Scenario: Temp file deleted after successful install

- **WHEN** a plugin is successfully installed from the feed
- **THEN** the temporary package file is deleted
- **AND** no `clihub-plugin-*.zip` file remains in the temp directory

#### Scenario: Temp file deleted after failed install

- **WHEN** a plugin installation fails (checksum mismatch, user cancellation, validation error)
- **THEN** the temporary package file is deleted
- **AND** no `clihub-plugin-*.zip` file remains in the temp directory

#### Scenario: Temp file deleted on exception

- **WHEN** an exception occurs during download or installation
- **THEN** the temporary package file is deleted in a `finally` block
- **AND** no temp file is leaked on disk
