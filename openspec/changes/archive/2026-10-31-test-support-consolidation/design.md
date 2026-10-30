# Design: test-support-consolidation

## Verified current state

All evidence verified 2026-10-30. Baseline gates: `dotnet test CLIHub.sln` = **1,305 tests** (CLIHub.Core.Tests 692, CLIHub.Core.Windows.Tests 63, CLIHub.Tests 550, 0 failed); `openspec validate --all` = 29 passed, 0 failed.

### 1. RunOnSta duplication

Seven byte-identical copies except for the thrown message string (33 call sites total):

- `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173` → `"STA layout test failed."` (9 call sites)
- `tests/CLIHub.Tests/Controls/SummaryCardTests.cs:31` → `"STA control test failed."` (2 call sites)
- `tests/CLIHub.Tests/Converters/PathToImageConverterTests.cs:92` → `"STA converter test failed."` (2 call sites)
- `tests/CLIHub.Tests/Converters/ResourceKeyToTemplateConverterTests.cs:104` → `"STA converter test failed."` (7 call sites)
- `tests/CLIHub.Tests/Imaging/LogoImageSourceCacheTests.cs:149` → `"STA image test failed."` (5 call sites)
- `tests/CLIHub.Tests/Settings/AccentApplierTests.cs:133` → `"STA accent applier test failed."` (6 call sites)
- `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs:73` → `"STA launch window test failed."` (2 call sites)

Body signature: `private static void RunOnSta(Action action)` (all 7 files). Implementation: `Exception? exception` captured on a `new Thread(...)`, `thread.SetApartmentState(ApartmentState.STA)`, `Start()`, `Join()`, then `if (exception is not null) throw new Xunit.Sdk.XunitException(<message>, exception);`. The only variation is the message passed to `XunitException`.

### 2. FindRepositoryRoot duplication

Seven byte-identical copies (same walk from `AppContext.BaseDirectory` up to `CLIHub.sln`, else `throw new DirectoryNotFoundException("Could not locate the repository root.")`):

- `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:170` (15 call sites)
- `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs:299` (2 call sites)
- `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs:129` (2 call sites)
- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:48` (1 call site)
- `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs:128` (1 call site)
- `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs:332` (2 call sites)
- `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs:161` (1 call site)

Total 24 call sites. Body signature: `private static string FindRepositoryRoot()`. Implementation (e.g. SettingsPageMetadataTests.cs:162-169):

```csharp
var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLIHub.sln")))
{
    directory = directory.Parent;
}
return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
```

### 3. InlineOperationLifetime duplication

Two byte-identical `private sealed class InlineOperationLifetime : IApplicationOperationLifetime` copies:

- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:2324-2336`
- `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs:210-222`

Body (both identical): `Token => CancellationToken.None`; `RunAsync` calls `operation(CancellationToken.None)`, invokes `task.GetAwaiter().GetResult()` (blocks until complete), then returns `task`; `StopAsync => Task.CompletedTask`.

**DeferredOperationLifetime is NOT shared**: `AgentManagementViewModelTests.cs:2338-2346` has a `private sealed class DeferredOperationLifetime` that differs from `InlineOperationLifetime` by NOT blocking on `GetAwaiter().GetResult()` — it returns the task directly. This is a one-off variant for testing non-blocking scenarios; sharing it with `InlineOperationLifetime` would create a misleading abstraction. Leave `DeferredOperationLifetime` where it is or move it with its partial (if split moves it to `.BusyState.cs`).

### 4. AgentManagementViewModelTests.cs structure

