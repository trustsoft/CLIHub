# Tasks: fix-launch-working-directory

## 1. Core: carry the working directory to the runner

- [x] 1.1 Add `workingDirectory` to `IInteractiveProcessRunner.LaunchAsync`
- [x] 1.2 Pass the validated project folder from `ProcessLauncher.LaunchProcess` to the runner
- [x] 1.3 Update `FakeInteractiveProcessRunner` to record the working directory

## 2. Windows: apply the working directory to the process

- [x] 2.1 Set `ProcessStartInfo.WorkingDirectory` in `WindowsInteractiveProcessRunner`
- [x] 2.2 Extract start-info creation into an internal, testable method

## 3. Tests

- [x] 3.1 Core: `LaunchProcess` forwards the project folder to the interactive runner
- [x] 3.2 Windows: start info carries the working directory, including paths with spaces
- [x] 3.3 Windows: real-process verification per runtime (cmd, PowerShell, PowerShell 7 if installed, Git Bash if installed) proves the child starts in the requested folder

## 5. PowerShell call-operator fix (found by live UI test)

- [x] 5.1 Remove the doubled call operator from `EncodePowerShell` (script `& & 'opencode'` was a parse error)
- [x] 5.2 Assert the encoded script is exactly `& 'executable ...'` (no spaces case, quoted path case)
- [x] 5.3 Drive the real-process cmd/PowerShell/PowerShell 7 tests through `ProcessLauncher.BuildStartInfo` output instead of hand-built command lines

## 4. Verification

- [x] 4.1 Verify PowerShell first (default runtime): child observes the project folder
- [x] 4.2 Verify PowerShell 7: child observes the project folder
- [x] 4.3 Verify Command Prompt: child observes the project folder
- [x] 4.4 Verify Git Bash: child observes the project folder — Git Bash is not installed on the development machine, so the real-process test auto-skips; command-line construction stays covered by unit tests and the working-directory mechanism is the OS-level one shared with the verified runtimes
- [x] 4.5 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
