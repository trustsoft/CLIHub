# Tasks: test-support-consolidation

> **Execution order (decided during implementation):** Stage 0 → 1 → 2 → **4 → 3** → 5 → 6 → 7 → 8.
> The test-file split runs **before** the builder migration. Rationale: stages 3 and 4 both edit
> `AgentManagementViewModelTests.cs`, and the file is 2,347 lines. Splitting first makes each builder
> migration edit a 150-600 line concern file instead of repeatedly re-reading and re-editing one large
> file, which is both cheaper and safer to verify. Stage numbering is unchanged so the archive report
> still lines up. Stage 3's tasks that name `AgentManagementViewModelTests.cs` refer to the concern
> partials created by Stage 4; `CreateViewModel` lives in `.Helpers.cs`.

## Stage 0: Baseline snapshot (before any code change)

- [ ] 0.1 Capture the authoritative pre-change inventory, and keep it out of the commit (`artifacts/` is gitignored):
  - `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --list-tests` → 550 names
  - `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --list-tests` → 692 names
  - `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj --list-tests` → 63 names
  - Save each list sorted to a file, and record per-file `[Fact]`/`[Theory]`/`Assert.` counts for `AgentManagementViewModelTests.cs` (expect **66 / 3 / 389**) and `SettingsViewModelApplicationTests.cs`.
  - Verified baseline on 2026-10-30: `dotnet test CLIHub.sln` = 1,305 passed, 0 failed; `dotnet build CLIHub.sln` = 0 warnings, 0 errors; `openspec validate --all` = 30 passed, 0 failed (29 specs + this change).
- [ ] 0.2 Confirm a clean starting tree: `git status --porcelain` shows only `openspec/changes/test-support-consolidation/`.

## Stage 1: Shared test helpers

- [ ] 1.1 Create `tests/CLIHub.Tests/Support/TestHelpers.cs` with namespace `CLIHub.Tests.Support`, `internal static class TestHelpers`.
- [ ] 1.2 Add `internal static void RunOnSta(Action action, string message = "STA test failed.")` with the exact body from `SpacedPanelTests.cs:173-196` (Thread, SetApartmentState, Start, Join, throw XunitException on exception).
- [ ] 1.3 Add `private static readonly Lazy<string> RepositoryRoot = new(() => { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLIHub.sln"))) { directory = directory.Parent; } return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root."); });` and `internal static string FindRepositoryRoot() => RepositoryRoot.Value;`.
- [ ] 1.4 Add `internal sealed class InlineOperationLifetime : IApplicationOperationLifetime` with the exact body from `AgentManagementViewModelTests.cs:2324-2336`.
- [ ] 1.5 Add required usings to `TestHelpers.cs`: `CLIHub.Lifetime` only (System, System.IO, System.Threading are implicit via ImplicitUsings; fully qualify `Xunit.Sdk.XunitException` as the existing copies do).
- [ ] 1.6 Delete `RunOnSta` from `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173-196` and add `using static CLIHub.Tests.Support.TestHelpers;` inside the namespace after the existing usings.
- [ ] 1.7 Update all 9 call sites in `SpacedPanelTests.cs` from `RunOnSta(...)` to `RunOnSta(..., "STA layout test failed.")` (add the second argument to every invocation).
- [ ] 1.8 Delete `RunOnSta` from `tests/CLIHub.Tests/Controls/SummaryCardTests.cs:31` (find exact line), add `using static CLIHub.Tests.Support.TestHelpers;`, update all 2 call sites to `RunOnSta(..., "STA control test failed.")`.
- [ ] 1.9 Delete `RunOnSta` from `tests/CLIHub.Tests/Converters/PathToImageConverterTests.cs:92`, add `using static`, update all 2 call sites to `RunOnSta(..., "STA converter test failed.")`.
- [ ] 1.10 Delete `RunOnSta` from `tests/CLIHub.Tests/Converters/ResourceKeyToTemplateConverterTests.cs:104`, add `using static`, update all 7 call sites to `RunOnSta(..., "STA converter test failed.")`.
- [ ] 1.11 Delete `RunOnSta` from `tests/CLIHub.Tests/Imaging/LogoImageSourceCacheTests.cs:149`, add `using static`, update all 5 call sites to `RunOnSta(..., "STA image test failed.")`.
- [ ] 1.12 Delete `RunOnSta` from `tests/CLIHub.Tests/Settings/AccentApplierTests.cs:133`, add `using static`, update all 6 call sites to `RunOnSta(..., "STA accent applier test failed.")`.
- [ ] 1.13 Delete `RunOnSta` from `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs:73`, add `using static`, update all 2 call sites to `RunOnSta(..., "STA launch window test failed.")`.
- [ ] 1.14 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:170` (find exact line), add `using static CLIHub.Tests.Support.TestHelpers;` inside the namespace. No call-site changes (signature unchanged).
- [ ] 1.15 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs:299`, add `using static`. No call-site changes.
- [ ] 1.16 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs:129`, add `using static`. No call-site changes.
- [ ] 1.17 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:48`, add `using static`. No call-site changes.
- [ ] 1.18 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs:128`, add `using static`. No call-site changes.
- [ ] 1.19 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs:332`, add `using static`. No call-site changes.
- [ ] 1.20 Delete `FindRepositoryRoot` from `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs:161`, add `using static`. No call-site changes.
- [ ] 1.21 Delete `InlineOperationLifetime` from `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:2324-2336`, add `using static CLIHub.Tests.Support.TestHelpers;` inside the namespace. Update call sites to `new InlineOperationLifetime()` (no qualifier needed with static import).
- [ ] 1.22 Delete `InlineOperationLifetime` from `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs:210-222`, add `using static`. Update call sites to `new InlineOperationLifetime()`.
- [ ] 1.23 Grep verification: run `rg "RunOnSta\(" tests/CLIHub.Tests/` and assert every match has an explicit second argument (33 call sites total: 9+2+2+7+5+6+2). Zero call sites may fall back to the default message.
- [ ] 1.24 Run UTF-8 safety check: `git diff` and confirm no mojibake (`â€`, `Ã`, `ï»¿`, `U+FFFD`).
- [ ] 1.25 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 2: Settings fixture promotion

