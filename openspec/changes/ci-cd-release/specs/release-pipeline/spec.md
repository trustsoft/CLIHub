# Spec Delta

## Purpose

Turns a pushed `v*` tag into a published Velopack release on GitHub Releases: tests gate the publish, the version comes from the tag, the package is framework-dependent win-x64 with a runtime bootstrap, and the release carries the version's user-facing notes.

## ADDED Requirements

### Requirement: Tag-triggered release

The release pipeline SHALL run when a tag whose name starts with `v` is pushed and SHALL use the tag name without the leading `v` as the release version. The pipeline SHALL NOT run for tags without the `v` prefix.

#### Scenario: Version tag starts the pipeline
- **WHEN** a tag such as `v1.2.3` is pushed
- **THEN** the release pipeline runs

#### Scenario: Non-version tag is ignored
- **WHEN** a tag without the leading `v` (for example `nightly`) is pushed
- **THEN** the release pipeline does not run

#### Scenario: Version comes from the tag
- **WHEN** the pipeline packages a release from tag `v1.2.3`
- **THEN** the packaged application and the published release both report version `1.2.3`, regardless of the development version in `Directory.Build.props`

### Requirement: Tests gate the release

The release pipeline SHALL run the test suite before producing packages and SHALL NOT publish a release when the build or any test fails.

#### Scenario: Failing tests block the release
- **WHEN** the test suite fails while the pipeline runs for a pushed tag
- **THEN** no packages are produced and no release is published

#### Scenario: Passing tests proceed to packaging
- **WHEN** the solution builds and all tests pass for the tagged commit
- **THEN** the pipeline continues with packaging

### Requirement: Framework-dependent Windows package

The release pipeline SHALL publish the application framework-dependent for `win-x64` and package it so that a machine without the .NET 8 Desktop Runtime is offered the runtime installation.

#### Scenario: Package targets win-x64 without bundling the runtime
- **WHEN** the pipeline produces the package
- **THEN** the package contains the framework-dependent `win-x64` build of the application rather than a self-contained copy of the runtime

#### Scenario: Runtime bootstrap on first install
- **WHEN** the installer runs on a machine without the .NET 8 Desktop Runtime
- **THEN** the installer offers to install the required runtime before continuing

### Requirement: Release notes accompany the release

The release pipeline SHALL extract the released version's section from `RELEASE-NOTES.md`, use it as the GitHub release body and pass it to the updater as the release notes, and SHALL fail without publishing when the section for the release version is missing.

#### Scenario: Notes section exists
- **WHEN** `RELEASE-NOTES.md` contains a section for the release version and the pipeline publishes the release
- **THEN** the GitHub release body and the updater's release notes contain that section's content

#### Scenario: Notes section missing
- **WHEN** `RELEASE-NOTES.md` has no section for the release version
- **THEN** the pipeline fails before packaging and no release is published

### Requirement: Publish to GitHub Releases

The release pipeline SHALL publish the produced packages as a published, non-draft release on the pushed tag of the CLIHub GitHub repository, and re-running the pipeline for an existing tag SHALL update the existing release instead of failing.

#### Scenario: Release published on the tag
- **WHEN** the pipeline completes for a pushed tag
- **THEN** the repository has a published release on that tag containing the installer, the portable package, and the update metadata

#### Scenario: Pipeline re-run for an existing tag
- **WHEN** the pipeline runs again for a tag whose release already exists
- **THEN** the existing release is updated with the new packages instead of the pipeline failing
