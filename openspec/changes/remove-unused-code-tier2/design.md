## Context

This change removes a second batch of dead code identified through whole-solution Roslyn reference analysis plus manual XAML/spec verification. The first tier removed simpler unused members. This tier includes interface chains, registration cleanup, and interface-member-only removals where the implementation stays live.

## Goals / Non-Goals

**Goals:**
- Remove all dead code items listed in proposal.md with zero behavior change
- Delete full chains (interface + implementation + tests) for category A items
- Remove interface members only for category B items (implementation/tests stay)
- Keep all spec-required members even if they have no production caller today

**Non-Goals:**
- Removing spec-required members (`IPluginCatalog.ReloadPlugins`, `IConfigurationRepository.Flush`)
- Removing test-only seams (`CreateForTesting`, `ParseOrDefault`, etc.)
- Cleaning unused `using` directives outside touched files
- Cleaning `assets/` or XAML
- Refactoring or renaming live code

## Current State

### A. Full Removals

#### 1. `AgentCardItem.HasCommands`

**Location:** `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:121`

```csharp
public bool HasCommands => this.CommandLabels.Count > 0;
```

**Evidence:** Zero references in source or XAML. Whole-solution Roslyn search returned no callers.

**Decision:** Delete the property. Keep `CommandLabels` and `CommandsText` (both are live).

---

#### 2. Five dead `LaunchWindowViewModel` members

**Location:** `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`

Dead members (leftovers from the `WindowActionCoordinator` refactor):
- `WindowActions` property (line 152): public property returning `this.windowActions`
- `LaunchCommand` (line 284): delegates to `windowActions.LaunchCommand`
- `ResumeCommand` (line 289): delegates to `windowActions.ResumeCommand`
- `VersionCommand` (line 299): delegates to `windowActions.VersionCommand`
- `ShowWarning` method (line 386): private method calling `notifications.ShowWarning`

**Evidence:** Zero references. The XAML binds directly to `WindowActions.LaunchCommand` etc., not to these top-level delegates.

**Decision:** Delete all five members. Keep the live siblings: `windowActions` field, `OpenDataFolderCommand`, `OpenSettingsCommand`, `ExitCommand`, `UpdateCommand`, `LaunchAgentCommand`, `ResumeAgentCommand`, `ApplyPathDisplayStyle`.

---

#### 3. `IProcessOrchestrator.ResolveRuntimeAsync` chain

**Interface:** `src/CLIHub.Core/Processes/IProcessOrchestrator.cs:55`
```csharp
Task<RuntimeExecutable?> ResolveRuntimeAsync(
    RuntimeKind kind,
    CancellationToken cancellationToken = default);
```

**Implementation:** `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:207-212`
```csharp
public Task<RuntimeExecutable?> ResolveRuntimeAsync(
    RuntimeKind kind,
    CancellationToken cancellationToken = default)
{
    return this.runtimeResolver.ResolveAsync(kind, cancellationToken);
}
```

**Fake:** `tests/CLIHub.Core.Tests/Fakes/FakeProcessOrchestrator.cs:37`

**Test:** `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs:318` (`ResolveRuntimeAsync_DelegatesToResolver`)

**Evidence:** No production callers. `ProcessOrchestrator` uses the injected `IRuntimeResolver` directly at lines 58 and 122. This is a redundant pass-through.

**Decision:** Delete the interface member, implementation, fake method, and the one test. The `runtimeResolver` field and its direct usage stay.

---

#### 4. `IProjectService.AddDiscoveredProject` chain

**Interface:** `src/CLIHub.Core/Projects/IProjectService.cs:51`
```csharp
void AddDiscoveredProject(Project project);
```

**Implementation:** `src/CLIHub.Core/Projects/ProjectService.cs:99`

**Tests:** `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`
- `AddDiscoveredProject_MarksDiscoveredSource` (starts line 299)
- `AddDiscoveredProject_ExistingManual_DoesNotReclassify` (starts line 307)

