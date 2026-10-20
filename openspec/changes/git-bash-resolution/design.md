# Design: Git Bash runtime resolution

Line numbers are from `master` at `cd11b88` ("docs(pipeline): add backlog and reference it in AGENTS.md"),
verified against the working tree during this refresh. Every claim below was read from the listed line
ranges; nothing is inferred from the pre-refresh plan.

## Verified current state

### The launch path emits a bare name and lets the OS resolve it

| Evidence | What it shows |
|---|---|
| `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs:19-32` | `BuildInteractive` returns `(FileName, Arguments)` per runtime. |
| `WindowsCommandLineBuilder.cs:29` | `RuntimeKind.GitBash => ("bash.exe", $"-lic {QuoteBashCommand(commandLine)}")` — the literal bare name. |
| `WindowsCommandLineBuilder.cs:133-152` | `QuoteBashCommand` / `EscapeBashArgumentTail`: the `-lic` argument quoting that must keep working unchanged. |
| `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:85` | `BuildStartInfo(this.runtime, command, workingDirectory)` produces the pair. |
| `ProcessLauncher.cs:88-99` | The file name is copied into `RuntimeInfo.ExecutablePath` (with `Type` = `Cmd` for everything that is not PowerShell, `:92`). |
| `src/CLIHub.Core/Infrastructure/Processes/RuntimeInfo.cs:6-16` | `RuntimeInfo` is an immutable record: `ExecutablePath` (`:11`), `Type` (`:16`). |
| `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs:81-94` | `CreateStartInfo` sets `FileName = runtime.ExecutablePath` (`:85`), `UseShellExecute = false` (`:88`), and never sets `Environment`, so the child inherits the parent's PATH. A bare name is therefore resolved by the OS at `process.Start()` (`:47`). |
| `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:26,53,89-95` | `GitBash` kind, its `"bash"` token, and its accepted aliases (`bash.exe`, `gitbash`, `git-bash`, `git bash`). |

### A failed resolution already has a contract; no new error type is needed

| Evidence | What it shows |
|---|---|
| `ProcessLauncher.cs:52-125` | `LaunchProcess` returns `bool`; validation failures return `false` (`:57`, `:63`, `:69`, `:75`). |
| `ProcessLauncher.cs:78-80` | Launch intent is logged at Information with command, executable, folder, and runtime. |
| `ProcessLauncher.cs:102` | `_ = this.interactiveRunner.LaunchAsync(...)` — the launch is fire-and-forget, so a failure *inside* the runner is not observed by this method. Pre-checks are the only synchronous failure channel. |
| `ProcessLauncher.cs:106-124` | `Win32Exception` / `UnauthorizedAccessException` / `Exception` are logged and mapped to `false`. |
| `src/CLIHub.Core/Agents/AgentCommandService.cs:50-53` | `false` becomes `AgentCommandResult(false, null, $"Failed to launch {plugin.Name}")`. |
| `src/CLIHub/ViewModels/Launch/LaunchCommandCoordinator.cs:114-117` | The launch window shows `"{kind} failed: {error}"` in its status area. |
| `src/CLIHub/Tray/TrayCommandHandlers.cs:101-105` | The tray launch path shows `result.Error` as a warning notification. |

Consequence: an unresolved Git Bash must be detected **before** `BuildStartInfo`, so the failure is
synchronous and travels the existing `bool` → `AgentCommandResult` → status/notification path. Widening
`LaunchProcess`'s return type or the result contract is unnecessary (see D7).

### Composition and adapter registration

