# ci-build Specification

## Purpose
Builds and tests every pull request and every push to `master` automatically on Windows, so regressions are caught before merge.

## Requirements

### Requirement: Build and test on pull requests

The CI pipeline SHALL use the repository's stable .NET 10 SDK policy, build the solution in Release configuration with warnings treated as errors, and run the test suite with coverage collection on Windows for every pull request. The check SHALL fail when the build, any compiler or analyzer warning, or any test fails, and SHALL publish the collected coverage regardless of the test outcome.

#### Scenario: Green pull request

- **WHEN** a pull request is opened or updated and the solution builds with the .NET 10 toolchain with no warnings and all tests pass
- **THEN** the CI check succeeds
- **AND** the CI run publishes a coverage artifact

#### Scenario: Failing test on a pull request

- **WHEN** a pull request contains a change that breaks a test
- **THEN** the CI check fails and reports the failing test
- **AND** the CI run still publishes the coverage collected up to the failure

#### Scenario: Build failure on a pull request

- **WHEN** a pull request does not build in Release configuration for the repository's .NET 10 target frameworks
- **THEN** the CI check fails

#### Scenario: Warning on a pull request

- **WHEN** a pull request builds in Release configuration but the build emits a compiler or analyzer warning
- **THEN** the CI check fails because the build treats warnings as errors

### Requirement: Build and test on master

The CI pipeline SHALL use the repository's stable .NET 10 SDK policy, build the solution in Release configuration with warnings treated as errors, and run the test suite with coverage collection on Windows for every push to `master`. The check SHALL fail when the build, any compiler or analyzer warning, or any test fails, and SHALL publish the collected coverage regardless of the test outcome.

#### Scenario: Green push to master

- **WHEN** a commit is pushed to `master` and the solution builds with the .NET 10 toolchain with no warnings and all tests pass
- **THEN** the CI run succeeds
- **AND** the CI run publishes a coverage artifact

#### Scenario: Broken push to master

- **WHEN** a commit pushed to `master` fails to build, emits a compiler or analyzer warning, or breaks a test
- **THEN** the CI run fails
- **AND** the CI run still publishes the coverage collected up to the failure

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
