# Phase 14: Agent Process Monitoring

**Status:** Completed  
**Date:** 2026-10-09

## Goal

Prevent an agent's self-update command from running while a matching agent process is active.

## Delivered

- `AgentProcessInstance` and `RunningAgent` models record process and plugin matches.
- `IAgentProcessInspector` and `WindowsAgentProcessInspector` enumerate accessible Windows processes.
- `AgentProcessMatcher` matches direct executable names and supports runtime-process matching when command
  line data is available.
- `AgentProcessMonitor` exposes matching running agents and handles inspection failures without taking
  down the application.
- `WindowActionCoordinator` checks the monitor both when the Update command's availability is evaluated
  and immediately before execution.
- `StatusMessageCoordinator` reports why the update action is blocked.
- DI registration and Core/application tests cover the new process-monitoring path.

## Current Limitation

The Windows inspector currently leaves command-line capture empty because the WMI query is disabled to
avoid slow or hanging process enumeration. Direct executable matching is therefore the reliable production
path; runtime-based matching remains supported by the matcher and test doubles.

## Verification

The full solution test run passes with 609 tests: 425 Core tests and 184 WPF/application tests.

## Related Documentation

- [Process execution and agent monitoring](architecture/processes.md)
- [Architecture overview](architecture.md)
- [Testing infrastructure](phase15-testing-infrastructure.md)
