# Roadmap — hardening the CLIHub test suite

- **Date:** 2026-10-21
- **Status:** draft (baseline measured; change sequence not yet approved)
- **Purpose:** navigation document. Sets the target state and the **sequence of OpenSpec changes** for the test suite. It is not part of the OpenSpec contract and is not validated by `openspec validate`.
- **Language:** English, per `openspec/config.yaml` (`All artifacts must be written in en`).
- **Baseline commit:** `master` at plan authoring time; working tree carries unrelated edits to `.opencode/scripts/check-model-policy.mjs`, `docs/agent-model-policy.json`, `docs/agent-model-policy.md` (not touched by this roadmap).

---

## 1. Objective

Turn the test suite from **green-but-opaque** into **green-and-falsifiable**: every claim in `openspec/specs/` that can be checked automatically is checked, coverage is measured on every CI run, timing-dependent tests are deterministic, and a test that cannot run on the current machine is reported as *skipped* rather than *passed*.

The suite is already structurally sound — three test projects mirroring the three source boundaries, strict mocks, hand-written fakes, injected time. The gaps are in **signal**, not in structure: coverage is never collected, some spec scenarios have no test at all, and the few fragile tests fail intermittently with no CI triage trail.

## 2. Measured baseline

Reproduce with the commands in §8. All figures below were measured on 2026-10-21, not estimated.

### 2.1 Suite state

| Test project | Target framework | Tests | Result | Duration |
|---|---|---|---|---|
| `CLIHub.Core.Tests` | `net10.0` | 605 | passed | 6 s |
| `CLIHub.Core.Windows.Tests` | `net10.0-windows` | 52 | passed | 2 s |
| `CLIHub.Tests` | `net10.0-windows` | 353 | passed | 382 ms |
| **Total** | | **1010** | **passed, 0 failed, 0 skipped** | ~9 s |

### 2.2 Line/branch coverage of the assembly under test

| Assembly | Tested by | Line | Branch |
|---|---|---|---|
| `CLIHub.Core` | `CLIHub.Core.Tests` | **87.2 %** | 75.8 % |
| `CLIHub.Core.Windows` | `CLIHub.Core.Windows.Tests` | **69.4 %** | 59.6 % |
| `CLIHub` (WPF app) | `CLIHub.Tests` | **61.5 %** | 47.6 % |

> Method note: a Cobertura report aggregates every referenced assembly, so the per-assembly rate must be read from the run that *targets* that assembly. A class that merely gets loaded by another project's tests shows a misleading 0 %. Figures above are the max across all three runs per class.

### 2.3 Classes at 0 % line coverage (43, merged across runs)

- **`CLIHub.Core` (5):** `Logging.PreferenceReader`, `Scanning.FileSystemRepositoryWatcher`, `Scanning.SystemScanTimer`, `Scanning.SystemScanTimerFactory`, `Infrastructure.FileSystem.DirectoryInitializer`
- **`CLIHub.Core.Windows` (2):** `SystemTheme`, `CurrentUserRegistryStartup`
- **`CLIHub` app (36):** `App`, `Program`, `Behaviors.ComboBoxBehaviors`, `Controls.NumericField`, `Controls.SectionHeader`, `Controls.SettingCard`, `Controls.SettingsGroup`, `Converters.IndexToDividerVisibilityConverter`, `Converters.PathDisplayConverter`, `Converters.UpperCaseTextConverter`, `Hotkeys.GlobalHotkeyService`, `Interop.DwmApi`, `Interop.User32`, `Interop.WindowPositioner`, `Launching.ExternalLauncher`, `Launching.ProjectDialogService`, `Launching.UserNotificationService`, `Settings.WpfAccentPreferenceTarget`, `Startup.WpfApplicationStartupUi`, `Themes.ThemeCatalog`, `Tray.TrayIconController`, `Tray.TrayMenuBuilder`, and all 13 `Views.*` code-behind classes.

### 2.4 Thin coverage (0 % < rate < 40 %)

`CLIHub.Updates.ReleaseNotesLauncher` 26.3 % · `CLIHub.Settings.SettingsLauncher` 33.3 % · `CLIHub.Core.Windows.WindowsRuntimeEnvironment` 34.7 % · `CLIHub.Startup.ApplicationHost` 37.8 %

### 2.5 Conventions already in force (keep them)