| Evidence | What it shows |
|---|---|
| `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:33-44` | `AddWindowsInfrastructure` registers the Windows adapters: `RuntimeSelector` (`:37`), `IInteractiveProcessRunner` (`:38`), `IProcessOutputRunner` (`:39`), `ProcessLauncher` (`:40`), `IAgentProcessInspector` (`:41`), and builds `StartupService` through a factory with its registry seam (`:42-44`). This is where the new adapter registration belongs. |
| `src/CLIHub/Composition/ServiceRegistration.cs:48-64` | `AddClIHubServices` calls `AddClIHubCoreServices` (`:50`), `AddWindowsInfrastructure(guard)` (`:51`), then the feature modules. |
| `ServiceRegistration.cs:154-180` | `AddSettingsServices`, including the explicit 12-argument `SettingsViewModel` factory (`:163-175`). A 13th constructor parameter is added here. |
| `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:48-66` | `DependencyRegistration_IsRestrictedToCompositionRoots` allows `AddSingleton`/`AddTransient`/`AddScoped` only in three files (`:53-55`): app `ServiceRegistration.cs`, Core `ServiceCollectionExtensions.cs`, Core.Windows `WindowsServiceCollectionExtensions.cs`. |
| `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs:11-27` | `AddWindowsInfrastructure_CombinedWithCoreServices_ResolvesFullGraph` resolves `ProcessLauncher`; it will also cover a new `IRuntimeExecutableLocator` resolution. |

### Settings surface for Terminal Profiles

| Evidence | What it shows |
|---|---|
| `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:31` | `public sealed record RuntimeOption(RuntimeKind Kind, string Name, string Icon)`. |
| `SettingsViewModel.cs:229-235` | Static `RuntimeOptions` array; Git Bash at `:234`. |
| `SettingsViewModel.cs:491` | `public IReadOnlyList<RuntimeOption> Runtimes => RuntimeOptions;`. |
| `SettingsViewModel.cs:557-561`, `:1231-1232` | `SelectedRuntime` property and the `Load()` assignment from the stored preference. |
| `SettingsViewModel.cs:433-444` | The 12-parameter constructor; no availability source today. |
| `src/CLIHub/Views/SettingsTerminalPage.xaml:10-33` | One `SettingCard` with a `ComboBox` over `Runtimes` (`:16`), `SelectedItem` bound to `SelectedRuntime` (`:17`), item template showing icon + `Name` (`:20-30`). |
| `src/CLIHub/Views/SettingsWindow.xaml.cs:38-40` | `ShowSettings()` calls `viewModel.Load()` on every show, so per-open recomputation is already the pattern. |
| `src/CLIHub/Views/SettingsAgentsPage.xaml:13-28,68-77` | Existing precedent for an "unavailable" mark: `DataTrigger Binding="{Binding IsInstalled}" Value="False"` sets danger/muted brushes; the ComboBox item container is disabled. |
| `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:12` | Precedent for the option record carrying availability: `AgentOption(string? Id, string Name, string? Version = null, bool IsInstalled = true)`. |

Cost of including the Settings marking: one record member, an instance list rebuilt in `Load()`, one
constructor parameter, one factory argument (`ServiceRegistration.cs:163-175`), four test construction
sites, and a small XAML trigger. Low; see D1.

### Test seams the design copies

| Evidence | What it shows |
|---|---|
| `src/CLIHub.Core.Windows/StartupRegistry.cs:6-13,18-54` | An `internal` interface over the OS (`IStartupRegistry`) plus a `Microsoft.Win32.Registry` implementation, both internal to Core.Windows. |
| `src/CLIHub.Core.Windows/StartupService.cs:20-28` | `internal` constructor taking the seam; the service is registered through a factory (`WindowsServiceCollectionExtensions.cs:42-44`). |
| `src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:18-21` | `InternalsVisibleTo` for `CLIHub.Core.Windows.Tests` and `CLIHub.Tests`. |
| `tests/CLIHub.Core.Windows.Tests/FakeStartupRegistry.cs:8-39` | In-memory fake of the seam, with a throw switch. |

### Tests that already work around this bug

