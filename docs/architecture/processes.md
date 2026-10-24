# Process Execution

## External Integration
- **Windows shells** (`powershell.exe`, `cmd.exe`) plus the resolved executables for the runtimes that must not be left to PATH order — Git for Windows `bash.exe` (registry `InstallPath`, well-known install locations, then a PATH entry outside the Windows directory) and PowerShell 7 `pwsh.exe` (the `PowerShellCore` registry `InstallPath`, the well-known `ProgramFiles\PowerShell\7` location, then a PATH entry) — spawn CLI tool sessions through the configured runtime
- **Windows process APIs** (`System.Diagnostics.Process`) — enumerates accessible processes for agent safety checks
- **Named mutex** (`Local\CLIHub.SingleInstance`) + **named pipe** (`CLIHub.SingleInstance`) — single instance enforcement and activation
- **user32.dll** (`RegisterHotKey`/`UnregisterHotKey`) — global hotkey registration via source-generated `[LibraryImport]` in `src/CLIHub/Interop/User32.cs`
- **Velopack** — update checks, package downloads, and apply-and-restart against the CLIHub GitHub Releases (`trustsoft/clihub`); the current version comes from the Velopack locator (falling back to the assembly informational version)
- **Registry (HKCU Run)** — the per-user `Software\Microsoft\Windows\CurrentVersion\Run` value `CLIHub` controls start-with-Windows

## Agent Process Monitoring

The Core process subsystem protects agent updates from running against an active agent process:

- `WindowsAgentProcessInspector` enumerates accessible Windows processes and records the process ID,
  executable path, and start time. System processes with low process IDs and processes whose executable
  path cannot be read are skipped. Processes are pre-filtered by name against the registered launch
  executables before their details are read — the process name is assumed to equal the executable
  image's file name without extension, so no name-based match can be dropped by the filter. Under the
  command-line detail tier it additionally joins command lines onto the candidate processes with one
  bulk WMI query (`SELECT ProcessId, CommandLine FROM Win32_Process`, name-filtered to the candidate
  names) inside a hard 2-second wall-clock budget. On a query failure or timeout it logs one warning
  and degrades to executable-name matching; no availability is withheld because retrieval degraded.
- `AgentProcessMatcher` matches a process to a plugin by the launch executable's file name, ignoring
  case, and — for plugins that declare an npm `updateCheck` package — by a runtime-host command line:
  a process running inside a known shared runtime host (`node`, `python`, `pythonw`; shells are
  deliberately excluded) matches when its command line contains `node_modules\` + the declared npm
  package + a trailing separator, compared ignoring case after forward slashes are normalized. The
  trailing separator is the boundary, so `@opencode/cli` does not match `@opencode/cli-extra`. Matching
  runs in two detail tiers: the cheap executable-name tier never retrieves command lines, and the
  command-line tier adds the runtime-host rule and is authoritative for the execution-time update
  gates.
- `AgentProcessMonitor` combines the inspector, matcher, and plugin catalog and exposes the matching
  `RunningAgent` records through `HasRunningAgents()` and `GetRunningAgents()`; the runtime-host names
  join the candidate set exactly when command-line detail is requested and at least one plugin declares
  an npm package, so the name pre-filter never drops a match the matcher would make, and a machine
  full of `node.exe` processes with no npm-declared agent never triggers a WMI read.
- `WindowActionCoordinator` checks the monitor before enabling an agent's **Update** command (the
  cheap executable-name tier as an advisory early-out; a runtime-hosted agent can therefore leave the
  button enabled) and re-checks at execution time with the full command-line tier off the UI thread
  before anything starts. `AgentManagementService.UpdateAsync` runs the same full-tier check for the
  Settings agent cards. When a running agent is found, the update is not started and the status area
  explains that the action is blocked.

Known residuals: command lines of elevated or other-session processes are often unreadable and simply
absent from the join; a degraded pass (WMI failure, timeout, or unreadable command line) matches by
executable name only and fails open — the update proceeds exactly as the enumeration-failure contract
already prescribes. Closing those residuals would require a different process-identity mechanism and
is deliberately out of scope.

## Process and Agent Errors

- **Missing executable / permission denied:** log the error, return a failed result, surface a message in the UI
- **Git Bash not installed:** the Git Bash runtime resolves to a Git for Windows `bash.exe` before the command line is built; when nothing resolves, the launch fails before any process starts, the log names Git for Windows and the lookup order, and the Terminal Profiles page marks the option as not installed while keeping it selectable
- **PowerShell 7 not installed:** the PowerShell 7 runtime resolves to a `pwsh.exe` before the command line is built (no Windows-directory exclusion on the PATH scan — `pwsh.exe` has no WSL-launcher-shaped namesake, and Store installs surface as an app-execution alias in `%LOCALAPPDATA%\Microsoft\WindowsApps`); when nothing resolves, the launch fails before any process starts, the log names PowerShell 7 and the lookup order, and the Terminal Profiles page marks the option as not installed while keeping it selectable
- **Non-zero exit for `version`:** report unknown and log
- **Update while an agent is running:** keep the current version, block the agent update action, and report
  the reason in the launch window status area
