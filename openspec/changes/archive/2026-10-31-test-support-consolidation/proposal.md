# Proposal: test-support-consolidation

## Why

The test suite has accumulated duplicated helpers and fixture code that makes test maintenance harder. Seven byte-identical copies of `FindRepositoryRoot` (24 call sites), seven copies of `RunOnSta` (7 call sites with distinct messages), two copies of `InlineOperationLifetime`, and large test files with repeated construction patterns increase the cost of changing test infrastructure or the corresponding production code. `AgentManagementViewModelTests.cs` is 2,347 lines with 71 `CreateViewModel(...)` calls constructing 70 mock instances each, and `SettingsViewModelApplicationTests.cs` repeats a 6-property `AppPreferences` object literal 5 times. These problems were catalogued in `docs/backlog.md` portfolio item 7.

This change consolidates duplicated test helpers into shared locations, introduces view-model builders for the two most complex test subjects, and splits the largest test file by concern using partial classes, improving maintainability without changing test behavior or production code.

## What changes

1. **Shared test helpers**: New `tests/CLIHub.Tests/Support/TestHelpers.cs` with `RunOnSta(Action action, string message = "STA test failed.")`, cached `FindRepositoryRoot()`, and `InlineOperationLifetime` class. All 16 duplicated copies are deleted and their call sites updated to use the shared implementations via `using static CLIHub.Tests.Support.TestHelpers;`.

2. **Agent view-model builder**: New `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs` with fluent configuration for plugins, detection results, versions, probe/update services, gating hooks, and preferences. The builder follows the exact style of the existing `LaunchWindowViewModelBuilder`: nullable `Mock<T>?` fields, `With*` methods returning `this`, and a `Compose()` method producing both the view model and a `AgentMocks` container. The 71 `CreateViewModel(...)` calls in `AgentManagementViewModelTests.cs` migrate to builder usage.

3. **Settings fixture promotion**: Move the existing `Fixture` class from `SettingsViewModelApplicationTests.cs` to `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs` and add an `AppPreferences` factory to collapse the 5 repeated 6-property object literals.

4. **Agent test file split**: Split `AgentManagementViewModelTests.cs` into 6 partial classes by concern (Load/DefaultAgent, Detection, CardActions, UpdateAvailability, BusyState, Cancellation), keeping one test class to preserve xUnit's serial collection scheduling and avoid flake risk. Shared helpers (`Launchable`, `Processes`, `GatedProcesses`, `DeferredOperationLifetime`) stay in one partial.

5. **Launch builder verification**: Verify that `LaunchWindowViewModelBuilder.Build()` and `BuildWithMocks()` already delegate to one `Compose()` method (requirement already satisfied at line 242-256).

6. **Composition uniqueness verification**: Verify that `ServiceRegistrationTests.cs:77-80` already has `Assert.Single` guards for the four process-registration types (requirement already satisfied).

7. **Optional test centralization**: Decision on `tests/Directory.Packages.props` for centralizing PackageReferences and global usings (see Design decision below).

## Impact

- **Test files**: 16 files lose a duplicated helper; `AgentManagementViewModelTests.cs` (2,347 lines, 69 test methods) becomes 6 partial-class files; `SettingsViewModelApplicationTests.cs` loses its 51-site nested `Fixture`; 2 new builders plus 1 new support file; 51 `new Fixture(...)` sites and 71 `CreateViewModel(...)` sites migrate mechanically; 3 test csprojs shrink to version-less `PackageReference` lines if the optional CPM stage lands. Nothing under `src/` is touched.
- **Production code**: Zero changes. No edits under `src/`. No changes to public APIs, composition, or behavior.
- **Test count**: Stays at 1,305 (692 + 63 + 550). Every test is preserved byte-for-byte except for mechanical arrange-block rewrites.
- **Behavior**: Zero user-visible changes. Zero production behavior changes. Test behavior unchanged (parallel scheduling, gating, timing-sensitive `SpinWait` loops all preserved).
- **OpenSpec**: `skip_specs: true` (test-only refactor, no capability changes).

## Non-goals

- Generic test-infrastructure abstractions beyond the immediate duplication (no xUnit theory data builders, no shared mocking framework, no assertion helpers).
- Parallel execution of agent tests (timing-sensitive, `ManualResetEventSlim`-gated tests stay serial by design).
- Migrating other test files to builders (only `AgentManagementViewModel` and `SettingsViewModel` fixture this change).
- Production code edits (this is test-only).
- Centralizing usings via `tests/Directory.Build.props` (rejected; see Design).
- Extending `IApplicationOperationLifetime` with test-only hooks or moving test fakes into production projects.

## Design decision: CPM for test packages

**Option (a)**: Add root `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` → **REJECTED**: this would force edits to production projects (`src/CLIHub.Core`, `src/CLIHub.Core.Windows`, `src/CLIHub`) and is out of scope for a test-only change.

**Option (b)**: Add `tests/Directory.Packages.props` only → **ACCEPTED WITH INDEPENDENCE GATE**: CPM scoped to the tests subtree (NuGet picks the nearest `Directory.Packages.props`; since no root file exists, a `tests/` one affects only test projects). The six shared `PackageReference` lines (coverlet.collector 10.1.0, Microsoft.Extensions.DependencyInjection 10.0.12, Microsoft.NET.Test.Sdk 18.10.1, Moq 4.21.0, xunit 2.9.3, xunit.runner.visualstudio 4.0.0) centralize to one file. Implemented as a separate, final, independently revertable stage with its own build+test gate.

**Option (c)**: Centralize `<Using Include="Xunit" />` / `<Using Include="System.IO" />` via `tests/Directory.Build.props` → **REJECTED AND PARKED**: `Directory.Build.props` stops at the first match walking up, so a `tests/Directory.Build.props` would shadow the root `Directory.Build.props` and silently break centralized `BaseOutputPath`/`BaseIntermediateOutputPath`/`Version`/`LangVersion` for test projects unless it re-imports the root file. The value (centralizing 2 using lines per project, 6 lines total) does not justify the shadowing risk. Parked in `docs/backlog.md` with a trigger for the next `Directory.Build.props` restructuring or a proposal for hierarchical inheritance.

## Dependencies

None. No dependencies on other changes or external work.