- **Strict mocks.** 104 Moq usages, all `MockBehavior.Strict`; zero `MockBehavior.Loose`. An unexpected collaborator call fails the test.
- **Injected time.** `FakeTimeProvider : TimeProvider` (`tests/CLIHub.Core.Tests/Fakes/FakeTimeProvider.cs`); production code depends on `TimeProvider`.
- **Naming.** 809 test methods; 578 use the full `Method_Condition_ExpectedResult` form, 231 use two segments, **0 violate the convention outright**.
- **Layering is compiler-enforced.** `CLIHub.Core.Tests` references only `CLIHub.Core`, so Core's WPF-independence is a build fact, not a convention.
- **Architecture drift is test-enforced.** `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` — 9 rules over source text.

## 3. Findings

Severity reflects impact on the suite's ability to catch regressions, not on product code.

### H1 — Coverage is collected by nobody

`coverlet.collector 10.1.0` is referenced in all three test projects, but `.github/workflows/ci.yml` never collects it: no `--collect:"XPlat Code Coverage"`, no `.runsettings`, no threshold, no artifact upload. The dependency is dead weight and there is **no coverage regression signal at all** — a class can lose 90 % of its tests and CI stays green.

*Verified fix path:* `dotnet test <proj> -c Release --no-build --collect:"XPlat Code Coverage" --results-directory <dir>` produces `coverage.cobertura.xml` (805 KB for `CLIHub.Core.Windows.Tests`). The collector works in the exact `--no-build` Release shape CI already uses.

### H2 — `code-style` claims "machine-enforced", but enforcement is advisory

`openspec/specs/code-style/spec.md` asserts that naming conventions, XML documentation, and using-directive ordering are machine-enforced. The mechanism exists — `.editorconfig` (126 lines, 7 naming rules, `IDE0009/0011/0065/0161/1006`) plus `EnforceCodeStyleInBuild=true` (`Directory.Build.props:18`) — but **`TreatWarningsAsErrors` is never set**. A violation produces a warning; the build stays green and CI passes. The spec's claim is false as CI currently stands.

*Verified:* `dotnet build CLIHub.sln -warnaserror` → **Build succeeded, 0 Warning(s), 0 Error(s)**. The repository is already compliant, so closing this gap is a one-line change with no cleanup cost.

### H3 — Spec scenarios with no automated verification

| Spec | Unverified scenario | Root cause |
|---|---|---|
| `launch-window-theme` | "Always on top", "Positioned on the pointer's monitor" | `Interop/WindowPositioner.cs` — 0 % coverage |
| `logging` | "Daily log rotation", "Framework noise filtering" | `LoggingSetupTests` covers level filtering and size-based rollover only |
| `settings-theme` | "Segmented selectors", "Themed footer", "Sidebar navigation styling", "Keyboard stepping", "Mouse wheel stepping" | XAML/visual |
| `main-window-layout` | "Uniform outer margins", "Rounded corners", "Hairline divider", "Pointer feedback" | XAML/visual |
| `code-style` | "Naming conventions are machine-enforced", "XML documentation comments are compiler-validated", "Using directives are grouped and sorted" | enforced by build warnings, not by a test — see H2 |
| `agent-update-detection` | release-request timeout degrades to `Unknown`; "no indicator for a missing installation" | no direct test (already in `docs/backlog.md:116-126`) |
| `release-pipeline` | "Green push to master", "Broken push to master" | CI behaviour, asserted only by string-matching `ci.yml` for csproj names |

### M1 — Integration tests silently pass when vacuous

`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:52` and `:72` `return;` early when Git Bash / pwsh cannot be resolved on the machine. xUnit reports those as **passed**, not skipped. The repository contains zero `[Fact(Skip=...)]` and the suite reports `Skipped: 0`, so on a CI agent lacking those runtimes the suite is green-but-meaningless with no signal. (Already parked in `docs/backlog.md:105-114`.)

### M2 — Real sleeps and timing windows

Confirmed sites, and the known cause of the unreproduced failure in `docs/backlog.md:209-220`:

| Site | Mechanism |
|---|---|
| `tests/CLIHub.Core.Tests/Infrastructure/Persistence/LogoCacheServiceTests.cs:173` | `Thread.Sleep(50)` then asserts `File.GetLastWriteTimeUtc` unchanged — filesystem timestamp-granularity race |
| `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:82` | `Thread.Sleep(10)` |
| `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:120-133` | races `Start()`'s background scan against `Stop()` (`docs/backlog.md:9-18`) |
| `tests/CLIHub.Core.Tests/Updates/UpdateCheckerTests.cs:119` | real `Task.Delay` |
| `tests/CLIHub.Core.Tests/Updates/UpdateDownloaderTests.cs:64`, `:182` | real `Task.Delay` |
| `tests/CLIHub.Core.Tests/Updates/UpdateServiceTests.cs:51`, `:136`, `:148` | real `Task.Delay` (50–180 ms) |
| `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:458` | `task.GetAwaiter().GetResult()` — sync-over-async, deadlock-prone |

