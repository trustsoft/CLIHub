## 1. Define The Bootstrapper Boundary

- [x] 1.1 Add an application bootstrapper contract and startup context that expose only the lifecycle, dispatcher, and UI callbacks required by the orchestration; verify the contract has no WPF service dependencies.
- [x] 1.2 Implement the bootstrapper with the existing first-instance startup order, explicit required versus best-effort failure handling, and provider-owned resource disposal; verify focused orchestration tests cover success, second-instance exit, fatal failure, and optional failure paths.

## 2. Integrate The WPF Composition Root

- [x] 2.1 Register the bootstrapper in `ServiceRegistration` and adapt `App.OnStartup` to prepare the environment, resolve the bootstrapper, and delegate startup; verify the application entry point no longer directly resolves individual startup workflow services.
- [x] 2.2 Preserve WPF dispatcher callbacks, activation handling, window visibility, update notifications, and existing `OnExit` cleanup; verify application composition tests confirm required services resolve and startup wiring remains single-owner.

## 3. Validate The Change

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and verify the solution builds without warnings or errors.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release` and verify all existing and new tests pass.
- [x] 3.3 Run `openspec validate "extract-application-bootstrapper"` and verify the change is valid and ready for archive.
