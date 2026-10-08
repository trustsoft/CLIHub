# Tasks: Extract ApplicationHost and Startup Coordinators

## Step 6.1: Extract InstanceCoordinator

### Task 6.1.1: Create InstanceCoordinator components
- [ ] Create `InstanceStatus` enum (FirstInstance, SecondInstance) in `src/CLIHub/`
- [ ] Create `IInstanceCoordinator` interface with `Coordinate()` method
- [ ] Create `InstanceCoordinator` class implementing the interface
- [ ] Move single-instance logic from `ApplicationBootstrapper` lines 72-77

**Files:**
- `src/CLIHub/InstanceStatus.cs` (new)
- `src/CLIHub/IInstanceCoordinator.cs` (new)
- `src/CLIHub/InstanceCoordinator.cs` (new)

### Task 6.1.2: Register InstanceCoordinator in DI
- [ ] Add `services.AddSingleton<IInstanceCoordinator, InstanceCoordinator>()` to `ServiceRegistration.cs`

**Files:**
- `src/CLIHub/ServiceRegistration.cs`

### Task 6.1.3: Update ApplicationBootstrapper to use InstanceCoordinator
- [ ] Inject `IInstanceCoordinator` instead of `ISingleInstanceGuard` and `IApplicationLifetime`
- [ ] Replace lines 72-77 with `var status = _instanceCoordinator.Coordinate(); if (status == InstanceStatus.SecondInstance) return;`
- [ ] Remove `_singleInstanceGuard` and `_applicationLifetime` fields
- [ ] Update constructor and XML docs

**Files:**
- `src/CLIHub/ApplicationBootstrapper.cs`

### Task 6.1.4: Add tests for InstanceCoordinator
- [ ] Create `InstanceCoordinatorTests.cs` with:
  - FirstInstance scenario
  - SecondInstance scenario (verifies SignalActivation + Shutdown called)

**Files:**
- `tests/CLIHub.Tests/InstanceCoordinatorTests.cs` (new)

### Task 6.1.5: Verify and commit
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all 497+ tests pass
- [ ] Commit: `refactor(startup): extract instance coordinator`

---

## Step 6.2: Extract StartupStateLoader

### Task 6.2.1: Create StartupState record
- [ ] Create `StartupState` record with `Preferences` property
- [ ] Add XML documentation

**Files:**
- `src/CLIHub/StartupState.cs` (new)

### Task 6.2.2: Create StartupStateLoader
- [ ] Create `IStartupStateLoader` interface with `Load()` method
- [ ] Create `StartupStateLoader` class implementing the interface
- [ ] Move plugin initialization logic (line 81)
- [ ] Move preferences load logic (line 83)
- [ ] Move startup preferences apply logic (line 84)
- [ ] Return `new StartupState(preferences)`

**Files:**
- `src/CLIHub/IStartupStateLoader.cs` (new)
- `src/CLIHub/StartupStateLoader.cs` (new)

### Task 6.2.3: Register StartupStateLoader in DI
- [ ] Add `services.AddSingleton<IStartupStateLoader, StartupStateLoader>()` to `ServiceRegistration.cs`

**Files:**
- `src/CLIHub/ServiceRegistration.cs`

### Task 6.2.4: Update ApplicationBootstrapper to use StartupStateLoader
- [ ] Inject `IStartupStateLoader` instead of `IPluginInitializationService`, `IPreferencesStore`, `IStartupPreferencesApplier`
- [ ] Replace lines 81-84 with `var startupState = _startupStateLoader.Load();`
- [ ] Use `startupState.Preferences` instead of local `preferences` variable
- [ ] Remove `_pluginInitialization`, `_preferencesStore`, `_startupPreferences` fields
- [ ] Update constructor and XML docs

**Files:**
- `src/CLIHub/ApplicationBootstrapper.cs`

### Task 6.2.5: Add tests for StartupStateLoader
- [ ] Create `StartupStateLoaderTests.cs` with:
  - Successful load scenario
  - Verify plugin initialization called
  - Verify preferences loaded and applied
  - Verify StartupState returned

**Files:**
- `tests/CLIHub.Tests/StartupStateLoaderTests.cs` (new)

### Task 6.2.6: Verify and commit
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all tests pass
- [ ] Commit: `refactor(startup): extract startup state loader`

---

## Step 6.3: Extract OptionalStartupCoordinator

### Task 6.3.1: Create OptionalStartupCoordinator
- [ ] Create `IOptionalStartupCoordinator` interface with `RunOptionalStartup(StartupState, IApplicationStartupUi, ApplicationStartupContext)` method
- [ ] Create `OptionalStartupCoordinator` class implementing the interface
- [ ] Move release notes logic (lines 102)
- [ ] Move startup update check logic (lines 103-111, method 122-139)
- [ ] Move `RunBestEffort` method (lines 141-151)

**Files:**
- `src/CLIHub/IOptionalStartupCoordinator.cs` (new)
- `src/CLIHub/OptionalStartupCoordinator.cs` (new)

### Task 6.3.2: Register OptionalStartupCoordinator in DI
- [ ] Add `services.AddSingleton<IOptionalStartupCoordinator, OptionalStartupCoordinator>()` to `ServiceRegistration.cs`

**Files:**
- `src/CLIHub/ServiceRegistration.cs`

