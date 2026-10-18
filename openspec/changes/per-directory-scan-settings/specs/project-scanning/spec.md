## ADDED Requirements

### Requirement: Per-directory scan settings

The system SHALL scan each watched root with its own scan depth and discovery mode when the stored preferences provide them, and SHALL fall back to the global scan depth and discovery mode when a root has no stored entry. Ignored patterns SHALL remain global.

#### Scenario: Full scan applies each root's settings

- **WHEN** a full scan runs over several watched roots
- **THEN** each root is walked with its own stored scan depth and discovery mode
- **AND** ignored patterns apply to every root

#### Scenario: Single-root rescan uses the root's settings

- **WHEN** a single-root rescan is requested for a root with stored settings
- **THEN** that root is walked with its own scan depth and discovery mode

#### Scenario: Root without stored settings uses the defaults

- **WHEN** a watched root has no stored per-root settings
- **THEN** it is scanned with the global scan depth and discovery mode

#### Scenario: Changing a row does not change other roots

- **WHEN** the scan depth or discovery mode of one watched root changes
- **THEN** the other roots keep their own settings

#### Scenario: Removed root settings are not kept

- **WHEN** a watched root is removed and the preferences are saved
- **THEN** the stored settings for that root are removed as well