- [ ] 2.1 Create `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs` with namespace `CLIHub.Tests.Builders`.
- [ ] 2.2 Copy the entire `Fixture` class from `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:938-1000` to the new file, rename it to `internal sealed class SettingsViewModelFixture`, keep its exact body (constructor, properties, mocks, setup calls).
- [ ] 2.3 Add `internal static AppPreferences CreatePreferences(string accentColor = AccentPalette.DefaultHex, RuntimeKind defaultRuntime = RuntimeKind.PowerShell, string hotkey = "Ctrl+Shift+A", PathDisplayStyles pathDisplayStyle = PathDisplayStyles.LeftTrimToken, bool checkForUpdatesOnStartup = true, bool startMinimized = true) => new AppPreferences { DefaultRuntime = defaultRuntime, Hotkey = hotkey, PathDisplayStyle = pathDisplayStyle, CheckForUpdatesOnStartup = checkForUpdatesOnStartup, StartMinimized = startMinimized, AccentColor = accentColor };` to `SettingsViewModelFixture`.
- [ ] 2.4 In `SettingsViewModelFixture` constructor, replace the `Load()` setup's `new AppPreferences { ... }` with `CreatePreferences(accentColor: accentColor)`.
- [ ] 2.5 Delete the `Fixture` class from `SettingsViewModelApplicationTests.cs:938-1000`.
- [ ] 2.6 Add `using CLIHub.Tests.Builders;` to `SettingsViewModelApplicationTests.cs` (inside the namespace, ordered per `.editorconfig`).
- [ ] 2.7 Replace all 51 `new Fixture(...)` call sites in `SettingsViewModelApplicationTests.cs` with `new SettingsViewModelFixture(...)` (no semantic changes, just the rename).
- [ ] 2.8 Replace the 4 repeated `new AppPreferences { DefaultRuntime = ..., Hotkey = ..., PathDisplayStyle = ..., CheckForUpdatesOnStartup = ..., StartMinimized = ..., AccentColor = ... }` object literals at lines 89, 140, 171, 573 with `SettingsViewModelFixture.CreatePreferences(accentColor: ..., defaultRuntime: ...)` calls, preserving each site's exact property values.
- [ ] 2.9 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [ ] 2.10 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 3: Agent view-model builder

