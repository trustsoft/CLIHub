# Testing Conventions

This is the repo-wide testing guide. It describes the conventions the suite follows today, not a target
state. System boundaries are in [`docs/architecture.md`](architecture.md); the durable behavior contract the
tests verify is in [`openspec/specs/`](../openspec/specs/).

## Overview

Three test projects mirror the three source projects. The split is compiler-enforced: each test project
references only the source projects whose behavior it covers, so a test cannot reach a type the project under
test cannot see. Inside the projects the suite mixes strict and loose Moq mocks, uses hand-written fakes and
builders for reusable setup, and injects `TimeProvider` into Core services so time-dependent behavior can be
deterministic. The sections below state when each of those is the right choice.

## Test project mapping

| Test project | Target framework | Covers | Boundary |
|---|---|---|---|
| `tests/CLIHub.Core.Tests` | `net10.0` | `src/CLIHub.Core` | only `ProjectReference` is `CLIHub.Core.csproj`, so no WPF type is reachable |
| `tests/CLIHub.Core.Windows.Tests` | `net10.0-windows` | `src/CLIHub.Core`, `src/CLIHub.Core.Windows` | references those two projects only |
| `tests/CLIHub.Tests` | `net10.0-windows` (`UseWPF=true`) | all three source projects | references `CLIHub.Core`, `CLIHub.Core.Windows`, and the WPF application |

Pick the project from the code under test, not from convenience. A Core behavior belongs in
`tests/CLIHub.Core.Tests` even when the WPF project also consumes it, and a Windows adapter belongs in
`tests/CLIHub.Core.Windows.Tests`. The mapping is also test-enforced:
`CiWorkflow_RunsBothTestProjects` (`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:144`) fails
if `.github/workflows/ci.yml` stops naming all three test project files.

Shared test build configuration lives in `tests/`: `tests/Directory.Build.props` re-imports the repository-root
`Directory.Build.props` and adds a global `using Xunit;` for all three projects, and
`tests/Directory.Packages.props` centralizes the test-only NuGet versions. Add a new test package there, not in
an individual test project.

## Naming convention

Test methods follow `MethodOrScenario_Condition_ExpectedResult`, the rule stated in
[`AGENTS.md`](../AGENTS.md): the first segment names the method or the spec scenario under test, the second
names the condition, and the third names the observable result. Real examples:

- `AddProject_WhenFolderSelectionIsCancelled_UsesDialogServiceAndDoesNotMutateProjects`
  (`tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelDialogTests.cs:13`)
- `RunAsync_UnsupportedCommand_ReportsWithoutExecuting`
  (`tests/CLIHub.Tests/ViewModels/Launch/LaunchCommandCoordinatorTests.cs:50`)
- `Load_InitializesPluginsLoadsAndAppliesPreferences` (`tests/CLIHub.Tests/StartupStateLoaderTests.cs:12`)
- `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory`
  (`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:43`)
- `CoreProject_DoesNotReferenceWpf`
  (`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:26`)

Test classes are named `<TypeUnderTest>Tests`. Core tests sit in folders that mirror the source subsystems; the
Windows-adapter and application projects use a flat root for small areas plus a folder per subsystem where the
area is large. A test that pins a spec scenario keeps the scenario's wording in the condition and result
segments so the scenario can be found by name.

Style-rule violations, naming included, are reported as build warnings rather than build failures; see the
"Gate commands" section for what that does and does not mean.

## Arrange/Act/Assert and one behavior per test

Each test is one behavior: one arranged state, one action, and the assertions for that action only. When a
second behavior needs a different condition, write a second test rather than adding a second act. Use the
`// Arrange`, `// Act`, `// Assert` comments when a test has more than a couple of setup statements, as in
`tests/CLIHub.Tests/StartupStateLoaderTests.cs:14-43`.

Do:

```csharp
[Fact]
public void Load_ReturnsImmutableStartupState()
{
    // Arrange
    var preferences = new AppPreferences();
    var store = new Mock<IPreferencesStore>();
    store.Setup(x => x.Load()).Returns(preferences);
    var loader = new StartupStateLoader(/* ... */);

    // Act
    var result = loader.Load();

    // Assert
    Assert.Same(preferences, result.Preferences);
}
```

Do not:

```csharp
[Fact]
public void Load_DoesEverything()
{
    // loads, applies preferences, notifies, and checks three unrelated results in one test
}
```

