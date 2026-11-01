# Proposal: runtime-test-conditional-skips

## Why

Two tests in `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` resolve an
external runtime through `WindowsRuntimeLocator.Locate(...)` and, when the runtime is not installed on
the machine running the suite, execute a bare `return;` and complete normally:

- `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory` (`WindowsInteractiveProcessRunnerTests.cs:43-60`)
  returns at line 52 when `locator.Locate(RuntimeKind.PowerShell7)` is `null`.
- `LaunchAsync_GitBash_ChildStartsInWorkingDirectory` (`WindowsInteractiveProcessRunnerTests.cs:63-111`)
  returns at line 72 when `locator.Locate(RuntimeKind.GitBash)` is `null`.

A test runner reports an early `return` from a `[Fact]` method as **Passed**. On a machine without
PowerShell 7 or Git for Windows installed, these two tests pass without exercising a single assertion.
CI test-count/pass-count summaries cannot distinguish "the runtime-present code path ran and every
assertion held" from "the method body exited before line 2". This change makes both tests report
**Skipped**, with an explicit reason naming the missing runtime, whenever the corresponding runtime is
not resolvable, so a dashboard or CI gate reading pass/fail/skip counts sees the true state.

## What changes

1. Add the `Xunit.SkippableFact` package (version `1.5.85`) to
   `tests/Directory.Packages.props` and reference it from
   `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj` only.
2. Change `[Fact]` to `[SkippableFact]` on `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory` and
   `LaunchAsync_GitBash_ChildStartsInWorkingDirectory`.
3. Replace each early `return;` with `Skip.If(resolved is null, "<runtime> is not installed on this
   machine");` (exact reason text decided in `design.md`), immediately after the `Locate(...)` call,
   so the skip is signaled before any further setup runs.
4. No other test in the file changes. `LaunchAsync_PowerShell_ChildStartsInWorkingDirectory` keeps
   `[Fact]` because Windows PowerShell ships with every supported Windows 10/11 host and needs no
   locator call or skip.
5. `VerifyChildWorkingDirectoryAsync` and its assertions are untouched; the runtime-present path keeps
   running through the exact same helper and assertions it does today.

## Impact

- **Test files**: 1 file edited (`WindowsInteractiveProcessRunnerTests.cs`), 2 attributes changed,
  2 `return;` statements replaced with `Skip.If(...)` calls. No test is added or removed; test count
  stays the same, but on a machine missing the runtime, the two tests report `Skipped` instead of
  `Passed`.
- **Project files**: `tests/Directory.Packages.props` gains one `<PackageVersion>` entry;
  `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj` gains one `<PackageReference>`
  entry. `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` and `tests/CLIHub.Tests/CLIHub.Tests.csproj`
  are not touched — `SkippableFact` is needed only by the Windows runtime-locator tests.
- **Production code**: zero changes. Nothing under `src/` is touched.
- **CI reporting**: on a CI runner (or developer machine) without PowerShell 7 and/or Git for Windows,
  `dotnet test` now reports a non-zero `Skipped` count for these two tests instead of folding them into
  `Passed`. On a machine with both runtimes installed, both tests keep running their existing
  assertions and report `Passed` exactly as before.
- **OpenSpec**: `skip_specs: true`. See "Specs decision" below.

## Non-goals

- No production code change (`src/` is untouched).
- No migration to xunit v3 (which has native theory/fact-level conditional skip). `Xunit.SkippableFact`
  is chosen specifically to avoid that migration; see `design.md` for the rejected alternative.
- No new shared test infrastructure beyond the one new package reference — no custom skip attribute, no
  shared "is runtime installed" helper beyond the existing `WindowsRuntimeLocator` call already in each
  test.
- No change to `LaunchAsync_PowerShell_ChildStartsInWorkingDirectory`,
  `LaunchAsync_CommandPrompt_ChildStartsInWorkingDirectory`,
  `LaunchAsync_PowerShell_PathResolvedShim_ReceivesArgumentsWithoutStopParsingToken`,
  `CreateStartInfo_SetsWorkingDirectory`, or `VerifyChildWorkingDirectoryAsync` itself.
- No change to `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` or
  `tests/CLIHub.Tests/CLIHub.Tests.csproj`; the package is added only where it is used.

## Dependencies

None. This change is self-contained and does not depend on any other open or archived OpenSpec change.

## Specs decision

`skip_specs: true`. This is a test-only change to how two already-existing tests report their own
outcome; it does not add, remove, or change any durable product behavior, user-facing capability, or
the CI pipeline's pass/fail contract. `openspec/specs/ci-build/spec.md` requirements (`Build and test on
pull requests`, `Build and test on master`, `Development version is current`) describe whether the CI
*pipeline* succeeds or fails on build/test outcomes; none of its scenarios mention per-test Skipped
reporting, and this change does not alter when the pipeline succeeds or fails — a skipped test does not
fail `dotnet test` or the CI check. No other spec file under `openspec/specs/` references test
execution or reporting semantics. There is accordingly no durable behavior contract to add a delta to;
the justification is recorded here per the `skip_specs: true` convention used in
`openspec/changes/archive/2026-10-31-test-support-consolidation`.
