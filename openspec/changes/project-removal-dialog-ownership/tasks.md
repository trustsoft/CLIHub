## 1. Owner resolution implementation

- [x] 1.1 (Optional) In `src/CLIHub/Launching/`, create `DialogOwnerResolver.cs` with a static `ResolveOwner()` method that takes `Application.Current.Windows` and `Application.Current.MainWindow` and returns the active window (where `IsActive == true`), falling back to MainWindow, then null. If extraction adds more ceremony than value, skip this and inline the resolution in `ProjectDialogService.Show`.
- [x] 1.2 In `src/CLIHub/Launching/ProjectDialogService.cs`, replace `Application.Current?.MainWindow` (`:42`) in the `Show` method with the resolution rule (active window → MainWindow → null). Use the extracted helper if created, otherwise inline the resolution. Verify with `dotnet build CLIHub.sln`.
- [x] 1.3 In `src/CLIHub/Launching/ProjectDialogService.cs`, replace `Application.Current?.MainWindow` (`:23`) in `SelectFolder` with the same resolution rule. Verify with `dotnet build CLIHub.sln`.

## 2. Tests (if helper extracted)

- [ ] 2.1 If `DialogOwnerResolver` was extracted, create `tests/CLIHub.Tests/Launching/DialogOwnerResolverTests.cs` with tests:
  - `ResolveOwner_ActiveWindow_ReturnsActiveWindow`: create two WPF `Window` instances on STA, activate one, assert it is returned.
  - `ResolveOwner_NoActiveWindow_ReturnsMainWindow`: no active window, assert MainWindow is returned.
  - `ResolveOwner_NoMainWindow_ReturnsNull`: no MainWindow, assert null is returned.
  - Use `TestHelpers.RunOnSta` to run the test body on an STA thread. Do not show real MessageBoxes.
- [ ] 2.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; all tests pass.

## 3. Manual smoke test (required)

- [ ] 3.1 Run `dotnet run --project src/CLIHub/CLIHub.csproj`. Open Settings → Projects, select a manual project, click "Remove Project ×". **Record:** The confirmation prompt appears above the Settings window, and the Settings window is disabled (grayed out or unclickable) while the prompt is open. Click "Yes" and verify the project is removed.
- [ ] 3.2 In the launch window, select a project, click "Remove Project". **Record:** The confirmation prompt appears above the launch window and the launch window is disabled while the prompt is open. Click "Yes" and verify the project is removed.
- [ ] 3.3 In the launch window, click "Add Project..." and select a folder. **Record:** The folder picker is modal to the launch window (launch window is disabled while the picker is open).

## 4. Verification gate

- [x] 4.1 Run `dotnet build CLIHub.sln` — builds without errors or warnings.
- [x] 4.2 Run `dotnet test CLIHub.sln` — all tests pass.
- [x] 4.3 Run `npx --yes openspec validate project-removal-dialog-ownership` — validation passes.

## 5. Finalization

- [x] 5.1 Report the smoke test results to the user (prompt z-order, Settings modality, launch window behavior).
- [ ] 5.2 **Wait for explicit user approval before committing.** Do not stage files or commit without approval.