File: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`, 2,347 lines.

- Class: `public class AgentManagementViewModelTests` (not sealed, no `[Collection]`).
- Tests: 60 methods ([Fact]/[Theory]) from line 137 to 2284.
- Construction calls: 71 `CreateViewModel(...)` calls, 70 `new Mock<IExternalLauncher>()`, 70 `new Mock<IAgentProbeService>()`, 70 `new Mock<IAgentUpdateService>()`, 35 `new Mock<IAgentUpdateAvailabilityService>()`, 7 `new Mock<IAgentVersionService>()`, 7 `new Mock<IAgentProcessInspector>()`, 2 `new Mock<IPluginCatalog>()`, 1 `new Mock<IAgentDetectionService>()`.
- Private helpers:
  - `Launchable(string id, bool withUpdate = false)` (line 21)
  - `CreateViewModel(...)` with 10 parameters including 4 optional mocks (line 39)
  - `Processes(params AgentProcessInstance[])` (line 96)
  - `Processes(string executablePath, string? commandLine = null)` (line 103)
  - `GatedProcesses(ManualResetEventSlim gate, params AgentProcessInstance[])` (line 108)
  - `Detection()` (line 123)
  - `Versions(string value = "1.0")` (line 130)
  - `InlineOperationLifetime` nested class (line 2324)
  - `DeferredOperationLifetime` nested class (line 2338)

Production constructor: `AgentManagementViewModel(IPluginCatalog catalog, IAgentDetectionService detection, IAgentVersionService version, IAgentUpdateAvailabilityService availability, IAgentProbeService probe, IAgentUpdateService update, IExternalLauncher launcher, AgentProcessMonitor processMonitor, IApplicationOperationLifetime operationLifetime, ILogger<AgentManagementViewModel> logger)` (10 parameters).

### 5. SettingsViewModelApplicationTests.cs fixture

File: `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`, 1,001 lines.

Fixture: `private sealed class Fixture` at lines 938-1000, constructed 51 times as `new Fixture(...)`.

Constructor signature (line 953-955): `Fixture(string accentColor = AccentPalette.DefaultHex, AccentSelectionMemory? accentMemory = null, Mock<IAgentManagementSection>? agentsState = null)`.

Properties:
- `Preferences` / `Startup` / `Applier` = `new Mock<T>(MockBehavior.Strict)` (lines 940-942)
- `Updates`, `ReleaseNotes`, `RuntimeExecutables`, `Scanner`, `Dialogs` (loose, lines 943-947)
- `Agents`, `AccentMemory`, `CapturedPreferences`, `ViewModel` (lines 948-951)

Default `AppPreferences` setup (lines 959-967): 6 properties:
```csharp
DefaultRuntime = RuntimeKind.PowerShell,
Hotkey = "Ctrl+Shift+A",
PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
CheckForUpdatesOnStartup = true,
StartMinimized = true,
AccentColor = accentColor
```

Repeated overrides: tests at lines 89, 140, 171, 573 repeat the same 6-property object literal with per-test variations (e.g., line 89 sets `DefaultRuntime = RuntimeKind.GitBash`, line 140 sets `DefaultRuntime = RuntimeKind.PowerShell7`). The repeated properties are always the same 6; what varies is their values.

### 6. LaunchWindowViewModelBuilder.cs — already collapsed (VERIFIED)

File: `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`.

- `Build()` (line 242) returns `this.Compose().ViewModel`.
- `BuildWithMocks()` (line 251) returns `this.Compose()`.
- Both delegate to one `private (LaunchWindowViewModel ViewModel, TestMocks Mocks) Compose()` (line 256).

**Requirement 5 is satisfied.** No action needed; record this as a verified no-op in tasks.md.

### 7. ServiceRegistrationTests.cs — uniqueness already present (VERIFIED)

File: `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`.

Lines 77-80 (inside `AddClIHubServices_ProcessPorts_HaveExactlyOneDescriptor` test):
```csharp
Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRuntimeResolver));
Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICommandLineBuilder));
Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAgentProcessFinder));
Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IProcessOrchestrator));
```

**Requirement 7 is satisfied.** No action needed; record this as a verified no-op in tasks.md.

### 8. Test project package structure

No `Directory.Packages.props` exists at repository root or under `tests/`.

Root `Directory.Build.props` exists and sets `BaseOutputPath` / `BaseIntermediateOutputPath` = `$(MSBuildThisFileDirectory)artifacts\...` / `obj\...`, plus `Version`, `LangVersion 14.0`, `EnforceCodeStyleInBuild`.

Three test csprojs each repeat:
- 6 identical `PackageReference` lines: coverlet.collector 10.1.0, Microsoft.Extensions.DependencyInjection 10.0.12, Microsoft.NET.Test.Sdk 18.10.1, Moq 4.21.0, xunit 2.9.3, xunit.runner.visualstudio 4.0.0.
- 2 identical `<Using Include="..." />` lines: Xunit and System.IO.

## Design decisions

### Decision 1: Shared helper location and import style

**Chosen**: `tests/CLIHub.Tests/Support/TestHelpers.cs`, namespace `CLIHub.Tests.Support`, `internal static class TestHelpers`. Call sites import via `using static CLIHub.Tests.Support.TestHelpers;` (placed inside the namespace per `.editorconfig` line 20: `csharp_using_directive_placement = inside_namespace:warning`).

**Rationale**: Static import keeps call sites byte-identical to today (`RunOnSta(...)` not `TestHelpers.RunOnSta(...)`). The `Support` namespace distinguishes test infrastructure from test subjects.

**Alternative rejected**: Fully-qualified call sites (`TestHelpers.RunOnSta(...)`) → changes call-site syntax and makes diffs noisier.

**Alternative rejected**: Move helpers to `CLIHub.Core` or `CLIHub.Core.Windows` test projects → `CLIHub.Tests` has all 16 call sites; Core and Windows tests have zero, so moving them there would misplace them.

### Decision 2: RunOnSta message parameter

**Chosen**: `internal static void RunOnSta(Action action, string message = "STA test failed.")` with default message. All 7 call sites pass their original distinct message as a second argument to preserve thrown text verbatim.

**Rationale**: The 7 messages carry domain context ("STA layout test failed.", "STA converter test failed.", etc.) that aids debugging. Losing them would make failures ambiguous.

**Alternative rejected**: Parameterless overload + one message → loses the distinct context per test file.

### Decision 3: FindRepositoryRoot caching

**Chosen**: Cache in a `private static readonly Lazy<string>` field so the directory walk happens once per test run. Signature stays `internal static string FindRepositoryRoot()` (no public API change for callers).

**Rationale**: 24 call sites across 7 files in the same test project; the walk is deterministic and the solution location does not change during a test run. Caching reduces redundant I/O.

**Alternative rejected**: No caching → 24 directory walks per run, but the walk is fast (~10 directories) so performance impact is negligible. Caching is a mild improvement, not a requirement; if implementation proves complex, skip it and document why.

### Decision 4: Partial class split — preserving xUnit collection

**Chosen**: Use `partial class AgentManagementViewModelTests` in the SAME namespace `CLIHub.Tests.ViewModels` with the SAME type name, split into 6 files under `tests/CLIHub.Tests/ViewModels/Agents/`:
- `AgentManagementViewModelTests.Load.cs` — Load, EditConfigCommand, and PopulateVersions tests (**28** tests: 26 `Load_*` + 1 `EditConfigCommand_*` + 1 `PopulateVersions_*`)
- `AgentManagementViewModelTests.Detection.cs` — DetectCommand and DetectCardCommand tests (**12** tests: 4 `DetectCommand_*` + 8 `DetectCardCommand_*`)
- `AgentManagementViewModelTests.UpdateAvailability.cs` — CheckUpdatesCommand and CheckCardUpdatesCommand tests (**22** tests: 7 `CheckUpdatesCommand_*` + 15 `CheckCardUpdatesCommand_*`)
- `AgentManagementViewModelTests.CardActions.cs` — UpdateCommand tests (**4** tests: 4 `UpdateCommand_*`)
- `AgentManagementViewModelTests.Cancellation.cs` — Dispose tests (**3** tests: 3 `Dispose_*`)
- `AgentManagementViewModelTests.Helpers.cs` — Shared helpers (`Launchable`, `Processes` ×2, `GatedProcesses`, `Detection`, `Versions`, `DeferredOperationLifetime`, and `CreateViewModel` if Stage 3 keeps it as adapter)

28 + 12 + 22 + 4 + 3 = **69** test methods, which matches the file exactly.

**Real test-name inventory** (69 test methods total: 66 `[Fact]` + 3 `[Theory]`), counted from the file on 2026-10-30:
- 26 `Load_*`
- 15 `CheckCardUpdatesCommand_*`
- 8 `DetectCardCommand_*`
- 7 `CheckUpdatesCommand_*`
- 4 `DetectCommand_*`
- 4 `UpdateCommand_*`
- 3 `Dispose_*`
- 1 `EditConfigCommand_*`
- 1 `PopulateVersions_*`

The earlier draft of this design missed the 7 `CheckUpdatesCommand_*` tests and undercounted the total as 60; the prefix list above is the authoritative enumeration and must be re-verified against the file before moving anything.

**Busy-state and notification assertions have no dedicated partial.** They are interleaved with the command tests they gate (e.g. `CheckCardUpdatesCommand_WhileChecking_GatesHeaderActionsAndRestoresAfter`, `DetectCardCommand_WhileHeaderActionRuns_CanExecuteIsFalse`), so a separate `.BusyState.cs` would either be empty or duplicate tests across files. They stay with the command family they assert on; the split keeps one test class, so the concern boundaries are for navigation only.

**Rationale**: xUnit creates one test collection per test *class* and runs collections in parallel but tests within a collection serially. Splitting into distinct classes (e.g., `AgentManagementViewModelLoadTests`, `AgentManagementViewModelDetectionTests`) would parallelize 69 currently-serial tests. Many of these tests are timing-sensitive and use `SpinWait.SpinUntil` with `ManualResetEventSlim` gates; parallel execution could introduce flakes. Partial classes keep one class ⇒ one collection ⇒ identical discovery, names, and serial scheduling ⇒ zero behavior change.

**Alternative rejected**: Split into distinct classes → changes xUnit scheduling from serial to parallel, introducing flake risk. Tests like `CheckCardUpdatesCommand_WhilePerCardCheckRuns_DisablesOnlyTheActingCardAndGatesHeader` (line 1586-1613) already flaked once on 2026-10-30 (see `docs/backlog.md`); parallelizing them would increase that risk.

**Alternative rejected**: Keep one 2,347-line file → readability and navigation cost when working on a subset of tests.

**Do NOT add `[Collection]` attributes**: the partial-class approach already ensures one collection; an explicit `[Collection("AgentManagement")]` would be redundant and signal intent that doesn't match the implementation (it would suggest multiple classes share the collection, but there's only one class).

### Decision 5: AgentManagementViewModelBuilder migration rule

**Chosen**: Migrate all 71 `CreateViewModel(...)` calls to `new AgentManagementViewModelBuilder()...BuildWithMocks()`. The existing `CreateViewModel` helper may be kept as a thin adapter over the builder if a mechanical rewrite would change test semantics, OR deleted if every call migrates cleanly. Document the decision after reconnaissance during implementation.

**Rationale**: 71 call sites is the biggest migration risk in this change. A gradual approach (keep the helper as an adapter) allows staged rollout and rollback if a test breaks.

**Builder signature and shape**: Follow `LaunchWindowViewModelBuilder` exactly:
- Nullable `Mock<T>?` fields for every dependency.
- `With*` methods returning `this`.
- `Build()` returns `AgentManagementViewModel`.
- `BuildWithMocks()` returns `(AgentManagementViewModel ViewModel, AgentMocks Mocks)` where `AgentMocks` is a nested record/class exposing the mocks.
- `private Compose()` method builds the VM and mocks once.

**Sensible defaults** for every constructor dependency:
- `IPluginCatalog`: `GetAllPlugins` returns `[]`, `GetInvalidPlugins` returns `[]` (loose mock).
- `IAgentDetectionService`: `IsInstalledInSystem` returns `true` (loose mock).
- `IAgentVersionService`: `GetVersionAsync` returns `"1.0"` (loose mock).
- `IAgentUpdateAvailabilityService`: loose mock with no setups (tests override per scenario).
- `IAgentProbeService`: loose mock with no setups.
- `IAgentUpdateService`: loose mock with no setups.
- `IExternalLauncher`: loose mock with no setups.
- `AgentProcessMonitor`: built exactly as `CreateViewModel` does today (over a `ProcessOrchestrator` with a defaulted `IAgentProcessInspector` returning `[]`).
- `IApplicationOperationLifetime`: `new InlineOperationLifetime()` (the shared helper).
- `ILogger<AgentManagementViewModel>`: `NullLogger<AgentManagementViewModel>.Instance`.

**Fluent configuration**:
- `WithPlugins(params Plugin[])`: sets the plugin array.
- `WithInvalidPlugins(params InvalidPluginEntry[])`: sets the invalid-plugin array.
- `WithDetection(...)`: overrides the detection mock.
- `WithVersions(...)`: overrides the version mock.
- `WithProbeService(...)`: overrides the probe mock.
- `WithUpdateService(...)`: overrides the update mock.
- `WithAvailability(...)`: overrides the availability mock.
- `WithLauncher(...)`: overrides the launcher mock.
- `WithProcessInspector(...)`: overrides the process inspector (the monitor construction adapts).
- `WithOperationLifetime(...)`: overrides the operation lifetime.
- `WithLogger(...)`: overrides the logger.
- Gating hooks (probe/update gates): inspect how `GatedProcesses` and the `TaskCompletionSource<T>` patterns are used in the existing tests, then provide a fluent API that matches (e.g., `WithGatedProbe(Func<Task<AgentProbeResult>>)` or a `ManualResetEventSlim`-based hook). The exact shape deferred to implementation.

**Migration verification**: After migration, run `dotnet test CLIHub.sln` and confirm the test count stays at 1,305 and 0 failures. If any test fails, investigate whether the builder's default differs from `CreateViewModel`'s default and adjust.

**Helper deletion rule**: `CreateViewModel` (lines 39-94) is a deletion candidate ONLY if all 71 calls migrate cleanly and grep shows zero remaining references. The test-data helpers (`Launchable` at line 21, `Processes` ×2 at lines 96 and 103, `GatedProcesses` at line 108, `Detection` at line 123, `Versions` at line 130) are deleted ONLY if a grep proves zero remaining references in the file after builder migration. These helpers have many direct test-data construction call sites (97 `Launchable`, 18 `Processes`, 2 `GatedProcesses`, 71 `Detection`, 66 `Versions`) and are likely still used; delete only if evidence confirms otherwise.

### Decision 6: SettingsViewModelFixture AppPreferences factory

**Chosen**: Add `internal static AppPreferences CreatePreferences(string accentColor = AccentPalette.DefaultHex, RuntimeKind defaultRuntime = RuntimeKind.PowerShell, string hotkey = "Ctrl+Shift+A", PathDisplayStyles pathDisplayStyle = PathDisplayStyles.LeftTrimToken, bool checkForUpdatesOnStartup = true, bool startMinimized = true)` to `SettingsViewModelFixture`.

**Rationale**: The 5 repeated object initialisers (lines 89, 140, 171, 573, and the fixture default at 959-967) all set the same 6 properties but with different values. A factory with named parameters collapses them while preserving every test's exact values.

**Alternative rejected**: `WithPreferences(...)` method on the fixture → the fixture ctor already takes `accentColor` as a parameter; adding a second `WithPreferences` override would create two configuration paths for the same data.

**Alternative rejected**: Keep the repeated object initialisers → 5 copies of 6 properties (30 lines of duplication) and no enforcement that all 6 are set consistently.

**Fixture rename decision**: The existing `Fixture` class becomes `SettingsViewModelFixture`. The 51 construction sites change from `new Fixture(...)` to `new SettingsViewModelFixture(...)`.

**Alternative considered**: Expose a `using Fixture = CLIHub.Tests.Builders.SettingsViewModelFixture;` alias at the top of `SettingsViewModelApplicationTests.cs` so the call sites stay `new Fixture(...)` → more invasive (adds a line to every test file that uses it) and hides the real type name. Prefer the direct rename.

### Decision 7: Central Package Management (CPM)

**Chosen**: Add `tests/Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and the 6 shared package versions. Implement as the final stage with its own build+test gate so it is independently revertable.

