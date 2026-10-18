# project-scanning Specification

## Purpose
Discovers repository directories under a user-configured set of watched root folders, keeps CLIHub's project list in sync with what is found, detects which AI agents are used in each discovered repository, and exposes a background watcher with an observable status.

## Requirements

### Requirement: Watched directories configuration

The system SHALL let users manage a list of watched root directories used for repository scanning.

#### Scenario: Add a watched directory
- **WHEN** a user adds an existing folder as a watched directory
- **THEN** the folder is stored in the watched-directories list

#### Scenario: Reject a non-existent directory
- **WHEN** a user adds a folder path that does not exist
- **THEN** the folder is not added and the user receives an error

#### Scenario: Reject a duplicate directory
- **WHEN** a user adds a folder already present in the watched-directories list (by normalized path)
- **THEN** no duplicate entry is stored and the existing entry is kept

#### Scenario: Remove a watched directory
- **WHEN** a user removes a watched directory
- **THEN** it is removed from the list and is no longer scanned or watched

### Requirement: Bounded repository scanning

The system SHALL scan each watched root directory recursively to a configured scan depth, skipping ignored directory patterns and never descending into a `.git` directory.

#### Scenario: Depth bounds the walk
- **WHEN** a watched root is scanned with a configured scan depth
- **THEN** directories deeper than the configured depth below the root are not visited

#### Scenario: Ignored patterns are skipped
- **WHEN** a directory name matches a configured ignored pattern
- **THEN** that directory and its descendants are not visited

#### Scenario: Git metadata is never descended
- **WHEN** a directory contains a `.git` entry
- **THEN** `.git` is not scanned as a child directory

#### Scenario: Missing root is skipped
- **WHEN** a watched root no longer exists or cannot be read
- **THEN** it is skipped with a warning and the remaining roots are still scanned

### Requirement: Repository discovery mode

The system SHALL discover a directory as a repository according to a configured discovery mode: `git` (a `.git` entry is present), `agents` (at least one plugin project indicator is present), or `both` (either condition holds). OpenSpec SHALL NOT be treated as an agent marker in any mode.

#### Scenario: Git mode discovers git repositories
- **WHEN** the discovery mode is `git` and a directory within a watched root contains a `.git` entry
- **THEN** that directory is discovered as a repository

#### Scenario: Git mode ignores agent-only folders
- **WHEN** the discovery mode is `git` and a directory contains a plugin project indicator but no `.git` entry
- **THEN** that directory is not discovered

#### Scenario: Agents mode discovers agent-marked folders
- **WHEN** the discovery mode is `agents` and a directory contains a plugin project indicator (for example `.claude` or `.opencode`)
- **THEN** that directory is discovered as a repository

#### Scenario: Agents mode ignores plain git repositories
- **WHEN** the discovery mode is `agents` and a directory contains a `.git` entry but no plugin project indicator
- **THEN** that directory is not discovered

#### Scenario: Both mode discovers either condition
- **WHEN** the discovery mode is `both` and a directory contains a `.git` entry or a plugin project indicator
- **THEN** that directory is discovered as a repository

#### Scenario: OpenSpec is not an agent marker
- **WHEN** a directory contains an `openspec` folder but no `.git` entry and no plugin project indicator
- **THEN** that directory is not discovered in any mode

### Requirement: Discovered repositories are auto-registered

The system SHALL register each discovered repository as a project, deduplicated by normalized path, without altering projects that already exist.

#### Scenario: New repository becomes a project
- **WHEN** a repository is discovered whose normalized path is not among the registered projects
- **THEN** a project is registered for it with a name derived from the folder name

#### Scenario: Existing project is not duplicated
- **WHEN** a repository is discovered whose normalized path is already registered
- **THEN** no new project is created

#### Scenario: Manual project is not reclassified
- **WHEN** a repository is discovered whose normalized path was registered manually
- **THEN** the existing project is kept as a manual project

#### Scenario: Registration persists
- **WHEN** repositories have been discovered and the application restarts
- **THEN** the discovered projects are still registered

### Requirement: Background watcher with status

The system SHALL watch the configured roots while at least one watched directory exists and give an observable status.

#### Scenario: Change triggers a re-scan
- **WHEN** a directory is created, renamed, or removed under a watched root
- **THEN** a debounced re-scan runs and discovers or reflects the change

#### Scenario: Debounce coalesces bursts
- **WHEN** many file-system changes occur within the debounce interval
- **THEN** only one re-scan runs after the changes settle

#### Scenario: Status is observable
- **WHEN** the watcher is active
- **THEN** a status surface reports whether a scan is running, the last scan time, and the number of discovered projects

#### Scenario: No watched directories means no watching
- **WHEN** the watched-directories list is empty
- **THEN** no watcher runs and the status reports that no watched directories are configured

### Requirement: Removing a watched root removes its discovered projects

The system SHALL remove projects discovered under a removed watched root unless those projects also fall under another watched root or were registered manually.

#### Scenario: Discovered projects under the removed root are removed
- **WHEN** a watched root is removed
- **THEN** each project discovered under it is removed if it is not under another watched root and was not registered manually

#### Scenario: Manual projects are kept
- **WHEN** a watched root is removed and a project under it was registered manually
- **THEN** that project is kept

#### Scenario: Projects under another root are kept
- **WHEN** a watched root is removed and a discovered project under it is also discoverable under another watched root
- **THEN** that project is kept

#### Scenario: Removal does not delete files
- **WHEN** discovered projects are removed because a watched root was removed
- **THEN** the folders and their contents on disk are left untouched

### Requirement: Detection requires no execution

The system SHALL perform discovery and agent-marker detection using only file-system checks.

#### Scenario: Discovery does not run agents
- **WHEN** repositories and agent markers are detected during a scan
- **THEN** no agent process is started

### Requirement: Single-root rescan

The system SHALL rescan one watched root on request without rescanning the other watched roots. The pass SHALL use the stored scanning settings and SHALL reconcile only the projects beneath the requested root.

#### Scenario: Only the requested root is enumerated

- **WHEN** a rescan is requested for one watched root
- **THEN** only that root is walked to the stored scan depth with the stored ignored patterns and discovery mode
- **AND** the other watched roots are not enumerated during that pass

#### Scenario: Vanished projects under the root are removed

- **WHEN** a rescan of a root no longer finds a discovered project that previously existed beneath it
- **THEN** that project is removed from the project list

#### Scenario: Projects under other roots are untouched

- **WHEN** a root is rescanned
- **THEN** discovered projects beneath the other watched roots are neither added nor removed by that pass

#### Scenario: Project under another watched root is kept

- **WHEN** a root is rescanned and a discovered project beneath it is also beneath another watched root but is not rediscovered
- **THEN** that project is kept because the other root was not part of the pass

#### Scenario: Manual projects are kept

- **WHEN** a root is rescanned and a project beneath it was registered manually
- **THEN** that project is kept even when it is not rediscovered

#### Scenario: Missing root removes nothing

- **WHEN** the requested root no longer exists or cannot be read
- **THEN** the pass discovers nothing and removes no projects

#### Scenario: Status reports the scoped pass

- **WHEN** a single-root rescan runs
- **THEN** the scanner status reports scanning while it runs and the last scan time and total discovered-project count afterwards

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
