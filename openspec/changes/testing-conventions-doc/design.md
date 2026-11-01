## Context

This is roadmap item C6 (`openspec/plans/2026-10-21-test-suite-hardening-roadmap.md:182-189`). The roadmap's
own figures are dated 2026-10-21 and the brief for this change explicitly flags them as suspect; `HEAD` at
planning time is `1c27693` (verified: `git log -1 --format=%H` printed `1c276932a8c51b64d8038a5b3dfeda0d762318f3`,
working tree clean per `git status --short`). Every claim below was re-verified against the tree at that
commit on 2026-11-01; `file:line` citations point at the state observed then.

## Verified current state

### Test project mapping (compiler-enforced split)

- `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj:4,22-24` — `TargetFramework=net10.0`; the only
  `ProjectReference` is `..\..\src\CLIHub.Core\CLIHub.Core.csproj`. It cannot see WPF types because the
  project it tests has none and it references nothing else.
- `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj:4,23-26` — `TargetFramework=net10.0-windows`;
  references `CLIHub.Core.csproj` and `CLIHub.Core.Windows.csproj`.
- `tests/CLIHub.Tests/CLIHub.Tests.csproj:4,8,23-27` — `TargetFramework=net10.0-windows`, `UseWPF=true`;
  references all three source projects (`CLIHub.Core`, `CLIHub.Core.Windows`, `CLIHub`).
- `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:144-151` (`CiWorkflow_RunsBothTestProjects`)
  asserts `.github/workflows/ci.yml` mentions all three `.csproj` names — the mapping is also
  test-enforced, not just a build fact.
- Current counts (this session, `dotnet build CLIHub.sln -c Release` then
  `dotnet test CLIHub.sln -c Release --no-build`, both run 2026-11-01): `CLIHub.Core.Tests` 703 passed,
  `CLIHub.Core.Windows.Tests` 63 passed, `CLIHub.Tests` 574 passed, 0 failed, 0 skipped, total 1,340. This
  **disagrees with both** the roadmap's 1,010 (2026-10-21) and `docs/backlog.md`'s 1,305 at `ccf4285`. The
  doc must not state a total count — see Decision 5.

### Naming rule