- [ ] 3.1 Create `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs` with namespace `CLIHub.Tests.Builders`, `public class AgentManagementViewModelBuilder`.
- [ ] 3.2 Add nullable `Mock<T>?` fields for all 10 constructor dependencies: `IPluginCatalog`, `IAgentDetectionService`, `IAgentVersionService`, `IAgentUpdateAvailabilityService`, `IAgentProbeService`, `IAgentUpdateService`, `IExternalLauncher`, `IAgentProcessInspector`, `IApplicationOperationLifetime`, `ILogger<AgentManagementViewModel>`, plus `ILogger<AgentProcessMonitor>`.
- [ ] 3.3 Add `Plugin[]?` and `InvalidPluginEntry[]?` fields for data configuration.
- [ ] 3.4 Add `With*` methods for each field, returning `this`: `WithPlugins(params Plugin[])`, `WithInvalidPlugins(params InvalidPluginEntry[])`, `WithPluginCatalog(Mock<IPluginCatalog>)`, `WithDetectionService(Mock<IAgentDetectionService>)`, `WithVersionService(Mock<IAgentVersionService>)`, `WithAvailability(Mock<IAgentUpdateAvailabilityService>)`, `WithProbeService(Mock<IAgentProbeService>)`, `WithUpdateService(Mock<IAgentUpdateService>)`, `WithLauncher(Mock<IExternalLauncher>)`, `WithProcessInspector(Mock<IAgentProcessInspector>)`, `WithOperationLifetime(Mock<IApplicationOperationLifetime>)`, `WithLogger(ILogger<AgentManagementViewModel>)`, `WithProcessMonitorLogger(ILogger<AgentProcessMonitor>)`.
- [ ] 3.5 Add `public AgentManagementViewModel Build() => this.Compose().ViewModel;` and `public (AgentManagementViewModel ViewModel, AgentMocks Mocks) BuildWithMocks() => this.Compose();`.
- [ ] 3.6 Add `private (AgentManagementViewModel ViewModel, AgentMocks Mocks) Compose()` that constructs all mocks with defaults (catalog returns `this.plugins ?? []` and `this.invalidPlugins ?? []`; detection returns `true`; version returns `"1.0"`; others are loose mocks; process inspector returns `[]`; operation lifetime is `new InlineOperationLifetime()`; logger is `NullLogger<AgentManagementViewModel>.Instance`), builds `AgentProcessMonitor` exactly as `CreateViewModel` does (line 70-81), constructs the view model, and returns it with a `AgentMocks` container.
- [ ] 3.7 Add `public class AgentMocks { public required Mock<IPluginCatalog> PluginCatalog { get; init; } ... }` (all 10 mocks).
- [ ] 3.8 Add required usings: `Microsoft.Extensions.Logging.Abstractions`, `Moq`, `CLIHub.Core.Agents`, `CLIHub.Core.Infrastructure.Logging`, `CLIHub.Core.Infrastructure.Processes`, `CLIHub.Core.Models`, `CLIHub.Core.Plugins`, `CLIHub.Core.Processes`, `CLIHub.ViewModels`, `CLIHub.Lifetime`, `CLIHub.Launching`, `CLIHub.Tests.Support` (for `InlineOperationLifetime`).
- [ ] 3.9 In `AgentManagementViewModelTests.cs`, add `using CLIHub.Tests.Builders;` inside the namespace.
- [ ] 3.10 Migrate the first 10 `CreateViewModel(...)` calls to `new AgentManagementViewModelBuilder()...BuildWithMocks()` (or `.Build()` if mocks are not asserted). Verify tests stay green.
- [ ] 3.11 Migrate the remaining 61 `CreateViewModel(...)` calls. If a test breaks, investigate the default difference and adjust the builder or keep `CreateViewModel` as a thin adapter.
- [ ] 3.12 Decision point: delete `CreateViewModel` helper (lines 39-94) only if all 71 calls migrate cleanly and zero tests reference it. Keep as adapter otherwise. For the test-data helpers (`Launchable`, `Processes` ×2, `GatedProcesses`, `Detection`, `Versions`): grep each name and delete ONLY if zero remaining references exist in the file. `Detection()` is called once per `CreateViewModel` call (71 sites), so if builder migration succeeds it may be unused; `Launchable` (97 calls), `Processes` (18), `GatedProcesses` (2), `Versions` (66) are test-data factories likely still used directly by tests — delete only if grep proves otherwise.
- [ ] 3.13 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [ ] 3.14 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 4: Agent test file split

