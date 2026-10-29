## Context

A whole-solution Roslyn reference analysis over 4617 source declarations identified 12 unreferenced items that were introduced for anticipated workflows but never consumed by production or test code. These are dead code: they add maintenance burden, pollute navigation, and create false-positive search hits.

This change removes all 12 items verified by the orchestrator's Roslyn analysis. Each item below is the complete, final scope — nothing more will be deleted in this change.

See `proposal.md` for the high-level motivation.

## Current State (file:line evidence)

All items confirmed as unreferenced in the codebase (checked 2026-10-29 against commit 984d5eb):

1. **`UpdateChecker` class** — `src/CLIHub.Core/Updates/UpdateChecker.cs:13` — entire `internal sealed class UpdateChecker` (lines 13-105) has zero production references. Its logic (`CheckAsync`, `ObserveLateUpdateCheckAsync`, `Normalize`) is duplicated inline inside `UpdateService` (`CheckForUpdateCoreAsync`, `ObserveLateUpdateCheckAsync`, `Normalize`). Test file: `tests/CLIHub.Core.Tests/Updates/UpdateCheckerTests.cs` (136 lines). The only consumer was `UpdateService.checker` field (item 5).

2. **`VelopackManagerProvider` class** — `src/CLIHub.Core/Updates/VelopackManagerProvider.cs:11` — entire `internal sealed class VelopackManagerProvider` (lines 11-69) is unreferenced in production code; only `tests/CLIHub.Core.Tests/Updates/VelopackManagerProviderTests.cs` uses it. `UpdateService.CreateDefaultManager` (at `UpdateService.cs:100`) performs the same initialization role.

3. **`UpdateDownloader.IsDownloading` property** — `src/CLIHub.Core/Updates/UpdateDownloader.cs:31` — public property (lines 31-40) is unreferenced. The `private bool isDownloading` field at line 16 MUST remain: it is the re-entrancy guard used by `DownloadAsync` at lines 62, 69, 100. Test file `tests/CLIHub.Core.Tests/Updates/UpdateDownloaderTests.cs` has the test `IsDownloading_Initially_ReturnsFalse` (lines 98-103) and two `Assert.False(this.downloader.IsDownloading)` assertions at lines 47 and 95 — all three must be deleted. **IMPORTANT:** `UpdateService.IsDownloading` at `UpdateService.cs:157` is a DIFFERENT, LIVE property (`IsDownloading => Volatile.Read(ref this.isDownloadInProgress) != 0;`) — it MUST NOT be touched.

4. **`LaunchWindowViewModel.NoProjectSelectedMessage` field** — `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:21` — `private const string NoProjectSelectedMessage = "Select a project first.";` is defined but never referenced (verified by grep across src and tests).

5. **`UpdateService.checker` field** — `src/CLIHub.Core/Updates/UpdateService.cs:32` — `private readonly UpdateChecker checker;` is assigned at line 49 (`this.checker = new UpdateChecker(logger);`) in the public constructor and at line 62 (`this.checker = new UpdateChecker(logger);`) in the private test constructor, but never read. This is the consumer that makes item 1 possible. Delete the field and both assignments.

6. **`StartupPreferencesApplier.preferencesStore` field** — `src/CLIHub/Startup/StartupPreferencesApplier.cs:12` — `private readonly IPreferencesStore preferencesStore;` is assigned (ctor param at line 21, null guard at line 24) but never read. The ctor `<param>` doc is at line 18. DI registration is type-based (`AddSingleton<IStartupPreferencesApplier, StartupPreferencesApplier>()` at `src/CLIHub/Composition/ServiceRegistration.cs:94`), so it adapts automatically. Test call site: `tests/CLIHub.Tests/Services/StartupPreferencesApplierTests.cs` has one `new StartupPreferencesApplier(preferencesStore.Object, startupService.Object)` call (line 21) plus a now-unused `preferencesStore` mock (line 18).

7. **`StatusMessageCoordinator.agentPane` field** — `src/CLIHub/ViewModels/Shared/StatusMessageCoordinator.cs:10` — `private readonly AgentPaneController agentPane;` is assigned (ctor param at line 22, null guard at line 26) but never read. The `<param>` doc at line 19 currently says "Agent pane controller (currently unused, reserved for future events)" — this is a deliberate hook being removed, so delete that sentence. DI registration is type-based. Construction sites: `tests/CLIHub.Tests/ViewModels/Shared/StatusMessageCoordinatorTests.cs` has three calls (lines 128, 171, 235); `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` has two calls (lines 270, 350); `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs` has one (line 192).

