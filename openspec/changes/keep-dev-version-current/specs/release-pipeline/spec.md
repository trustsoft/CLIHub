## ADDED Requirements

### Requirement: Release gate ensures current development version

The release pipeline's test job SHALL check that the development version in `Directory.Build.props` is equal to or ahead of the highest stable release tag before running tests, preventing releases from commits that carry a stale development version.

#### Scenario: Release tag from a commit with current development version
- **GIVEN** a commit is tagged for release and its `Directory.Build.props` version is equal to or ahead of the highest stable release tag
- **WHEN** the release pipeline runs the test job
- **THEN** the development version check passes and tests proceed

#### Scenario: Release tag from a commit with stale development version
- **GIVEN** a commit is tagged for release but its `Directory.Build.props` version is older than the highest stable release tag
- **WHEN** the release pipeline runs the test job
- **THEN** the development version check fails and no packages are produced
