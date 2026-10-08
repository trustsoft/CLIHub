# Design: Extract ApplicationHost and Startup Coordinators

## Overview

Phase 6 of the `improvements.md` refactoring: extract startup responsibilities from `ApplicationBootstrapper` into focused coordinators, achieving the target architecture.

## Architecture

### Current State

```
App.OnStartup
  -> directories, logging, DI
  -> ApplicationBootstrapper.Start
      -> single-instance check
      -> plugin init, preferences load/apply
      -> ApplicationSession.Start (UI + events)
      -> hotkey registration
      -> optional: release notes, update check
```

### Target State

```
App.OnStartup
  -> ApplicationHost.Start
      -> InstanceCoordinator.Coordinate
      -> StartupStateLoader.Load
      -> ApplicationBootstrapper.Start
          -> ApplicationSession.Start (UI + events)
          -> HotkeyStartupRegistrar.Register
          -> OptionalStartupCoordinator.RunOptionalStartup

App.OnExit
  -> ApplicationHost.Shutdown
```

## Components

### 1. InstanceCoordinator

**Responsibility:** Single-instance coordination.

**Interface:**
```csharp
public interface IInstanceCoordinator
{
    InstanceStatus Coordinate();
}

public enum InstanceStatus
{
    FirstInstance,
    SecondInstance
}
```

**Implementation:**
```csharp
public sealed class InstanceCoordinator : IInstanceCoordinator
{
    private readonly ISingleInstanceGuard _singleInstanceGuard;
    private readonly IApplicationLifetime _applicationLifetime;

    public InstanceStatus Coordinate()
    {
        if (!_singleInstanceGuard.IsFirstInstance)
        {
            _singleInstanceGuard.SignalActivation();
            _applicationLifetime.Shutdown();
            return InstanceStatus.SecondInstance;
        }

        return InstanceStatus.FirstInstance;
    }
}
```

### 2. StartupStateLoader

**Responsibility:** Load required startup state (plugins, preferences).

**Interface:**
```csharp
public interface IStartupStateLoader
{
    StartupState Load();
}

public sealed record StartupState(UserPreferences Preferences);
```

**Implementation:**
```csharp
public sealed class StartupStateLoader : IStartupStateLoader
{
    private readonly IPluginInitializationService _pluginInitialization;
    private readonly IPreferencesStore _preferencesStore;
    private readonly IStartupPreferencesApplier _startupPreferences;

    public StartupState Load()
    {
        _pluginInitialization.Initialize();

        var preferences = _preferencesStore.Load();
        _startupPreferences.Apply(preferences);

        return new StartupState(preferences);
    }
}
```

### 3. OptionalStartupCoordinator

**Responsibility:** Best-effort optional operations.

**Interface:**
```csharp
public interface IOptionalStartupCoordinator
{
    void RunOptionalStartup(
        StartupState startupState,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context);
}
```

**Implementation:**
```csharp
public sealed class OptionalStartupCoordinator : IOptionalStartupCoordinator
{
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IReleaseNotesStartupCoordinator _releaseNotesStartup;
    private readonly IUpdateStartupCoordinator _updateStartup;
    private readonly ILogger<OptionalStartupCoordinator> _logger;

    public void RunOptionalStartup(
        StartupState startupState,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context)
    {
        RunBestEffort("release notes", () => 
            _releaseNotesStartup.Evaluate(startupState.Preferences));

        RunBestEffort("startup update check", () => 
            _ = _operationLifetime.RunAsync(
                "Startup update check",
                ct => RunStartupUpdateCheckAsync(
                    startupState.Preferences.CheckForUpdatesOnStartup,
                    startupUi,
                    context,
                    ct)));
    }

    private async Task RunStartupUpdateCheckAsync(...)
    {
        try
        {
            await _updateStartup.CheckAsync(...);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup update check failed");
        }
    }

    private void RunBestEffort(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Optional startup operation failed: {Operation}", operation);
        }
    }
}
```

### 4. ApplicationHost

**Responsibility:** WPF lifecycle boundary.

**Interface:**
```csharp
public interface IApplicationHost
{
    void Start(Action<Action> dispatch, Action<object> setMainWindow);
    void Shutdown();
}
```

**Implementation:**
```csharp
public sealed class ApplicationHost : IApplicationHost
{
    private ServiceProvider? _services;

    public void Start(Action<Action> dispatch, Action<object> setMainWindow)
    {
        DirectoryInitializer.EnsureAppDataLayout();
        ConfigureLogging();

        Log.Information("CLIHub starting");

        var services = new ServiceCollection();
        services.AddClIHubServices();
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<IApplicationBootstrapper>().Start(
            new ApplicationStartupContext(dispatch, setMainWindow));
    }

    public void Shutdown()
    {
        Log.Information("CLIHub shutting down");

        try
        {
            _services?.GetService<IApplicationSession>()?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application session shutdown failed");
        }

        try
        {
            _services?
                .GetService<IApplicationOperationLifetime>()?
                .StopAsync(ApplicationOperationLifetime.DefaultShutdownTimeout)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Application operation shutdown failed");
        }

        _services?.Dispose();

        Log.CloseAndFlush();
    }

    private static void ConfigureLogging() { ... }
}
```

