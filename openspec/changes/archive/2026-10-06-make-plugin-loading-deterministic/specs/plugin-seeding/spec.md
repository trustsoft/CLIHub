## ADDED Requirements

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
