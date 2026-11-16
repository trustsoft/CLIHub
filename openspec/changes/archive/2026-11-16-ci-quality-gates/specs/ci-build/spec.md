## MODIFIED Requirements

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