## Why

`CLIHub.Core` currently contains domain logic, application coordination, persistence, process execution, and Windows-specific infrastructure in one project with a largely flat service layout. The implementation is functional, but the boundaries between agents, projects, plugins, configuration, and platform integrations are difficult to discover, which increases the risk of accidental coupling as new functionality is added.

This change establishes an explicit architectural direction and a controlled migration plan so that future Core work remains discoverable, replaceable, and aligned with the agreed responsibility boundaries.

## What Changes

- Define the responsibility boundaries for the Core subsystems: agents, projects, plugins, configuration, updates, and infrastructure.
- Organize Core code so its directory structure reflects those subsystem boundaries.
- Separate application policies from infrastructure implementations where the current services mix both concerns.
- Reduce hidden coupling through the shared mutable `AppConfig` object by introducing narrower access boundaries incrementally.
- Clarify process execution boundaries between interactive agent launches and captured command output.
- Make plugin catalog responsibilities distinct from agent behavior and availability policies.
- Group dependency injection registration by subsystem so the composition root documents the architecture.
- Establish dependency rules, review checkpoints, and deviation records for the migration.
- Preserve externally observable application behavior and existing public service contracts unless a later decision explicitly approves a breaking change.

## Capabilities

### New Capabilities

None. This is an internal architecture and maintainability change; it does not add or alter user-visible behavior.

### Modified Capabilities

None. Existing capability requirements remain unchanged.

## Impact

- Affects `src/CLIHub.Core`, especially `Services/`, `Interfaces/`, `Models/`, and DI registration.
- Affects Core unit tests when types move or responsibilities are split; test behavior and coverage must be preserved.
- May require namespace, constructor, and internal API changes during implementation, while public contracts should remain stable by default.
- Does not require new runtime dependencies or changes to the WPF project behavior.
- The architecture records created by this change become the reference for subsequent Core refactoring work and code review.
