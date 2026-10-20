# Process Execution

## External Integration
- **Windows shells** (`powershell.exe`, `pwsh.exe`, `cmd.exe`) and the resolved Git for Windows `bash.exe` (registry `InstallPath`, well-known install locations, then a PATH entry outside the Windows directory) — spawn CLI tool sessions through the configured runtime
- **Windows process APIs** (`System.Diagnostics.Process`) — enumerates accessible processes for agent safety checks
- **Named mutex** (`Local\CLIHub.SingleInstance`) + **named pipe** (`CLIHub.SingleInstance`) — single instance enforcement and activation
- **user32.dll** (`RegisterHotKey`/`UnregisterHotKey`) — global hotkey registration via source-generated `[LibraryImport]` in `src/CLIHub/Interop/User32.cs`
- **Velopack** — update checks, package downloads, and apply-and-restart against the CLIHub GitHub Releases (`trustsoft/clihub`); the current version comes from the Velopack locator (falling back to the assembly informational version)
- **Registry (HKCU Run)** — the per-user `Software\Microsoft\Windows\CurrentVersion\Run` value `CLIHub` controls start-with-Windows

## Agent Process Monitoring

The Core process subsystem protects agent updates from running against an active agent process:

- `WindowsAgentProcessInspector` enumerates accessible Windows processes and records the process ID,
  executable path, and start time. System processes with low process IDs and processes whose executable
  path cannot be read are skipped.
- `AgentProcessMatcher` matches a process to a plugin by the launch executable's file name, ignoring
  case. Agents launched through a command shim into a shared runtime — for example an npm package that
  runs as `node.exe` — are not matched.
- `AgentProcessMonitor` combines the inspector, matcher, and plugin catalog and exposes the matching
  `RunningAgent` records through `HasRunningAgents()` and `GetRunningAgents()`.
- `WindowActionCoordinator` checks the monitor before enabling an agent's **Update** command and checks
  again immediately before execution. When a running agent is found, the update is not started and the
  status area explains that the action is blocked.

The Windows inspector does not retrieve command lines; it supplies only the process ID, executable path,
and start time. Process matching is therefore executable-name based, and an agent hosted inside a shared
runtime such as `node.exe` is not detected, so its update is not blocked. Closing that gap requires a
separate process-identity design, either by tracking the processes CLIHub launches or by adding a cached,
asynchronous process snapshot with command-line details.

## Process and Agent Errors

- **Missing executable / permission denied:** log the error, return a failed result, surface a message in the UI
- **Git Bash not installed:** the Git Bash runtime resolves to a Git for Windows `bash.exe` before the command line is built; when nothing resolves, the launch fails before any process starts, the log names Git for Windows and the lookup order, and the Terminal Profiles page marks the option as not installed while keeping it selectable
- **Non-zero exit for `version`:** report unknown and log
- **Update while an agent is running:** keep the current version, block the agent update action, and report
  the reason in the launch window status area
