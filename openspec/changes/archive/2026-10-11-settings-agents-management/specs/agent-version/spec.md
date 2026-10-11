## ADDED Requirements

### Requirement: On-demand version re-probe

The system SHALL re-probe an agent's version on demand, bypassing the cached value and its time-to-live, and report the freshly probed result.

#### Scenario: Manual re-probe bypasses the cache
- **WHEN** a manual version probe is requested for an agent whose version is cached
- **THEN** the version command runs again instead of returning the cached value

#### Scenario: Fresh version reported
- **WHEN** a manual re-probe succeeds
- **THEN** the freshly probed version is reported

#### Scenario: Unknown version reported
- **WHEN** a manual re-probe cannot produce a version
- **THEN** the version is reported as unknown

#### Scenario: Re-probe has no project requirement
- **WHEN** a manual version probe is requested and no project is selected
- **THEN** the version is still probed