| Evidence | What it shows |
|---|---|
| `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:57-104` | `LaunchAsync_GitBash_ChildStartsInWorkingDirectory`. |
| `WindowsInteractiveProcessRunnerTests.cs:59-62` | Gate `if (!IsOnPath("bash.exe")) return;` — availability, not usability. |
| `WindowsInteractiveProcessRunnerTests.cs:71` | `RuntimeInfo { ExecutablePath = "bash.exe", Type = RuntimeType.Cmd }` — the bug reproduced in a test. |
| `WindowsInteractiveProcessRunnerTests.cs:74-84` | Comment block and a `-c "true"` probe added by `5eb8eae` to skip when the resolved bash cannot run commands. |
| `WindowsInteractiveProcessRunnerTests.cs:86-98` | Relative-path report probe made flavor-agnostic by `b483166` (`pwd > cwd-report.txt`), tolerant of `/c/...` and `/mnt/c/...`. |
| `WindowsInteractiveProcessRunnerTests.cs:199-209` | `IsOnPath` helper — also used by the PowerShell 7 test at `:45`, so it must stay. *(Superseded by D8 + D10: both tests are locator-gated now and the helper was deleted.)* |
| `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs:11-37,91-103` | `BuildStartInfo` theory pins `RuntimeKind.GitBash → "bash.exe"` (`:15`) and the Git Bash builder test asserts `Assert.Equal("bash.exe", fileName)` (`:99`). |
| `ProcessLauncherTests.cs:146-172` | `LaunchProcess` behavior test through `FakeInteractiveProcessRunner` — the shape to extend for the resolved-path and unresolved cases. |

`ProcessLauncher` is constructed directly in seven places that must gain the new dependency:
`tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs:149,177`,
`tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs:20`,
`tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:73`,
`tests/CLIHub.Core.Tests/Agents/AgentCommandServiceTests.cs:23`,
`tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs:81`,
`tests/CLIHub.Tests/Services/StartupPreferencesApplierTests.cs:50`.

### Observed machine state (development machine, 2026-10-20)

- `HKLM\SOFTWARE\GitForWindows` exists: `CurrentVersion` = `2.56.0.2`, `InstallPath` = `C:\Program Files\Git`,
  `LibexecPath` = `C:\Program Files\Git\ucrt64\libexec\git-core`. Value name `InstallPath` confirmed.
- `HKCU\SOFTWARE\GitForWindows` does not exist here, so the per-user branch is specified but unverified.
- `C:\Program Files\Git\bin\bash.exe` exists (45,416 bytes); `C:\Windows\System32\bash.exe` does not.
- No `bash.exe` on PATH (`Get-Command bash.exe -All` → nothing), so today's Git Bash runtime cannot start
  at all on this machine even though Git is installed. This is failure shape 3 and a ready manual check
  for the fixed behaviour.
- A WSL-first PATH cannot be reproduced here (no `System32\bash.exe`); shape 1 stays verified only by the
  original CI observation and by unit tests.

## Goals / Non-goals

**Goals:** the Git Bash runtime starts a Git for Windows `bash.exe` or fails synchronously and visibly;
resolution is testable without touching the real registry or PATH; the Terminal Profiles page tells the
truth about the option.

**Non-goals:** WSL as a runtime kind; resolving `cmd.exe`/`powershell.exe` (PowerShell 7 was added to
scope by D10); auto-switching the runtime; caching; changing `-lic` quoting or the captured-output path;
deleting `RuntimeSelector`; any change to agent process matching.

## Decisions

### D1 - The Settings availability marking stays in scope

The committed plan contradicted itself (`design.md:33-34` called the UI follow-up optional while
`proposal.md` and `tasks.md` 2.1 required it). **Scope: in.**

Reason: a missing runtime is already a user-visible failure on the launch path; the honest place to
surface it before the user hits that failure is the page that offers the option. The implementation cost
is bounded and measured above (one record member, one `Load()` rebuild, one constructor parameter and its
factory, four test call sites, one XAML trigger), and the page already reloads on every show
(`SettingsWindow.xaml.cs:38-40`). Dropping the bullet would save less than it costs in user confusion.

### D2 - Port in Core, implementation in Core.Windows

`IRuntimeExecutableLocator` goes in `src/CLIHub.Core/Infrastructure/Processes/` next to
`IInteractiveProcessRunner` / `IProcessOutputRunner` (ports pattern already in use; `AGENTS.md` keeps
UI-independent contracts in Core). Signature:

```csharp
string? Locate(RuntimeKind kind);
```

