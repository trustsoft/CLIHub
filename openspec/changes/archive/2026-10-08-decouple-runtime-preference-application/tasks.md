## 1. Application Ports

- [x] 1.1 Add narrow runtime and display-style ports plus the runtime adapter over `IProcessLauncher`; verify the ports expose only the required operations.
- [x] 1.2 Update `PreferenceApplier` to depend only on application ports and preserve logging, independent application methods, and hotkey failure return semantics; verify direct unit tests cover runtime, display, startup, hotkey success, and hotkey failure.

## 2. Composition And State

- [x] 2.1 Register the ports and adapters in DI and make `LaunchWindowViewModel` implement the display-style target without changing binding notifications; verify composition tests do not require a WPF window to inspect the port registrations.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and verify the solution builds without warnings or errors.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release` and verify all existing and new tests pass.
- [x] 3.3 Run `graphify update .`, validate the change, update `improvements.md`, and archive the completed change.
