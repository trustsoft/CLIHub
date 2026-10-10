## ADDED Requirements

### Requirement: Project scanning preferences default safely and round-trip

The configuration boundary SHALL default the project-scanning preferences when they are absent from a stored document and SHALL round-trip them through the single `config.json` document: `watchedDirectories` default empty, `scanDepth` default `2`, `ignoredPatterns` default the built-in set when absent, and `discoveryMode` default `both`. Adding these fields SHALL NOT change the numeric `schemaVersion`.

#### Scenario: Absent fields use defaults
- **GIVEN** a stored configuration has no project-scanning preference fields
- **WHEN** the configuration is loaded
- **THEN** the watched-directories list is empty, the scan depth is 2, the ignored patterns are the built-in set, and the discovery mode is `both`

#### Scenario: Missing or unknown discovery mode uses both
- **GIVEN** a stored configuration has no discovery mode or an unrecognized token
- **WHEN** the configuration is loaded
- **THEN** the discovery mode resolves to `both` and the application starts normally

#### Scenario: Scanning preferences round-trip
- **GIVEN** the project-scanning preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value

#### Scenario: Explicit empty pattern list is preserved
- **GIVEN** the ignored patterns are set to an explicit empty list
- **WHEN** the configuration is persisted and loaded again
- **THEN** the ignored patterns remain empty rather than reverting to the built-in set

#### Scenario: Out-of-range scan depth is normalized
- **GIVEN** a stored configuration has a scan depth below or above the allowed range
- **WHEN** the configuration is loaded
- **THEN** the built-in default scan depth is used and the application starts normally

#### Scenario: Schema version is unchanged
- **GIVEN** a configuration is persisted after project-scanning preferences are added
- **WHEN** the resulting `config.json` is read
- **THEN** it contains the same `schemaVersion` as before the scanning fields were added

### Requirement: Project provenance defaults safely and round-trips

The configuration boundary SHALL persist each project's provenance (`source`: `manual` or `discovered`) and SHALL treat a missing or unrecognized value as `manual`. Adding this field SHALL NOT change the numeric `schemaVersion`.

#### Scenario: Missing provenance is manual
- **GIVEN** a stored project has no provenance field
- **WHEN** the configuration is loaded
- **THEN** the project is treated as manually registered

#### Scenario: Provenance round-trips
- **GIVEN** a project is marked discovered
- **WHEN** the configuration is persisted and loaded again
- **THEN** the project is still marked discovered
