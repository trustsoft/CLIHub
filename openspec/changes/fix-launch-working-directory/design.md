# Design: launch working directory

## Context

`ProcessLauncher.LaunchProcess(command, workingDirectory)` already validates the project folder,
then builds the runtime-specific command line and hands only `(fileName, arguments)` to the
interactive runner. The folder never reaches `ProcessStartInfo`, so every interactive command
inherits CLIHub's own working directory.

## Decision

Apply the working directory at the operating-system level:

- `IInteractiveProcessRunner.LaunchAsync` gains a `workingDirectory` parameter.
- `WindowsInteractiveProcessRunner` sets `ProcessStartInfo.WorkingDirectory` to it.
- Shell command lines stay unchanged.

### Alternatives considered

- Embed `cd` / `Set-Location` in each shell's command line — rejected: it needs per-shell quoting
  and path translation (Git Bash needs POSIX paths), duplicates information already available to
  the OS, and cannot be verified independently of the shell parser.
- Set `Environment.CurrentDirectory` of the CLIHub process before launching — rejected: mutating
  the whole application's working directory is a process-wide side effect and is racy for
  concurrent launches.

The OS-level working directory is applied by `CreateProcess` before any shell profile or command
runs, behaves identically for PowerShell, PowerShell 7, Command Prompt, and Git Bash, and is
directly assertable in tests.

## Per-runtime verification order

One runtime at a time, next only after the previous one passes:

1. PowerShell (`powershell.exe`) — the default runtime.
2. PowerShell 7 (`pwsh.exe`) — verified only when installed.
3. Command Prompt (`cmd.exe`).
4. Git Bash (`bash.exe`) — verified only when installed.

Headless verification runs the same start-info shape the launcher produces, minus the
keep-open flags (`-NoExit`, `/k`), and asserts the child process observes the requested folder.
Keep-open flags do not affect working-directory propagation.
