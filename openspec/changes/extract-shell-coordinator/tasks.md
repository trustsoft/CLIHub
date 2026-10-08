## 1. Create ShellCoordinator Interface and Implementation

- [ ] 1.1 Create `IShellCoordinator.cs` with StartupUi property and IDisposable inheritance, verify interface compiles and follows project conventions (file-scoped namespace, XML docs)
- [ ] 1.2 Create `ShellCoordinator.cs` with constructor accepting 5 dependencies (IApplicationOperationLifetime, Func<IApplicationStartupUi>, Func<IUpdateRequestSource>, ISingleInstanceGuard, IUpdateWorkflow), verify class compiles with file-scoped namespace and nullable annotations
- [ ] 1.3 Add 6 event handler fields to ShellCoordinator (_updateStateChangedHandler, _startupUiUpdateDownloadRequestedHandler, _updateRequestedHandler, _activationRequestedHandler, _updateDownloadedHandler, _updateDownloadFailedHandler) and verify fields follow naming conventions
- [ ] 1.4 Implement ShellCoordinator initialization logic: call factories to create IApplicationStartupUi and IUpdateRequestSource, store in fields, set StartupUi property, verify logic matches current ApplicationSession.Start pattern
- [ ] 1.5 Implement 6 event handler creation in ShellCoordinator: updateStateChanged dispatches RefreshMenu, startupUiUpdateDownloadRequested runs DownloadAndApplyAsync via operation lifetime, updateRequested runs DownloadAndApplyAsync, activationRequested dispatches ShowLaunchWindow, updateDownloaded dispatches NotifyUpdateDownloaded, updateDownloadFailed dispatches NotifyUpdateFailed, verify handlers match current ApplicationSession signatures exactly
- [ ] 1.6 Implement event subscription in ShellCoordinator: subscribe to UpdateStateChanged, UpdateDownloadRequested, UpdateRequested (from source), ActivationRequested, UpdateDownloaded, UpdateDownloadFailed, store handler references in fields, verify subscription pattern matches current ApplicationSession.Start
- [ ] 1.7 Implement ShellCoordinator.Dispose: unsubscribe from all 6 events in the exact order from current ApplicationSession.Dispose (update workflow → UI → source → guard → outcomes), dispose IApplicationStartupUi if IDisposable, verify disposal logic preserves exact current order

## 2. Update ApplicationSession to Delegate to ShellCoordinator

- [ ] 2.1 Update ApplicationSession constructor to replace 4 current dependencies (startupUiFactory, updateRequestSourceFactory, singleInstanceGuard, updateWorkflow) with single IShellCoordinator dependency, verify constructor compiles with 3 total dependencies (operationLifetime, shellCoordinator, remaining)
- [ ] 2.2 Remove 7 event handler fields from ApplicationSession (_updateStateChangedHandler, _startupUiUpdateDownloadRequestedHandler, _updateRequestedHandler, _activationRequestedHandler, _updateDownloadedHandler, _updateDownloadFailedHandler, plus _updateRequestSource), verify fields removed cleanly
- [ ] 2.3 Simplify ApplicationSession.Start: call shellCoordinator initialization (assume it creates and wires), access shellCoordinator.StartupUi, return it, verify Start reduces to ~10 lines and preserves return contract
- [ ] 2.4 Simplify ApplicationSession.Dispose: call shellCoordinator.Dispose() (which handles all unsubscriptions and UI disposal), remove individual unsubscription logic, verify Dispose reduces to ~5 lines
- [ ] 2.5 Update IApplicationSession XML documentation to reflect delegation to shell coordinator, verify docs match new responsibility boundary

## 3. Update Dependency Injection Registration

- [ ] 3.1 Add `services.AddSingleton<IShellCoordinator, ShellCoordinator>();` to ServiceRegistration.ConfigureServices before ApplicationSession registration, verify registration follows existing singleton pattern
- [ ] 3.2 Update ApplicationSession registration to remove 4 factory dependencies (startupUiFactory, updateRequestSourceFactory, singleInstanceGuard, updateWorkflow) and add IShellCoordinator resolution, verify DI container can resolve ApplicationSession with new constructor
- [ ] 3.3 Move factory registrations (Func<IApplicationStartupUi>, Func<IUpdateRequestSource>) to ShellCoordinator dependencies in DI container if needed, verify factories resolve correctly for ShellCoordinator