Contract in XML docs: returns an absolute executable path, or `null` when this locator cannot resolve the
kind. The Windows implementation resolves **only** `RuntimeKind.GitBash` and returns `null` for the other
kinds; callers must not read `null` for a non-Git-Bash kind as "not installed". A future `pwsh` probe can
extend the same port without a new interface. *(Extended by D10: the implementation now resolves
`GitBash` and `PowerShell7`; `null` for `PowerShell`/`CommandPrompt` still means "not covered",
not "not installed".)*

### D3 - Resolution order and no caching

1. Registry: `HKCU` and `HKLM` `SOFTWARE\GitForWindows` value `InstallPath` → `bin\bash.exe` when the file
   exists. `HKLM` is read with `RegistryView.Registry64` and then `RegistryView.Registry32`
   (`SOFTWARE\WOW6432Node\GitForWindows`) because a 32-bit Git install writes the 32-bit view. `HKCU` is
   read in the default view first and the 32-bit view as fallback. Any `IOException` /
   `SecurityException` / `UnauthorizedAccessException` on a key is treated as a miss, never as a failure.
2. Well-known: `Environment.GetFolderPath(ProgramFiles)`, `ProgramFilesX86`, `LocalApplicationData` +
   `Programs\Git`, each + `bin\bash.exe` when the file exists.
3. PATH: split on `Path.PathSeparator`, trim and drop empty entries, take the first `Path.Combine(dir,
   "bash.exe")` that exists **and** whose directory is not under `Environment.GetFolderPath(Windows)`
   (the WSL launcher in `System32` is thereby excluded).

First hit wins. The absolute path is returned; nothing is cached. Launch-time resolution was already the
committed design ("installations change between saves"), the Settings page resolves per open, and the work
is one registry read plus at most a handful of `File.Exists` calls — below any measurable launch cost.
The existing `RuntimeSelector` cache (`src/CLIHub.Core.Windows/RuntimeSelector.cs:22-37`) is deliberately
not imitated.

### D4 - One injectable OS seam, internal, with the existing test seam precedent

Core.Windows is Windows-only, so the production environment is read directly; only the *seam* needs
injection for tests. Follow `IStartupRegistry` / `StartupService`:

```csharp
internal interface IWindowsRuntimeEnvironment   // named IGitBashEnvironment before D10
{
    string? ReadGitForWindowsInstallPath();          // HKCU/HKLM, both views, null on miss/error
    IReadOnlyList<string> GetGitWellKnownInstallRoots(); // ProgramFiles, ProgramFilesX86, LocalAppData\Programs\Git
    IReadOnlyList<string> GetPathDirectories();       // PATH entries, empties removed
    string GetWindowsDirectory();                     // %SystemRoot% (no trailing separator)
    bool FileExists(string path);                     // File.Exists
}
```

*(D10 generalizes this seam in place: it is renamed `IWindowsRuntimeEnvironment` and gains
`ReadPowerShellCoreInstallPath()` and `GetPowerShellWellKnownExecutablePath()`; the implementation class
becomes `WindowsRuntimeEnvironment`, and `GitForWindowsRuntimeLocator` becomes `WindowsRuntimeLocator`.)*

- `public sealed class WindowsRuntimeLocator : IRuntimeExecutableLocator` with an `internal`
  constructor `(IWindowsRuntimeEnvironment environment, ILogger<WindowsRuntimeLocator> logger)`, registered
  through a factory in `AddWindowsInfrastructure` exactly like `StartupService`
  (`WindowsServiceCollectionExtensions.cs:42-44`). No public constructor means DI cannot silently build a
  locator without its environment.
- Tests use a small in-memory fake (`FakeGitBashEnvironment`) with settable install path, roots, PATH
  entries, Windows directory, and an existing-file set. `InternalsVisibleTo` is already in place
  (`CLIHub.Core.Windows.csproj:18-21`).
- Registry access reuses the `Microsoft.Win32.Registry` style of `StartupRegistry.cs:18-54`, including the
  `OperatingSystem.IsWindows()` guard (`:24-27`).

Logging: `Debug` per candidate rejected, `Information` with the resolved path, `Warning` when nothing
resolves. The authoritative failure log stays in `ProcessLauncher` (D7).