### 5. ApplicationBootstrapper (simplified)

**New responsibilities:** Pure sequencing only.

```csharp
public sealed class ApplicationBootstrapper : IApplicationBootstrapper
{
    private readonly IInstanceCoordinator _instanceCoordinator;
    private readonly IStartupStateLoader _startupStateLoader;
    private readonly IApplicationSession _session;
    private readonly Func<IHotkeyStartupRegistrar> _hotkeyStartupRegistrarFactory;
    private readonly IOptionalStartupCoordinator _optionalStartup;
    private readonly ILogger<ApplicationBootstrapper> _logger;

    public void Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);
        ArgumentNullException.ThrowIfNull(context.SetMainWindow);

        var instanceStatus = _instanceCoordinator.Coordinate();
        if (instanceStatus == InstanceStatus.SecondInstance)
        {
            return; // shutdown already triggered by coordinator
        }

        try
        {
            var startupState = _startupStateLoader.Load();
            var startupUi = _session.Start(context);
            var hotkeyStartupRegistrar = _hotkeyStartupRegistrarFactory();

            context.SetMainWindow(startupUi.LaunchWindow);

            if (startupState.Preferences.ShowWindowOnStartup)
            {
                startupUi.ShowLaunchWindow();
            }
            else
            {
                _logger.LogInformation("Starting in the system tray (show window on startup disabled)");
            }

            hotkeyStartupRegistrar.Register(startupState.Preferences);

            _optionalStartup.RunOptionalStartup(startupState, startupUi, context);

            _logger.LogInformation("CLIHub started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CLIHub startup failed");
            throw; // ApplicationHost handles shutdown on fatal failure
        }
    }
}
```

Target: ~60 lines (currently 152).

## App.xaml.cs (simplified)

```csharp
public partial class App : Application
{
    private IApplicationHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = new ApplicationHost();
        _host.Start(
            action => Dispatcher.Invoke(action),
            window => MainWindow = (Window)window);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Shutdown();
        base.OnExit(e);
    }
}
```

Target: ~20 lines (currently 88).

## DI Registration

Add to `ServiceRegistration.cs`:

```csharp
services.AddSingleton<IInstanceCoordinator, InstanceCoordinator>();
services.AddSingleton<IStartupStateLoader, StartupStateLoader>();
services.AddSingleton<IOptionalStartupCoordinator, OptionalStartupCoordinator>();
```

`ApplicationHost` is created directly by `App` (not registered in DI).

## Testing Strategy

### Unit Tests

1. **InstanceCoordinator**
   - First instance returns FirstInstance
   - Second instance signals activation, triggers shutdown, returns SecondInstance

2. **StartupStateLoader**
   - Initializes plugins
   - Loads preferences
   - Applies startup preferences
   - Returns immutable StartupState

3. **OptionalStartupCoordinator**
   - Runs release notes evaluation
   - Runs startup update check
   - Logs warnings on failure, does not throw

4. **ApplicationBootstrapper** (updated tests)
   - Second instance returns early
   - First instance sequences coordinators correctly
   - Fatal failure logs and throws

### Integration Tests

Keep existing `ApplicationBootstrapper` integration test, update to verify new coordinator sequence.

## Migration Path

### Step 6.1: InstanceCoordinator
1. Create `InstanceCoordinator`, `IInstanceCoordinator`, `InstanceStatus`
2. Register in DI
3. Update `ApplicationBootstrapper` to use coordinator
4. Add tests
5. Commit: `refactor(startup): extract instance coordinator`

### Step 6.2: StartupStateLoader
1. Create `StartupStateLoader`, `IStartupStateLoader`, `StartupState`
2. Register in DI
3. Update `ApplicationBootstrapper` to use loader
4. Add tests
5. Commit: `refactor(startup): extract startup state loader`

### Step 6.3: OptionalStartupCoordinator
1. Create `OptionalStartupCoordinator`, `IOptionalStartupCoordinator`
2. Move release notes + update check logic
3. Register in DI
4. Update `ApplicationBootstrapper` to delegate optional work
5. Add tests
6. Commit: `refactor(startup): extract optional startup coordinator`

### Step 6.4: ApplicationHost
1. Create `ApplicationHost`, `IApplicationHost`
2. Move environment + DI + cleanup logic from `App`
3. Update `App.OnStartup`/`OnExit` to delegate
4. Add integration test
5. Commit: `refactor(startup): extract application host`

### Step 6.5: Documentation
1. Update `docs/architecture/startup.md`
2. Update `improvements.md` progress table (add Phase 6 row)
3. Commit: `docs: update startup architecture documentation`

### Step 6.6: Archive
1. Run `openspec archive extract-application-host-and-coordinators`
2. Commit: `docs: archive application host extraction change`

## Success Metrics

- `ApplicationBootstrapper`: 152 → ~60 lines
- `App.xaml.cs`: 88 → ~20 lines
- New coordinators: 3 focused components (~50-80 lines each)
- All 497 tests pass
- Startup order unchanged
- No behavior changes
