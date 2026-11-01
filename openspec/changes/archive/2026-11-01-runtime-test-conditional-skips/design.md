# Design: runtime-test-conditional-skips

## Verified current state

All evidence re-verified 2026-11-01 against commit `6610e88` (clean working tree).

### 1. The two tests that mask a missing runtime as "Passed"

`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`:

```csharp
42:    [Fact]
43:    public async Task LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory()
44:    {
45:        var locator = new WindowsRuntimeLocator(
46:            new WindowsRuntimeEnvironment(),
47:            NullLogger<WindowsRuntimeLocator>.Instance);
48:        var resolvedPwsh = locator.Locate(RuntimeKind.PowerShell7);
49:        if (resolvedPwsh is null)
50:        {
51:            // PowerShell 7 is not installed on this machine; there is nothing to launch.
52:            return;
53:        }
54:
55:        await this.VerifyChildWorkingDirectoryAsync(
56:            RuntimeKind.PowerShell7,
57:            arguments => arguments.Replace("-NoExit ", string.Empty, StringComparison.Ordinal),
58:            new RuntimeInfo { ExecutablePath = resolvedPwsh, Type = RuntimeType.PowerShell },
59:            resolvedPwsh);
60:    }
```

```csharp
62:    [Fact]
63:    public async Task LaunchAsync_GitBash_ChildStartsInWorkingDirectory()
64:    {
65:        var locator = new WindowsRuntimeLocator(
66:            new WindowsRuntimeEnvironment(),
67:            NullLogger<WindowsRuntimeLocator>.Instance);
68:        var resolvedGitBash = locator.Locate(RuntimeKind.GitBash);
69:        if (resolvedGitBash is null)
70:        {
71:            // Git for Windows is not installed on this machine; there is nothing to launch.
72:            return;
73:        }
74:        ... (lines 75-110: builds a working directory, launches bash.exe, asserts exit code,
75:             reported cwd, and that the resolved path is outside the Windows directory) ...
76:    }
```

Both `return;` statements complete the `[Fact]` method with no exception and no assertion executed;
xUnit (and `dotnet test`) record this as **Passed**.

`LaunchAsync_PowerShell_ChildStartsInWorkingDirectory` (`WindowsInteractiveProcessRunnerTests.cs:33-40`)
has no locator call and no early return — Windows PowerShell (`powershell.exe`) ships with every
supported Windows 10/11 host, so it needs no skip and is out of scope.

### 2. Reproduced on the current machine (confirms the bug and the fix surface)

- `where.exe pwsh.exe` → `C:\Program Files\PowerShell\7\pwsh.exe` (PowerShell 7 **is** installed here).
- `where.exe bash.exe` → `INFO: Could not find files for the given pattern(s).` (Git for Windows **bash.exe is
  not** on PATH on this machine via `where.exe`; `WindowsRuntimeLocator` also probes the registry and
  well-known install roots, so this is an upper bound, not proof the locator itself returns `null`).
- Baseline `dotnet test CLIHub.sln` on this machine (commit `6610e88`):
  `CLIHub.Core.Windows.Tests.dll`: **Failed: 0, Passed: 63, Skipped: 0, Total: 63**;
  `CLIHub.Tests.dll`: Failed: 0, Passed: 572, Skipped: 0, Total: 572;
  `CLIHub.Core.Tests.dll`: Failed: 0, Passed: 703, Skipped: 0, Total: 703.
  Combined: **1,338 passed, 0 failed, 0 skipped.**
  Zero `Skipped` across the whole solution confirms today's behavior: even if the Git Bash locator
  returns `null` on this exact machine, that test still reports inside the `Passed: 63` bucket, not as
  a skip — exactly the bug this change fixes.

### 3. `WindowsRuntimeLocator` and its seam (what the fix can use for a deterministic proof)

`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs`:

- `internal sealed class WindowsRuntimeLocator : IRuntimeExecutableLocator` (line 236), constructor
  `internal WindowsRuntimeLocator(IWindowsRuntimeEnvironment environment, ILogger<WindowsRuntimeLocator> logger)`
  (line 249-254).
- `public string? Locate(RuntimeKind kind)` (line 257) switches only on `RuntimeKind.GitBash` and
  `RuntimeKind.PowerShell7`; every other `RuntimeKind` value returns `null` unconditionally (line 265,
  the `_ => null` arm) — there is no "unresolvable kind" input to pass; the two tests already only ever
  call `Locate` with `GitBash` or `PowerShell7`.
