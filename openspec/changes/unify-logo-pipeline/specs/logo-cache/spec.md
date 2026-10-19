# logo-cache (partial): scoped invalidation

## MODIFIED Requirements

### Requirement: Cache invalidation on demand

The system SHALL clear cached logo entries on demand, either all entries at once or only the entries whose keys share a given namespace prefix, so the next lookup re-resolves from the file system and updates the cache.

#### Scenario: Full clear re-resolves

- **WHEN** invalidation is requested without a prefix
- **THEN** all cached logo entries are cleared, including negative outcomes, and subsequent lookups re-resolve and update the cache

#### Scenario: Manual refresh re-resolves

- **WHEN** the user triggers a refresh in the launch window (or a plugin reload occurs)
- **THEN** only the agent (`plugin:`) cache entries are cleared and subsequent agent logo lookups re-resolve and update the cache
- **AND** project (`project:`) entries are preserved and project folders are not rescanned by that refresh

#### Scenario: Scoped invalidation is prefix-based

- **WHEN** invalidation is requested for a namespace prefix
- **THEN** exactly the entries whose keys start with that prefix are removed, including cached negative outcomes
- **AND** entries outside the prefix are left untouched

#### Scenario: Invalidation is non-fatal to resolution

- **WHEN** a re-resolve after invalidation finds no logo
- **THEN** the negative outcome is cached again and the default logo is used for display