### D5 - `ProcessLauncher` resolves before building; the builder receives the path; no bare fallback

- `ProcessLauncher` gains a required fourth constructor parameter `IRuntimeExecutableLocator` (required,
  not optional, so a production misconfiguration cannot compile into a silent fallback). All seven test
  construction sites pass a fake.
- In `LaunchProcess`, after the existing validation and the Information log, and before `BuildStartInfo`:

  ```csharp
  string? gitBashExecutable = null;
  if (this.runtime is RuntimeKind.GitBash)
  {
      gitBashExecutable = this.runtimeExecutables.Locate(RuntimeKind.GitBash);
      if (gitBashExecutable is null)
      {
          this.logger.LogError("Git Bash is configured but no Git for Windows bash.exe was found ...");
          return false;
      }
  }
  ```

  The message names Git for Windows and the fallback order so the log alone is actionable.
- `ProcessLauncher.BuildStartInfo` (internal, `:197-202`) and
  `WindowsCommandLineBuilder.BuildInteractive` (`:19-32`) gain a fourth parameter
  `string? gitBashExecutable`. The Git Bash arm becomes
  `(gitBashExecutable ?? throw new InvalidOperationException("Git Bash executable was not resolved"), …)`.
  The throw is unreachable from public code because `LaunchProcess` returns `false` first; it exists so no
  call site can keep the bare name by accident.
- The static builder entry point is *not* given a default value for the new parameter: every caller states
  what it means. Test call sites pass `null` for non-Git-Bash runtimes and a concrete path for Git Bash.
- `RuntimeInfo.ExecutablePath` now carries the absolute path; `RuntimeInfo` itself does not change.
- The fire-and-forget `_ = this.interactiveRunner.LaunchAsync(...)` (`:102`) is untouched: resolution
  failure is synchronous by construction.

### D6 - Settings presentation: label, do not disable

`RuntimeOption` becomes `RuntimeOption(RuntimeKind Kind, string Name, string Icon, bool IsInstalled = true)`
(mirroring `AgentOption`, `AgentCardItem.cs:12`) with a computed
`DisplayName => IsInstalled ? Name : $"{Name} (not installed)"`. `SettingsViewModel` keeps the static
template array (`:229-235`) as the source of truth and gains an instance `RuntimeOption[] runtimes`
initialized to the template; `Runtimes` (`:491`) returns the instance array and `Load()` rebuilds it with
`RuntimeOptions.Select(o => o with { IsInstalled = o.Kind != RuntimeKind.GitBash
|| this.runtimeExecutables.Locate(RuntimeKind.GitBash) is not null })` before selecting the stored value
(`:1231-1232`). `OnPropertyChanged(nameof(this.Runtimes))` is raised from `Load()` like the other fields
(`:1295-1336`).

The XAML item template binds `Text="{Binding DisplayName}"` and mutes the text with a
`DataTrigger Binding="{Binding IsInstalled}" Value="False"` (`SettingsAgentsPage.xaml:13-28` pattern),
plus `AutomationProperties.HelpText` stating that Git for Windows was not found. One trigger, no extra
element: the option reads "Git Bash (not installed)" in the muted brush and nothing else changes.

The option stays **selectable**. Disabling it (as `SettingsAgentsPage.xaml:68-77` does for agents) was
rejected: the stored value must remain readable and re-selectable, installation state can change between
Settings opens, and the launch path already fails deterministically with the reason in the log.

### D7 - Failure message contract is unchanged

`LaunchProcess` keeps returning `bool`; `AgentCommandService` keeps formatting
`"Failed to launch {plugin.Name}"` (`AgentCommandService.cs:53`), which the launch status
(`LaunchCommandCoordinator.cs:114-117`) and the tray notification (`TrayCommandHandlers.cs:104`) already
show. The specific cause ("no Git for Windows bash.exe") is logged, which is what the spec delta requires.
Threading a reason string through `bool` was rejected as a wider contract change than the symptom needs;
it is recorded as a follow-up candidate, not folded in.

### D8 - The Windows Git Bash test switches to the resolver and drops the usability probe

`WindowsInteractiveProcessRunnerTests.cs:57-104` stops guessing from PATH:

