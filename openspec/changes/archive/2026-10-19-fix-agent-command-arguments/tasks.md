# Tasks: fix-agent-command-arguments

## 1. Builder

- [ ] 1.1 `QuotePowerShellExecutable`: keep direct form for commands without arguments
- [ ] 1.2 Bare PATH name with arguments: resolve via `Get-Command -CommandType Application`, invoke with `--%`
- [ ] 1.3 Executable path with arguments: direct `--%` for `.exe`/`.com`, resolution form otherwise (wildcard-escaped literal)

## 2. Tests

- [ ] 2.1 Exact script shapes: bare name + args; quoted `.exe` path + args; no args
- [ ] 2.2 Real-process: PATH-resolved `.cmd` probe receives only the intended flag (no `--%`)

## 3. Verification

- [ ] 3.1 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`
- [ ] 3.2 Live UI: resume for OpenCode starts the TUI; opencode log shows `args=["--continue"]`
- [ ] 3.3 Live UI: launch still works (no-argument form unchanged)
- [ ] 3.4 `openspec validate --all`
