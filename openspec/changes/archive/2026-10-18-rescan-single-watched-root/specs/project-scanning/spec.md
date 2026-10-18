## ADDED Requirements

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
