# ci-build Specification

## Purpose
Builds and tests every pull request and every push to `master` automatically on Windows, so regressions are caught before merge.

## Requirements

### Requirement: Build and test on pull requests

The CI pipeline SHALL build the solution in Release configuration and run the test suite on Windows for every pull request, and the check SHALL fail when the build or any test fails.

#### Scenario: Green pull request
- **WHEN** a pull request is opened or updated and the solution builds and all tests pass
- **THEN** the CI check succeeds

#### Scenario: Failing test on a pull request
- **WHEN** a pull request contains a change that breaks a test
- **THEN** the CI check fails and reports the failing test

#### Scenario: Build failure on a pull request
- **WHEN** a pull request does not build in Release configuration
- **THEN** the CI check fails

### Requirement: Build and test on master

The CI pipeline SHALL build the solution in Release configuration and run the test suite on Windows for every push to `master`.

#### Scenario: Green push to master
- **WHEN** a commit is pushed to `master` and the solution builds and all tests pass
- **THEN** the CI run succeeds

#### Scenario: Broken push to master
- **WHEN** a commit pushed to `master` fails to build or breaks a test
- **THEN** the CI run fails
