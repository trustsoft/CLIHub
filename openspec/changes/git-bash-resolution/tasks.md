# Tasks: git-bash-resolution

Not implemented yet. Written against `master` at `cd11b88` (2026-10-20) and verified against the tree;
`design.md` carries the evidence and the decisions. Work the stages in order: each one ends green.

Conventions: file-scoped namespaces with `using` directives inside the namespace, `this.` for instance
members, XML documentation on public APIs, test names `MethodOrScenario_Condition_ExpectedResult`.

## 1. Preflight

- [ ] 1.1 Re-read `design.md` §"Verified current state". Open the six cited files and confirm the anchors
      still exist at HEAD: `WindowsCommandLineBuilder.cs:29`, `ProcessLauncher.cs:52-125`,
      `WindowsInteractiveProcessRunner.cs:81-94`, `WindowsServiceCollectionExtensions.cs:33-44`,
      `SettingsViewModel.cs:229-235,491,1231-1232`, `SettingsTerminalPage.xaml:14-32`. If a cited symbol is
      gone, stop and update `design.md` before coding.
- [ ] 1.2 Baseline: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` must be green before any edit.
- [ ] 1.3 Confirm no unrelated change is in flight in the same files. `openspec/changes/agent-update-availability`
      touches plugin descriptors, an HTTP service, and the agents pane — no overlap.

## 2. Core port

- [ ] 2.1 Add `src/CLIHub.Core/Infrastructure/Processes/IRuntimeExecutableLocator.cs`: public interface,
      one method `string? Locate(RuntimeKind kind)`, XML docs stating (a) the return is an absolute path or
      `null` when this locator cannot resolve the kind, (b) the Windows implementation resolves only
      `RuntimeKind.GitBash` today and `null` for other kinds means "not covered", not "not installed",
      (c) the result is not cached. `using CLIHub.Core.Models;` inside the namespace.
- [ ] 2.2 Gate: `dotnet build CLIHub.sln`.

## 3. Windows locator

- [ ] 3.1 Add `src/CLIHub.Core.Windows/GitForWindowsRuntimeLocator.cs`:
      - `internal interface IGitBashEnvironment` with the five members from design.md D4
        (`ReadGitForWindowsInstallPath`, `GetWellKnownInstallRoots`, `GetPathDirectories`,
        `GetWindowsDirectory`, `FileExists`).
      - `internal sealed class WindowsGitBashEnvironment : IGitBashEnvironment` backed by
        `Microsoft.Win32.Registry` (same style as `StartupRegistry.cs:18-54`, including the
        `OperatingSystem.IsWindows()` guard), `Environment.GetFolderPath` / `GetEnvironmentVariable`, and
        `File.Exists`. Registry: `InstallPath` under `GitForWindows`, `HKLM` with `RegistryView.Registry64`
        then `Registry32`, `HKCU` default view then `Registry32`; every read wrapped so
        `IOException`/`SecurityException`/`UnauthorizedAccessException` is a miss.
      - `public sealed class GitForWindowsRuntimeLocator : IRuntimeExecutableLocator` implementing the
        three-step order from design.md D3 with an `internal` constructor
        `(IGitBashEnvironment environment, ILogger<GitForWindowsRuntimeLocator> logger)`. Debug-log each
        rejected candidate, Information-log the resolved path, Warning-log total failure.
      - Path candidates are `Path.Combine(root, "bin", "bash.exe")`; the PATH step skips any directory under
        `GetWindowsDirectory()` and requires `FileExists`.
- [ ] 3.2 Register the adapter in `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:37-41`
      through a factory, next to `ProcessLauncher`:
      `services.AddSingleton<IRuntimeExecutableLocator>(sp => new GitForWindowsRuntimeLocator(new WindowsGitBashEnvironment(), sp.GetRequiredService<ILogger<GitForWindowsRuntimeLocator>>()));`
      Add `IRuntimeExecutableLocator` to the interface-registration group and keep the XML docs of
      `AddWindowsInfrastructure` truthful about the added adapter. Do not move any existing registration.
- [ ] 3.3 Add `tests/CLIHub.Core.Windows.Tests/FakeGitBashEnvironment.cs` implementing
      `IGitBashEnvironment` with a settable install path, install roots, PATH entries, Windows directory,
      and an existing-path set (guard paths with `Path.GetFullPath` + `StringComparer.OrdinalIgnoreCase`).
- [ ] 3.4 Add `tests/CLIHub.Core.Windows.Tests/GitForWindowsRuntimeLocatorTests.cs`:
      - `Locate_GitBash_RegistryInstallPathWins` (all three sources offer a candidate; the registry path is
        returned, case-insensitively compared)
      - `Locate_GitBash_FallsBackToWellKnownRootWhenRegistryMisses` (ProgramFiles root hit; also cover the
        `ProgramFilesX86` and `LocalAppData\Programs\Git` roots)
      - `Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed` (temp-style path entry)
      - `Locate_GitBash_WslLauncherOnPath_ReturnsNull` (PATH holds `C:\Windows\System32` only; Windows
        directory = `C:\Windows`)
      - `Locate_GitBash_NoCandidate_ReturnsNull`
      - `Locate_NonGitBashKind_ReturnsNull` (`PowerShell`, `PowerShell7`, `CommandPrompt`)
      - `Locate_GitBash_RegistryThrows_FallsThroughToPath` (fake throws, PATH still resolves) if the fake
        supports a throw switch; otherwise assert the exception-tolerant read inside
        `WindowsGitBashEnvironment` via a separate narrow test.
      Use `NullLogger<GitForWindowsRuntimeLocator>.Instance`. Do not touch the process-wide PATH.
- [ ] 3.5 Gate: `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Core.Windows.Tests`.

## 4. Launcher wiring

- [ ] 4.1 `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs:19-32`: add a fourth
      parameter `string? gitBashExecutable` to `BuildInteractive`; Git Bash arm uses it and throws
      `InvalidOperationException("Git Bash executable was not resolved")` when it is null. Document the
      parameter (absolute path from the locator; other runtimes ignore it). Do not change `-lic` or the
      quoting helpers (`:133-152`).
- [ ] 4.2 `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`:
      - add the required `IRuntimeExecutableLocator` constructor parameter (`:27-34`) and its null check,
        XML doc line;
      - in `LaunchProcess`, after `:78-80` and before `:85`, resolve for `RuntimeKind.GitBash` and return
        `false` with a `LogError` naming Git for Windows and the lookup order when the result is null;
      - pass the resolved path through `BuildStartInfo` (`:197-202`) into `BuildInteractive`.
      No other behavior change; keep the fire-and-forget `LaunchAsync` at `:102` and the catch blocks at
      `:106-124`.
- [ ] 4.3 Add `tests/CLIHub.Core.Tests/Fakes/FakeRuntimeExecutableLocator.cs`: `Locate` returns a
      configurable path per kind (default null) and records the kinds asked for.
- [ ] 4.4 Update the seven `new ProcessLauncher(...)` sites listed in design.md with the fake:
      `ProcessLauncherTests.cs:149,177`, `AgentVersionServiceTests.cs:20`, `AgentManagementServiceTests.cs:73`,
      `AgentCommandServiceTests.cs:23`, `ProcessLauncherCaptureOutputTests.cs:81`,
      `StartupPreferencesApplierTests.cs:50`.
- [ ] 4.5 `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs`:
      - remove the `GitBash` `InlineData` from `BuildStartInfo_UsesRuntimeExecutable` (`:15`) and pass
        `gitBashExecutable: null` for the remaining runtimes;
      - rewrite `BuildStartInfo_GitBash_UsesLoginInteractiveShell` (`:91-103`) to pass
        `@"C:\Program Files\Git\bin\bash.exe"` and assert that exact `fileName` plus the unchanged `-lic`
        and argument content;
      - add `LaunchProcess_GitBash_UsesResolvedExecutablePath` — fake locator returns the Git path, assert
        one launch and `launch.Runtime.ExecutablePath` equals it;
      - add `LaunchProcess_GitBashUnresolved_ReturnsFalseAndDoesNotStart` — fake returns null, assert
        `false` and `Assert.Empty(runner.Launches)`;
      - add `LaunchProcess_CommandPrompt_DoesNotConsultLocator` — assert the fake recorded no kinds (other
        runtimes must not resolve).
- [ ] 4.6 `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs:11-27`: add
      `Assert.NotNull(provider.GetRequiredService<IRuntimeExecutableLocator>());` so the composition
      contract is pinned.
- [ ] 4.7 Gate: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` (all three test projects).