### Task 6.3.3: Update ApplicationBootstrapper to use OptionalStartupCoordinator
- [ ] Inject `IOptionalStartupCoordinator` instead of `IReleaseNotesStartupCoordinator`, `IUpdateStartupCoordinator`
- [ ] Replace lines 102-111 with `_optionalStartup.RunOptionalStartup(startupState, startupUi, context);`
- [ ] Remove `_releaseNotesStartup`, `_updateStartup` fields
- [ ] Remove `RunStartupUpdateCheckAsync` method (lines 122-139)
- [ ] Remove `RunBestEffort` method (lines 141-151)
- [ ] Update constructor and XML docs

**Files:**
- `src/CLIHub/ApplicationBootstrapper.cs`

### Task 6.3.4: Add tests for OptionalStartupCoordinator
- [ ] Create `OptionalStartupCoordinatorTests.cs` with:
  - Successful release notes evaluation
  - Successful update check
  - Release notes failure logged, does not throw
  - Update check failure logged, does not throw

**Files:**
- `tests/CLIHub.Tests/OptionalStartupCoordinatorTests.cs` (new)

### Task 6.3.5: Verify and commit
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all tests pass
- [ ] Verify `ApplicationBootstrapper` < 80 lines
- [ ] Commit: `refactor(startup): extract optional startup coordinator`

---

## Step 6.4: Extract ApplicationHost

### Task 6.4.1: Create ApplicationHost
- [ ] Create `IApplicationHost` interface with `Start(Action<Action>, Action<object>)` and `Shutdown()` methods
- [ ] Create `ApplicationHost` class implementing the interface
- [ ] Move directory initialization from `App.OnStartup` line 29
- [ ] Move logging configuration from `App.OnStartup` lines 30, 44-50
- [ ] Move service provider building from `App.OnStartup` lines 34-36
- [ ] Move bootstrapper call from `App.OnStartup` lines 38-41
- [ ] Move shutdown sequence from `App.OnExit` lines 59-84

**Files:**
- `src/CLIHub/IApplicationHost.cs` (new)
- `src/CLIHub/ApplicationHost.cs` (new)

### Task 6.4.2: Update App.xaml.cs to use ApplicationHost
- [ ] Add `private IApplicationHost? _host;` field
- [ ] Replace `OnStartup` body with host creation and delegation
- [ ] Replace `OnExit` body with host shutdown delegation
- [ ] Remove `_services` field
- [ ] Remove `ConfigureLogging` method

**Files:**
- `src/CLIHub/App.xaml.cs`

### Task 6.4.3: Update ApplicationBootstrapper error handling
- [ ] Change catch block to throw instead of calling `_applicationLifetime.Shutdown()`
- [ ] Remove `_applicationLifetime` field if still present
- [ ] Update XML docs to note ApplicationHost handles fatal failure shutdown

**Files:**
- `src/CLIHub/ApplicationBootstrapper.cs`

### Task 6.4.4: Add tests for ApplicationHost
- [ ] Create `ApplicationHostTests.cs` with:
  - Start creates DI container and calls bootstrapper
  - Shutdown disposes session, stops operations, disposes provider, flushes logs
  - Shutdown handles session disposal failure
  - Shutdown handles operation stop failure

**Files:**
- `tests/CLIHub.Tests/ApplicationHostTests.cs` (new)

### Task 6.4.5: Verify and commit
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all tests pass
- [ ] Verify `App.xaml.cs` ~20 lines
- [ ] Commit: `refactor(startup): extract application host`

---

## Step 6.5: Update documentation

### Task 6.5.1: Update startup documentation
- [ ] Update `docs/architecture/startup.md` with new flow diagram
- [ ] Document `ApplicationHost` responsibilities
- [ ] Document `InstanceCoordinator` responsibilities
- [ ] Document `StartupStateLoader` responsibilities
- [ ] Document `OptionalStartupCoordinator` responsibilities
- [ ] Document simplified `ApplicationBootstrapper` role

**Files:**
- `docs/architecture/startup.md`

### Task 6.5.2: Update architecture documentation
- [ ] Update `docs/architecture.md` with new coordinator components
- [ ] Update composition root section with new registrations

**Files:**
- `docs/architecture.md`

### Task 6.5.3: Update improvements.md progress
- [ ] Add Phase 6 row to Execution Progress table with delivered/remaining status

**Files:**
- `improvements.md`

### Task 6.5.4: Verify and commit
- [ ] Verify documentation builds/renders correctly
- [ ] Commit: `docs: update startup architecture documentation`

---

## Step 6.6: Archive change

### Task 6.6.1: Run OpenSpec validation
- [ ] Run `openspec validate extract-application-host-and-coordinators`
- [ ] Fix any validation issues

### Task 6.6.2: Archive change
- [ ] Run `openspec archive extract-application-host-and-coordinators`
- [ ] Verify specs synced

### Task 6.6.3: Final verification
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all tests pass
- [ ] Verify working tree clean

### Task 6.6.4: Final commit
- [ ] Commit: `docs: archive application host extraction change`

---

## Completion Criteria

- [x] All tasks completed
- [ ] `ApplicationBootstrapper` < 80 lines (target: ~60)
- [ ] `App.xaml.cs` < 30 lines (target: ~20)
- [ ] 4 new coordinator components created
- [ ] All 497+ tests pass
- [ ] Startup order unchanged
- [ ] No behavior changes
- [ ] Documentation reflects new structure
- [ ] OpenSpec change archived