### M3 — No parallelism policy

There is no `xunit.runner.json` and no `[Collection]` definitions, so xUnit defaults to parallel-by-class. Windows-global resources (named mutexes/pipes, `%APPDATA%`, the real registry) are isolated ad hoc — `SingleInstanceGuardTestIsolation` swaps in per-test mutex/pipe names, `FakeStartupRegistry` is in-memory — rather than by an explicit collection lock. The isolation works today but is convention, not structure.

### M4 — Duplicated test infrastructure

`RunOnSta` (an STA-thread harness) is copy-pasted into three files: `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173`, `tests/CLIHub.Tests/Converters/PathToImageConverterTests.cs:92`, `tests/CLIHub.Tests/Imaging/LogoImageSourceCacheTests.cs:149`.

### L1 — No testing-conventions documentation

`docs/` has no `testing.md`. The conventions that make this suite good (strict mocks, injected time, fake-based seams, `Method_Condition_ExpectedResult`) live only in `AGENTS.md` one line each and in the code itself. A contributor cannot learn the seams to reuse from the docs.

## 4. Target state

1. Every CI run publishes a Cobertura coverage report per test project, and a failing run identifies the failing test.
2. A build warning fails the build, so `code-style`'s "machine-enforced" claim is true.
3. No test depends on wall-clock timing; no test blocks on a thread-pool thread.
4. A test that cannot run on the current machine is reported as *skipped*.
5. Every spec scenario that is mechanically checkable has a test that checks it.
6. Coverage on `CLIHub` and `CLIHub.Core.Windows` rises from 61.5 % / 69.4 % toward Core's 87.2 %, measured per change.
7. The testing conventions are written down and linked from the docs index.

## 5. Change sequence

Six OpenSpec changes. Each is independently shippable and independently verifiable.

### C1 — `test-coverage-in-ci`

**Goal:** every CI run produces coverage and can triage a failure.

- **Scope:** `.github/workflows/ci.yml`; `tests/*/*.csproj` (keep `coverlet.collector`, add a pinned `coverlet.collector` version bump only if needed).
- **Concrete:** append `--collect:"XPlat Code Coverage" --results-directory artifacts/coverage/<project>` and `--logger trx --blame` to each of the three `dotnet test` steps; upload `artifacts/coverage/**` and `**/*.trx` via `actions/upload-artifact`. Keep the three separate `dotnet test` invocations — they map to the three boundaries and make per-assembly coverage attributable.
- **Spec delta:** MODIFIED `ci-build` — add a requirement *"Coverage and failure triage artifacts"* with scenarios for report publication and failing-test identification. **Preserve the existing scenario names** in "Build and test on pull requests" and "Build and test on master".
- **Non-goals:** no coverage *threshold* gate yet (trend the baseline first, then decide the number); no coverage collection in `release.yml`.
- **Verified:** the `--no-build --collect:"XPlat Code Coverage"` combination works in Release and emits `coverage.cobertura.xml`.

### C2 — `enforce-warnings-as-errors`

**Goal:** make `code-style`'s "machine-enforced" claim true.

- **Scope:** `Directory.Build.props` — add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- **Concrete:** one property. The repository already builds clean under `-warnaserror` (0 warnings), so no code cleanup is required.
- **Spec delta:** MODIFIED `code-style` — state that analyzer and naming violations fail the build. Preserve existing scenario names.
- **Non-goals:** enabling new analyzer rules; turning on rules that are not yet satisfied.
- **Risk:** a future SDK or analyzer update can introduce a new warning and break the build. Mitigate with a targeted `<WarningsNotAsErrors>` only if a specific rule proves noisy — and record the reason in the spec.

### C3 — `deterministic-test-timing`

**Goal:** remove wall-clock dependence from the suite.

- **Scope:** the seven sites in §3/M2 plus `AgentManagementViewModelTests.cs:458`.
- **Concrete:** give the affected production classes an injectable delay/abort seam (an internal `IDelay` port, mirroring the existing `TimeProvider` pattern), and replace `Thread.Sleep` / `Task.Delay` windows with a deterministic completion signal or a `FakeTimeProvider` advance. For `ProjectScanningService.Stop`, expose a deterministic "scan settled" signal rather than a stopwatch-bounded wait loop. Replace the `GetAwaiter().GetResult()` call with `await`.
- **Spec delta:** likely `skip_specs: true` — an internal refactor with no durable behavior change. If the `agent-update-detection` timeout scenario gains a direct test as a side effect, add that scenario to `agent-update-detection` instead.
- **Non-goals:** rewriting the update subsystem's cancellation model.
- **Risk:** the seams touch production code. Keep them internal; do not widen public API.