**Evidence:** No production callers. Production registers discovered projects through `ReconcileDiscoveredProjects` (the spec's bulk auto-register path per `openspec/specs/project-scanning/spec.md`).

**Decision:** Delete the interface member, implementation, and both tests. Keep `ReconcileDiscoveredProjects`.

---

#### 5. `IRepositoryWatcher.Stop` chain

**Interface:** `src/CLIHub.Core/Scanning/IRepositoryWatcher.cs:26`
```csharp
void Stop();
```

**Implementation:** `src/CLIHub.Core/Scanning/FileSystemRepositoryWatcher.cs:89`

**Fake:** `tests/CLIHub.Core.Tests/Fakes/ScanningFakes.cs:15`

**Evidence:** No production or test callers. Production closes the watcher through `Dispose`.

**Decision:** Delete the interface member, implementation, and fake. Keep `Dispose`.

---

#### 6. `AgentProcess.IsRuntimeHosted`

**Location:** `src/CLIHub.Core/Processes/AgentProcess.cs:48`
```csharp
public bool IsRuntimeHosted => this.RuntimeKind is not null;
```

**Evidence:** Zero references.

**Decision:** Delete the property. Keep `RuntimeKind` (it is live).

---

#### 7. `GlobalHotkeyService.IsRegistered`

**Location:** `src/CLIHub/Hotkeys/GlobalHotkeyService.cs:28`
```csharp
public bool IsRegistered => this.registered;
```

**Evidence:** Zero references. The `registered` field is still used by `Register`/`Unregister` logic.

**Decision:** Delete the property. Keep the `registered` field.

---

#### 8. `ITrayHost` interface

**Interface file:** `src/CLIHub/Tray/ITrayHost.cs`

**Registration:** `src/CLIHub/Composition/ServiceRegistration.cs:135`
```csharp
services.AddSingleton<ITrayHost>(sp => sp.GetRequiredService<TrayIconController>());
```

**Implementer:** `src/CLIHub/Tray/TrayIconController.cs:13`
```csharp
public sealed class TrayIconController : ITrayHost, IDisposable
```

**Doc mention:** `src/CLIHub/Composition/ServiceRegistration.cs:125` — XML comment lists `ITrayHost` as a registered port.

**Test impact:**
- `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:158` — asserts `ITrayHost` registration
- `tests/CLIHub.Tests/Services/TrayMenuBuilderRegistrationTests.cs` — doc assertions may mention `ITrayHost`

**Evidence:** Registered as a singleton but never resolved. The sole consumer, `WpfApplicationStartupUi`, takes the concrete `TrayIconController`.

**Decision:** 
- Delete `src/CLIHub/Tray/ITrayHost.cs`
- Remove `services.AddSingleton<ITrayHost>(...)` from `ServiceRegistration.cs:135`
- Remove the `<c> ITrayHost </c>` XML doc mention from `ServiceRegistration.cs:125`
- Remove `ITrayHost,` from `TrayIconController.cs:13` (keep the implementation methods; they are called concretely)
- In `ServiceRegistrationTests.cs:158`, remove the `ITrayHost` assertion from the collection check
- In `TrayMenuBuilderRegistrationTests.cs`, drop any `ITrayHost` descriptor assertions; keep `ITrayActions` ones; rename the test if its name mentions "host ports"

---

### B. Interface-Only Removals

For these items, the interface member is dead but the implementation is live through concrete type usage or test-anchored, so only the interface member is removed.

#### 9. `IApplicationOperationLifetime.Token`

**Interface:** `src/CLIHub/Lifetime/IApplicationOperationLifetime.cs:11`

**Implementation:** `ApplicationOperationLifetime.Token` at `src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:40` — KEEP

**Test anchor:** `tests/CLIHub.Tests/Services/ApplicationOperationLifetimeTests.cs:26` asserts `Assert.Equal(lifetime.Token, receivedToken)`, which verifies that `RunAsync` passes the application token. KEEP the assertion and the concrete property.

**Decision:** Remove the interface member only. Keep the implementation and test.

**Rationale:** The test verifies that `RunAsync` correctly threads the application cancellation token to its delegate, which is a core guarantee. The property itself is not interface-consumed but the test anchors the implementation behavior.

---

#### 10. `IStartupWindow.ShowOnPointerMonitor`

**Interface:** `src/CLIHub/Startup/IStartupWindow.cs:11`

**Implementation:** `src/CLIHub/Views/LaunchWindow.xaml.cs:65` — KEEP (called concretely at `src/CLIHub/Tray/TrayIconController.cs:59`)

**Decision:** Remove the interface member only. Keep the implementation (it is called through the concrete type).

---

#### 11. `IAgentManagementSection.SupportedAgentCount` + `HasDefaultAgent`

**Interface:** `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:26` (SupportedAgentCount), `:46` (HasDefaultAgent)

**Implementation:** `AgentManagementViewModel` has concrete properties at lines 103, 106, 121, 128, tested through the concrete type.

**Decision:** Remove both interface members. Keep the concrete properties and their tests.

---

#### 12. `IProjectRegistrySection.MissingFolderCount`

**Interface:** `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs:35`

**Implementation:** Concrete property in `ProjectRegistryViewModel` feeds `HasMissingFolders` and `FolderStatusSummary`, tested through the concrete type.

**Decision:** Remove the interface member. Keep the concrete property and its tests.

---

#### 13. `IProjectScanner.Stop`

**Interface:** `src/CLIHub.Core/Scanning/IProjectScanner.cs:40`

**Implementation:** `ProjectScanningService.Stop` at `src/CLIHub.Core/Scanning/ProjectScanningService.cs:156` — KEEP

**Tests:** 
- `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:133`
- `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:215`

Both tests verify `Stop` behavior through the concrete type.

**Decision:** Remove the interface member only. Keep the implementation and both tests.

---

## Spec-Required Members (KEEP — Explicitly Out of Scope)

These members have zero production callers but are required by the behavior contract, so they are NOT removed:

### `IPluginCatalog.ReloadPlugins` + `PluginCatalog.ReloadPlugins`

**Spec:** `openspec/specs/plugin-seeding/spec.md` lines 117-140

Requirements:
- "Reload the plugin catalog on demand" (line 117)
- "Notify consumers after catalog reload" (line 129)

Scenarios:
- "New plugin is available after reload" (line 121)
- "Removed plugin is absent after reload" (line 125)
- "Reload notification is raised after replacement" (line 134)
- "Initial load does not create a duplicate reload notification" (line 137)

**Decision:** KEEP both interface and implementation. The spec requires a synchronous manual operation for on-demand catalog reload. No production caller today does not invalidate the contract.

---

### `IConfigurationRepository.Flush` + `ConfigurationRepository.Flush`

**Spec:** `openspec/specs/configuration-snapshot/spec.md:39`

Requirement: "synchronous flush"

Additional context: `openspec/specs/app-lifecycle/spec.md` shutdown flush requirement.

**Decision:** KEEP both interface and implementation. Called internally from `Dispose` and required by the persistence contract.

---

### Test-Only Seams

The earlier scan identified test-facing members that are deliberately exposed:
- `HotkeyParser.ParseOrDefault`
- `ProjectState.From(AppConfig)` / `ApplyTo(AppConfig)`
- `ProjectLogoResolver.ResolvePath`
- Parameterless `AgentProcessMonitor.HasRunningAgents()`
- `IAgentUpdateAvailabilityService.Invalidate`
- `SettingsViewModel.PageGroups`
- `ApplicationHost.HasBuiltServiceProvider`
- The one-argument `AddCLIHubServices` / `AddWindowsInfrastructure` overloads
- `UpdateInstaller.ApplyAndExit`
- `AppConfigDocument.From(AppConfig)` / `ToAppConfig()`
- `DiscoveryResult.ScannedDirectoryCount`
- `ProjectModelCatalog.Models`

**Decision:** KEEP all test-only seams for this change. They are not dead code; they are test API.

---

## Decisions

### Decision 1: Two-category split (full removal vs interface-only trim)

**Chosen:** Category A deletes the full chain (interface + implementation + tests); category B removes the interface member only and keeps the implementation (it is live through concrete usage or test-anchored).

**Rationale:** 
- Category A items are truly dead — no production caller, no test anchor, and removing them reduces API surface with zero risk.
- Category B items have dead interface contracts but live implementations: the concrete type is called directly or the implementation is verified through tests that ensure a behavior guarantee. Removing the implementation would break production or lose test coverage.

**Alternatives considered:**
- Remove interface and implementation for all items: Rejected — would break concrete callers and lose behavior anchors.
- Keep everything: Rejected — defeats the cleanup goal and leaves dead interface surface.

---

### Decision 2: Keep spec-required members even with zero production callers

**Chosen:** `IPluginCatalog.ReloadPlugins` and `IConfigurationRepository.Flush` stay (both interface and implementation).

**Rationale:** The behavior specs explicitly require these operations. A missing production caller today does not invalidate the contract — the API is part of the documented surface and may be used in the future or through test/tooling paths not visible to Roslyn.

**Spec evidence:**
- `plugin-seeding/spec.md:117-140` requires "Reload the plugin catalog on demand" with scenario coverage.
- `configuration-snapshot/spec.md:39` requires "synchronous flush".
- `app-lifecycle` shutdown flush path uses `Flush` internally through `Dispose`.

**Alternatives considered:** Remove them since they have no callers — Rejected, contradicts the spec.

---

### Decision 3: Interface-only removal for test-anchored properties

**Chosen:** For items where the concrete property is tested (e.g., `ApplicationOperationLifetime.Token`), remove the interface member but keep the implementation and test.

**Rationale:** The test verifies a behavior guarantee (e.g., `RunAsync` correctly threads the application cancellation token). The interface member is unused, but the property and its test anchor the implementation's correctness. Removing the property would lose that verification.

**Example:** `ApplicationOperationLifetimeTests.cs:26` asserts `Assert.Equal(lifetime.Token, receivedToken)`, which verifies that `RunAsync` passes the application token to the delegate. This is a core contract; dropping it would weaken coverage.

**Alternatives considered:**
- Remove the property and test: Rejected — loses behavior verification.
- Keep the interface member: Rejected — it has zero interface-based callers and adds dead API surface.

---

## Risks

**Low-risk change:**
- All removals are verified through whole-solution Roslyn reference analysis + manual XAML/spec checks.
- The split into full-removal vs interface-only ensures no live implementation or test anchor is deleted.
- Build + test gates catch any missed reference.

**Specific mitigations:**
- Category B removals explicitly preserve implementations and tests to avoid breaking concrete callers or losing behavior anchors.
- Spec citations document why `ReloadPlugins` and `Flush` are kept despite having no production callers.
- The task plan includes per-stage build + test gates to catch errors early.

---

## Implementation Notes

**Task staging:**
1. Remove `ITrayHost` (interface file, registration, doc mention, implementer's `: ITrayHost`, test assertions) — standalone, no cross-file dependencies
2. Remove dead properties (`HasCommands`, five `LaunchWindowViewModel` members) — small, focused
3. Remove `ResolveRuntimeAsync` chain (interface, impl, fake, test)
4. Remove `AddDiscoveredProject` chain (interface, impl, tests)
5. Remove `IRepositoryWatcher.Stop` chain (interface, impl, fake)
6. Remove the two standalone dead properties (`IsRuntimeHosted`, `IsRegistered`)
7. Remove interface-only members (9 interface lines across 5 files)
8. Final gates: build, test, `openspec validate --all`

Each stage ends with `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` to catch errors immediately.

**Post-verification cleanup (2026-10-30):** independent verification found seven orphaned XML doc comments — the `AgentCardItem.HasCommands` doc block (the member and its doc were separated by a blank line) and dangling `<inheritdoc />` tags on six implementations whose interface member this change removed (`ApplicationOperationLifetime.Token`, `ProjectScanningService.Stop`, `AgentManagementViewModel.SupportedAgentCount`, `AgentManagementViewModel.HasDefaultAgent`, `ProjectRegistryViewModel.MissingFolderCount`), plus `AgentManagementViewModel.IsCheckingUpdates`, orphaned since tier 1. Each now carries its own `<summary>`. No behavior, member, or test changed; build remains 0 warnings / 0 errors.

---

## Open Questions

None — all removals are evidence-verified and the spec citations for kept members are documented.