- `AGENTS.md:22-24` states the convention as `MethodOrScenario_Condition_ExpectedResult` (not the roadmap's
  `Method_Condition_ExpectedResult` — the roadmap's own wording differs from `AGENTS.md`). The doc uses the
  `AGENTS.md` wording since that file is the canonical rule source for this repository.
- `tests/CLIHub.Tests/README.md:124-129` already documents and exemplifies the same rule for one project.

### Strict mocks — deviation from the roadmap's claim

The roadmap (`openspec/plans/2026-10-21-test-suite-hardening-roadmap.md:52`) states "104 Moq usages, all
`MockBehavior.Strict`; zero `MockBehavior.Loose`." This is **false for the current tree** and was already
inaccurate for 2026-10-21 in spirit (it conflates "no explicit `MockBehavior.Loose` literal" with "no loose
mock," but Moq's parameterless `new Mock<T>()` constructor defaults to loose behavior without ever writing
the word `Loose`). Verified by a text scan across `tests/**/*.cs` at commit `1c27693` (two independent
scans, this session's and the orchestrator's, landed within a few constructions of each other because they
counted multi-line and target-typed `new(...)` forms slightly differently — the exact figure is not
load-bearing and is deliberately not repeated in `docs/testing.md`; see Decision 5):

- Roughly 470 `new Mock<T>(...)` constructions in total.
- About 100 of those pass `MockBehavior.Strict` explicitly (e.g.
  `tests/CLIHub.Tests/StartupStateLoaderTests.cs:16-18`,
  `tests/CLIHub.Tests/ViewModels/Updates/UpdateControlLifetimeTests.cs:15`).
  Zero constructions name `MockBehavior.Loose` literally anywhere in `tests/`.
- Roughly 370 rely on Moq's loose default via the parameterless `new Mock<T>()` — demonstrated in the same
  file, same test class: `tests/CLIHub.Tests/StartupStateLoaderTests.cs:50-52` (loose, no `.Raise`/strict
  sequencing needed) versus lines 16-18 of the same file (strict, because the test asserts call order with
  `MockSequence` and checks `Times.Once` on every collaborator).
- About 40 additional call sites use `Mock.Of<T>()` (also loose), e.g. for inert dependencies that are passed
  through but never asserted on (`tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:68`).
- `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:21-28` shows the actual pattern inside one fixture:
  `Preferences`, `Startup`, `Applier` are strict (the test suite verifies exact calls against them); `Updates`,
  `ReleaseNotes`, `RuntimeExecutables`, `Scanner`, `Dialogs` are loose (constructed with bare `new()`), and
  `Agents` defaults to a loose `Mock<IAgentManagementSection>()` unless the caller supplies one.
- `tests/CLIHub.Tests/README.md:151-170` already states the real rule in prose: "Only Mock What You Verify" —
  strict only for collaborators the test asserts against; this matches the counts above and is the
  conclusion `docs/testing.md` must reuse instead of the "zero loose" claim.

**Deviation recorded:** `docs/testing.md` states the mixed convention ("strict mocks for collaborators the
test verifies interactions with; loose `new Mock<T>()` or `Mock.Of<T>()` for incidental dependencies that
are supplied but not asserted on") rather than the roadmap's "100% strict, 0% loose" framing. No mock-behavior
count — exact or approximate — is written into `docs/testing.md` itself; the approximate figures above exist
only in this `design.md` as evidence for the deviation, because duplicating a count that already disagrees
with itself across two documents (roadmap vs. `tests/CLIHub.Tests/README.md`, which doesn't claim a mock
count at all) would add a third stale number to maintain. The concrete `file:line` examples carry the
evidence; the count does not need to.

### `FakeTimeProvider`

- `tests/CLIHub.Core.Tests/Fakes/FakeTimeProvider.cs:1-15` — `namespace CLIHub.Core.Tests.Fakes`, `sealed
  class FakeTimeProvider : TimeProvider`, constructor `FakeTimeProvider(DateTimeOffset start)`, overrides
  `GetUtcNow()`, exposes `Advance(TimeSpan by)`. This matches the brief's expectation exactly; no duplicate
  copy was found elsewhere in `tests/` (confirmed by `glob **/FakeTimeProvider.cs`, one result).
- There is no equivalent fake time provider under `tests/CLIHub.Tests/` or
  `tests/CLIHub.Core.Windows.Tests/` as of this session — the doc should say the type lives in
  `CLIHub.Core.Tests.Fakes` and is not (yet) shared across projects, rather than imply it is a
  cross-project shared helper.

### Shared STA harness and `TestHelpers`

- `tests/CLIHub.Tests/Support/TestHelpers.cs:8-74` (`internal static class TestHelpers`) owns three things:
  - `RunOnSta(Action action, string message = "STA test failed.")` (`:26-49`) — runs the action on a thread
    set to `ApartmentState.STA`, rethrowing any captured exception as `Xunit.Sdk.XunitException`.
  - `FindRepositoryRoot()` (`:55`, backed by the lazy `RepositoryRoot` field at `:10-19`) — walks up from
    `AppContext.BaseDirectory` until it finds `CLIHub.sln`.
  - `InlineOperationLifetime` (`:61-73`) — an `IApplicationOperationLifetime` that runs synchronously via
    `GetAwaiter().GetResult()`, used by tests that need a deterministic operation lifetime double.
  - This is the **only** copy; the roadmap's implication that duplicated copies "are really gone" is
    consistent with what is on disk — no second `RunOnSta`/`FindRepositoryRoot` was found elsewhere in
    `tests/`.
  - **Deviation from the brief's C4 framing:** the roadmap's C4 item (`tests-suite-hardening-roadmap.md:164-165`)
    describes extracting `RunOnSta` into a *new* `tests/CLIHub.Tests/Helpers/StaRunner.cs` as future work. That
    extraction has not happened; `RunOnSta` already lives in exactly one place
    (`tests/CLIHub.Tests/Support/TestHelpers.cs`), so the doc describes the current single-owner state and
    does not promise the not-yet-created `StaRunner.cs` path.

### Test builders

- `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs:23-36` — fluent builder for
  `AgentManagementViewModel`; fields for every collaborator mock plus `plugins`/`invalidPlugins` data,
  `With...` methods (e.g. `WithPlugins` at `:43-47`) that return `this`.
- `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:19-72` — not a fluent builder but a constructed
  fixture object: its constructor builds a full `SettingsViewModel` with sensible mock defaults and exposes
  the mocks as public properties (`Preferences`, `Startup`, `Applier`, ...) for the test to configure or
  assert against before/after construction.
- `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:25-40` — the most complete example; documented
  at length, with a before/after example, in `tests/CLIHub.Tests/README.md:17-94`.
- All three replace what `tests/CLIHub.Tests/README.md:28-33` calls "roughly sixty lines of [hand-wired]
  setup repeated verbatim in each test" that "broke whenever a constructor changed."

### Package versions and the central package management file

- `tests/Directory.Packages.props:1-22` centralizes NuGet versions for the three test projects only (no
  root-level `Directory.Packages.props` exists, so production projects keep inline versions — stated in the
  file's own header comment at `:3-9`). Verified versions: `xunit` 2.9.3, `xunit.runner.visualstudio` 4.0.0,
  `Xunit.SkippableFact` 1.5.85, `Moq` 4.21.0, `Microsoft.NET.Test.Sdk` 18.10.1, `coverlet.collector` 10.1.0,
  `Microsoft.Extensions.DependencyInjection` 10.0.12.
- `tests/Directory.Build.props:1-13` imports the repository-root `Directory.Build.props` explicitly (MSBuild
  stops walking up at the nearest `Directory.Build.props`, so this file re-imports the root one by path) and
  adds a global `using Xunit;` for all three test projects.
- Confirmed absent from the repository: `xunit.runner.json` (glob returned no matches) and any `.runsettings`
  file (glob returned no matches). The doc must not describe either as existing.

### CI — what actually runs today

- `.github/workflows/ci.yml:26-34` — one `dotnet build CLIHub.sln -c Release` step, then three separate
  `dotnet test <project>.csproj -c Release --no-build` invocations (Core.Tests, Core.Windows.Tests, Tests),
  no `--collect` flag, no coverage step, no trx upload, no artifact publish step in this file. This matches
  the brief's expectation exactly and contradicts the roadmap's target-state framing (which assumes coverage
  collection exists) — the doc describes this file's current content, not the roadmap's C1 target.

### Architecture-drift tests a contributor must not break

`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` has 9 fact methods, matching the roadmap's
"9 rules" claim, each asserting one architectural invariant over source text:

1. `CoreProject_DoesNotReferenceWpf` (`:26-37`) — `CLIHub.Core.csproj` and all `.cs` files under
   `src/CLIHub.Core` must not mention `UseWPF` or `System.Windows`.
2. `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` (`:39-51`) — any `src/CLIHub` file referencing
   `System.Windows` must be in an allow-listed folder or file (`Views/`, `Behaviors/`, `Controls/`,
   `Converters/`, `Interop/`, `Hotkeys/`, `ViewModels/`, or one of the explicit files at `:7-23`).
3. `DependencyRegistration_IsRestrictedToCompositionRoots` (`:53-72`) — `AddSingleton`/`AddTransient`/
   `AddScoped` calls may only appear in the three listed composition-root files (`:57-62`).
4. `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` (`:74-84`) — specific DI
   registrations must be present, and `MainWindow` must not be registered.
5. `SingletonEventOwners_ImplementDisposable` (`:86-105`) — four named view models/controllers must declare
   `IDisposable` in their class declaration line.
6. `ProductionSource_DoesNotReintroduceRetiredCompatibilityApis` (`:107-119`) — `IPluginManager`,
   `PluginManager`, `IProcessLauncher` must not appear anywhere under `src/`.
7. `ProjectReferences_KeepApplicationAboveCore` (`:121-130`) — `CLIHub.Core.csproj` must not reference
   `CLIHub.csproj`; `CLIHub.csproj` must reference `CLIHub.Core.csproj`.
8. `WindowsProject_ReferencesCoreOnly` (`:132-141`) — `CLIHub.Core.Windows.csproj` must reference
   `CLIHub.Core.csproj`, must not reference `CLIHub.csproj`, and must target `net10.0-windows`.
9. `CiWorkflow_RunsBothTestProjects` (`:143-151`) — `.github/workflows/ci.yml` must mention all three test
   `.csproj` names.

Confirmed by reading the full file end to end. No deviation here — the roadmap's "9 rules" claim holds.

### `.editorconfig` / `TreatWarningsAsErrors`

- `.editorconfig:1-9,27-31` — `root = true`; build-time severity pins
  (`dotnet_diagnostic.IDE0009/IDE0011/IDE0065/IDE0161/IDE1006.severity = warning`) are called out in a
  comment (`:24-26`) as deliberately pinned by rule ID because the IDE-only `option:severity` suffix form is
  not respected by `dotnet build` on the .NET 10 SDK.
- `Directory.Build.props:1-19` sets `LangVersion=14.0` and `EnforceCodeStyleInBuild=true` but does **not**
  set `TreatWarningsAsErrors`. Confirmed by reading the full file (19 lines) — no such property exists
  anywhere in it.
- Conclusion for the doc: style violations surface as build warnings today; a warning does not fail
  `dotnet build` or `dotnet test` because `TreatWarningsAsErrors` is unset. The doc must say this, not call
  style enforcement "machine-enforced" in the sense of "build-breaking" — it is machine-*reported*, and
  separately, `openspec/specs/code-style/spec.md:22-24` requires naming violations to be *flagged*, not to
  fail the build.

### Skip semantics already in force

- `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:42-53` and `:62-73` — two
  `[SkippableFact]` methods that call `Skip.If(true, "<reason>")` when an optional runtime
  (PowerShell 7, Git for Windows) is not installed on the current machine. This is exactly the pattern the
  brief describes as "already in force" — no early `return;` silent-pass exists in this file; `Skip.If`
  reports the test as skipped, not passed.
- `Xunit.SkippableFact` 1.5.85 is already a package reference in
  `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj:20` (confirmed) — the brief's framing of
  this as upcoming (roadmap C4) only applies to the *other* test files the roadmap names
  (`SpacedPanelTests.cs`, `PathToImageConverterTests.cs`, `LogoImageSourceCacheTests.cs`), which this change
  does not touch or claim are already skip-aware.

### Existing overlapping/stale documentation — `tests/CLIHub.Tests/README.md`

Not named in the brief, found during verification: `tests/CLIHub.Tests/README.md` (273 lines) already
documents, for `CLIHub.Tests` only: the naming rule (`:122-129`), AAA-adjacent "one behavior per test"
guidance (`:190-211`), "mock what you verify" (`:151-170`), hand-written fakes (`:172-184`), the
`LaunchWindowViewModelBuilder` API in detail (`:17-94`), directory layout (`:100-120`), and "Adding New
Tests" (`:236-250`). Its header (`:7-11`) states "**667 Core tests** ... **60 Windows adapter tests** ...
**555 WPF/application tests** ... **Total: 1,282 tests** (counts as of 2026-10-30; they drift with each
change)" — already wrong against this session's 703/63/574/1,340. See Decision 4 for the resulting scope
change.

## Decisions

### Decision 1: `docs/testing.md` location and link sites

**Chosen:** New file at `docs/testing.md` (flat, not under `docs/architecture/`), linked from two places:

- `docs/architecture.md` — add one line under "## Focused Documents" (`docs/architecture.md:78-87`), the same
  pattern every other focused doc already uses. Rejected alternative: a new "## Testing" section with inline
  content inside `architecture.md` itself — rejected because "Focused Documents" already exists as exactly
  this kind of pointer list and `architecture.md`'s own convention (established in the
  `2026-10-06-split-architecture-documentation` change, see its `design.md:3,23`) is "link, don't duplicate."
- `docs/project-context.md` — add one new row to the "## Documentation Routing" table
  (`docs/project-context.md:65-83`), not to "## Verification" (`:88-98`). The brief's wording ("link it from
  `docs/project-context.md` § Verification") does not match the file: "Verification" is three gate commands
  and nothing else (no table, no doc links); "Documentation Routing" is the table that maps a question to its
  canonical source, which is exactly what "where do I learn testing conventions" is. Adding a testing-doc
  pointer to "Verification" would be out of place structurally and would not match the row-based pattern
  every other "which doc answers X" mapping already uses.

**Deviation from the brief recorded:** the brief says "§ Verification"; the actual routing table is the
correct target. This is flagged explicitly per the brief's own instruction to record where its wording
doesn't match the file.

### Decision 2: `docs/testing.md` is repo-wide, not `CLIHub.Tests`-scoped

**Chosen:** `docs/testing.md` documents conventions across all three test projects (Core, Core.Windows,
`CLIHub.Tests`), not just the WPF/application project.

**Rationale:** the brief's required content list (project→test-project mapping, `FakeTimeProvider` which
lives in `CLIHub.Core.Tests`, the STA harness which lives in `tests/CLIHub.Tests/Support`) is inherently
cross-project. A `CLIHub.Tests`-only doc cannot answer "why does `CLIHub.Core.Tests` have no WPF reference"
or "where does `FakeTimeProvider` live."

### Decision 3: do not add `docs/testing.md` to `AGENTS.md`'s enumerated list

**Chosen:** leave `AGENTS.md`'s "Documentation and OpenSpec" section (`AGENTS.md:79-88`) unchanged.

**Rationale:** that list is deliberately short — `README.md`, `docs/project-context.md`,
`docs/architecture.md`, `openspec/specs/`, `docs/backlog.md`, `docs/adr/` — six canonical entry points, each
covering a distinct kind of content (user docs, compact agent context, architecture index, behavior
contract, deferred work, decision rationale). `docs/testing.md` is itself one level down from
`docs/architecture.md` in the same way `docs/architecture/startup.md` or `docs/architecture/plugins.md` are:
those focused topic docs are *not* individually listed in `AGENTS.md` either, precisely because
`docs/architecture.md` is the enumerated pointer and the topic docs hang off it. Adding `docs/testing.md`
directly to `AGENTS.md` would break that one-level-of-indirection pattern for no new benefit, since
`docs/project-context.md`'s routing table (which `AGENTS.md:3-5` already tells the reader to read first) is
reachable from `AGENTS.md` in one hop and will carry the new row from Decision 1.

### Decision 4: trim and redirect `tests/CLIHub.Tests/README.md`, do not leave it untouched

**Chosen (deviation from the brief, which only named `docs/architecture.md`, `docs/project-context.md`, and
`AGENTS.md` as link sites):** add a task to trim `tests/CLIHub.Tests/README.md` so it keeps only the
`CLIHub.Tests`-specific `LaunchWindowViewModelBuilder` walkthrough and its before/after example
(`:17-94`), replaces its naming/AAA/mock-guidance/fakes/"Adding New Tests" sections with a one-line pointer
to `docs/testing.md`, and removes the stale "1,282 tests (as of 2026-10-30)" count entirely rather than
updating it to a new number that will go stale again.

**Rationale:** this file was not named in the brief because the brief's author did not know it existed (it
surfaced only during this planning session's verification pass). Shipping `docs/testing.md` while leaving
`tests/CLIHub.Tests/README.md` in place, unedited, with an already-wrong test count and a second, divergent
statement of the same naming/mocking conventions would defeat the change's own purpose: a reader who opens
the `CLIHub.Tests` folder first (a very likely path) would hit the stale, narrower doc before the new
canonical one. Rejected alternative: leave it alone and let a future change deal with it — rejected because
"narrowly scoped" in the brief's own non-goals is about not doing C1-C5's work, not about leaving a
freshly-contradicted document in the same commit that claims to fix exactly this problem.

### Decision 5: do not state a global test count, coverage percentage, or Moq-strict percentage in `docs/testing.md`

**Chosen:** the document describes conventions qualitatively (project→project mapping, naming rule, AAA,
mixed strict/loose mock pattern with the concrete decision rule from Decision in "Strict mocks" above,
fakes, time/temp-dir patterns, STA harness, spec-scenario workflow, gate commands) and cites zero counts —
no test total, no coverage percentage, no naming-form census (578/231/0), and no Moq strict/loose count,
exact or approximate. The approximate mock-construction figures in "Strict mocks" above exist only in this
`design.md`, as evidence for the deviation from the roadmap's claim; they are not copied into
`docs/testing.md`.

**Rationale:** three independent sources already disagree about the test count at three different points in
time (roadmap: 1,010 at 2026-10-21; backlog: 1,305 at `ccf4285`; this session: 1,340 at `1c27693`), and
`tests/CLIHub.Tests/README.md` is proof that a count baked into a conventions doc goes stale within one
month. A convention doc should describe what does not change release to release; a headline count changes
every time a test is added. If the implementer wants a live count, `AGENTS.md:28-39`'s existing
`dotnet test CLIHub.sln` command already produces one on demand — the doc can point to that command instead
of embedding a snapshot. The same reasoning extends to the mock-construction count: two independent scans in
this review cycle (this session's ~469/~100/~369/~40 and the orchestrator's independent 466/100/0 named
`Loose`/366/40) already landed at slightly different totals for the same commit, which is itself proof the
number is not stable enough to print.

### Decision 7: delete "## Future Improvements" from `tests/CLIHub.Tests/README.md`; relocate its content to `docs/backlog.md`, do not drop it silently

**Chosen (orchestrator decision, resolving former open question 1):** delete the README's "## Future
Improvements" section (`:252-267`) as part of the Decision 4 trim, and add one new entry to `docs/backlog.md`
under "## Test suite" relocating its content, titled roughly "Speculative test-infrastructure ideas from
`tests/CLIHub.Tests/README.md`", severity Low, evidence citing `tests/CLIHub.Tests/README.md:252-267` at
`1c27693`. The entry preserves: more builders for `ProjectPaneController`/`AgentPaneController`, AutoFixture
integration, snapshot testing, BenchmarkDotNet performance benchmarks, and cross-layer integration tests. It
does **not** preserve the README's `SettingsViewModelBuilder` idea, because `SettingsViewModelFixture`
(`tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs:19-72`, already documented above) already serves
that need — carrying it forward as an open idea would misrepresent something already done as something still
missing. Trigger: "a contributor wants a builder, UI-state verification, or performance measurement that the
current suite does not provide."

**Rationale:** "## Future Improvements" lists speculative ideas, not conventions — it does not belong in
either `docs/testing.md` (a conventions doc) or the trimmed `README.md` (which becomes a focused
`LaunchWindowViewModelBuilder` walkthrough per Decision 4). Deleting it outright without a destination would
silently drop five ideas a prior author recorded; `docs/backlog.md` is the repository's existing "parked
findings and deferred work" ledger (`docs/backlog.md:1-5`) and is the correct destination, not a new location
invented for this change. This is a documentation relocation, planned here as an implementation task (see
`tasks.md` Stage 2) — `docs/backlog.md` itself is not edited by this planning session.

### Decision 6: outline order for `docs/testing.md`

**Chosen order and one-line intent per section:**

1. **Overview** — one paragraph: three test projects, why the split exists, strict/loose mock mix, fakes,
   injected time, one link each to `docs/architecture.md` and `openspec/specs/`.
2. **Test project mapping** — a small table: project, target framework, what it tests, why (compiler-enforced
   boundary via `ProjectReference`).
3. **Naming convention** — the `MethodOrScenario_Condition_ExpectedResult` rule plus 2-3 real examples pulled
   from the tree. No per-form census (the roadmap's 578/231/0 full-form/two-segment/violation split is
   omitted entirely, not recomputed — see Decision 9, resolving former open question 2) and no "two-segment
   form is acceptable" nuance, since that nuance only matters if the census is stated.
4. **Arrange/Act/Assert and one behavior per test** — the convention plus a short do/don't pair.
5. **Mocking: strict vs. loose vs. hand-written fakes** — the actual mixed rule from the "Strict mocks"
   section above: strict when the test verifies interaction, loose/`Mock.Of<T>()` when the dependency is
   supplied but not asserted on, a hand-written fake when the system under test needs stateful behavior a
   mock can't express cleanly (point at `FakeTimeProvider` as the canonical fake example).
6. **Time and temp-directory patterns** — `FakeTimeProvider` (exact type/namespace/members), and a short note
   on the temp-directory pattern used by filesystem-touching tests (unique `Path.GetTempPath()` subfolder,
   `try/finally` cleanup) with one citation (e.g.
   `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:166-169,192-195`).
7. **Shared test helpers** — `TestHelpers.RunOnSta`, `FindRepositoryRoot`, `InlineOperationLifetime`; the
   three builders/fixtures (`AgentManagementViewModelBuilder`, `SettingsViewModelFixture`,
   `LaunchWindowViewModelBuilder`) and when to reach for each; point to
   `tests/CLIHub.Tests/README.md` for the full `LaunchWindowViewModelBuilder` API after Decision 4's trim.
8. **Skip semantics** — `[SkippableFact]` + `Skip.If` for machine-dependent tests, with the existing
   `WindowsInteractiveProcessRunnerTests.cs` citation; state plainly that `xunit.runner.json` does not exist
   yet (do not describe parallelism configuration that isn't there).
9. **Architecture-boundary tests** — list the 9 invariants in `ArchitectureBoundaryTests.cs` at a summary
   level (one line each) so a contributor knows which changes will be caught before they try something that
   breaks one.
10. **Adding a test for a new spec scenario** — a short numbered workflow: find the scenario in
    `openspec/specs/<capability>/spec.md`, pick the test project per the mapping, name the test after the
    scenario, use AAA, use the right mock strictness.
11. **Adding a spec scenario for new behavior** — the inverse workflow: write the scenario first (Given/When
    /Then shape already used across `openspec/specs/`), then the test, referencing the OpenSpec propose/apply
    skills if useful context (`openspec-propose`, `openspec-apply-change`) without duplicating their content.
12. **Gate commands** — `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all` from
    `AGENTS.md:30-38`, plus the three per-project `dotnet test <project>.csproj -c Release --no-build`
    invocations CI actually runs (`.github/workflows/ci.yml:29-34`). State the `TreatWarningsAsErrors`/style
    posture from "Decisions" above as a fact, not a target.

This outline does not mention roadmap items C1-C5 by id anywhere (resolving former open question 3 — see
Decision 9): the document describes the current state only, and roadmap status belongs in the roadmap, not
in a conventions doc that would otherwise need an edit the moment any of C1-C5 lands.

**Rationale for this order:** overview and mapping first (orientation), then the rules a new test must follow
(naming, AAA, mocking, fakes/time/helpers) before the workflow sections (how to add a test, how to add a
scenario), ending on the gates a contributor runs before calling the work done. This mirrors the
narrative order of `tests/CLIHub.Tests/README.md` itself (overview → builders → organization → naming →
best practices → running tests → adding new tests), which is already a reasonable reading order for this
kind of document.

### Decision 8: the roadmap "Delivered" marker is required, with exact wording

**Chosen (orchestrator decision):** `tasks.md` Stage 3 requires — not optionally offers — adding one line
next to the C6 entry in `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md` §5 (around line 182-189):

```
- **Delivered:** `testing-conventions-doc` (2026-11-01) — `docs/testing.md`; roadmap items C1-C5 remain open.
```

**Rationale:** the roadmap is navigation, not a validated contract (`:5`), but it is the single place a
future reader checks to see what from this sequence has shipped; leaving C6 marked only "draft" after this
change lands would make the roadmap actively misleading about its own most recently completed item. Making
it required (not optional, as this plan's first draft proposed) removes the ambiguity of "the implementer
can skip it" — it is now a fixed Stage 3 task with exact wording, so there is nothing left to decide at
implementation time.

### Decision 9: replace unresolved open questions with orchestrator resolutions

**Chosen:** the three open questions raised in this change's first draft (Future Improvements disposition;
naming-form census; C1-C5 status note inside `docs/testing.md`) were put to the orchestrator and resolved —
see Decision 7 (Future Improvements → relocated to `docs/backlog.md`, `SettingsViewModelBuilder` excluded as
already served), Decision 6's "Naming convention" section item 3 (naming-form census omitted, not
recomputed), and Decision 6's closing note after item 12 (no C1-C5 status note in `docs/testing.md`). The
former "## Open Questions" section is removed; nothing remains open for the implementer to re-decide on
these three points.

## Alternatives rejected

- **Nest `docs/testing.md` under `docs/architecture/`** (alongside `startup.md`, `configuration.md`, etc.).
  Rejected: those topic docs are architecture topics; testing conventions are a different axis (process/
  quality, not system structure), and the brief and the roadmap both refer to `docs/testing.md` as a
  top-level path, not `docs/architecture/testing.md`.
- **Fold `tests/CLIHub.Tests/README.md` entirely into `docs/testing.md` and delete the former.** Rejected:
  the `LaunchWindowViewModelBuilder` walkthrough (with its before/after code sample) is detailed enough that
  it would unbalance a repo-wide conventions doc if inlined, and keeping it next to the code it documents
  (inside `tests/CLIHub.Tests/`) matches the general principle that implementation-adjacent detail lives
  near the implementation while cross-cutting convention lives in `docs/`.
- **State a coverage percentage or test count "as of this change" with a one-line staleness disclaimer** (the
  pattern `tests/CLIHub.Tests/README.md:11` already tried). Rejected: that disclaimer already exists in the
  README and the number is stale anyway one month later; a disclaimer does not prevent staleness, it only
  excuses it after the fact.

## Non-goals (restated from `proposal.md` for traceability)

Covered in `proposal.md`; not repeated here. See that file's "Non-Goals" section for the C1-C5 boundary.

## Open questions

None remaining. All three questions raised in this change's first draft were resolved by the orchestrator
(Decision 9):

1. **Resolved by the orchestrator:** `tests/CLIHub.Tests/README.md`'s "## Future Improvements" section is
   deleted and relocated to a new `docs/backlog.md` entry — see Decision 7 for the exact content and
   exclusions.
2. **Resolved by the orchestrator:** the naming-rule form census (578 full-form / 231 two-segment /
   0 violations) is omitted from `docs/testing.md` entirely, not recomputed — see Decision 6's "Naming
   convention" outline item.
3. **Resolved by the orchestrator:** `docs/testing.md` does not mention roadmap items C1-C5 by id anywhere —
   see Decision 6's closing note after outline item 12.