**Rationale**: NuGet's CPM scoping rule: when no root `Directory.Packages.props` exists, it picks the nearest one walking up from each csproj. A `tests/Directory.Packages.props` affects only `tests/CLIHub.Tests`, `tests/CLIHub.Core.Tests`, and `tests/CLIHub.Core.Windows.Tests`, leaving the three production projects (`src/CLIHub`, `src/CLIHub.Core`, `src/CLIHub.Core.Windows`) untouched. The 18 duplicated lines (6 `PackageReference` × 3 projects) centralize to one file.

**Rejected alternative (a)**: Root `Directory.Packages.props` → would force edits to production projects; out of scope for a test-only change.

**Rejected alternative (c)**: Centralize `<Using Include="Xunit" />` / `<Using Include="System.IO" />` via `tests/Directory.Build.props` → MSBuild's inheritance rule: the first `Directory.Build.props` found walking up stops the search. A `tests/Directory.Build.props` would shadow the root `Directory.Build.props` and silently break `BaseOutputPath`/`BaseIntermediateOutputPath`/`Version`/`LangVersion` for test projects unless it explicitly re-imports `<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`. The value (centralizing 2 using lines × 3 projects = 6 lines) does not justify the shadowing risk and the need for explicit re-import logic. **Parked in `docs/backlog.md`** with trigger: "the next Directory.Build.props restructuring, a proposal for hierarchical Build.props inheritance, or a third duplicated using across all test projects."

