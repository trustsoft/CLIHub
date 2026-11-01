# Tasks: runtime-test-conditional-skips

## Stage 0: Baseline snapshot (before any code change)

- [ ] 0.1 Confirm a clean starting tree: `git status --porcelain` shows only
  `openspec/changes/runtime-test-conditional-skips/`.
- [ ] 0.2 Run `dotnet build CLIHub.sln` and record the warning/error count (expected: 0 warnings,
  0 errors, matching the baseline recorded in `design.md`).
- [ ] 0.3 Run `dotnet test CLIHub.sln` and record the per-assembly Passed/Failed/Skipped/Total counts
  (expected baseline on a typical machine: `CLIHub.Core.Windows.Tests.dll` 63 total; combined solution
  1,338 total, 0 skipped — re-verify the exact numbers on the machine in use, since Skipped may already
  be non-zero if that machine is missing both runtimes, which does not match this design's recorded
  baseline and must be noted if so).
- [ ] 0.4 Determine whether Git for Windows is actually resolvable on this machine by running the
  unmodified `WindowsRuntimeLocator.Locate(RuntimeKind.GitBash)` path in isolation (e.g. a scratch
  console check or a temporary `Console.WriteLine` in a throwaway test run), not just `where.exe
  bash.exe`, per `design.md` open question 2. Record the result; it decides whether Stage 3's "no-fake"
  corroboration path is available on this machine.

## Stage 1: Add the package

- [ ] 1.1 In `tests/Directory.Packages.props`, add `<PackageVersion Include="Xunit.SkippableFact"
  Version="1.5.85" />` inside the existing `<ItemGroup>`, alongside the other test package versions,
  keeping the file's existing alphabetical-ish ordering convention if one is discernible from the
  current six entries.
- [ ] 1.2 In `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj`, add
  `<PackageReference Include="Xunit.SkippableFact" />` (no `Version` attribute — CPM-managed, matching
  the existing six `<PackageReference>` lines in that file) inside the existing `<ItemGroup>`. Do not
  touch `tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` or `tests/CLIHub.Tests/CLIHub.Tests.csproj`.
- [ ] 1.3 Run `dotnet restore` and confirm it resolves `Xunit.SkippableFact 1.5.85` against the pinned
  `xunit 2.9.3` / `xunit.runner.visualstudio 4.0.0` without a version conflict or downgrade warning. If
  restore reports a conflict, stop and report the exact message instead of forcing a different version.
- [ ] 1.4 Run `dotnet build CLIHub.sln` and confirm it still reports 0 warnings, 0 errors.
- [ ] 1.5 Run `dotnet test CLIHub.sln` and confirm the totals are unchanged from Stage 0 (no test
  behavior should change yet — only a package reference was added).

**Gate**: build and test must be clean before Stage 2. If `dotnet restore` reports any conflict with
the pinned `xunit`/`xunit.runner.visualstudio` versions, stop and report the exact message instead of
proceeding — do not silently change the pinned xunit version to resolve it.

## Stage 2: Convert the two tests to `[SkippableFact]`

- [ ] 2.1 No new `using` is expected: `SkippableFactAttribute` and `Skip` live in the `Xunit` namespace in
      1.5.85, and `tests/Directory.Build.props` already applies `<Using Include="Xunit" />` to all three
      test projects. Do not add `using Xunit.Sdk;` speculatively — add a using only if the compiler asks
      for one, and record what it asked for.
- [ ] 2.2 Change the attribute on `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory`
  (`WindowsInteractiveProcessRunnerTests.cs:42`) from `[Fact]` to `[SkippableFact]`.
- [ ] 2.3 Replace lines 49-53 (`if (resolvedPwsh is null) { // comment; return; }`) with the minimal-diff
  form: keep the `if (resolvedPwsh is null)` block, replace its comment and bare `return;` with
  `Skip.If(true, "PowerShell 7 is not installed on this machine.");` followed by `return;`. Keeping the
  `return;` preserves nullable flow analysis, so the later use sites need no null-forgiving operator and
  the diff stays confined to this block.
- [ ] 2.4 Build with `<Nullable>enable</Nullable>` in force: confirm no `CS8604`/`CS8602` nullable warning
  appears at the `resolvedPwsh` use sites (`:58`, `:59`). If a warning appears, stop and report it rather
  than suppressing it project-wide.
- [ ] 2.5 Change the attribute on `LaunchAsync_GitBash_ChildStartsInWorkingDirectory`
  (`WindowsInteractiveProcessRunnerTests.cs:62`) from `[Fact]` to `[SkippableFact]`.
- [ ] 2.6 Replace lines 69-73 (`if (resolvedGitBash is null) { // comment; return; }`) with the same
  minimal-diff form used in 2.3: keep the null check, replace its comment and `return;` with
  `Skip.If(true, "Git for Windows is not installed on this machine.");` followed by `return;`.
- [ ] 2.7 Confirm the later `resolvedGitBash` use sites (`:85`, `:104-105` and any others) need no change
  and raise no nullable warning, for the same flow-analysis reason as 2.4.
