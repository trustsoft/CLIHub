## 1. Lifetime Boundary

- [x] 1.1 Add `IApplicationOperationLifetime` and its singleton implementation with one application CTS, tracked operation registry, cancellation-aware execution, exception observation, and idempotent bounded stop; verify unit tests cover success, cancellation, exception, duplicate stop, and timeout paths.
- [x] 1.2 Register the lifetime service in the composition root and add a configurable production shutdown bound; verify the service resolves as one singleton without constructing WPF UI resources.

## 2. Async Workflow Integration

- [x] 2.1 Thread cancellation tokens through startup update checks and update download workflows; route bootstrapper startup checks and tray/What's New download requests through the lifetime boundary; verify cancellation reaches `IUpdateService` and expected result handling is unchanged.
- [x] 2.2 Route `LaunchWindowViewModel` agent commands and agent version population through the application lifetime while preserving local refresh cancellation and status behavior; verify command cancellation and version refresh tests pass.
- [x] 2.3 Route `SettingsViewModel` and `UpdateControlViewModel` asynchronous update actions through the lifetime boundary; verify UI status and cancellation paths do not report shutdown cancellation as unexpected failure.

## 3. Shutdown Integration

- [x] 3.1 Update `App.OnExit` to stop and bounded-await application operations before disposing the service provider, while keeping cleanup idempotent; verify shutdown tests cover cancellation, timeout, and provider disposal ordering.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release` and verify the solution builds without warnings or errors.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release` and verify all existing and new tests pass.
- [x] 3.4 Run `openspec validate "add-application-operation-lifetime"`, update `improvements.md`, and archive the completed change.
