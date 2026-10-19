# agent-version (partial): scoped on-demand re-probe

## MODIFIED Requirements

### Requirement: On-demand version re-probe

The system SHALL re-probe an agent's version on demand, bypassing the cached value and its time-to-live, and report the freshly probed result. The re-probe SHALL invalidate only the requested agent's cached version; cached versions of other agents SHALL remain valid and SHALL be reused without running their commands.

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

#### Scenario: Re-probe invalidates only the requested agent
- **WHEN** a manual version probe is requested for an agent
- **THEN** the requested agent's cached version is invalidated and its version command runs again
- **AND** cached versions of other agents remain valid

#### Scenario: Unrelated agents stay cache-served
- **WHEN** versions of other agents were resolved within their time-to-live and one agent is re-probed
- **THEN** the other agents' version commands do not run again for their next lookup
