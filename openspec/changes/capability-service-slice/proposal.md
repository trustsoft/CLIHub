# Capability-based service decomposition - agent probe and update slice

## Why

IAgentManagementService bundles two independent capabilities (probe: fresh installation/version, update: launch the update command) into one interface. AgentManagementViewModel and tests depend on both members regardless of which capability a test exercises, forcing tests that probe installation state to stub UpdateAsync and tests that launch updates to stub ProbeAsync. The Updates subsystem already demonstrates the target pattern: UpdateWorkflow depends on five focused capability interfaces (IUpdateVersionProvider, IUpdateChecker, IUpdateStateSource, IUpdateDownloader, IUpdateInstaller) instead of one aggregate - the pattern that Proposal 3 (architecture-analysis-2026-10-29.md) targets for agents.

## What Changes

Split IAgentManagementService (2 members: ProbeAsync, UpdateAsync) into IAgentProbeService (ProbeAsync) and IAgentUpdateService (UpdateAsync). AgentManagementService keeps one public DI constructor that accepts both capability interfaces plus its existing collaborators (detection, version, availability, commands, processMonitor). AgentManagementViewModel replaces the IAgentManagementService dependency with IAgentProbeService + IAgentUpdateService. DI registration wires both new interfaces to the same AgentManagementService singleton. Every test setup that used one member now depends on one capability; unconfigured-mock tests that exercised neither member remain unconfigured.

## Non-Goals

- Decompose other agent services (detection, version, availability already exist as capability interfaces)
- Split AgentManagementService implementation into separate classes (single composition point preserved per architecture-analysis guidance against speculative abstractions)
- Extract any new interfaces for members of IAgentDetectionService, IAgentVersionService, IAgentUpdateAvailabilityService, IAgentCommandService (reconnaissance found no subset-use evidence there)

## Dependencies

None - isolated internal refactoring, no cross-change coordination needed.
