# Resolve the Git Bash runtime to Git for Windows, never to the WSL launcher

## Why

The Git Bash runtime launches the bare name `bash.exe`, and the operating system resolves it via
PATH order. On machines where the WSL launcher (`C:\Windows\System32\bash.exe`) precedes Git's
bash, the "Git Bash" runtime actually starts WSL: a different environment (`/mnt/c/...` instead
of `/c/...` mounts, the Linux-side home, no Git-for-Windows profile), and on systems without an
initialized WSL distribution the launcher fails for any command.

Observed directly: on GitHub Actions `windows-latest` runners the first `bash.exe` on PATH is the
System32 WSL launcher without an installed distribution, and even `bash -c true` exits non-zero.
The same machine shape exists for users (WSL present but unused, or Git installed per-user only),
and for them the Git Bash runtime setting produces an immediate, confusing failure.

## What Changes

- Deterministic resolution of the Git Bash executable, in priority order:
  1. the `GitForWindows` install path from the registry (`HKLM`/`HKCU` `SOFTWARE\GitForWindows\InstallPath`) + `bin\bash.exe`
  2. well-known locations: `%ProgramFiles%\Git\bin\bash.exe`, `%ProgramFiles(x86)%\Git\bin\bash.exe`, `%LocalAppData%\Programs\Git\bin\bash.exe`
  3. PATH scan excluding anything under the Windows directory (never the WSL launcher)
- When Git Bash resolves, its full path is used as the runtime executable instead of the bare
  name; when it does not, the Git Bash runtime is unavailable: launching fails with a clear
  logged error instead of silently starting WSL, and the Terminal Profiles settings page marks
  Git Bash as not installed.
- A launcher (port in `CLIHub.Core`, implementation in `CLIHub.Core.Windows`) performs the
  resolution; `ProcessLauncher` consults it for the Git Bash runtime.

## Impact

- Affected specs: `agent-commands` (modified requirement: terminal vs captured execution)
- Affected code (future): `CLIHub.Core` port and `ProcessLauncher`, `CLIHub.Core.Windows`
  resolver, Terminal Profiles settings availability
- Non-goals: supporting WSL as an explicit runtime kind; changing the other runtimes'
  resolution; auto-switching the configured runtime when Git Bash is missing
