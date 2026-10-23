## ADDED Requirements

### Requirement: Development version is current

The CI pipeline SHALL fail when the development version in `Directory.Build.props` is older than the highest stable release tag, ensuring that the version reported by dev builds is never behind the published releases.

#### Scenario: Development version matches or exceeds the highest release tag
- **GIVEN** the development version in `Directory.Build.props` is equal to or ahead of the highest stable release tag
- **WHEN** the CI pipeline runs for a pull request or push to master
- **THEN** the version check passes and the build proceeds

#### Scenario: Development version is behind the highest release tag
- **GIVEN** the development version in `Directory.Build.props` is older than the highest stable release tag
- **WHEN** the CI pipeline runs
- **THEN** the version check fails with an actionable message before the build starts

#### Scenario: Release tags are unavailable
- **GIVEN** the repository checkout does not include tag history (shallow clone)
- **WHEN** the version check attempts to enumerate release tags
- **THEN** the check fails with a clear error indicating full tag history is required