An "And" in the result segment is fine when it names one observable outcome —
`Load_InitializesPluginsLoadsAndAppliesPreferences` is one call with several effects. What does not belong in
one test is a second act: a different condition that produces a different result is a different test.

## Mocking: strict, loose, and hand-written fakes

The suite does not use one mock behavior everywhere. The rule is:

- **Strict** (`new Mock<T>(MockBehavior.Strict)`) for a collaborator whose interactions the test asserts — call
  order, call counts, or argument values. An unconfigured call fails the test instead of silently returning a
  default. `tests/CLIHub.Tests/StartupStateLoaderTests.cs:16-18` is the canonical example: the three mocks are
  strict because the test sequences them with `MockSequence` and verifies `Times.Once` on each.
- **Loose** (`new Mock<T>()`) for a collaborator that is supplied but not asserted on. Unconfigured members
  return default values instead of failing, which keeps setup short. The second test in the same file
  (`tests/CLIHub.Tests/StartupStateLoaderTests.cs:50-52`) uses three loose mocks for exactly that reason.
- **`Mock.Of<T>()`** for an inert dependency that is passed through and never touched, for example
  `Mock.Of<IProjectRegistrySection>()` in `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:68`.

Both behaviors appear side by side in one fixture:
`tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:21-28` declares `Preferences`, `Startup`, and `Applier`
strict because the Settings tests verify exact calls against them, and `Updates`, `ReleaseNotes`,
`RuntimeExecutables`, `Scanner`, and `Dialogs` loose because they are incidental to most of those tests. Follow
that shape: strict where the assertion is about interaction, loose where the dependency only needs to exist.

**Prefer a hand-written fake** when the double has to hold state, evolve across calls, or behave like the real
implementation in a way a mock setup cannot express cleanly. Fakes live next to the tests that own them:
`tests/CLIHub.Core.Tests/Fakes/` (for example `FakeTimeProvider`, `FakeConfigurationRepository`,
`FakeInteractiveProcessRunner`) and the `Fake*.cs` files at the root of `tests/CLIHub.Core.Windows.Tests/` (for
example `FakeStartupRegistry`, `FakeWindowsRuntimeEnvironment`). `FakeTimeProvider` is the canonical example
because the system under test needs a clock that actually advances between calls.

Do not replace a fake with a mock just to reach `Verify`; assert the observable outcome instead. Conversely, do
not add a strict mock for a collaborator the test never verifies — it only makes unrelated future changes fail.

## Time and temp-directory patterns

### Time

Production code that needs the current time takes an optional `TimeProvider` and falls back to
`TimeProvider.System` (for example `src/CLIHub.Core/Agents/AgentDetectionService.cs:23-26`). Tests pass
`FakeTimeProvider` instead of sleeping:

- Type: `tests/CLIHub.Core.Tests/Fakes/FakeTimeProvider.cs`
- Namespace: `CLIHub.Core.Tests.Fakes`
- Shape: `sealed class FakeTimeProvider : TimeProvider`; the constructor takes the start
  `DateTimeOffset`, `GetUtcNow()` returns the current fake time, and `Advance(TimeSpan by)` moves it forward.

Typical use: construct it at a fixed instant, pass it to the service, and call
`Advance` to cross a cache TTL — `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:86` for the
construction and `tests/CLIHub.Core.Tests/Agents/AgentDetectionServiceTests.cs:118` for the advance.

`FakeTimeProvider` lives in `tests/CLIHub.Core.Tests` only; it is not shared into the other two test projects,
so a time-dependent test outside `CLIHub.Core.Tests` needs its own double or a reference change.

### Temporary directories

Filesystem-touching tests create a unique folder under the system temp path, use it, and delete it in a
`finally` block so a failed assertion does not leave state behind:

```csharp
var workingDirectory = Path.Combine(
    Path.GetTempPath(), "clihub launch dir " + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workingDirectory);

try
{
    // write inputs, run the adapter, assert on the files it produced
}
finally
{
    try { Directory.Delete(workingDirectory, recursive: true); } catch { }
}
```

The live example is `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:165-170,192-195`.
The `Guid` suffix is what makes the folder safe under parallel test execution; do not derive the folder name
from the test name alone.

## Shared test helpers

`tests/CLIHub.Tests/Support/TestHelpers.cs` is the single owner of the cross-cutting test helpers:

- `TestHelpers.RunOnSta(Action action, string message = "STA test failed.")`
  (`tests/CLIHub.Tests/Support/TestHelpers.cs:26`) runs the action on a thread set to `ApartmentState.STA` and
  rethrows any captured exception as `Xunit.Sdk.XunitException` with the supplied message. Use it for any test
  that constructs or touches WPF types.
- `TestHelpers.FindRepositoryRoot()` (`:55`, backed by the lazy `RepositoryRoot` field at `:10-19`) walks up from
  `AppContext.BaseDirectory` until it finds `CLIHub.sln`. Use it instead of hard-coded absolute paths when a test
  reads repository files.
- `TestHelpers.InlineOperationLifetime` (`:61-73`) is an `IApplicationOperationLifetime` that runs the operation
  synchronously and returns a completed task, so tests that schedule work through the operation lifetime settle
  deterministically.

There is no separate `StaRunner` type and no second copy of these helpers in the other test projects; add to
`TestHelpers` rather than creating a parallel helper.

Builders and fixtures in `tests/CLIHub.Tests/Builders/` cover the three large ViewModels:

| Type | Shape | Reach for it when |
|---|---|---|
| `LaunchWindowViewModelBuilder` | fluent builder with one `With...` method per dependency, `Build()` and `BuildWithMocks()` | a launch-window test needs the composed ViewModel plus the mocks it will assert on |
| `AgentManagementViewModelBuilder` | fluent builder with mock overrides and `WithPlugins`/`WithInvalidPlugins` data | an agent-management test needs probe, version, or update collaborators replaced |
| `SettingsViewModelFixture` | constructed fixture that exposes its mocks as properties | a Settings test needs the ViewModel and the strict `Preferences`/`Startup`/`Applier` assertions |

The full `LaunchWindowViewModelBuilder` API, including the before/after example, is documented next to the code
in [`tests/CLIHub.Tests/README.md`](../tests/CLIHub.Tests/README.md). Add a builder when the same complex setup
would otherwise be repeated; keep the defaults usable so a test only overrides what it asserts on.

## Skip semantics

Tests that need an optional piece of software on the machine use `[SkippableFact]` plus `Skip.If` so the result
is reported as *skipped*, not as *passed*:

```csharp
[SkippableFact]
public async Task LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory()
{
    if (resolvedPwsh is null)
    {
        Skip.If(true, "PowerShell 7 is not installed on this machine.");
        return;
    }

    // ...
}
```

Both live examples are in
`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:42-53` (PowerShell 7) and `:62-73` (Git
for Windows). The package is `Xunit.SkippableFact`, referenced by
`tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj:20` and versioned in
`tests/Directory.Packages.props:21`. Never use an early `return;` on its own for a machine-dependent test — a
silent pass hides a test that never ran.

`Skip.If` is not annotated `[DoesNotReturn]`, so the `return;` after it also narrows the nullable flow analysis
at the use site. Keep it unless a package upgrade makes it unreachable code.

There is no `xunit.runner.json` and no `.runsettings` file in the repository, so xUnit's default parallelism
applies to all three projects. Do not assume a serial or explicitly configured collection; make a test
independent instead of relying on execution order.

## Architecture-boundary tests

`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` asserts nine invariants over source text. They
run with the normal suite and fail on the change that breaks them, before review:

1. `CoreProject_DoesNotReferenceWpf` (`:26`) — `CLIHub.Core.csproj` mentions neither `UseWPF` nor
   `System.Windows`, and no `.cs` file under `src/CLIHub.Core` mentions `System.Windows`.
2. `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` (`:40`) — a file under `src/CLIHub` that mentions
   `System.Windows` must be in `Views/`, `Behaviors/`, `Controls/`, `Converters/`, `Interop/`, `Hotkeys/`,
   `ViewModels/`, `Tray/TrayMenuBuilder.cs`, or one of the allow-listed files at `:7-23`.
3. `DependencyRegistration_IsRestrictedToCompositionRoots` (`:54`) — `AddSingleton`, `AddTransient`, and
   `AddScoped` appear only in the three composition-root files listed at `:57-62`.
4. `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` (`:75`) — the application session,
   tray state projection, and tray command handlers are registered, and `MainWindow` is not.
5. `SingletonEventOwners_ImplementDisposable` (`:87`) — `ProjectPaneController`, `AgentPaneController`,
   `LaunchWindowViewModel`, and `UpdateControlViewModel` declare `IDisposable` on their class declaration line.
6. `ProductionSource_DoesNotReintroduceRetiredCompatibilityApis` (`:108`) — `IPluginManager`, `PluginManager`,
   and `IProcessLauncher` do not reappear anywhere under `src/`.