- [ ] 4.1 Enumerate the 69 test names from `AgentManagementViewModelTests.cs` (66 `[Fact]` + 3 `[Theory]`) and assign each to exactly one partial file per the placement rule below. Record the assignment in the task completion message.
- [ ] 4.2 Placement rule: `Load_*` (26) + `EditConfigCommand_*` (1) + `PopulateVersions_*` (1) → `.Load.cs` (**28** tests); `DetectCommand_*` (4) + `DetectCardCommand_*` (8) → `.Detection.cs` (**12** tests); `CheckUpdatesCommand_*` (7) + `CheckCardUpdatesCommand_*` (15) → `.UpdateAvailability.cs` (**22** tests); `UpdateCommand_*` (4) → `.CardActions.cs` (**4** tests); `Dispose_*` (3) → `.Cancellation.cs` (**3** tests). Sum = 28 + 12 + 22 + 4 + 3 = **69**, with no duplicates or missing names.
- [ ] 4.3 Create `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Helpers.cs` with `namespace CLIHub.Tests.ViewModels;` and `public partial class AgentManagementViewModelTests { }`. Move `Launchable` (line 21), `Processes` ×2 (lines 96, 103), `GatedProcesses` (line 108), `Detection` (line 123), `Versions` (line 130), `DeferredOperationLifetime` (line 2338), and `CreateViewModel` (lines 39-94, only if Stage 3 kept it as adapter) into the partial class body.
- [ ] 4.4 Create `AgentManagementViewModelTests.Load.cs` with `namespace CLIHub.Tests.ViewModels;` and `public partial class AgentManagementViewModelTests { }`. Move the 28 tests assigned to Load in task 4.2.
- [ ] 4.5 Create `AgentManagementViewModelTests.Detection.cs` and move the 12 tests assigned to Detection in task 4.2.
- [ ] 4.6 Create `AgentManagementViewModelTests.UpdateAvailability.cs` and move the 22 tests assigned to UpdateAvailability in task 4.2.
- [ ] 4.7 Create `AgentManagementViewModelTests.CardActions.cs` and move the 4 tests assigned to CardActions in task 4.2.
- [ ] 4.8 Create `AgentManagementViewModelTests.Cancellation.cs` and move the 3 tests assigned to Cancellation in task 4.2.
- [ ] 4.9 In the original `AgentManagementViewModelTests.cs`, change `public class AgentManagementViewModelTests` to `public partial class AgentManagementViewModelTests` and verify the file now contains only the namespace, usings, and the partial class declaration (no test methods or helpers remain).
- [ ] 4.10 Verify that every test method from the original file is present exactly once across the 5 test partials plus the Helpers partial: 28 + 12 + 22 + 4 + 3 = **69**. Verify no `[Collection]` attributes were added.
- [ ] 4.11 Record per-file test counts in the task completion message and prove the sum is 69 with no duplicates. Cross-check against the unchanged `[Fact]`/`[Theory]` totals for this file (66 / 3) from task 0.1's snapshot.
- [ ] 4.12 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [ ] 4.13 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 5: Verified no-ops

- [ ] 5.1 Open `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` and verify lines 242-256: `Build()` returns `this.Compose().ViewModel`, `BuildWithMocks()` returns `this.Compose()`, both delegate to one `private Compose()`. Record evidence in the task completion message: "Verified: LaunchWindowViewModelBuilder lines 242-256 already collapsed to one Compose() method. No action needed."
- [ ] 5.2 Open `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` and verify lines 77-80: `Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRuntimeResolver))`, same for `ICommandLineBuilder`, `IAgentProcessFinder`, `IProcessOrchestrator`. Record evidence: "Verified: ServiceRegistrationTests.cs lines 77-80 already have uniqueness assertions. No action needed."
- [ ] 5.3 Run UTF-8 safety check: `git diff` and confirm no mojibake (should show no changes for this stage).
- [ ] 5.4 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 6: Optional CPM (independently revertable)

