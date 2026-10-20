# Resolve the Git Bash runtime to Git for Windows, never to the WSL launcher

## Why

The Git Bash runtime launches the bare name `bash.exe` (`src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs:29`)
and the child process inherits the operating system's PATH lookup
(`src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs:81-94`). The setting therefore identifies no
executable: whatever `bash.exe` PATH yields first runs.

Three machine shapes break the setting:

1. **WSL launcher first on PATH, no distribution** — `bash.exe` is `C:\Windows\System32\bash.exe` and exits
   non-zero for every command (observed on GitHub Actions `windows-latest` when this change was first
   written; not re-observed in this refresh).
2. **WSL initialized** — the command runs, but inside WSL: `/mnt/c/...` mounts, Linux home and profile,
   none of the Git-for-Windows tooling the setting promises.
3. **Git for Windows installed but not on PATH** — the bare name resolves to nothing and the launch fails
   outright. Verified on the development machine on 2026-10-20: `HKLM\SOFTWARE\GitForWindows\InstallPath`
   = `C:\Program Files\Git`, `C:\Program Files\Git\bin\bash.exe` exists, no `bash.exe` is on PATH, and
   `C:\Windows\System32\bash.exe` is absent. Choosing "Git Bash" there cannot start anything.

The Git for Windows installation path is recorded in the registry and in well-known locations, so the
runtime can be identified deterministically instead of left to PATH order.

## What Changes

- New port `IRuntimeExecutableLocator` in `CLIHub.Core` (`string? Locate(RuntimeKind)`) and a Windows
  implementation that resolves Git Bash in this order, first hit wins:
  1. registry `HKCU`/`HKLM` `SOFTWARE\GitForWindows` value `InstallPath` + `bin\bash.exe`
     (64-bit and 32-bit registry views for `HKLM`);
  2. well-known locations: `%ProgramFiles%\Git`, `%ProgramFiles(x86)%\Git`,
     `%LocalAppData%\Programs\Git` + `bin\bash.exe`;
  3. PATH scan taking the first `bash.exe` whose directory is **not** under the Windows directory.
- `ProcessLauncher` asks the locator for the Git Bash executable before building the command line and puts
  the absolute path into `RuntimeInfo.ExecutablePath`. No bare-name fallback remains on the launch path.
- When resolution fails, the launch returns the existing failure result (`LaunchProcess` → `false` →
  `AgentCommandResult(false, …, "Failed to launch <agent>")`) with a clear error logged; no terminal opens.
- The Terminal Profiles settings page marks Git Bash as not installed when resolution fails. The option
  stays selectable and the stored value stays shown.
- **PowerShell 7 gets the same availability treatment** (added 2026-10-20 before archiving):
  `ProcessLauncher` resolves `pwsh.exe` before building the command line, in this order, first hit wins:
  1. registry `HKLM\SOFTWARE\Microsoft\PowerShellCore` value `InstallPath` — the directory containing
     `pwsh.exe` (64-bit view, then 32-bit view; no `HKCU`, because the MSI is machine-wide and per-user
     installs surface through PATH);
  2. well-known `%ProgramFiles%\PowerShell\7\pwsh.exe`;
  3. PATH scan taking the first `pwsh.exe` — no Windows-directory exclusion, because `pwsh.exe` has no
     WSL-launcher-shaped namesake and Microsoft Store installs surface as an app-execution alias in
     `%LOCALAPPDATA%\Microsoft\WindowsApps` that the scan must find.

  An unresolved PowerShell 7 fails the launch synchronously with a logged lookup-order error, and the
  Terminal Profiles page marks PowerShell 7 as not installed (label-only, selection preserved).
  `cmd.exe` and `powershell.exe` stay bare-name by design.
- Tests: locator unit tests, `ProcessLauncher` resolution/contract tests, and the Windows test that
  currently probes PATH for a working `bash` switches to the resolver.

## Impact

- Affected specs:
  - `agent-commands` (MODIFIED: *Terminal vs captured execution* — launch-time resolution and safe failure)
  - `preferences-ui` (MODIFIED: *Terminal runtime options* — Git Bash and PowerShell 7 availability marking)
- Affected code (future):
  - `src/CLIHub.Core/Infrastructure/Processes/IRuntimeExecutableLocator.cs` (new port)
  - `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`, `WindowsCommandLineBuilder.cs`
  - `src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs` and its OS seam (new; named
    `GitForWindowsRuntimeLocator.cs` until the PowerShell 7 extension renamed it)
  - `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs` (adapter registration)
  - `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`,
    `src/CLIHub/Views/SettingsTerminalPage.xaml`, `src/CLIHub/Composition/ServiceRegistration.cs`
  - tests in `CLIHub.Core.Tests`, `CLIHub.Core.Windows.Tests`, `CLIHub.Tests`
- Docs (future): `docs/architecture/processes.md` gains the Git Bash resolution contract and, with the
  PowerShell 7 extension, the `pwsh.exe` resolution contract beside it.
- Non-goals:
  - supporting WSL as an explicit runtime kind;
  - resolving or gating the remaining Windows-shipped runtimes (`powershell.exe`, `cmd.exe`) —
    PowerShell 7 was moved out of the non-goals by the 2026-10-20 extension;
  - auto-switching the configured runtime when Git Bash is missing;
  - caching the resolution (read on launch and when Settings opens);
  - removing the unused `RuntimeSelector` or changing the captured-output path;
  - changing how a resolved executable's arguments are quoted (`-lic` quoting is untouched).

## Dependencies

- None blocking. The plan is written against `master` at `cd11b88` and assumes the post-`9941cd6` DI layout
  (feature modules in `src/CLIHub/Composition/ServiceRegistration.cs`) and the post-`91035c2` process
  subsystem (no runtime matching in the matcher).
- The active change `agent-update-availability` touches plugin descriptors, an HTTP availability service,
  and the agents pane; it does not touch `ProcessLauncher`, the terminal page, or the same tests.
