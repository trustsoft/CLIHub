## Why

A whole-solution Roslyn reference analysis identified 12 unused code items (classes, methods, properties, and fields) that were introduced for anticipated workflows but never consumed by production or test code. Removing them reduces maintenance burden, simplifies navigation, eliminates false-positive code search hits, and eliminates three compiler CA1823 warnings.

This is a pure internal refactor: no durable capability changes, no external API modifications, and no behavior changes. The change opts out of spec deltas (`skip_specs: true` in `.openspec.yaml`).

## What Changes

Delete or trim 12 unreferenced items across Core and WPF projects:

1. Delete the entire `UpdateChecker` class (`src/CLIHub.Core/Updates/UpdateChecker.cs`) plus its test file, and remove the `UpdateService.checker` field that held its only consumer reference. `UpdateService` duplicates the logic inline (`CheckForUpdateCoreAsync`, `ObserveLateUpdateCheckAsync`, `Normalize`). **Important:** `IUpdateChecker` is a separate, live interface used by `UpdateWorkflow` — it remains untouched.

2. Delete the entire `VelopackManagerProvider` class (`src/CLIHub.Core/Updates/VelopackManagerProvider.cs`) plus its test file. `UpdateService.CreateDefaultManager` performs the same role.

3. Remove the `UpdateDownloader.IsDownloading` public property; keep the `isDownloading` field (it guards re-entrancy inside `DownloadAsync`). Delete the `IsDownloading_Initially_ReturnsFalse` test and two test assertions. **Important:** `UpdateService.IsDownloading` is a different, live property — it remains untouched.

4. Delete the `LaunchWindowViewModel.NoProjectSelectedMessage` field (defined but never referenced).

5. Delete the `UpdateService.checker` field and both `this.checker = new UpdateChecker(logger);` assignments (public and private test constructors). This enables item 1.

6. Delete the `StartupPreferencesApplier.preferencesStore` field (assigned but never read), its constructor parameter, null guard, and `<param>` doc line. DI registration is type-based and adapts automatically. Update the single test call site and drop its now-unused mock.

7. Delete the `StatusMessageCoordinator.agentPane` field (assigned but never read), its constructor parameter, null guard, and the sentence in its `<param>` doc that says "currently unused, reserved for future events." This is a deliberate hook being removed. Update five construction sites (three in `StatusMessageCoordinatorTests.cs`, two in `LaunchWindowViewModelBuilder.cs`). DI registration is type-based and adapts automatically.

8. Delete the `AgentProcessFinder.catalog` field (assigned but never read), the `IPluginCatalog catalog` constructor parameter, and its `<param>` doc. Update the explicit factory at `WindowsServiceCollectionExtensions.cs:53` and five test call sites. Type-based DI adapts automatically.

9. Delete the private `PluginCatalog.ValidatePlugin` method (zero callers; logic is duplicated inline inside `LoadPluginsCore`).

10. Delete the private `AgentVersionService.ProbeTimeout` property (zero callers). **Important:** Leave the `AgentProbeTimeoutSeconds` preference and the separate `AgentReleaseClient.ProbeTimeout` member untouched — they are live.

11. Delete the entire `ProcessCaptureResult.cs` file (unreferenced public record in src and tests).

12. Remove the `IAgentManagementSection.IsCheckingUpdates` interface member only. **Important:** `AgentManagementViewModel.IsCheckingUpdates` remains; it is used by the class's own `CanDetect`/`CanCheckUpdates` logic and by tests through the concrete type. Confirm no XAML binding references it (there are none).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure internal refactor with zero behavior change, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

- **Code**: 13 source files touched (3 files deleted, 10 trimmed); 2 test files deleted, 7 test files modified.
- **Behavior**: None. No durable capability changes, no external API surface changes, no configuration or data migration.
- **Dependencies**: None.
- **Build**: The single CA1823 warning for `NoProjectSelectedMessage` is eliminated. All builds and tests remain green.

## Non-Goals

- Tier 2 items (dead interface members beyond item 12: `IApplicationOperationLifetime.Token`, `IStartupWindow.ShowOnPointerMonitor`, `ITrayHost.*`, `IAgentManagementSection.SupportedAgentCount`/`HasDefaultAgent`, `IProjectRegistrySection.MissingFolderCount`, `IProcessOrchestrator.ResolveRuntimeAsync`, `IConfigurationRepository.Flush`, `IPluginCatalog.ReloadPlugins`, `IProjectService.AddDiscoveredProject`, `IProjectScanner.Stop`, `IRepositoryWatcher.Stop`, `AgentProcess.IsRuntimeHosted`, `GlobalHotkeyService.IsRegistered`, and `AgentCardItem.HasCommands`) will be handled in a separate change.
- Unused `using` directives (handled by IDE cleanup).
- Unused files under `assets/` or XAML-name-only findings.
- Any change to `IUpdateChecker`, `IUpdateService`, the update feed, or DI wiring (all remain live).

## Dependencies

None. This change is independent and can be implemented immediately.