### C4 — `test-harness-hardening`

**Goal:** no silent passes, no duplicated harness, explicit parallelism, no sync-over-async.

- **Scope:** `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`; `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs`; `tests/CLIHub.Tests/Converters/PathToImageConverterTests.cs`; `tests/CLIHub.Tests/Imaging/LogoImageSourceCacheTests.cs`; new `tests/CLIHub.Tests/Helpers/StaRunner.cs`; new `xunit.runner.json`; new `[Collection("WindowsIntegration")]` grouping.
- **Concrete:** (a) convert the two early `return;` exits to a skippable fact so CI reports *skipped*, not *passed* — xunit 2.9.3 has no `Assert.Skip`, so this needs the `Xunit.SkippableFact` package; (b) extract `RunOnSta` into one shared helper; (c) add `xunit.runner.json` with an explicit `maxParallelThreads` and group the process-global tests (`SingleInstanceGuardTests`, `ApplicationHostTests`, `ApplicationLifetimeRegistrationTests`) into one non-parallel collection; (d) `await` instead of `GetAwaiter().GetResult()`.
- **Spec delta:** `skip_specs: true` — test-infrastructure only, no product behavior changes.
- **Non-goals:** adding a WPF UI-test harness (no `Dispatcher`-level window tests in this change).

### C5 — `close-coverage-gaps`

**Goal:** close the 0 % classes and the unverified spec scenarios.

