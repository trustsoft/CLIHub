## Context

See proposal.md - Why. Today `App.xaml.cs` builds services through a static `AppServices` class and creates the `TaskbarIcon` directly; `MainWindow` reaches into `AppServices`. There is no single-instance guard, so a second launch spawns a second tray icon (observed). `ShutdownMode` is already `OnExplicitShutdown`, and the tray icon is created with `ForceCreate()`. `Microsoft.Extensions.DependencyInjection` is already referenced. This change introduces lifecycle infrastructure without altering `project-management` behavior.

## Goals / Non-Goals

**Goals:**
- One running instance; duplicate launches activate the existing window and exit
- Services composed once via DI, injected into `MainWindow` and the tray owner
- AppData structure guaranteed on every start
- Deterministic teardown (tray disposed, container disposed, mutex released)
- Keep the mutex/pipe logic in `CLIHub.Core` so it is testable without WPF

**Non-Goals:**
- Logging (separate change) — `logs\` is created here but nothing writes to it yet
- Start-with-Windows / run-at-login
- Auto-update
- Multi-user or elevated scenarios
- Changing how `project-management` behaves

## Decisions

### Decision 1: Named mutex for the single-instance guard

**Chosen:** A named `Mutex` created at startup; `createdNew == false` means another instance owns it.

**Rationale:**
- OS-level primitive; the kernel releases it automatically if the process dies, so no stale-lock cleanup
- Synchronous and cheap at startup
- Cross-process by name

**Alternatives considered:**
- File lock: Rejected — stale lock files and cleanup complexity
- Socket bind: Rejected — firewall/port concerns

**Naming:** Use `Local\CLIHub` by default. A `Global\` mutex spans sessions but can fail to create for a standard user in some environments and would conflate different logged-in users. Since CLIHub is a per-user tray app, session scope is the correct boundary. (The project note that referenced `Global\CLIHub` is superseded here; recorded as a deviation.)

### Decision 2: Named pipe for activation signaling

**Chosen:** The first instance runs a named-pipe server loop; a second instance connects as a client, writes a one-line message, and exits.

**Rationale:**
- Simple, dependency-free IPC
- The message is a verb ("SHOW"), not a data channel, so no protocol work
- Server runs on a background task and marshals to the UI thread via `Dispatcher` in `App`

**Alternatives considered:**
- `PostMessage`/`FindWindow`: Rejected — brittle if the window is hidden or its class changes
- Named `EventWaitHandle` alone: Chosen not to use — a pipe keeps the door open for future commands

**Robustness:** Client connect uses a short timeout with one retry; failure just exits the second instance (never blocks the user). Server loop tolerates repeated connect/disconnect.

### Decision 3: DI container with a service-registration module

**Chosen:** `Microsoft.Extensions.DependencyInjection` (`ServiceCollection`), with a single `ServiceRegistration.AddClIHubServices(...)` entry point.

**Registrations:**
```csharp
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<IProjectService>(sp => new ProjectService(sp.GetRequiredService<IConfigService>())
{
    DefaultLogoPath = Path.Combine(AppContext.BaseDirectory, "default-project.png")
});
services.AddSingleton<IPluginManager, PluginManager>();
services.AddSingleton<IProcessLauncher, ProcessLauncher>();
services.AddSingleton<SingleInstanceGuard>();
services.AddSingleton<TrayIconController>();   // wraps TaskbarIcon
services.AddSingleton<MainWindow>();
```

**Rationale:**
- Constructor injection removes the static `AppServices` accessor and the hidden global state
- `MainWindow` as a singleton lets the tray controller show/hide the same window instance
- The `DefaultLogoPath` factory keeps the UI-layer path knowledge out of `ProjectService`

**Alternatives considered:**
- Keep `AppServices` with an internal container: Rejected — static locator undermines testability
- Full `IHost`/Generic Host: Rejected — heavier than needed for a tray app; revisit if config/options/monitoring grow

### Decision 4: Tray icon ownership moves behind a controller

**Chosen:** A small `TrayIconController` owns the `TaskbarIcon`, builds the context menu, and exposes `ShowMainWindow()`.

**Rationale:**
- Keeps `App.xaml.cs` thin (lifecycle only)
- Gives the single-instance activation handler and the menu a single place to call `ShowMainWindow()`
- Testable menu construction is possible without instantiating `App`

**Alternatives considered:**
- Keep everything in `App.xaml.cs`: Rejected — `App` becomes a god-object as menus grow

### Decision 5: Startup window visibility

**Chosen:** Show the main window on startup (preserve current behavior); start-hidden is deferred.

**Rationale:** Changing to start-hidden would silently alter what testers see; the window-visibility policy is a separate UX decision.

**Alternatives considered:**
- Start hidden (tray-only): Deferred to a future change, noted as an open question.

### Decision 6: AppData initialization as an explicit startup step

**Chosen:** A `DirectoryInitializer.EnsureAppDataLayout()` (in `CLIHub.Core`) invoked before services that read/write files.

**Rationale:** `ConfigService` and `PluginManager` each currently create their own folder; centralizing the full layout (`logs\`, `plugins\`, `cache\`) makes the contract explicit and avoids ordering surprises.

**Alternatives considered:**
- Rely on each service to create its own path: Partially kept (harmless), but the layout is now also guaranteed up front.

## Risks / Trade-offs

**[Risk] Mutex name collision with another product** → Mitigation: use a distinctive name (`Local\CLIHub.SingleInstance`); documented here.

**[Risk] Pipe server not yet listening when a very fast second launch connects** → Mitigation: startup order starts the server before the tray icon; client retries once with a short timeout, then exits quietly.

**[Risk] `Local\` scope means per-session only** (two different users could each run an instance) → Accepted: correct for a per-user tray companion.

**[Risk] Removing `StartupUri` breaks window creation** → Mitigation: create `MainWindow` from the container explicitly during `OnStartup` and keep a reference for the tray controller; covered by a startup smoke check.

**[Trade-off] No Generic Host** → Benefit: minimal moving parts. Cost: no built-in options/config pipeline; acceptable while services are few.

**[Trade-off] Second instance exits silently** → Benefit: no confusing duplicate UI. Cost: if signaling fails, the user sees "nothing happened"; mitigated by the short retry and a logged (later) warning.

## Migration Plan

1. Add `CLIHub.Core.Services.SingleInstanceGuard` and `DirectoryInitializer`
2. Add `CLIHub.ServiceRegistration` and `CLIHub.TrayIconController`
3. Rewrite `App.xaml.cs` startup: guard → init dirs → build container → tray → (conditionally) window
4. Remove `StartupUri` from `App.xaml`; make `MainWindow` constructor-injected
5. Delete `AppServices.cs`
6. Update tests: add `SingleInstanceGuard` coverage (second guard sees `IsFirstInstance == false`)

Rollback: revert the code; `config.json` is unaffected, so downgrading is safe.

## Open Questions

- **Start hidden vs. shown?** Deferred; a later change can add a "Start minimized" preference.
- **Should `cache\` be created if unused?** Kept for forward-compatibility and documented layout parity.