- `internal interface IWindowsRuntimeEnvironment` (line 17) is the operating-system seam: registry
  reads, well-known install roots, PATH directories, `GetWindowsDirectory()`, `FileExists(...)`.
  `internal sealed class WindowsRuntimeEnvironment : IWindowsRuntimeEnvironment` (line 73) is the real,
  registry/filesystem-backed implementation the two tests construct today (`new
  WindowsRuntimeEnvironment()` at lines 46 and 66).
- `src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:19-20`:
  `<InternalsVisibleTo Include="CLIHub.Core.Windows.Tests" />` and
  `<InternalsVisibleTo Include="CLIHub.Tests" />` — confirmed the test assembly already has access to
  both `internal` types, which is how the existing test file already compiles today's
  `new WindowsRuntimeLocator(new WindowsRuntimeEnvironment(), ...)` calls.

This seam is what makes the "deliberately unresolvable locator input" proof option possible: a fake
`IWindowsRuntimeEnvironment` that returns empty collections / nulls from every member forces
`WindowsRuntimeLocator.Locate(RuntimeKind.GitBash)` (or `.PowerShell7`) to return `null`
**unconditionally, on any machine**, including one that has the real runtime installed.

### 4. Package versions and compatibility (`tests/Directory.Packages.props`)

`tests/Directory.Packages.props:14-21` (current, before this change):

```xml
<PackageVersion Include="coverlet.collector" Version="10.1.0" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.12" />
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
<PackageVersion Include="Moq" Version="4.21.0" />
<PackageVersion Include="xunit" Version="2.9.3" />
<PackageVersion Include="xunit.runner.visualstudio" Version="4.0.0" />
```

`tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj:13-20` (current):

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="Moq" />
  <PackageReference Include="xunit" />
  <PackageReference Include="xunit.runner.visualstudio" />
</ItemGroup>
```

Central Package Management is active for all three test projects (comment at
`tests/Directory.Packages.props:3-9`): NuGet resolves the nearest `Directory.Packages.props` walking up
from each `.csproj`, there is no root-level file, so `tests/Directory.Packages.props` governs
`tests/CLIHub.Core.Tests`, `tests/CLIHub.Core.Windows.Tests`, and `tests/CLIHub.Tests`; production
projects keep inline `Version` attributes and are unaffected by this file.

**Package chosen: `Xunit.SkippableFact`, version `1.5.85`.**

Verified via the NuGet Gallery package page (`https://www.nuget.org/packages/Xunit.SkippableFact`,
fetched 2026-11-01):

- Latest stable version is `1.5.85` (published 8/24/2026), MS-PL licensed, by the package's original
  author/maintainer (`AArnott`), with 69.0M total downloads and 1.3M downloads on the current version.
- **Target framework**: `.NET Standard 2.0` and `.NET Framework 4.6.2`; the NuGet Gallery computes
  `net10.0` and `net10.0-windows` as compatible (`"net10.0 was computed"`, `"net10.0-windows was
  computed"`), consistent with any `netstandard2.0`-targeted library being usable from a `net10.0-windows`
  test project.
- **Declared dependencies** (NuGet Gallery "Dependencies" panel):
  - `.NETFramework 4.6.2`: `Validation (>= 2.6.68)`, `xunit.extensibility.execution (>= 2.4.0)`.
  - `.NETStandard 2.0`: `Validation (>= 2.6.68)`, `xunit.extensibility.execution (>= 2.4.0)`.
  Both dependency sets declare `xunit.extensibility.execution >= 2.4.0` with **no upper bound**. The
  repository's pinned `xunit` version is `2.9.3` (`tests/Directory.Packages.props:19`), which is the
  umbrella package that brings in `xunit.extensibility.execution` at the same `2.9.3` version — well
  above the `>= 2.4.0` floor and inside the only constraint `Xunit.SkippableFact` declares. There is no
  declared dependency on `xunit.runner.visualstudio`, so the pinned runner version (`4.0.0`) is
  unaffected.
  - `Xunit.SkippableFact` provides its own `[SkippableFact]` / `[SkippableTheory]` attributes and a
    custom `XunitTestFramework`-compatible discoverer/executor that plugs into the existing
    `xunit.runner.visualstudio` test adapter; it does not replace or fork the xunit execution engine.
- **Real-world compatibility signal**: the NuGet Gallery "GitHub repositories" panel lists this package
  as a direct dependency of `PowerShell/PowerShell` (56.0K stars), `git-ecosystem/git-credential-manager`
  (9.4K stars), `dafny-lang/dafny`, `libgit2/libgit2sharp`, and 131 repositories total — all consuming it
  against mainline xunit 2.x, not xunit v3, matching this repository's pinned `xunit 2.9.3`.

