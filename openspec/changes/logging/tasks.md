## 1. Logging Setup (Core)

- [x] 1.1 Add `Microsoft.Extensions.Logging.Abstractions` to `src/CLIHub.Core/CLIHub.Core.csproj` and verify `CLIHub.Core` restores
- [x] 1.2 Add `src/CLIHub.Core/Logging/LoggingSetup.cs` with `CreateLogger(logsDirectory, minimumLevel)` configuring a file sink to `clihub-.log` (daily rolling, 7-file retention) and the `[timestamp level] message` template, and verify a unit test writes an entry to a temp directory
- [x] 1.3 Add `src/CLIHub.Core/Logging/LogLevelParser.cs` mapping `"Debug"|"Information"|"Warning"|"Error"|"Verbose"|"Fatal"` (case-insensitive) to `LogEventLevel`, defaulting to Information, and verify unit tests cover valid, invalid, and null input

## 2. Service Logging Integration (Core)

- [x] 2.1 Change `PluginManager` to accept `ILogger<PluginManager>` and replace `Console.WriteLine` with Debug/Information/Warning/Error logs (loaded plugin, skipped invalid plugin, duplicate id, missing logo), and verify `CLIHub.Core` compiles
- [x] 2.2 Change `ProcessLauncher` to accept `ILogger<ProcessLauncher>` and log launch attempts/success/failure (including `Win32Exception` and access-denied), and verify it compiles
- [x] 2.3 Change `ConfigService` to accept `ILogger<ConfigService>` and log load fallback/save errors instead of swallowing silently, and verify it compiles
- [x] 2.4 Change `ProjectService` to accept `ILogger<ProjectService>` and log project add/remove/select at Debug/Information, and verify it compiles
- [x] 2.5 Update `ServiceCollectionExtensions.AddClIHubCoreServices` so services resolve their loggers from the container, and verify the container resolves every service

## 3. Host Wiring (UI)

- [x] 3.1 In `App.OnStartup`, after ensuring the AppData layout, read the level via `LogLevelParser` from `config.json` (fallback Information), assign `Log.Logger = LoggingSetup.CreateLogger(logsDir, level)`, and verify the log file is created on first run
- [x] 3.2 In `ServiceRegistration.AddClIHubServices`, add `services.AddLogging(b => b.AddSerilog(Log.Logger, dispose: true))` so `ILogger<T>` resolves, and verify services receive loggers
- [x] 3.3 In `App.OnExit`, call `Log.CloseAndFlush()` before disposing the container and verify the log file contains entries after a normal exit

## 4. Tests

- [x] 4.1 Add `LoggingSetupTests` verifying an entry is written to a temp log directory with the expected format, and that a configured level suppresses lower-level messages
- [x] 4.2 Add `LogLevelParserTests` covering valid/invalid/null input
- [x] 4.3 Update existing service tests to pass a `NullLogger<T>` (or `Microsoft.Extensions.Logging.Abstractions.NullLogger`) where constructors now require `ILogger<T>`, and verify `dotnet test` passes

## 5. Verification

- [x] 5.1 Build the full solution with 0 warnings/errors
- [x] 5.2 Manually verify: start the app, confirm `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log` exists and contains startup entries
- [x] 5.3 Manually verify: triggering a plugin load and an agent launch produces Information/Warning entries with the service category name
- [x] 5.4 Manually verify: with `preferences.logLevel` set to `Debug`, Debug entries appear; with `Warning`, Information entries do not

> Verified: `clihub-20260927.log` contains `INF CLIHub starting`, `INF Loaded plugin opencode (OpenCode)`, `INF Loaded 1 plugin(s)`, `INF CLIHub started`. With `Debug`, `DBG` entries appear; with `Warning`, no `INF` entries are written. 30/30 tests pass.
