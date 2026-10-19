# Tasks: git-bash-resolution (future implementation)

Not implemented yet — this change is the approved design proposal. Tasks are intentionally
unchecked until the implementation is scheduled.

## 1. Resolution

- [ ] 1.1 Port in `CLIHub.Core`: runtime executable locator (Git Bash → absolute path or null)
- [ ] 1.2 Windows implementation: GitForWindows registry key (HKLM/HKCU), well-known install locations, PATH scan excluding the Windows directory
- [ ] 1.3 `ProcessLauncher`: consult the locator for `RuntimeKind.GitBash`; null → logged launch failure

## 2. Settings

- [ ] 2.1 Terminal Profiles page marks Git Bash as not installed when resolution fails

## 3. Tests

- [ ] 3.1 Resolver unit tests: registry hit, well-known hit, PATH hit outside Windows directory, WSL launcher excluded, no hit → null
- [ ] 3.2 `ProcessLauncher`: Git Bash launch uses the resolved path; unresolved → failure result
- [ ] 3.3 Rework the Windows-test bash gating to use the resolver (skip when unresolved) instead of the raw PATH availability check

## 4. Verification

- [ ] 4.1 Unit tests: parsing, resolution order, failure contract
- [ ] 4.2 Live: with WSL launcher first in PATH and Git installed, the Git Bash runtime starts Git's bash in the project folder
- [ ] 4.3 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
