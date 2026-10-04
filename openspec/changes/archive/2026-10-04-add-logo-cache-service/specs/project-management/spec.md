## MODIFIED Requirements

### Requirement: Project logo detection

The system SHALL resolve a project logo from well-known image filenames in the project folder, falling back to a default. Resolution SHALL go through the persistent logo cache keyed by the project ID, so repeated lookups and restarts reuse resolved values without rescanning.

#### Scenario: Logo file found
- **WHEN** a project folder contains an image matching a known filename (for example `logo.png`, `icon.png`)
- **THEN** the first matching file is used as the project logo and stored in the cache under the project's ID

#### Scenario: No logo file found
- **WHEN** a project folder contains no known logo filename
- **THEN** the default project logo is used and the negative outcome is cached under the project's ID

#### Scenario: Cached resolution is reused
- **WHEN** a project logo is requested again for a project whose resolution is already cached
- **THEN** the cached value is returned without scanning the project folder again

#### Scenario: Resolution persists across restarts
- **WHEN** the application restarts
- **THEN** previously resolved project logos are reused from the cache state file without rescanning

#### Scenario: Manual refresh re-resolves
- **WHEN** the user triggers a refresh in the launch window
- **THEN** cached project logo entries are cleared and project logos are re-resolved from the project folders
