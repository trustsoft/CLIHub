# Fix: launch command ignores the project working directory

## Why

The launch-window launch action violates the `agent-commands` contract: the interactive terminal
does not start in the current project folder. The project folder is validated and logged in
`ProcessLauncher.LaunchProcess`, but it is dropped on the way to the process:

- `WindowsCommandLineBuilder.BuildInteractive` accepts `workingDirectory` and never uses it.
- `IInteractiveProcessRunner.LaunchAsync` has no working-directory parameter.
- `WindowsInteractiveProcessRunner` builds a `ProcessStartInfo` without `WorkingDirectory`, so the
  terminal inherits CLIHub's own working directory (for example the install folder or System32).

As a result `launch` (and `resume`) start the agent in the wrong directory in every supported
runtime (PowerShell, PowerShell 7, Command Prompt, Git Bash).

## What Changes

- Pass the validated project folder from `ProcessLauncher` through
  `IInteractiveProcessRunner.LaunchAsync` to the Windows runner.
- Apply the folder as the operating-system process working directory
  (`ProcessStartInfo.WorkingDirectory`) before any shell command runs.
- Fix the PowerShell/PowerShell 7 encoded script: `EncodePowerShell` prepended a call operator
  on top of the one `QuotePowerShellExecutable` already produces, so the terminal received
  `& & 'opencode'` — a parse error — and the agent never started. The script is now a single
  call (`& 'opencode'`). Found by a live UI launch test; the old unit test passed because it
  only asserted the script contained the executable name.
- Keep shell command lines otherwise unchanged; verification is per runtime: PowerShell,
  PowerShell 7, Command Prompt, Git Bash.
- Add tests: the runner receives the project folder; the Windows start info carries it
  (including paths with spaces); a real-process test per available runtime runs the exact
  builder-produced command line and confirms the child starts in the requested folder; the
  encoded PowerShell script is a single call with no doubled call operator.

## Impact

- Affected specs: `agent-commands` (modified requirement: execute a command in a project)
- Affected code:
  - `src/CLIHub.Core/Infrastructure/Processes/IInteractiveProcessRunner.cs`
  - `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
  - `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs`
  - `tests/CLIHub.Core.Tests/Fakes/FakeInteractiveProcessRunner.cs`
  - `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs`
  - `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` (new)