8. **`AgentProcessFinder.catalog` field** — `src/CLIHub.Core/Processes/AgentProcessFinder.cs:15` — `private readonly IPluginCatalog catalog;` is assigned (ctor param at line 26, line 30) but never read. The `<param>` doc is at line 22. Explicit factory registration: `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:53` creates `new AgentProcessFinder(...)` with three arguments. Test call sites: `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs` (line 38), `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` (lines 287, 367), `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` (line 76), `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs` (line 131). The type-based DI registration adapts automatically.

9. **`PluginCatalog.ValidatePlugin` method** — `src/CLIHub.Core/Plugins/PluginCatalog.cs:138` — private method (lines 138-151) has zero callers. Its logic is duplicated inline inside `LoadPluginsCore` at lines 85-93.

10. **`AgentVersionService.ProbeTimeout` property** — `src/CLIHub.Core/Agents/AgentVersionService.cs:133` — private property (lines 133-140) has zero callers (verified by grep; the only hit is the definition itself). **IMPORTANT:** The `AgentProbeTimeoutSeconds` preference at line 137 is LIVE and used by `AgentReleaseClient` — do not touch it. `AgentReleaseClient.ProbeTimeout` at `src/CLIHub.Core/Agents/AgentReleaseClient.cs:24` is also a DIFFERENT, LIVE member — do not touch it.

11. **`ProcessCaptureResult.cs` file** — `src/CLIHub.Core/Models/Processes/ProcessCaptureResult.cs` — entire public record (lines 1-10) is unreferenced in src and tests (verified by grep: zero matches in the codebase). Delete the file.

12. **`IAgentManagementSection.IsCheckingUpdates` interface member** — `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:121` — interface property (lines 118-121) with doc at lines 119-120. REMOVE this member from the interface ONLY. **IMPORTANT:** `AgentManagementViewModel.IsCheckingUpdates` at `AgentManagementViewModel.cs:201` (lines 201-207) is LIVE — it is used by the class's own `CanDetect` (line 198), `CanCheckUpdates` (line 214), `CanDetectCard` (line 268), and `CanCheckCardUpdates` (line 279), and by tests through the concrete type. XAML binding check: no `.xaml` files in `src/CLIHub` reference `IsCheckingUpdates` (verified by grep).

## Decisions

- **`IUpdateChecker` interface is untouched**: It is a live interface implemented by `IUpdateService` and consumed by `UpdateWorkflow` (`src/CLIHub/Updates/UpdateWorkflow.cs:17,41`) and registered in DI (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:149`). Only the concrete `UpdateChecker` class is dead.

- **Item 7: deliberate "reserved for future events" hook removal**: The `<param>` doc at `StatusMessageCoordinator.cs:19` explicitly says "currently unused, reserved for future events." Since the orchestrator verified it is never read, we are removing the reservation — delete the field, ctor param, null guard, and that sentence from the doc.

- **Item 3: test assertion removal judgment**: The `UpdateDownloader.IsDownloading` property is being deleted, so the test `IsDownloading_Initially_ReturnsFalse` and the two `Assert.False(this.downloader.IsDownloading)` assertions (lines 47 and 95 of `UpdateDownloaderTests.cs`) must also be deleted. The test file itself remains (other tests are live).

- **Staging order ensures every commit compiles and tests pass**: The tasks are ordered so that each stage leaves the tree in a compilable, test-green state.

## Alternatives Considered

- **Keep the "reserved for future events" hook** (item 7): Rejected. The orchestrator verified it is never read, and the Roslyn analysis confirms it is dead. If a future requirement needs it, it can be re-introduced at that time.

- **Remove only the interface member but not the concrete property** (item 12): Rejected for item 12 because the concrete property is LIVE (used by the class's own logic and tests). The interface member is the dead part.

- **Combine this with Tier 2 interface member cleanup**: Rejected. The orchestrator explicitly scoped this to Tier 1 (these 12 items) and stated Tier 2 (other dead interface members and `AgentCardItem.HasCommands`) will be a separate change. This change addresses only the items listed above.

## Open Questions

None. All items are verified, scoped, and ready for implementation.

## Risks / Trade-offs

- **Two-stage UpdateChecker removal** (items 1 and 5): Delete the `UpdateService.checker` field first (item 5), then delete the `UpdateChecker` class and its test file (item 1). Reversing the order would leave a field referencing a deleted type (compile error). The staging in tasks.md handles this correctly.

- **Re-entrancy guard in UpdateDownloader** (item 3): The `isDownloading` field MUST remain even though the public `IsDownloading` property is being deleted. The field is the lock-protected re-entrancy guard inside `DownloadAsync`.

- **IAgentManagementSection interface contract** (item 12): Removing the interface member is safe because no XAML binding references it, and the concrete implementation remains live for the class's own internal use and tests. Consumers that interact with `IAgentManagementSection` never call `IsCheckingUpdates`.

## Migration Plan

Single-commit pure refactor; rollback is a revert. No config, data, or deployment migration.
