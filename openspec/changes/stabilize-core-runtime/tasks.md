## 1. Baseline and regression coverage

- [x] 1.1 Record the current runtime-stability baseline and confirm `dotnet build CLIHub.sln -c Release` plus `dotnet test CLIHub.sln -c Release` pass before implementation.
- [x] 1.2 Add a test seam that can count concurrent version-command launches and verify the current duplicate-probe behavior is reproduced before the fix.
- [x] 1.3 Add regression cases for executable paths, working directories, quoted arguments, shell metacharacters, and each supported interactive runtime; verify the cases fail or expose the current unsafe command construction.
- [x] 1.4 Add an update-service test seam that completes the underlying check after the configured timeout with both success and failure; verify late completion is observable without changing the returned timeout result.

## 2. Version probe concurrency and lifecycle

- [x] 2.1 Add per-agent in-flight task coalescing while preserving the completed-result TTL cache; verify concurrent callers start one process and receive the same result.
- [x] 2.2 Make caller cancellation stop waiting without cancelling a shared probe needed by another caller; verify one cancelled waiter does not cancel another caller's result.
- [x] 2.3 Make the legacy `IProcessLauncher` constructor of `AgentVersionService` internal and keep the `IProcessOutputRunner` constructor as the production boundary; verify Core composition and direct tests resolve the intended constructor.
- [ ] 2.4 Add generation and cancellation handling to `LaunchWindowViewModel.PopulateVersionsAsync`; verify a superseded refresh cannot update current agent items and current results still populate.
- [ ] 2.5 Apply equivalent exception-safe population handling to the retained `MainWindow` path; verify the solution builds and no population task can escape unobserved from the refresh path.

## 3. Windows command construction

- [x] 3.1 Add an internal runtime command builder with separate construction paths for Windows Terminal, Command Prompt, PowerShell, and captured `cmd.exe` execution; verify each output is deterministic in focused unit tests.
- [x] 3.2 Implement executable and argument quoting/escaping for spaces, embedded quotes, and shell metacharacters while retaining the existing raw plugin descriptor format; verify command builder regression tests pass.
- [x] 3.3 Use the builder from both interactive launch and captured-output execution; verify `ProcessLauncherTests` and `AgentCommandServiceTests` pass for successful and failed commands.
- [x] 3.4 Cover `.cmd`, `.bat`, and PATH-resolved shim execution without weakening timeout and process-tree termination behavior; verify captured-output tests pass on Windows.

## 4. Update timeout lifecycle

- [x] 4.1 Pass a linked cancellation token to the update operation when the installed Velopack API supports it, otherwise attach an explicit observation continuation; verify timeout returns promptly and late task exceptions are logged/observed.
- [x] 4.2 Ensure timeout and caller cancellation clear the available-version state and leave the service ready for a later check; verify sequential check tests pass.
- [x] 4.3 Preserve update-control, tray, and What's New behavior for successful, failed, cancelled, and timed-out checks; verify `UpdateServiceTests` and the full solution tests pass.

## 5. Final verification and backlog update

- [x] 5.1 Run focused agent-version, process-launcher, agent-command, and update-service tests; verify all new concurrency, cancellation, quoting, and timeout scenarios pass.
- [ ] 5.2 Run `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln -c Release`, and `openspec validate "stabilize-core-runtime"`; record the results.
- [ ] 5.3 Update `improvements.md` to mark completed runtime items and retain only verified follow-up work with current file paths; verify the backlog matches the implemented behavior.