- **Scope, in priority order:**
  1. **`CLIHub.Core` (5 classes):** `PreferenceReader`, `FileSystemRepositoryWatcher`, `SystemScanTimer`, `SystemScanTimerFactory`, `DirectoryInitializer`.
  2. **`CLIHub.Core.Windows` (2 classes):** `SystemTheme`, `CurrentUserRegistryStartup`, plus `WindowsRuntimeEnvironment` (34.7 %).
  3. **App, pure-logic classes (13):** `Themes.ThemeCatalog`, `Tray.TrayMenuBuilder`, `Behaviors.ComboBoxBehaviors`, `Controls.NumericField` / `SectionHeader` / `SettingCard` / `SettingsGroup`, `Converters.IndexToDividerVisibilityConverter` / `PathDisplayConverter` / `UpperCaseTextConverter`, `Updates.ReleaseNotesLauncher` (26.3 %), `Settings.SettingsLauncher` (33.3 %), `Startup.ApplicationHost` (37.8 %).
  4. **Spec traceability:** add scenarios to `launch-window-theme` ("Always on top", "Positioned on the pointer's monitor" — cover `WindowPositioner`), `logging` ("Daily log rotation", "Framework noise filtering"), `agent-update-detection` (timeout path, missing-installation indicator — already in `docs/backlog.md`), and `code-style` (a test that scans for undocumented public members, complementing C2's build-level enforcement).
- **Spec delta:** ADD scenarios to the four capabilities above; MODIFY `agent-update-detection` to name the two new scenarios.
- **Non-goals:** covering `Views.*` code-behind, `App`, `Program`, `Interop/User32`, `Interop/DwmApi`, `WpfApplicationStartupUi` — P/Invoke and WPF-lifecycle shells that would need a UI-test harness (explicitly out of scope in C4).
- **Dependency:** run **after C3** — new tests must not land on a flaky timing foundation; and **after C1** so the coverage delta is measurable.

### C6 — `testing-conventions-doc`

**Goal:** make the suite's good conventions learnable.

- **Scope:** new `docs/testing.md`; link it from `docs/architecture.md` and `docs/project-context.md` § Verification.
- **Content:** the project→test-project mapping; the `Method_Condition_ExpectedResult` naming rule; AAA and one-behavior-per-test; strict mocks and when to prefer a hand-written fake; the `FakeTimeProvider` and temp-directory patterns; the shared STA harness; how to add a test for a new spec scenario; how to add a spec scenario for new behavior; the gate commands.
- **Spec delta:** `skip_specs: true` — documentation only, no durable repository behavior.
- **Dependency:** last, so it documents the end state.

## 6. Sequencing

```
C1 test-coverage-in-ci ──────┐
C2 enforce-warnings-as-errors├──> C5 close-coverage-gaps ──> C6 testing-conventions-doc
C3 deterministic-test-timing ─┘
C4 test-harness-hardening ─────> (independent, parallelizable with C1/C2/C3)
```

- **Wave 1 (immediate value, low risk):** C1, C2, C4. Independent of each other; all three are low-risk and can ship in any order.
- **Wave 2 (production-code touch):** C3. Sequenced after Wave 1 so coverage collection is already measuring when the seams land.
- **Wave 3 (largest):** C5. Requires C1 (measurability) and C3 (stable foundation).
- **Wave 4:** C6.

## 7. Out of scope for this roadmap

- A **coverage threshold gate**. Trend the baseline first; a number chosen without a trend is a number that gets ratcheted down to green.
- **UI/E2E testing** (WPF `Dispatcher`, window automation, screenshot comparison). The `Views.*` code-behind and `Interop/*` classes will stay uncovered.
- **Migrating xUnit 2 → 3.** It would give `Assert.Skip` natively and remove the `Xunit.SkippableFact` dependency, but it is a version-bump migration across 1010 tests and does not belong in a hardening roadmap.
- **Performance benchmarking.**
- Changing product behavior. Every change here is either test infrastructure, build configuration, or documentation.

## 8. Verification gates

Run from the repository root. These are the gates for **every** change in this roadmap.

```powershell
dotnet build CLIHub.sln -warnaserror          # 0 warnings, 0 errors
dotnet test CLIHub.sln                        # 1010+ passed, 0 failed
openspec validate --all                       # all valid
node .opencode/scripts/check-model-policy.mjs # OK
```

Per-change additions:

```powershell
# C1 — coverage baseline
dotnet test tests\CLIHub.Core.Tests\CLIHub.Core.Tests.csproj -c Release --no-build `
  --collect:"XPlat Code Coverage" --results-directory artifacts\coverage\core
# C2 — enforcement
dotnet build CLIHub.sln -warnaserror
# C3 — determinism
dotnet test CLIHub.sln -- RunConfiguration.DisableAppDomain=true
# C4 — skip semantics (expect Skipped > 0 on a machine without Git Bash / pwsh)
dotnet test tests\CLIHub.Core.Windows.Tests\CLIHub.Core.Windows.Tests.csproj
# C5 — coverage delta vs the §2.2 baseline
```

## 9. Risks and open questions

| # | Item | Impact | Handling |
|---|---|---|---|
| R1 | `TreatWarningsAsErrors` breaks the build after an SDK/analyzer bump | Build red on unrelated change | Targeted `<WarningsNotAsErrors>` with a recorded reason; revisit at each SDK bump |
| R2 | C3's injected seams widen the public API of Core | Violates the Core boundary rule | Seams stay `internal`; `ArchitectureBoundaryTests` re-run as a gate |
| R3 | `Xunit.SkippableFact` is a new dependency | Supply-chain surface | xunit 2.9.3 has no `Assert.Skip`; the alternative is a manual `Assert.True(false, reason)`-style guard, which is worse. Defer the decision to C4 implementation |
| R4 | Coverage figures differ between Debug and Release | Baseline drift | §2.2 was measured in **Release** (`-c Release --no-build`), matching CI. Keep Release as the canonical measurement configuration |
| R5 | Adding tests for `launch-window-theme` / `settings-theme` visual scenarios is not possible without a UI harness | H3 partially unresolved | C5 covers what is mechanically checkable (e.g. `WindowPositioner` computes a monitor-relative rect); pure visual assertions stay uncovered and are recorded as such |
| R6 | 0 % coverage on `Interop/User32` / `DwmApi` is permanent | Acceptable | P/Invoke shells; explicitly excluded in C5's non-goals |
| R7 | `docs/backlog.md` already tracks M1, M2 and the unreproduced failure | Duplicate tracking | C4 and C5 close the backlog entries; the backlog entries are updated to point at the change that resolves them |

## 10. Definition of done for the roadmap

- C1–C6 are implemented, verified, and archived.
- CI publishes coverage per test project; a failing run identifies the failing test.
- The build fails on a warning.
- `dotnet test CLIHub.sln` reports **0 failed**, and the skip count is non-zero only when a runtime is genuinely absent.
- Every scenario in `openspec/specs/` that is mechanically checkable has a test; the remainder are explicitly recorded as uncovered with a reason.
- `CLIHub` and `CLIHub.Core.Windows` line coverage are above their §2.2 baseline, measured in Release.
- `docs/testing.md` exists and is linked from the docs index.
- `docs/backlog.md` no longer lists the entries closed by C4/C5.
