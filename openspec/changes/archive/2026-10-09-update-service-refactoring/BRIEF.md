# Update Service Refactoring

## Goal

Extract UpdateService internal implementation into focused components while maintaining all existing public interfaces unchanged.

## Background

UpdateService currently implements five interfaces (IUpdateVersionProvider, IUpdateChecker, IUpdateStateSource, IUpdateDownloader, IUpdateInstaller) and mixes multiple concerns in a single 317-line class:
- Velopack manager initialization and lifecycle
- Version detection (assembly attributes vs. Velopack)
- Update checking with timeout handling
- Download orchestration with state management
- Update installation

While the public interfaces are already well-separated and consumed correctly by UpdateWorkflow and ViewModels, the internal implementation remains monolithic and harder to test in isolation.

## What We're Building

Refactor UpdateService into a thin coordination layer that delegates to focused internal components:

1. **VelopackManagerProvider** - Manages Velopack UpdateManager lifecycle, initialization, and source configuration
2. **UpdateChecker** - Handles update checking logic with timeout and error handling
3. **UpdateDownloader** - Manages download state, coordination, and event notification
4. **UpdateInstaller** - Applies downloaded updates and triggers restart

UpdateService becomes a facade that:
- Implements all existing public interfaces unchanged
- Delegates to internal components
- Maintains backward compatibility with all consumers

## Success Criteria

- ✅ All 14 existing UpdateService tests pass without modification
- ✅ All 555 project tests pass
- ✅ UpdateService reduced from ~317 lines to ~150 lines
- ✅ Four new focused components with clear single responsibilities
- ✅ Minimum 12 new unit tests for internal components
- ✅ No changes to public interfaces or consumers (UpdateWorkflow, ViewModels, DI registration)
- ✅ Test seam (CreateForTesting) preserved for existing tests

## Constraints

- **No breaking changes** - All public interfaces, DI registration, and consumers remain unchanged
- **Preserve existing tests** - All 14 UpdateService tests must pass without modification
- **Maintain thread safety** - Download state synchronization must work correctly
- **Keep Velopack abstraction** - Manager initialization logic stays internal to Core

## Non-Goals

- Not changing the public API surface (IUpdateService, IUpdateChecker, etc.)
- Not modifying UpdateWorkflow or any consumers
- Not adding new features or changing update behavior
- Not replacing Velopack with a different update provider