## 5. Terminal Profiles availability marking

- [ ] 5.1 `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`:
      - `:31` — `RuntimeOption(RuntimeKind Kind, string Name, string Icon, bool IsInstalled = true)` plus
        `DisplayName` (`"{Name} (not installed)"` when unavailable) with XML docs on both;
      - `:229-235` — keep the static template array as the source of truth;
      - add `private RuntimeOption[] runtimes = RuntimeOptions;` and change `:491` to return it;
      - add the required `IRuntimeExecutableLocator` constructor parameter (`:433-444`) and field;
      - in `Load()` (`:1227-1337`), rebuild `this.runtimes` — Git Bash `IsInstalled` = locator resolved —
        before the stored-runtime assignment (`:1231-1232`), and raise
        `OnPropertyChanged(nameof(this.Runtimes))` with the other notifications (`:1295-1336`).
- [ ] 5.2 `src/CLIHub/Composition/ServiceRegistration.cs:163-175`: pass
      `sp.GetRequiredService<IRuntimeExecutableLocator>()` as the new argument. Update the XML doc of
      `AddSettingsServices` if it enumerates dependencies.
- [ ] 5.3 `src/CLIHub/Views/SettingsTerminalPage.xaml:15-31`: bind the item text (`:27`) to `DisplayName`
      and add a `TextBlock` style trigger that mutes the text when `IsInstalled` is `False`
      (`DataTrigger`, pattern from `SettingsAgentsPage.xaml:13-28`), plus
      `AutomationProperties.HelpText` explaining that Git for Windows was not found. Keep the option
      selectable; do not add an `ItemContainerStyle` disabling it.
