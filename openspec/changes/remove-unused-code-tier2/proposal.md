## Why

This is the second phase of dead code removal following whole-solution Roslyn reference analysis. These members have zero production references and no XAML bindings, and removing them reduces maintenance surface while preserving all documented behavior.

## What Changes

Remove 13 dead code items grouped into two categories:

**A. Full removals** (delete interface + implementation + tests):
1. `AgentCardItem.HasCommands` — public property, no references
2. Five dead `LaunchWindowViewModel` properties — leftovers from `WindowActionCoordinator` refactor
3. `IProcessOrchestrator.ResolveRuntimeAsync` chain — redundant pass-through, production uses injected `IRuntimeResolver` directly
4. `IProjectService.AddDiscoveredProject` chain — production uses `ReconcileDiscoveredProjects` bulk path
5. `IRepositoryWatcher.Stop` chain — production uses `Dispose`
6. `AgentProcess.IsRuntimeHosted` — computed property, no references
7. `GlobalHotkeyService.IsRegistered` — property, no references
8. `ITrayHost` interface — registered but never resolved; concrete `TrayIconController` used directly

**B. Interface-only removals** (implementation stays; it is live through concrete type or test-anchored):
9. `IApplicationOperationLifetime.Token` — implementation keeps the token and test anchor
10. `IStartupWindow.ShowOnPointerMonitor` — implementation called concretely
11. `IAgentManagementSection.SupportedAgentCount` + `HasDefaultAgent` — concrete properties tested
12. `IProjectRegistrySection.MissingFolderCount` — concrete property tested
13. `IProjectScanner.Stop` — implementation has two behavior tests

## Capabilities

This is a **pure internal refactor** with zero behavior change.

### Modified Capabilities

None — all deleted items are internal API only, with no behavior contract.

## Impact

**Deleted code:**
- One interface file: `src/CLIHub/Tray/ITrayHost.cs` (3 members)
- 9 further interface members across 8 interfaces
- 5 implementations (2 methods, 3 properties) plus the 5 dead `LaunchWindowViewModel` members

**Modified code:**
- 18 source files (1 deleted, 17 trimmed), across Core, Core.Windows and the WPF app
- 6 test files (test methods and fake members removed; no test file deleted)
- 2 composition/test files where the dead `ITrayHost` registration is asserted

**No data, UI, or runtime behavior changes.**

## Non-Goals

- Removing members required by the behavior contract, even if no production caller exists today (`IPluginCatalog.ReloadPlugins`, `IConfigurationRepository.Flush`)
- Removing test-only seams that are deliberately exposed (`CreateForTesting`, `ParseOrDefault`, etc.)
- Cleaning unused `using` directives outside touched files
- Cleaning `assets/` or XAML files
- Refactoring or renaming live code

## Dependencies

None — standalone refactor.
