# Quick Wins: Implementation Tasks

Scope note (2026-11-14, re-verified after the interrupted session): three of the five backlog
candidates were already resolved in the tree and are dropped from implementation Ã¢â‚¬â€ plugin-feed
cancellation (`src/CLIHub.Core/Plugins/PluginFeedClient.cs:794`), the two unused glyph constants
(`src/CLIHub/Theming/IconGlyphs.cs` has no `Diagnostic`/`Process`), and the maximize/restore
tooltip (`src/CLIHub/Views/WindowHeaderBar.xaml:67-74` already flips `Maximize`/`Restore` from a
`WindowState` data trigger). Only Tasks 1-3 below remain.

## Task 1: Synchronize DomainEventBus

The tree already carries a partial edit (`ReaderWriterLockSlim subscriptionsGate` field,
`IDisposable` on the class, XML doc paragraph). Continue it; do not revert.

- [x] 1.1 `Publish`: take `EnterReadLock`, copy the handler list, `ExitReadLock` in `finally`; invoke handlers outside the lock in subscription order with the existing catch-and-log behavior
- [x] 1.2 `Subscribe`: take `EnterWriteLock` / `ExitWriteLock` in `try`/`finally` around the dictionary lookup, list creation, and `Add`
- [x] 1.3 `Subscription` unsubscribe callback: take `EnterWriteLock` / `ExitWriteLock` in `try`/`finally` around the lookup and `Remove`
- [x] 1.4 Implement `Dispose()` disposing `subscriptionsGate`; confirm the DI registration is a singleton the provider disposes (no manual wiring needed)
- [x] 1.5 Add concurrency test: concurrent publishes racing subscribes/unsubscribes (at least 1000 publishes), assert no exception escapes and every published value is observed in order (`tests/CLIHub.Core.Tests/Events/DomainEventBusTests.cs`)
- [x] 1.6 Keep the existing `DomainEventBusTests` scenarios green

### Gate: Build and Test
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~DomainEventBusTests"
```

## Task 2: Add Plugin-Management Members to IAgentManagementSection

- [x] 2.1 Add `InvalidCards` and `DisabledCards` (`ObservableCollection<...>`) after `Cards`
- [x] 2.2 Add `HasDisabledPlugins` after `DisabledCards`
- [x] 2.3 Add `PluginCatalogSummary` after `AvailabilitySummary`
- [x] 2.4 Add `EnablePluginCommand` after `CheckUpdatesCommand`
- [x] 2.5 XML doc each new member, matching the implementation's doc comments
- [x] 2.6 Confirm `AgentManagementViewModel` already satisfies the widened interface (no implementation changes) and that the Settings CLI Agents bindings still compile

### Gate: Build and Test
```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~AgentManagementViewModelTests"
```

## Task 3: Update Backlog Entries

- [x] 3.1 `docs/backlog.md` DomainEventBus entry: mark resolved by `followups-quick-wins` (2026-11-14) with a one-line outcome, or remove it
- [x] 3.2 PluginFeedClient cancellation entry: mark resolved earlier (evidence `PluginFeedClient.cs:794` rethrows caller cancellation)
- [x] 3.3 Unused IconGlyphs entry: mark resolved earlier (constants no longer present)
- [x] 3.4 Maximize-tooltip entry: mark resolved earlier (chrome refactor, `WindowHeaderBar.xaml:67-74`)
- [x] 3.5 Plugin-management interface entry: mark resolved by `followups-quick-wins`

## Task 4: Final Verification

- [x] 4.1 `dotnet build CLIHub.sln`
- [x] 4.2 `dotnet test CLIHub.sln`
- [x] 4.3 `openspec validate --all`
- [x] 4.4 Manual smoke: launch CLIHub, open Settings and the CLI Agents page, maximize/restore the window (tooltip flips), confirm plugin catalog summary and disabled-plugin list still render

### Gate: All Gates Passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
```

## Task 5: Commit and Archive Preparation

- [ ] 5.1 Stage only the files that belong to this change (no unrelated working-tree edits)
- [ ] 5.2 Wait for explicit user approval, then commit and archive

---

## Notes

- **No spec deltas:** internal synchronization, interface hygiene, and backlog bookkeeping; the
  change declares `skip_specs: true`.
- **Concurrency test scope:** behavior-only. The lock is an implementation detail and is not
  exposed for testing.
- **Smoke:** no new user-visible behavior is expected; the smoke is a regression check.
