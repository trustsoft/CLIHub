## Why

Three small fixes from the quick-wins sweep (item 2) complete accessibility and internal-coupling improvements identified during recent verification sessions (2026-11-04 through 2026-11-14). These are latent issues with low immediate impact but high maintainability and consistency benefits: a potential race condition in the domain event bus, a cosmetic tooltip regression, and a type-safety/documentation gap in the agent-management interface.

## What Changes

### 1. Synchronize DomainEventBus subscription lists

- Add a `ReaderWriterLockSlim` to protect concurrent subscribe/unsubscribe and publish-snapshot operations.
- Normal operation is safe today (UI-thread publishers, single-subscription-per-page lifecycle); the change hardens the code against future background publishers (e.g., `PluginFeedSyncService` and `PluginFeedUpdatedEvent`).

### 2. Maximize/restore tooltip — already fixed, dropped

- Re-verified 2026-11-14: `src/CLIHub/Views/WindowHeaderBar.xaml:67-74` already switches the
  tooltip between `Maximize` and `Restore` from a `WindowState` data trigger (the pre-refactor
  `ChromeWindowHeader` template is gone). No implementation work; the backlog entry is closed.

### 3. Add plugin-management members to IAgentManagementSection

- Add `DisabledCards`, `HasDisabledPlugins`, `PluginCatalogSummary`, `EnablePluginCommand`, and `InvalidCards` to the interface.
- The interface-typed `SettingsViewModel.AgentsState` property can then be consumed directly by non-XAML code without reflection or interface narrowing.
- Update the interface documentation to reflect the complete scope.

## Capabilities

### Modified Capabilities

- `plugin-feed-sync`: No behavior change; the synchronization fix simply makes the bus thread-safe for the existing `PluginFeedUpdatedEvent` publish from `PluginFeedSyncService` (which publishes on a background thread).
- `agent-availability-display` / `preferences-ui`: Plugin-management interface now exposes its full surface in the contract, improving type safety and discoverability.

### New Capabilities

None.

## Impact

**Behavior changes:**
- None observable to users. The DomainEventBus synchronization prevents a latent race condition but does not alter publish/subscribe semantics. The interface additions expose existing members, preserving XAML binding behavior.

**Code changes:**
- `src/CLIHub.Core/Events/DomainEventBus.cs` — reader/writer lock around subscribe/unsubscribe and the publish snapshot.
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — add five members and update doc comments.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — no changes (implementation already present; only the interface needs updating).
- Test coverage: `DomainEventBusTests.cs` gains a concurrency scenario.
- `docs/backlog.md` — five entries reconciled against the verified tree.

**Spec deltas:**
- None. These are internal refactors (DomainEventBus), interface hygiene (exposing existing members), and backlog reconciliation. No durable behavior contract is affected. The DomainEventBus thread-safety guarantee is deliberately spec-external: the bus is internal infrastructure with no user-facing capability contract, and the concurrency test is the durable evidence.

## Explicit Non-Goals

- Do not add a generic `IPersistenceService<T>` or `ICacheService` (deferred by ADR-0013; ADR-0014 decision notes the narrow reuse and divergent policies).
- Do not redesign `MenuAction` or add a separate plugin-delete flow (both are separate backlog items with their own change lifecycle).
- Do not remove `AvailabilitySummary` from the interface (it is unused but harmless; removal can happen in a property-cleanup pass).

## Dependencies

- None. This change is self-contained and does not block or depend on other changes.