## 4. Add Unit Tests for ShellCoordinator

- [ ] 4.1 Create `ShellCoordinatorTests.cs` in tests/CLIHub.Tests/ with test fixture and mock dependencies setup (operationLifetime, startupUi, updateRequestSource, guard, updateWorkflow), verify test file compiles and follows project test conventions
- [ ] 4.2 Add test `Constructor_InitializesStartupUi_AndWiresEvents`: verify StartupUi property populated, verify 6 event subscriptions registered (check each event has non-null handler), verify test passes
- [ ] 4.3 Add test `UpdateStateChanged_DispatchesRefreshMenu`: raise UpdateStateChanged from workflow, verify Dispatch called with RefreshMenu action, verify test passes
- [ ] 4.4 Add test `StartupUi_UpdateDownloadRequested_StartsDownloadWorkflow`: raise UpdateDownloadRequested from startupUi, verify operation lifetime runs DownloadAndApplyAsync, verify test passes
- [ ] 4.5 Add test `UpdateRequestSource_UpdateRequested_StartsDownloadWorkflow`: raise UpdateRequested from source, verify operation lifetime runs DownloadAndApplyAsync, verify test passes
- [ ] 4.6 Add test `ActivationRequested_DispatchesShowLaunchWindow`: raise ActivationRequested from guard, verify Dispatch called with ShowLaunchWindow action, verify test passes
- [ ] 4.7 Add test `UpdateDownloaded_DispatchesNotification`: raise UpdateDownloaded from workflow with version, verify Dispatch called with NotifyUpdateDownloaded, verify test passes
- [ ] 4.8 Add test `UpdateDownloadFailed_DispatchesNotification`: raise UpdateDownloadFailed from workflow with version, verify Dispatch called with NotifyUpdateFailed, verify test passes
- [ ] 4.9 Add test `Dispose_UnsubscribesAllEvents_AndDisposesStartupUi`: call Dispose, raise all 6 events, verify no handlers invoked, verify startupUi.Dispose called if IDisposable, verify test passes
- [ ] 4.10 Add test `Dispose_HandlesNullStartupUi_Gracefully`: initialize coordinator without calling factories, dispose, verify no exceptions, verify test passes

## 5. Update ApplicationSession Tests

- [ ] 5.1 Update existing ApplicationSession tests to use IShellCoordinator mock instead of factory mocks (startupUiFactory, updateRequestSourceFactory, singleInstanceGuard, updateWorkflow), verify all existing session tests still pass with mocked coordinator
- [ ] 5.2 Simplify ApplicationSession test assertions: verify session calls coordinator.Dispose during session disposal, verify session returns coordinator.StartupUi from Start, verify tests reflect reduced session responsibilities
- [ ] 5.3 Remove obsolete ApplicationSession event-wiring tests (now covered by ShellCoordinator tests), verify no test duplication between session and coordinator test suites

## 6. Build and Verify

- [ ] 6.1 Run `dotnet build CLIHub.sln` from repository root, verify build succeeds with zero errors and zero warnings
- [ ] 6.2 Run `dotnet test CLIHub.sln --no-build` from repository root, verify all tests pass (expect 516+ total: 128+ app tests including ~10 new ShellCoordinator tests, 388 Core tests)
- [ ] 6.3 Manually launch CLIHub in Debug mode, verify application starts normally (tray icon visible, launch window opens if configured), verify hotkey works, verify tray menu shows update state correctly, verify no exceptions in logs
- [ ] 6.4 Trigger second-instance activation by launching CLIHub again while first instance runs, verify first instance shows launch window, verify second instance exits cleanly
- [ ] 6.5 Graceful shutdown: Exit from tray menu, verify application exits without exceptions, verify logs show clean disposal sequence

## 7. Update Documentation

- [ ] 7.1 Update `docs/architecture/startup.md` to document ShellCoordinator role in startup flow (between ApplicationSession and UI factories), verify documentation reflects new coordinator boundary
- [ ] 7.2 Update `docs/architecture.md` Components section to add IShellCoordinator and ShellCoordinator with responsibilities, verify architecture documentation consistent with implementation
- [ ] 7.3 Update any inline code comments in ApplicationBootstrapper or ApplicationHost that reference ApplicationSession event wiring, verify comments reflect delegation to ShellCoordinator
