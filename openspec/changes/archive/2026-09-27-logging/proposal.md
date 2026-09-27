## Why

The app currently has no diagnostics: Core services report problems with `Console.WriteLine`, which is invisible in a `WinExe`, so failures during plugin loading, launching, or config persistence leave no trace. The `%APPDATA%\CLIHub\logs\` folder exists but stays empty. This change adds durable file logging so issues can be diagnosed after the fact.

## What Changes

- Configure Serilog with a rolling file sink writing to `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`
- Retain 7 days of logs (daily rolling, older files deleted)
- Use a structured text format: `[timestamp level] message` with exception stack traces
- Route Serilog through `Microsoft.Extensions.Logging` so services receive `ILogger<T>` from DI
- Replace `Console.WriteLine` diagnostics in Core services with structured logging
- Default level Information; suppress `Microsoft.*` / `System.*` below Warning
- Flush and close the logger on shutdown

## Capabilities

### New Capabilities

- `logging`: durable structured file logging with rotation, retention, level configuration, and framework-noise filtering

### Modified Capabilities

<!-- None: no existing main-spec requirement changes. -->

## Impact

**New code:**
- `src/CLIHub.Core/Logging/LoggingSetup.cs` — Serilog configuration (path, rotation, retention, format, levels) usable from both app and tests
- `src/CLIHub/Logging/` (or reuse Core) — wiring Serilog into the DI container via `AddSerilog`

**Modified code:**
- `src/CLIHub/App.xaml.cs` — initialize logging before other services; `Log.CloseAndFlush()` on exit
- `src/CLIHub.Core/Services/PluginManager.cs`, `ProcessLauncher.cs`, `ConfigService.cs`, `ProjectService.cs` — accept `ILogger<T>` and log instead of `Console.WriteLine`
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — services receive loggers from the container

**Dependencies:**
- Core gains `Microsoft.Extensions.Logging.Abstractions`; `Serilog`, `Serilog.Sinks.File`, `Serilog.Extensions.Logging` already referenced in `CLIHub` and/or Core

**File system:**
- Writes to `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`; deletes logs older than 7 days

**No breaking changes.**