### Decision 8: DeferredOperationLifetime is NOT shared

**Chosen**: `DeferredOperationLifetime` (non-blocking variant) stays in `AgentManagementViewModelTests.cs` or moves with its partial (if split places it in `.BusyState.cs`). It is NOT moved to `TestHelpers.cs`.

**Rationale**: `DeferredOperationLifetime` differs from `InlineOperationLifetime` by returning the task directly (non-blocking) instead of blocking on `GetAwaiter().GetResult()`. It is used only in 1 test file for non-blocking async scenarios. Sharing it with `InlineOperationLifetime` would suggest they are interchangeable variants, but they have opposite synchronization semantics. A misleading abstraction is worse than localized duplication.

## Open questions

None. All design decisions are made; implementation is mechanical.

## File changes

### Files created (3)
- `tests/CLIHub.Tests/Support/TestHelpers.cs` (new shared helpers)
- `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs` (new builder)
- `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs` (extracted fixture)
- `tests/Directory.Packages.props` (optional CPM stage)

### Files edited (73)
- 7 files with `RunOnSta` duplication (delete helper, add `using static`)
- 7 files with `FindRepositoryRoot` duplication (delete helper, add `using static`)
- 2 files with `InlineOperationLifetime` duplication (delete helper, add `using static`)
- 1 file split into 6 partials (`AgentManagementViewModelTests.cs` → `.Load.cs`, `.Detection.cs`, `.CardActions.cs`, `.UpdateAvailability.cs`, `.BusyState.cs`, `.Cancellation.cs`, `.Helpers.cs`)
- 51 files with `new Fixture(...)` → `new SettingsViewModelFixture(...)`
- 1 file with `AppPreferences` repeated literals (5 call sites collapse to factory)
- 3 test csprojs (remove duplicated `PackageReference` lines if CPM stage proceeds)

### Files verified no-op (2)
- `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` (already collapsed)
- `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` (uniqueness already present)

## Deviations from brief

None. All requirements implemented as specified.