**Why not pin an older `Xunit.SkippableFact` version**: no compatibility reason to. The dependency floor
(`xunit.extensibility.execution >= 2.4.0`) has been unchanged across the version history visible on the
Gallery page (versions back to `1.3.x` list the same `xunit.extensibility.execution (>= 2.4.0)`
constraint), so there is no newer constraint in `1.5.85` that the repository's `2.9.3` pin would violate.
Taking the latest stable version is consistent with how the repository already pins current major
versions of its other test packages (`coverlet.collector` 10.1.0, `Microsoft.NET.Test.Sdk` 18.10.1).

### 5. Build/test baseline to compare against after the change

- `dotnet build CLIHub.sln` (commit `6610e88`): must stay at 0 warnings, 0 errors after the package
  addition.
- `dotnet test CLIHub.sln` (commit `6610e88`, this machine): 1,338 passed, 0 failed, 0 skipped
  (63 + 572 + 703). After the change, on a machine missing Git Bash and/or PowerShell 7, the total stays
  1,338 but some of today's 63 `CLIHub.Core.Windows.Tests.dll` passes become skips; on a machine with
  both runtimes installed (like the one used to verify this proposal), the totals are unchanged because
  neither `Skip.If` condition is true.
- `openspec validate --all` (quoted in this document's own validation run below).

## Decisions

### Decision 1: `Xunit.SkippableFact` over xunit v3's native conditional skip

**Chosen**: Add the `Xunit.SkippableFact` package (`[SkippableFact]` + `Skip.If(condition, reason)`).

**Rationale**: xunit 2.x (pinned at `2.9.3` across all three test projects) has no built-in way for a
`[Fact]` method to decide *during execution* that it should report Skipped — xunit 2.x's `Skip` property
on `[Fact]`/`[Theory]` only accepts a compile-time constant string, so it cannot express "skip only if
the runtime locator returns null at run time". `Xunit.SkippableFact` is the long-standing, widely-used
library that fills exactly this gap for xunit 2.x by adding a custom test-case discoverer that lets
`Skip.If(...)` inside the test body convert a running test into a skip result.

**Alternative rejected**: Migrate to xunit v3, which added native per-test dynamic skip support
(`Assert.Skip(...)` / dynamic `Skip` in v3). Rejected because: (a) the task's non-goals explicitly
exclude an xunit v3 migration; (b) migrating all three test projects' pinned `xunit`/
`xunit.runner.visualstudio` versions and re-verifying 1,338 tests is a disproportionate blast radius for
a two-test reporting fix; (c) xunit v3 is a major-version jump with its own breaking changes to
assembly/runner configuration that deserves its own proposal and verification, not a side effect of this
change.

**Alternative rejected**: Keep `[Fact]` and throw a custom marker exception caught by a wrapping
`try`/`catch` that the runner interprets as inconclusive. Rejected because xunit 2.x's standard runner
(`xunit.runner.visualstudio` 4.0.0, already pinned) has no "inconclusive" result category distinct from
Failed/Passed — this would either still report Passed (if swallowed) or report Failed (misrepresenting a
missing-runtime environment limitation as a real test failure), which is worse than today's bug for CI
gating purposes.

**Alternative rejected**: Hand-roll a `[SkippableFact]`-equivalent attribute and discoverer in the test
project. Rejected because it duplicates a mature, widely-depended-on (69M downloads, used by
`PowerShell/PowerShell` itself) library for no benefit, and the non-goals explicitly exclude adding new
test infrastructure beyond the one package.

### Decision 2: scope the package reference to `CLIHub.Core.Windows.Tests` only

**Chosen**: Add `<PackageVersion Include="Xunit.SkippableFact" Version="1.5.85" />` to
`tests/Directory.Packages.props` (so the version is centrally pinned, consistent with every other test
package in that file) and add `<PackageReference Include="Xunit.SkippableFact" />` (no `Version`
attribute, per the existing CPM-managed pattern in this `.csproj`) to
`tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj` only.

**Rationale**: the only two tests needing a runtime-conditional skip live in
`CLIHub.Core.Windows.Tests`. `CLIHub.Core.Tests` and `CLIHub.Tests` have no such tests today and no
request to add any; adding the reference there would be an unrequested, unused dependency.

**Alternative rejected**: add the `<PackageReference>` to all three test `.csproj` files for
"consistency" with the other shared packages. Rejected: those other packages (`xunit`,
`Moq`, etc.) are used by every test project; `Xunit.SkippableFact` is not, and the task brief explicitly
scopes the `.csproj` edit to `CLIHub.Core.Windows.Tests` only.

### Decision 3: skip point and reason text

**Chosen**: immediately after each `Locate(...)` call, replace:

```csharp
if (resolvedPwsh is null)
{
    // PowerShell 7 is not installed on this machine; there is nothing to launch.
    return;
}
```

with:

```csharp
Skip.If(resolvedPwsh is null, "PowerShell 7 is not installed on this machine.");
```

(and the Git Bash test's equivalent using `resolvedGitBash` and `"Git for Windows is not installed on
this machine."`), then continue the method as today using `resolvedPwsh`/`resolvedGitBash` — which
`Skip.If` has now proven non-null for the rest of the method (the implementation session should confirm
whether the nullable-reference-type analyzer requires a `!` null-forgiving suffix or an explicit `is not
null` narrowing afterward, since `Skip.If` is not recognized by the compiler as a null-check).

**Rationale**: preserves the exact reason wording already present in the removed comments (just turned
into the shown-to-the-user skip reason instead of a comment nobody sees in a passing-test report), and
keeps the skip decision at the same place in the method body — no restructuring of
`VerifyChildWorkingDirectoryAsync` or the surrounding setup.

**Alternative rejected**: keep the `if (... is null) { return; }` shape and add a separate
`[SkippableFact]`-aware guard clause helper. Rejected: `Skip.If` already is that guard clause; a
wrapping helper would be an unneeded layer for a two-line change.

### Decision 4: proof that the skip actually reports as Skipped, not Passed

**Chosen (accept as evidence)**: run the test with a **deliberately unresolvable locator input** via a
fake `IWindowsRuntimeEnvironment`, not a temporary forced-skip-and-revert hack.

Concretely, in the implementation session, add a temporary one-off test (or temporarily parameterize an
existing call) that constructs:

```csharp
var locator = new WindowsRuntimeLocator(new AlwaysMissingRuntimeEnvironment(), NullLogger<WindowsRuntimeLocator>.Instance);
```

where `AlwaysMissingRuntimeEnvironment : IWindowsRuntimeEnvironment` returns `[]` from every
`IReadOnlyList<string>`-returning member, `null` from `ReadPowerShellCoreInstallPath()` and
`GetPowerShellWellKnownExecutablePath()`, `false` from `FileExists(...)`, and any fixed string from
`GetWindowsDirectory()`. Run that one test via `dotnet test --filter` and read the console/trx output;
it must show **Skipped: 1** with the exact reason text, not **Passed: 1**. Then delete the temporary
fake/test before committing — it exists only to prove the mechanism works, not as permanent test
infrastructure (the brief's non-goals exclude new test infrastructure beyond the one package).

**Why this option over a temporary forced-skip-and-revert**: `IWindowsRuntimeEnvironment` is already an
`internal` seam built exactly for this purpose (`WindowsRuntimeLocator.cs:14`, "so Windows runtime
resolution is testable without touching the real registry or process-wide PATH") and
`InternalsVisibleTo` already grants `CLIHub.Core.Windows.Tests` access to it
(`CLIHub.Core.Windows.csproj:19`). Routing the proof through the locator's existing seam is deterministic
on **any** machine regardless of what is actually installed (this machine has PowerShell 7 installed,
so the real `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory` test will not naturally skip here —
confirmed by the 0-skipped baseline above), whereas a temporary forced `Skip.If(true, ...)` edit is
unverifiable as *correct* (it would report Skipped even if the real condition were wired up backwards)
and requires a revert step that is easy to forget before a commit. The fake-environment approach proves
the exact production code path (`Skip.If` wired to the real `Locate(...)` result) actually surfaces as
Skipped, not just that `[SkippableFact]`/`Skip.If` work in the abstract.

**Secondary corroboration already available without any new code**: this machine's Git Bash lookup
already returns `null` today (per the `where.exe bash.exe` miss above, modulo registry/well-known-root
checks the implementation session should also rule out or confirm) — so after the fix lands, simply
re-running `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj` on this same
machine and checking whether `LaunchAsync_GitBash_ChildStartsInWorkingDirectory` now reports Skipped
(rather than folding into Passed) is real, no-fake-needed evidence, *if* this machine indeed has no Git
for Windows installed by any of the three lookup paths the locator checks. The implementation session
must confirm this by running the unmodified locator in isolation (or just observing the post-fix test
result directly) rather than assuming the `where.exe` miss is conclusive, since the locator also checks
the registry and well-known install roots that `where.exe` does not see.

### Decision 5: do not touch `VerifyChildWorkingDirectoryAsync` or its assertions

**Chosen**: no changes to the shared helper (`WindowsInteractiveProcessRunnerTests.cs:159-196`) or to
any `Assert.*` call in either target test's runtime-present branch.

**Rationale**: explicitly required by the task brief; the helper is shared by three other tests
(`LaunchAsync_CommandPrompt_ChildStartsInWorkingDirectory`,
`LaunchAsync_PowerShell_ChildStartsInWorkingDirectory`, and the PowerShell-7 branch of this change) that
are out of scope.

## Open questions for the implementation session

1. **Nullable-reference-type narrowing after `Skip.If`.** `Skip.If(condition, reason)` is a normal
   method call from the compiler's point of view, so it does not narrow `resolvedPwsh`/`resolvedGitBash`
   from `string?` to `string` the way an `if (x is null) return;` guard does. The implementation session
   must check whether the project's nullable-reference-types setting (`<Nullable>enable</Nullable>`,
   confirmed in `CLIHub.Core.Windows.Tests.csproj:6`) produces a CS8604/CS8600-class warning at the
   `VerifyChildWorkingDirectoryAsync(...)` call site that passes `resolvedPwsh`/`resolvedGitBash`
   afterward, and resolve it with whichever idiom matches `.editorconfig`'s existing conventions (e.g.
   `resolvedPwsh!` with a short justifying comment, or restructuring to `var resolved = locator.Locate(...); Skip.If(resolved is null, ...); var resolvedNonNull = resolved!;`) — this proposal does not
   prescribe the exact syntax, only that the build must stay at 0 warnings after the change (per the
   existing baseline).
2. **Git Bash presence on the implementation machine.** This design's Decision 4 secondary corroboration
   assumes the implementation/verification machine has no Git for Windows installed via any of the three
   lookup paths `WindowsRuntimeLocator.LocateGitBash()` checks (registry, well-known roots, PATH outside
   the Windows directory). If that machine *does* have Git for Windows installed, the only usable proof
   is the fake-`IWindowsRuntimeEnvironment` path in Decision 4's primary option — this should be
   confirmed empirically (e.g. by temporarily logging or asserting the locator's return value) before
   relying on the "already skips today" shortcut.
3. **`Xunit.SkippableFact`'s `[assembly: TestFramework(...)]` requirement.** Some versions of
   `Xunit.SkippableFact` document that the test assembly needs an `[assembly:
   Xunit.Sdk.TestFramework("Xunit.Sdk.SkippableFactTestFramework", "Xunit.SkippableFact")]` (or similar)
   attribute for `Skip.If` to be honored by the discoverer, depending on how the package wires itself
   into xunit's pluggable test framework. The implementation session must check the installed package's
   README/release notes for `1.5.85` specifically and add any required assembly-level attribute to
   `CLIHub.Core.Windows.Tests` if the proof run in Decision 4 shows the skip is not honored without it.

## Orchestrator resolution of the open questions (2026-11-01)

1. **Nullable narrowing — use the minimal-diff `if` block, no `!` operator.** `Skip.If` returns `void`,
   so the compiler cannot use it to narrow a nullable reference. Keeping the existing
   `if (resolvedPwsh is null) { ...; return; }` shape and putting `Skip.If(true, reason);` before the
   `return;` preserves flow analysis: every use site after the block is already known non-null, so no
   null-forgiving operator, no extra local, and the diff stays inside the two blocks that already exist.
   `tasks.md` 2.3/2.6 now prescribe this form, and 2.4/2.7 check that no `CS8604`/`CS8602` appears.
2. **Git Bash presence.** Left as an empirical Stage 0.4 check as designed; the fake-environment proof in
   Stage 3 is authoritative either way, so the answer only decides whether an extra "no-fake"
   corroboration is available on the machine — not whether the change is provable.
3. **Assembly-level `TestFramework` attribute is not required for 1.5.85.** Verified against the
   package's own README (`https://raw.githubusercontent.com/AArnott/Xunit.SkippableFact/main/README.md`),
   whose usage sample is `[SkippableFact]` plus `Skip.IfNot(...)` with no assembly attribute, and against
   the nuspec, which depends only on `xunit.extensibility.execution >= 2.4.0` (satisfied by the pinned
   2.9.3) plus `Validation 2.6.68`. `SkippableFactAttribute` and `Skip` live in the `Xunit` namespace,
   which `tests/Directory.Build.props` already imports globally for all three test projects — so no new
   `using` is expected either. Stage 3's proof remains the decisive check; if it does not report Skipped,
   revisit this answer with the observed discoverer behaviour rather than adding the attribute blindly.