7. `ProjectReferences_KeepApplicationAboveCore` (`:122`) — `CLIHub.Core.csproj` does not reference
   `CLIHub.csproj`, and `CLIHub.csproj` does reference `CLIHub.Core.csproj`.
8. `WindowsProject_ReferencesCoreOnly` (`:133`) — `CLIHub.Core.Windows.csproj` references Core, does not
   reference the application, and targets `net10.0-windows`.
9. `CiWorkflow_RunsBothTestProjects` (`:144`) — `.github/workflows/ci.yml` names all three test project files.

If one of these fails, fix the code or the composition — do not edit the assertion to accommodate the change
unless the invariant itself is being retired by an approved change.

## Adding a test for a new spec scenario

1. Find the requirement and the scenario in `openspec/specs/<capability>/spec.md`. Read the scenario's `WHEN`
   and `THEN` lines; they define what the test must observe.
2. Choose the test project from the mapping above: Core logic in `tests/CLIHub.Core.Tests`, Windows adapters in
   `tests/CLIHub.Core.Windows.Tests`, ViewModels/composition/WPF in `tests/CLIHub.Tests`.
3. Put the test in the matching folder for its source subsystem — or at the project root where the project keeps
   small areas flat — in a `<TypeUnderTest>Tests` class.
4. Name the test with `MethodOrScenario_Condition_ExpectedResult`, echoing the scenario's condition and result.
5. Structure it as Arrange/Act/Assert and assert one behavior.
6. Mock per the mixed rule: strict for the assertions on interaction, loose or `Mock.Of<T>()` for supplied but
   unasserted collaborators, a fake when the double must hold state. Use a builder or fixture for a large
   ViewModel.
7. Run `dotnet test CLIHub.sln`, then `openspec validate --all`.

## Adding a spec scenario for new behavior

1. Decide whether the behavior is durable — something a future change could regress without a test noticing. If
   not, a test without a spec delta is enough.
2. Propose an OpenSpec change (`openspec/changes/<change-id>/`) using the `openspec-propose` skill. The change
   directory carries `proposal.md`, `design.md`, `tasks.md`, and, unless the change sets `skip_specs: true`, a
   delta under `openspec/changes/<change-id>/specs/<capability>/spec.md`.
3. Write the requirement and scenario in the delta with the shape already used across `openspec/specs/`:
   `### Requirement:` with `SHALL`/`MUST` wording, then `#### Scenario:` with `- **WHEN**` / `- **THEN**` /
   `- **AND**` bullets. A documentation-only or test-infrastructure-only change sets `skip_specs: true` in
   `.openspec.yaml` instead of writing a delta.
4. Implement the behavior and the test together (`openspec-apply-change` walks the tasks file). The test is what
   makes the scenario falsifiable; a scenario with no test is a claim, not a check.
5. Run `openspec validate --all`, then the gates below.

## Gate commands

Run from the repository root:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
```

CI runs the same work in a slightly different shape (`.github/workflows/ci.yml:26-34`): one Release build, then
three separate test invocations with `--no-build`, and no coverage collection:

```powershell
dotnet build CLIHub.sln -c Release
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj -c Release --no-build
dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj -c Release --no-build
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj -c Release --no-build
```

Use the per-project form when a failure needs triage, and `--filter "FullyQualifiedName~<TypeName>"` to run one
class. No test count, coverage percentage, or mock count is recorded in this document, because all three drift
with every change; the commands above produce the current numbers on demand.

### What a green build does and does not mean

`Directory.Build.props` sets `LangVersion=14.0` and `EnforceCodeStyleInBuild=true`, and `.editorconfig` pins the
style rules `IDE0009`, `IDE0011`, `IDE0065`, `IDE0161`, and `IDE1006` to warning severity. `TreatWarningsAsErrors`
is not set, so a style or naming violation is reported as a build warning and does not fail `dotnet build` or
`dotnet test`. The `code-style` spec (`openspec/specs/code-style/spec.md:24-29`) requires violations to be
*flagged*, not to break the build.

Read the warning list as part of the gate: a green build with new warnings is not a clean change just because
CI passes. The suite result is likewise independent of coverage — `coverlet.collector` is referenced by the test
projects but CI does not collect it, and no threshold compares the current suite against a baseline. Deleting a
test or letting one stop running produces the same green CI run, so the count in the `dotnet test` output is the
only signal, and the reviewer has to read it.
