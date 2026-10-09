# Process Execution

## External Integration
- **Windows Terminal** (`wt.exe`) — spawns CLI tool sessions
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
- `AgentProcessMatcher` matches a process to a plugin by executable name. It also supports matching
  Node.js and Python runtime processes when the inspected process includes a command line containing the
  plugin ID, launch executable, or one of its configured system paths.
- `AgentProcessMonitor` combines the inspector, matcher, and plugin catalog and exposes the matching
  `RunningAgent` records through `HasRunningAgents()` and `GetRunningAgents()`.
- `WindowActionCoordinator` checks the monitor before enabling an agent's **Update** command and checks
  again immediately before execution. When a running agent is found, the update is not started and the
  status area explains that the action is blocked.

The current Windows inspector intentionally does not perform the potentially slow WMI command-line query;
its command-line field is empty in production. Direct executable matching therefore provides the reliable
current detection path, while runtime-based matching remains available to inspectors that supply command
lines.

## Process and Agent Errors

- **Missing executable / permission denied:** log the error, return a failed result, surface a message in the UI
- **Non-zero exit for `version`:** report unknown and log
- **Update while an agent is running:** keep the current version, block the agent update action, and report
  the reason in the launch window status area
