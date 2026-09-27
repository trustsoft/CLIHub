## Context

See proposal.md - Why. `Serilog`, `Serilog.Sinks.File`, and `Serilog.Extensions.Logging` are already referenced by `CLIHub`; `Serilog` is referenced by `CLIHub.Core`. `DirectoryInitializer` already creates `%APPDATA%\CLIHub\logs\`. Services are now constructed by the DI container (`app-lifecycle`), so `ILogger<T>` injection is straightforward. Core services currently use `Console.WriteLine` for diagnostics.

## Goals / Non-Goals

**Goals:**
- Durable daily file logs under `%APPDATA%\CLIHub\logs\`
- 7-day retention with no manual cleanup
- One logging pipeline shared by the app and all services via `ILogger<T>`
- Filter framework noise; keep the file readable
- Flush on shutdown so nothing is lost
- Keep logging configuration in `CLIHub.Core` so it is testable without WPF

**Non-Goals:**
- Console sink, UI log viewer, or in-app log display
- Remote/telemetry log shipping
- Per-category log levels beyond the framework override
- Changing `logging` behavior at runtime without restart (level is read at startup)

## Decisions

### Decision 1: Serilog file sink (already the project stack)

**Chosen:** Serilog with `Serilog.Sinks.File`, rolling by day.

**Rationale:**
- Already referenced; consistent with the project's stated stack and `AGENTS.md`
- Structured logging with a compact text template
- Built-in `rollingInterval` and `retainedFileCountLimit`

**Alternatives considered:**
- `Microsoft.Extensions.Logging` built-in file providers: Rejected — no first-party file provider; Serilog is already present
- NLog: Rejected earlier for this project (Serilog chosen)

### Decision 2: Logging configuration lives in Core

**Chosen:** `CLIHub.Core.Logging.LoggingSetup` exposes the Serilog configuration so both the app and tests can build the same pipeline.

```csharp
public static class LoggingSetup
{
    public static Logger CreateLogger(string logsDirectory, LogEventLevel minimumLevel)
    {
        Directory.CreateDirectory(logsDirectory);
        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(
                path: Path.Combine(logsDirectory, "clihub-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
```

**Rationale:** A single, dependency-light factory keeps behavior identical across app and tests, and avoids duplicating the template/retention values. `"clihub-.log"` + `RollingInterval.Day` yields `clihub-YYYYMMDD.log`.

**Alternatives considered:**
- Configure inline in `App.xaml.cs`: Rejected — untestable and duplicated if other entry points appear

### Decision 3: Bridge Serilog into Microsoft.Extensions.Logging

**Chosen:** `services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));` in `ServiceRegistration`, so `ILogger<T>` resolves for every service.

**Rationale:** Services depend only on `Microsoft.Extensions.Logging.Abstractions` (no Serilog in Core service code), keeping Core testable with a null logger. Serilog remains an implementation detail wired at the host.

**Alternatives considered:**
- Inject `Serilog.ILogger` into Core services: Rejected — couples domain services to a specific logging library
- Static `Log.ForContext<T>()` in services: Rejected — a service locator; harder to test

### Decision 4: Log level sourced from `AppPreferences.LogLevel`, read before the container

**Chosen:** `App` reads the configured level from `config.json` with a small, dependency-free parse (`LogLevelParser`) and passes it to `LoggingSetup.CreateLogger`. Default is `Information` when the file or value is missing/invalid.

**Rationale:**
- `ConfigService` itself may log, creating a chicken-and-egg if logging depended on the service
- A raw `System.Text.Json` read of one field avoids constructing any service
- Matches the existing `AppPreferences.LogLevel` field already in `AppConfig`

**Alternatives considered:**
- Re-configure Serilog after config load: Rejected — `LoggerConfiguration` cannot be safely rebuilt with `AddSerilog` already bound; two pipelines is worse
- Hardcode Information: Rejected — the spec requires configurability

### Decision 5: Replace `Console.WriteLine` with `ILogger<T>`

**Chosen:** `PluginManager`, `ProcessLauncher`, `ConfigService`, and `ProjectService` accept `ILogger<T>` and log at appropriate levels (Debug for traced steps, Information for lifecycle, Warning for skipped/invalid items, Error for failures).

**Rationale:** `Console.WriteLine` is invisible in a `WinExe`; structured logs are searchable and carry category names.

**Alternatives considered:**
- Keep `Console.WriteLine` and log separately: Rejected — duplicate diagnostic paths

### Decision 6: Flush on shutdown

**Chosen:** `Log.CloseAndFlush()` in `App.OnExit`, before disposing the container.

**Rationale:** The file sink buffers; without a flush the last entries can be lost on exit. `AddSerilog(..., dispose: true)` also disposes the logger; ordering the explicit flush first guarantees the buffer is written.

**Alternatives considered:**
- Rely on `AppDomain.ProcessExit`: Rejected — not guaranteed for a tray app that exits via `Shutdown()`.

## Risks / Trade-offs

**[Risk] Log file grows large within a day** → Mitigation: 7-day retention bounds total size; a size cap can be added later.

**[Risk] Writing logs on the UI thread causes I/O stalls** → Mitigation: the Serilog file sink is buffered and flushed on a background thread; entries are short.

**[Risk] `dispose: true` plus explicit `CloseAndFlush` double-disposes** → Mitigation: `CloseAndFlush` is idempotent; call the explicit flush first, then dispose the container.

**[Risk] Log level mismatch between bootstrap read and `AppConfig` defaults** → Mitigation: `AppConfig.Preferences.LogLevel` already defaults to `"Information"`; the parser falls back to Information on any problem.

**[Trade-off] Level fixed at startup** → Benefit: simple, no reconfiguration races. Cost: changing level requires restart; acceptable and documented.

**[Trade-off] No console/file output during tests** → Benefit: tests stay quiet; `CreateLogger` can target a temp directory when a test wants to assert output.

## Migration Plan

1. Add `Microsoft.Extensions.Logging.Abstractions` to Core
2. Add `CLIHub.Core.Logging.LoggingSetup` and `LogLevelParser`
3. Add `ILogger<T>` parameters to Core services; replace `Console.WriteLine`
4. In `App.OnStartup`: ensure logs dir → parse level → `Log.Logger = LoggingSetup.CreateLogger(...)` → `AddLogging(AddSerilog)`
5. In `App.OnExit`: `Log.CloseAndFlush()` before disposing the container
6. Tests: assert `LoggingSetup` writes to a temp directory and that services accept a null logger

Rollback: revert code; log files under `%APPDATA%\CLIHub\logs\` are inert and can be deleted.

## Open Questions

- **Should the level also be settable per-session via an environment variable?** Deferred; startup config is sufficient for now.
