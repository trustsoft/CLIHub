## MODIFIED Requirements

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

#### Scenario: Browse card updates in place after install

- **WHEN** a plugin is successfully installed or updated from the Browse Plugins dialog
- **THEN** that plugin's card updates in place to reflect the installed version: status "Installed", the installed version shown, and no Install/Update action button
- **AND** the feed index is not re-fetched as part of the install
- **AND** changes made to the feed after the dialog was loaded are reflected only when the user clicks Refresh
