## MODIFIED Requirements

### Requirement: Concurrent version probes are coalesced

The system SHALL ensure that concurrent requests for the same agent version share one in-flight probe rather than starting duplicate processes.

#### Scenario: Concurrent cache miss

- **WHEN** multiple callers request the same uncached agent version concurrently
- **THEN** one version command runs and all callers receive the same completed result

#### Scenario: Different agents probe independently

- **WHEN** callers request versions for different agents concurrently
- **THEN** each agent may run its own version command without being blocked by another agent's probe

#### Scenario: No eviction of newer in-flight probe

- **WHEN** a version probe completes and its in-flight entry is retired
- **AND** a newer probe for the same agent is started before the first probe's continuation runs
- **THEN** the delayed continuation of the first probe SHALL NOT evict the newer probe's in-flight entry
- **AND** concurrent callers for the same agent continue to share the newer probe rather than starting a duplicate