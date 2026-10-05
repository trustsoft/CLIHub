## 1. Add Startup Preference Boundary

- [x] 1.1 Add `IStartupPreferencesApplier` and its implementation, applying the default runtime before the Windows startup registration; verify ordered calls with focused tests.
- [x] 1.2 Register the service in WPF composition and replace the two inline `App.OnStartup` calls; verify DI resolution and that the same loaded preferences snapshot remains available for later startup decisions.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that `PreferenceApplier`, `IProcessLauncher`, `IStartupService`, and preference parsing remain unchanged and that startup service failure boundaries are preserved.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-startup-preferences` and verify the change has no spec deltas.