- [ ] 2.8 Confirm no other line in the file changed: `LaunchAsync_CommandPrompt_ChildStartsInWorkingDirectory`,
  `LaunchAsync_PowerShell_ChildStartsInWorkingDirectory`,
  `LaunchAsync_PowerShell_PathResolvedShim_ReceivesArgumentsWithoutStopParsingToken`,
  `CreateStartInfo_SetsWorkingDirectory`, and `VerifyChildWorkingDirectoryAsync` keep their exact current
  bodies and attributes (`git diff` the file and check only the two target tests' hunks changed).
- [ ] 2.9 Run UTF-8 safety check: `git diff` on the edited file and confirm no mojibake
  (`â€`, `Ã`, `ï»¿`, `U+FFFD`).
- [ ] 2.10 Run `dotnet build CLIHub.sln` and confirm 0 warnings, 0 errors.
- [ ] 2.11 Run `dotnet test CLIHub.sln`. On this machine's actual runtime availability (per Stage 0.4):
  if PowerShell 7 is installed here (as recorded in `design.md`'s evidence for the machine used to write
  this proposal), `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory` must still report Passed, not
  Skipped, and its assertions must still run. If Git Bash is confirmed absent per Stage 0.4,
  `LaunchAsync_GitBash_ChildStartsInWorkingDirectory` must now report Skipped (not folded into Passed),
  and the solution's total `Skipped` count must be exactly 1 higher than the Stage 0 baseline. Record
  the exact Passed/Failed/Skipped/Total numbers.

**Gate**: build must be clean and the test run in 2.11 must show the expected shift from Passed to
Skipped for whichever runtime is actually missing on the verification machine, with no new Failed
tests, before Stage 3.

## Stage 3: Prove the skip reports as Skipped, not Passed

- [ ] 3.1 Per `design.md` Decision 4, write a temporary throwaway `AlwaysMissingRuntimeEnvironment :
  IWindowsRuntimeEnvironment` (in a scratch file outside `tests/CLIHub.Core.Windows.Tests/`'s normal
  source, or inline in a temporary test method) that returns `[]` from every
  `IReadOnlyList<string>`-returning member, `null` from `ReadPowerShellCoreInstallPath()` and
  `GetPowerShellWellKnownExecutablePath()`, `false` from `FileExists(...)`, and a fixed path string from
  `GetWindowsDirectory()`.
- [ ] 3.2 Add one temporary `[SkippableFact]` test that constructs
  `new WindowsRuntimeLocator(new AlwaysMissingRuntimeEnvironment(), NullLogger<WindowsRuntimeLocator>.Instance)`,
  calls `.Locate(RuntimeKind.GitBash)` (or `.PowerShell7`), and calls the same
  `Skip.If(result is null, "<reason>")` line used in the real fix, to prove the mechanism end-to-end
  against a result that is `null` on every machine regardless of what is actually installed.
- [ ] 3.3 Run `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj --filter
  "FullyQualifiedName~<TemporaryTestName>"` and confirm the console/trx output shows **Skipped: 1**
  (not Passed: 1, not Failed: 1) with the exact configured reason text visible in the output.
- [ ] 3.4 Record the exact command and output quoted in the task completion message as the proof
  artifact.
- [ ] 3.5 Delete the temporary `AlwaysMissingRuntimeEnvironment` class and the temporary test method
  added in 3.1-3.2. Confirm via `git diff`/`git status --porcelain` that no trace of the temporary proof
  code remains staged for commit.
- [ ] 3.6 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` again after the deletion and
  confirm the totals match Stage 2's task 2.11 numbers exactly (the temporary proof code must leave no
  residue).

**Gate**: the proof in 3.3 must show Skipped, not Passed, before Stage 4. If it does not, stop and
investigate per `design.md` open question 3 (the assembly-level test-framework attribute) before
proceeding.

## Stage 4: Final verification

- [ ] 4.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings, 0 errors.
- [ ] 4.2 Run `dotnet test CLIHub.sln` and confirm the final Passed/Failed/Skipped/Total numbers, and
  that they match Stage 2 task 2.11's numbers (no drift from the Stage 3 proof-and-revert).
- [ ] 4.3 Run `git diff --stat` and `git status --porcelain` and confirm only
  `tests/Directory.Packages.props`,
  `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj`,
  `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`, and
  `openspec/changes/runtime-test-conditional-skips/` appear — nothing under `src/`,
  `tests/CLIHub.Core.Tests/`, or `tests/CLIHub.Tests/` changed.
- [ ] 4.4 Run `git diff --check` and confirm no whitespace issues.
- [ ] 4.5 Run `openspec validate --all` and quote the exact result.
- [ ] 4.6 Run `graphify update .` to keep the knowledge graph current.
- [ ] 4.7 Report: final Passed/Failed/Skipped/Total counts, which runtime(s) were actually missing on
  the verification machine, the Stage 3 proof output, and confirmation that `src/` is untouched.

## User approval gate (MANDATORY)

**This gate MUST be cleared by the user before `git commit` or `openspec archive`.**

The user must explicitly approve commit and archive after reviewing the implementation and
verification results. Per `AGENTS.md` Change, Commit, and Archive Policy, approval applies only to
this specific change and does not carry over to later work.
