# Design: Git Bash runtime resolution

## Failure shape being fixed

`WindowsCommandLineBuilder.BuildInteractive` emits the bare name `bash.exe` for the Git Bash
runtime, and the child process inherits PATH resolution. Two machine shapes break it:

1. WSL installed, distribution missing/uninitialized — `bash.exe` exits non-zero for every
   command (reproduced on GitHub Actions `windows-latest`: `bash -c true` → exit 1).
2. WSL initialized — the command runs, but inside WSL: `/mnt/c/...` mounts, Linux home and
   profile, Git-for-Windows tooling absent — not the environment the setting promises.

## Resolution order

1. Registry: `HKLM\SOFTWARE\GitForWindows\InstallPath` and
   `HKCU\SOFTWARE\GitForWindows\InstallPath` → `bin\bash.exe` when the file exists.
2. Well-known locations: `%ProgramFiles%\Git\bin\bash.exe`,
   `%ProgramFiles(x86)%\Git\bin\bash.exe`, `%LocalAppData%\Programs\Git\bin\bash.exe`.
3. PATH scan taking the first `bash.exe` that is not under the Windows directory (this excludes
   the WSL launcher in System32).

First hit wins; the resolved absolute path becomes the runtime executable. No hit → the Git
Bash runtime is unavailable.

## Architecture

- New port in `CLIHub.Core` infrastructure (ports pattern): for example
  `IRuntimeExecutableLocator.Locate(RuntimeKind)` returning the absolute executable path or
  null; the Windows implementation holds the registry and filesystem logic.
- `ProcessLauncher.LaunchProcess` consults the locator for `RuntimeKind.GitBash`; the resolved
  path goes into `RuntimeInfo.ExecutablePath`. A null result logs a clear error and reports a
  failed launch (existing failure contract, no throw across the boundary).
- The Terminal Profiles settings page can later read the same locator to mark Git Bash as
  installed/not installed; that UI follow-up is noted but not required by this change.

## Alternatives considered

- Keep the bare name and document PATH requirements — rejected: the failure is silent and
  environment-dependent, exactly what the CI run exposed.
- Detect WSL and refuse only when the resolved bash is the System32 launcher — insufficient:
  an initialized WSL would still silently run the wrong environment.
- Resolve at settings-save time instead of launch time — rejected: installations change between
  saves; launch-time resolution keeps the check truthful.
