# Fix: interactive arguments leak the stop-parsing token to PATH-resolved agents

## Why

A live UI test of the resume action (plugin command `opencode --continue`) showed the agent
exiting immediately with its help screen. The opencode log recorded the received arguments as
`["--%", "--continue"]`: the PowerShell stop-parsing token `--%` reached the agent literally.

Cause: `WindowsCommandLineBuilder.QuotePowerShellExecutable` builds `& 'opencode' --% <args>`.
The stop-parsing token is honored only when the invoked command is a native executable. On PATH,
`opencode` resolves to `opencode.ps1` (an ExternalScript shim), for which `--%` is a plain string —
so the shim forwarded `--%` to the agent, which rejected it as an unknown flag and exited.
Version probes masked the defect because `--version` is intercepted before flag validation.

The same shape affects every interactive command with arguments (launch, resume, update) whenever
the agent resolves to a script shim, which is the normal case for npm-installed agents.

## What Changes

- When an interactive command carries arguments and the executable is a bare PATH name, the
  constructed PowerShell script first resolves the name to a native application
  (`Get-Command -CommandType Application`) and invokes that with the stop-parsing token.
- Executable paths ending in a native extension (`.exe`/`.com`) keep the direct
  `& '<path>' --% <args>` form (verified working).
- Commands without arguments keep the direct `& '<name>'` form (verified working: the script
  shim runs the agent without argument translation).
- Tests: exact script shapes for bare-name-with-arguments, quoted-exe, and no-argument cases;
  a real-process test runs a PATH-resolved `.cmd` probe through the builder output and asserts
  the agent-side arguments arrive without the stop-parsing token.

## Impact

- Affected specs: `agent-commands` (modified requirement: command arguments preserve boundaries)
- Affected code:
  - `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs`
  - `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs`
  - `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`
