## MODIFIED Requirements

### Requirement: Framework-dependent Windows package

The release pipeline SHALL publish the application framework-dependent for `win-x64` and package it so that a machine without the .NET 10 Desktop Runtime is offered the runtime installation.

#### Scenario: Package targets win-x64 without bundling the runtime
- **WHEN** the pipeline produces the package
- **THEN** the package contains the framework-dependent `win-x64` build of the application targeting .NET 10 rather than a self-contained copy of the runtime

#### Scenario: Runtime bootstrap on first install
- **WHEN** the installer runs on a machine without the .NET 10 Desktop Runtime
- **THEN** the installer offers to install the required runtime before continuing
