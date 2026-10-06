# Process Execution

## External Integration
- **Windows Terminal** (`wt.exe`) — spawns CLI tool sessions
- **Named mutex** (`Local\CLIHub.SingleInstance`) + **named pipe** (`CLIHub.SingleInstance`) — single instance enforcement and activation
- **user32.dll** (`RegisterHotKey`/`UnregisterHotKey`) — global hotkey registration via source-generated `[LibraryImport]` in `src/CLIHub/Interop/User32.cs`
- **Velopack** — update checks, package downloads, and apply-and-restart against the CLIHub GitHub Releases (`trustsoft/clihub`); the current version comes from the Velopack locator (falling back to the assembly informational version)
- **Registry (HKCU Run)** — the per-user `Software\Microsoft\Windows\CurrentVersion\Run` value `CLIHub` controls start-with-Windows

## Process and Agent Errors

- **Missing executable / permission denied:** log the error, return a failed result, surface a message in the UI
- **Non-zero exit for `version`:** report unknown and log
