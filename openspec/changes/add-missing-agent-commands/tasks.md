# Tasks: add-missing-agent-commands

## 1. Descriptors

- [x] 1.1 `cline-cli`: add `update` = `cline update`
- [x] 1.2 `github-copilot`: add `resume` = `copilot --continue`
- [x] 1.3 `openclaude`: add `update` = `openclaude update`
- [x] 1.4 `qwen-code`: add `resume` = `qwen --continue`
- [x] 1.5 Update the installed copies under `%APPDATA%\CLIHub\plugins\` the same way (seeding never overwrites user content)
- [x] 1.6 `cline` resume intentionally omitted: the CLI resumes only by explicit session id (`cline --id <session-id>`), verified against docs.cline.bot CLI reference

## 2. Tests

- [x] 2.1 Lock the expected command set per built-in agent in `PluginSeederTests`

## 3. Verification

- [x] 3.1 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
- [x] 3.2 Live: GitHub Copilot resume started `copilot --continue` (child copilot.exe); Qwen Code resume started `qwen --continue` (node shim) in the project folder
- [x] 3.3 Live: Update entries for Cline CLI and OpenClaude are present and disabled by the running-agents safety block (user's opencode processes active); run them manually with no agents running