- gate: `IRuntimeExecutableLocator.Locate(RuntimeKind.GitBash) is null → return;`
- `RuntimeInfo.ExecutablePath` = the resolved path (`:71`), not the literal `"bash.exe"`;
- delete the `-c "true"` usability probe and its 74-84 comment: with a resolved Git for Windows bash the
  WSL launcher can no longer be selected, and the machine either has Git Bash (test runs) or not (skipped).
  `Assert.Equal(0, exitCode)` then becomes a hard assertion instead of a probe-guarded one;
- keep the flavor-agnostic report probe (`:86-98`) and `NormalizeReportedPath`-style tolerance — it is
  correct and costs nothing; keep the `IsOnPath` helper for the PowerShell 7 test (`:45`);
  *(superseded by D10: the PowerShell 7 test is locator-gated too, and the helper was deleted)*;
- optionally assert the resolved path is outside the Windows directory, which states the invariant in the
  test itself.

### D9 - Settings behavior is specified in `preferences-ui`, not duplicated in `agent-commands`

`openspec/specs/preferences-ui/spec.md:189-207` owns "Terminal runtime options" (the page and its options);
`openspec/specs/agent-commands/spec.md:64-97` owns launch behavior. The Settings marking therefore lands as
a MODIFIED requirement in `preferences-ui` and the availability scenario is removed from the
`agent-commands` delta (it previously lived there, in the wrong capability). Both delta files preserve
their capability's original scenario names.

### D10 - PowerShell 7 gets the same availability treatment as Git Bash

