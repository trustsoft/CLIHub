## MODIFIED Requirements

### Requirement: Background watcher with status

The system SHALL watch the configured roots while at least one watched directory exists and give an observable status that reports the scan coverage of the last pass.

#### Scenario: Change triggers a re-scan

- **WHEN** a directory is created, renamed, or removed under a watched root
- **THEN** a debounced re-scan runs and discovers or reflects the change

#### Scenario: Debounce coalesces bursts

- **WHEN** many file-system changes occur within the debounce interval
- **THEN** only one re-scan runs after the changes settle

#### Scenario: Status is observable

- **WHEN** the watcher is active
- **THEN** a status surface reports whether a scan is running, the last scan time, the number of discovered projects, and the number of folders the last scan examined across the watched roots

#### Scenario: Single-root rescan refreshes only its coverage

- **WHEN** one watched root is rescanned
- **THEN** the folder count of that root reflects the new pass
- **AND** the folder counts of the other watched roots are unchanged

#### Scenario: No watched directories means no watching

- **WHEN** the watched-directories list is empty
- **THEN** no watcher runs, the status reports that no watched directories are configured, and the examined-folder count is zero
