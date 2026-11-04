# plugin-seeding Specification (MODIFIED)

## Purpose

Populates the plugins folder with built-in agent descriptors on first run and refreshes them on application updates, while preserving user modifications and respecting disabled state.

## MODIFIED Requirements

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

## ADDED Requirements

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