- [ ] 6.1 Create `tests/Directory.Packages.props` with `<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup>` and six `<PackageVersion Include="coverlet.collector" Version="10.1.0" />` entries (coverlet.collector 10.1.0, Microsoft.Extensions.DependencyInjection 10.0.12, Microsoft.NET.Test.Sdk 18.10.1, Moq 4.21.0, xunit 2.9.3, xunit.runner.visualstudio 4.0.0).
- [ ] 6.2 In `tests/CLIHub.Tests/CLIHub.Tests.csproj`, replace `<PackageReference Include="coverlet.collector" Version="10.1.0" />` with `<PackageReference Include="coverlet.collector" />` (remove `Version` attribute, keep `PrivateAssets` and other attributes). Repeat for the other 5 packages.
- [ ] 6.3 Repeat 6.2 for `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` and `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj`.
- [ ] 6.4 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [ ] 6.5 Verify: `dotnet restore` succeeds and reports CPM is enabled for the 3 test projects (check output for "Central Package Management" messages).
- [ ] 6.6 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 7: Backlog update for deferred using centralization

- [ ] 7.1 Add a new entry to `docs/backlog.md` under "Test suite" (or create the section if it does not exist, after "Agent tooling" or at the end). Title: "Centralize test project global usings via hierarchical Directory.Build.props".
- [ ] 7.2 Entry content: Severity: Low (6 duplicated lines, test-only). Evidence: `<Using Include="Xunit" />` and `<Using Include="System.IO" />` repeated in 3 test csprojs. Proposed remedy: add `tests/Directory.Build.props` with explicit re-import of root `Directory.Build.props` (via `<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`) to avoid shadowing the root file's `BaseOutputPath`/`BaseIntermediateOutputPath`/`Version`/`LangVersion`, then move the 2 using lines to the test-level file. Trigger: "the next Directory.Build.props restructuring, a proposal for hierarchical Build.props inheritance, or a third duplicated using across all test projects."
- [ ] 7.3 Mark portfolio item 7 ("Duplicated test helpers and no shared view-model builders") as done in `docs/backlog.md` using the established style: `- **[done 2026-10-30: test-support-consolidation]** 7. Duplicated test helpers and no shared view-model builders. <original description>`.
- [ ] 7.4 Run UTF-8 safety check: `git diff` on `docs/backlog.md` and confirm no mojibake.
- [ ] 7.5 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (expect 1,305 passed, 0 failed).

## Stage 8: Final verification

- [ ] 8.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings, 0 errors.
- [ ] 8.2 Run `dotnet test CLIHub.sln` and confirm exactly 1,305 passed, 0 failed (692 Core, 63 Core.Windows, 550 CLIHub.Tests).
- [ ] 8.3 Re-run the three `dotnet test ... --list-tests` commands and diff the sorted name lists against the Stage 0 baseline (`artifacts/verify/tests-baseline-*.txt` or the copies made in task 0.1). Expect zero changed names — the split is a file reorganization, not a rename. A count match alone is not sufficient evidence.
- [ ] 8.4 Run `openspec validate --all` and confirm 30 passed, 0 failed (29 specs + 1 change while the change directory exists; will be 29 specs after archive).
- [ ] 8.5 Run `git diff --stat` and `git status --porcelain` and assert nothing under `src/` is modified and no production project file changed (only `tests/`, `docs/backlog.md`, and `openspec/changes/test-support-consolidation/` should appear).
- [ ] 8.6 Run `git diff --check` and confirm no whitespace issues.
- [ ] 8.7 After Stage 6 (CPM) completion, verify build output still lands under `artifacts\<Project>\` — the real path is `artifacts\CLIHub.Tests\Debug\net10.0-windows\CLIHub.Tests.dll` (and `artifacts\CLIHub.Core.Tests\`, `artifacts\CLIHub.Core.Windows.Tests\`). Confirms the root `Directory.Build.props` `BaseOutputPath` still applies and `tests/Directory.Packages.props` did not shadow it. Note `artifacts\` is gitignored, so check the build output rather than `git status`.
- [ ] 8.8 Run `graphify update .` to keep the knowledge graph current.
- [ ] 8.9 Report: test count unchanged at 1,305, test names unchanged (diff from the task 0.1 baseline), all stages green, CPM stage included/skipped, all tasks complete.

## User approval gate (MANDATORY)

**This gate MUST be cleared by the user before `git commit` or `openspec archive`.**

The user must explicitly approve commit and archive after reviewing the implementation and verification results. Per `AGENTS.md` Change, Commit, and Archive Policy (lines 63-77), approval applies only to this specific change and does not carry over to later work.