- [ ] 5.4 Update the four `new SettingsViewModel(...)` sites with a fake/stub locator:
      `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:735`,
      `SettingsNavigationTests.cs:125`, `SettingsOperationLifetimeTests.cs:33`,
      `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs:128`. Reuse
      `Moq` (`new Mock<IRuntimeExecutableLocator>()` with `Locate` returning a path or null) — do not add a
      second fake type unless one is shared.
- [ ] 5.5 Add to `SettingsViewModelApplicationTests`:
      `Load_GitBashUnresolved_MarksGitBashNotInstalled` (locator returns null → Git Bash
      `IsInstalled` false, `DisplayName` contains "not installed", the other three true) and
      `Load_GitBashResolved_LeavesAllRuntimesInstalled`. Assert by `Kind`, not by index.
- [ ] 5.6 Gate: `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests`.

## 6. Refit the PATH-dependent Windows Git Bash test

- [ ] 6.1 `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:57-104`:
      - replace the `IsOnPath("bash.exe")` gate (`:59-62`) with
        `new GitForWindowsRuntimeLocator(...)`/locator `Locate(RuntimeKind.GitBash)`; return when null. Keep
        the `IsOnPath` helper (`:199-209`) — the PowerShell 7 test still uses it (`:45`).
      - set `RuntimeInfo.ExecutablePath` to the resolved path (`:71`) instead of `"bash.exe"`.
      - delete the `-c "true"` usability probe and its comment (`:74-84`); with the resolved Git for Windows
        bash the WSL launcher is out of reach. `Assert.Equal(0, exitCode)` (`:96`) stays and now applies to
        every machine with Git installed.
      - keep the flavor-agnostic report probe (`:86-98`) and `Assert.Contains("clihub launch dir", reported)`;
        optionally add an assertion that the resolved path is not under the Windows directory.
      - note in a comment that the test now proves the resolver contract end to end: resolved executable →
        child starts in the requested folder.
- [ ] 6.2 Gate: `dotnet test tests/CLIHub.Core.Windows.Tests` and confirm the test either runs to green or
      skips only because Git for Windows is absent (on this machine it runs — see design.md "Observed
      machine state").

## 7. Docs and final gates

- [ ] 7.1 `docs/architecture/processes.md`: in "External Integration" (`:4`) replace the plain `bash.exe`
      mention with the resolved Git for Windows path contract; add one bullet under "Process and Agent
      Errors" stating that an unresolved Git Bash fails before the process starts, is logged with the
      lookup order, and that the Terminal Profiles page marks the option as not installed.
- [ ] 7.2 `dotnet build CLIHub.sln`.
- [ ] 7.3 `dotnet test CLIHub.sln` — all three projects, including
      `ArchitectureBoundaryTests.DependencyRegistration_IsRestrictedToCompositionRoots` (the new
      registration lives in an allowed file).
- [ ] 7.4 `openspec validate --all`.
- [ ] 7.5 `graphify update .` after all source edits are complete.
- [ ] 7.6 Confirm the diff contains no unrelated edits (no `RuntimeSelector` removal, no quoting changes, no
      backlog edits) and report the result for approval; do not commit.

## 8. Manual verification (user-executed)

- [ ] 8.1 Run the recipe in `design.md` §"Manual verification recipe" (run the app, confirm the option is
      labelled, launch an agent, check the `/c/...` prompt and the resolved path in the log, then the negative
      path by renaming `C:\Program Files\Git\bin\bash.exe` and restoring it). Report the outcome; keep this
      item unchecked until the user confirms.
- [ ] 8.2 If a machine with `C:\Windows\System32\bash.exe` is available, repeat with `System32` first on PATH
      to confirm the WSL launcher is never selected.

## 9. Out of scope (do not fold into this change)

- WSL as an explicit runtime kind; resolving or gating `cmd.exe`, `powershell.exe`, or `pwsh.exe`.
- Auto-switching the configured runtime when Git Bash is missing; caching the resolution.
- Widening `LaunchProcess`'s `bool` into a reason-carrying result (design.md D7, open question 4).
- Removing `RuntimeSelector` / `RuntimeType.WindowsTerminal`, the matcher's unused logger, or any other
  dead-member sweep (design.md findings, `docs/backlog.md:100-104`).
- Any change to the captured-output path, `RuntimeInfo`, or the `-lic` quoting rules.