Decided 2026-10-20, before archiving: a missing PowerShell 7 was the one remaining silent launch
failure (it sat in this proposal's non-goals until now). The product decision, mirrored on Git Bash's
contract: when `pwsh.exe` cannot be resolved, Terminal Profiles marks PowerShell 7 as not installed —
label-only, option stays selectable, stored selection never rewritten — and `LaunchProcess` fails
synchronously with a logged lookup-order error (the `bool` contract, no throw escaping). When it
resolves, launches use the resolved absolute path. `cmd.exe` and `powershell.exe` stay bare-name by
design because they ship with Windows and cannot be missing.

Resolution order (first hit wins; nothing cached):

1. Registry `HKLM\SOFTWARE\Microsoft\PowerShellCore` value `InstallPath` — the value is the directory
   containing `pwsh.exe`; read in the 64-bit view, then the 32-bit view. Faults are misses. No `HKCU`:
   the PowerShell 7 MSI is machine-wide, and per-user installs surface through PATH.
2. Well-known `%ProgramFiles%\PowerShell\7\pwsh.exe`.
3. PATH scan taking the first `pwsh.exe`. **No Windows-directory exclusion**: unlike `bash.exe`,
   `pwsh.exe` has no WSL-launcher-shaped namesake in the Windows directory, so there is no hazard to
   exclude. Excluding it would also break Microsoft Store installs, which surface as an app-execution
   alias in `%LOCALAPPDATA%\Microsoft\WindowsApps` that the PATH scan must find.

Mechanics mirror D4–D7 exactly: the OS seam is generalized in place rather than duplicated
(`IGitBashEnvironment` → `IWindowsRuntimeEnvironment`, gaining `ReadPowerShellCoreInstallPath()` and
`GetPowerShellWellKnownExecutablePath()` beside the unchanged Git members; `WindowsGitBashEnvironment`
→ `WindowsRuntimeEnvironment`; `GitForWindowsRuntimeLocator` → `WindowsRuntimeLocator`; the DI factory
and every test reference updated), `ProcessLauncher` resolves PowerShell 7 before building the command
line and returns `false` with a `LogError` naming PowerShell 7 and the lookup order, `BuildInteractive`'s
PowerShell 7 arm requires the resolved path and throws `InvalidOperationException` when null (same guard
shape as the Git Bash arm; a new `powerShell7Executable` parameter is threaded through `BuildStartInfo`
with no default), and `SettingsViewModel.Load()` resolves `IsInstalled` for Git Bash *and* PowerShell 7,
leaving the Windows-shipped kinds at the default true. The port's XML docs now name both covered kinds.

## Alternatives rejected

- **Keep the bare name and document PATH requirements.** Rejected already in the committed plan: the
  failure is silent and environment-dependent, and the verified local machine shows it needs no WSL at all.
- **Detect the WSL launcher and refuse only that path.** Insufficient: an initialized WSL would still run
  the wrong environment.
- **Resolve at settings-save time.** Installations change between saves; launch-time resolution keeps the
  answer truthful and costs microseconds.
- **Git-Bash-only port (`LocateGitBash()`).** Rejected: `RuntimeKind` is the natural key and a `pwsh`
  probe later fits the same shape; a kind-agnostic `Locate` documents its limited coverage.
- **Cache the result in the locator, like `RuntimeSelector.SelectRuntime`.** Rejected (D3): the page must
  reflect a reinstall within the same session, and there is nothing to amortize.
- **Optional `IRuntimeExecutableLocator?` constructor parameter on `ProcessLauncher`.** Rejected: a null
  locator in production would resurrect the bare-name bug silently.
- **Disable the unavailable ComboBox item.** Rejected (D6): hides the stored configuration and duplicates
  the launch guard.
- **Extend `RuntimeSelector` instead of a new type.** Rejected: it answers a different question (which
  terminal host exists — `wt.exe`/`cmd.exe`, `RuntimeSelector.cs:39-57`) and has no production consumer
  (see findings).

## Deviations from the committed plan (recorded, not silent)

1. **Scope contradiction resolved in favour of including the Settings marking** (D1). `proposal.md`,
   `design.md`, and `tasks.md` now agree; the old "noted but not required" sentence is gone.
2. **DI site named precisely.** The committed plan left it open; registrations cannot go anywhere new
   (`ArchitectureBoundaryTests.cs:53-55`), and the Windows adapters live in
   `WindowsServiceCollectionExtensions.AddWindowsInfrastructure` (`:33-44`), *not* in
   `src/CLIHub/Composition/ServiceRegistration.cs`. The app file changes only for the `SettingsViewModel`
   factory argument (`:163-175`).
3. **Spec delta split by capability** (D9): `agent-commands` for launch behavior, `preferences-ui` for the
   page. The capability file `agent-commands` is kept.
4. **`BuildInteractive` signature changes** (D5); the old delta assumed the locator was consulted only for
   `RuntimeInfo`, which would have left the bare name reachable from the builder.
5. **Test refit promoted from one line (task 3.3) to a stage with a named procedure** (D8).

## Findings outside the backlog (reported, not fixed here)

- `src/CLIHub.Core.Windows/RuntimeSelector.cs:11-86` is registered at
  `WindowsServiceCollectionExtensions.cs:37` but has no production consumer — text search finds only that
  registration and `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs`. It returns
  `RuntimeType.WindowsTerminal` / `Cmd` for a "best runtime", which is unrelated to resolving a configured
  runtime.
- `RuntimeType.WindowsTerminal` (`RuntimeType.cs:8-11`) has no remaining producer other than
  `RuntimeSelector`.

Neither is this change's business; removing them is a dead-code decision that deserves its own change and
a backlog entry (the repository's `docs/backlog.md` has no such item today).

## Backlog cross-check (`docs/backlog.md` at `cd11b88`)

| Item | Trigger | Verdict for this change |
|---|---|---|
| Runtime-hosted agent blocking (Option B), `:9-19` | user reports an update not blocked for a node/python agent | Not fired. This change touches `ProcessLauncher` only; `AgentProcessMatcher` / `WindowsAgentProcessInspector` are untouched. Deferred. |
| Version population loop consolidation, `:21-29` | touching agent version population | Not fired. `AgentVersionService` is not edited; only its test constructor gains a fake locator. Deferred. |
| Magic version sentinels, `:31-37` | adding another agent surface or touching version display | Not fired. Deferred. |
| Transient IDisposable chain, `:91-98` | memory-growth report | Not fired, though the DI file is edited (one factory argument + one adapter registration). Lifetimes are unchanged: the locator is a singleton, the Settings view model stays transient. Deferred. |
| Matcher's unused logger, `:100-104` | next dead-member sweep | Not fired. `AgentProcessMatcher` is not edited. Deferred. |

## Risks / trade-offs

- **Registry layout variance.** `InstallPath` is confirmed for the machine-wide key; the per-user key and
  the 32-bit view are specified but unverified here. Mitigation: they are fallbacks behind well-known
  locations and the PATH scan, and every read is exception-tolerant.
- **PATH scan breadth.** A PATH entry containing a non-Git POSIX shim named `bash.exe` outside the Windows
  directory would be accepted. Accepted trade-off: the alternative (verifying the executable's identity)
  needs process start-up or file inspection the runtime does not have; the registry and well-known steps
  come first.
- **Test environment mutation.** Locator tests must not mutate process PATH (the shim test at
  `WindowsInteractiveProcessRunnerTests.cs:124-125` already does, and parallel tests can race). The
  environment seam exists to avoid adding another mutation.
- **`RuntimeOption` equality.** Rebuilding options with `with` keeps record equality; nothing compares
  options today (grep finds only `SettingsViewModel.cs`), but the new `IsInstalled` member participates in
  equality — harmless because comparison is by `Kind` (`:1231-1232`).
- **Line drift.** Every referenced line was verified at `cd11b88`; a later commit may shift them. The named
  symbols are the stable anchors.

## Manual verification recipe (user-executed, unchecked in tasks.md)

1. `dotnet run --project src/CLIHub/CLIHub.csproj`.
2. Settings → **Terminal Profiles**: Git Bash is offered and (on this machine, where Git is installed at
   `C:\Program Files\Git` but `bash.exe` is not on PATH) labelled as installed, because the registry step
   resolves it.
3. Select Git Bash, Save, close Settings.
4. Launch an agent from the launch window. Expected: an interactive Git Bash window opens in the project
   folder; `pwd` shows a `/c/...` path (Git for Windows), never `/mnt/c/...` (WSL). The log
   (`%APPDATA%\CLIHub\logs\`) contains the resolved absolute path from `WindowsRuntimeLocator`.
5. Negative path (machine without Git for Windows, or temporarily rename
   `C:\Program Files\Git\bin\bash.exe`): the option shows "Not installed", and launching reports
   `launch failed: Failed to launch <agent>` in the launch window status area while the log names the
   missing Git for Windows executable. Restore the file afterwards.
6. PowerShell 7: select PowerShell 7, Save, close Settings, launch an agent. Expected: an interactive
   PowerShell 7 window opens in the project folder and the log contains the resolved absolute
   `pwsh.exe` path. Negative path (temporarily rename `%ProgramFiles%\PowerShell\7\pwsh.exe`): the
   option shows "PowerShell 7 (not installed)" and launching fails with the log naming PowerShell 7
   and the lookup order. Restore the file afterwards.
7. WSL-priority check (only on a machine with `C:\Windows\System32\bash.exe`): prepend
   `C:\Windows\System32` to PATH, repeat step 4, confirm the Git Bash install still starts.

## Open questions

1. Per-user install layout: is the value path really `HKCU\SOFTWARE\GitForWindows\InstallPath` on a
   non-elevated Git for Windows install? It is absent on the development machine, so the implementation
   should treat the per-user read as best-effort and note it in the XML doc rather than assert it.
2. Should the Settings page also disable the Git Bash option (product decision)? Specified as label-only
   in D6; switching to disable is a one-line `ItemContainerStyle` addition if the user prefers it.
3. Should the locator log at Information or Debug on a successful resolve? Proposed Information (one line
   per launch) so support can confirm which executable ran; adjust if the log feels noisy.
4. Is the follow-up worth taking now: carry a failure reason (`string?`) out of `LaunchProcess` so the UI
   can say "Git for Windows was not found"? Deferred by D7; needs a decision because it touches
   `AgentCommandService` and the spec's failure scenario wording.
5. `RuntimeSelector` and `RuntimeType.WindowsTerminal` are dead weight next to this code. Add a backlog
   entry, or fold the removal into a separate change? Not part of this plan.
