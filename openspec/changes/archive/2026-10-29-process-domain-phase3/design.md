# Design

## Current State (Verified)

### Consumer 1: AgentCommandService

**File:** `src/CLIHub.Core/Agents/AgentCommandService.cs:11-23`

```csharp
public class AgentCommandService : IAgentCommandService
{
    private readonly ProcessLauncher processLauncher;
    private readonly ILogger<AgentCommandService> logger;

    public AgentCommandService(ProcessLauncher processLauncher, ILogger<AgentCommandService> logger)
    {
        this.processLauncher = processLauncher;
        this.logger = logger;
    }
}
```

**Usage:**
- `ExecuteAsync` (lines 26-54) — calls `processLauncher.LaunchProcess` for interactive commands
- `RunVersionAsync` (lines 56-74) — calls `processLauncher.CaptureOutputAsync` for version retrieval
- Maps `ProcessCaptureResult` → `AgentCommandResult`
- No runtime selection — uses `ProcessLauncher`'s internal state

### Consumer 2: AgentVersionService

**File:** `src/CLIHub.Core/Agents/AgentVersionService.cs:16-48`

```csharp
public class AgentVersionService : IAgentVersionService
{
    private readonly ProcessLauncher processLauncher;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentVersionService> logger;
    private readonly TimeProvider timeProvider;

    public AgentVersionService(ProcessLauncher processLauncher,
                               IPreferencesStore preferencesStore,
                               ILogger<AgentVersionService> logger,
                               TimeProvider? timeProvider = null)
    {
        this.processLauncher = processLauncher;
        this.preferencesStore = preferencesStore;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }
}
```

**Usage:**
- `ProbeAndCacheAsync` (lines 79-104) — calls `processLauncher.CaptureOutputAsync`
- Uses `UserProfile` as working directory
- Applies `ProbeTimeout` from preferences (default 10s)
- Already injects `IPreferencesStore` for TTL/timeout configuration

### Consumer 3: ProcessRuntimePreferenceTarget

**File:** `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:9-24`

```csharp
public sealed class ProcessRuntimePreferenceTarget : IRuntimePreferenceTarget
{
    private readonly ProcessLauncher processLauncher;

    public ProcessRuntimePreferenceTarget(ProcessLauncher processLauncher)
    {
        this.processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
    }

    public void SetRuntime(RuntimeKind runtime) => this.processLauncher.SetRuntime(runtime);
}
```

**Usage:**
- Thin adapter over `ProcessLauncher.SetRuntime`
- Called from Settings UI when runtime preference changes
- No other logic

### Consumer 4: StartupPreferencesApplier

**File:** `src/CLIHub/Startup/StartupPreferencesApplier.cs:10-36`

```csharp
public sealed class StartupPreferencesApplier : IStartupPreferencesApplier
{
    private readonly ProcessLauncher processLauncher;
    private readonly IStartupService startupService;

    public StartupPreferencesApplier(
        ProcessLauncher processLauncher,
        IStartupService startupService)
    {
        this.processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
        this.startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
    }

    public void Apply(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        this.processLauncher.SetRuntime(preferences.DefaultRuntime);
        this.startupService.SetEnabled(preferences.StartWithWindows);
    }
}
```

**Usage:**
- Applies runtime preference on application startup
- Calls `ProcessLauncher.SetRuntime` once during startup
- Also applies Windows startup setting

### Consumer 5: AgentProcessMonitor

**File:** `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:12-35`

```csharp
public sealed class AgentProcessMonitor
{
    private readonly IAgentProcessInspector processInspector;
    private readonly AgentProcessMatcher processMatcher;
    private readonly IPluginCatalog pluginCatalog;
    private readonly ILogger<AgentProcessMonitor> logger;

    public AgentProcessMonitor(IAgentProcessInspector processInspector,
                               AgentProcessMatcher processMatcher,
                               IPluginCatalog pluginCatalog,
                               ILogger<AgentProcessMonitor> logger)
    {
        this.processInspector = processInspector;
        this.processMatcher = processMatcher;
        this.pluginCatalog = pluginCatalog;
        this.logger = logger;
    }
}
```

**Usage:**
- `GetRunningAgents` (lines 78-107) — orchestrates:
  1. Gets all plugins from catalog
  2. Gets candidate names from matcher
  3. Gets running processes from inspector
  4. Filters matches via matcher
  5. Returns `RunningAgent` records
- `HasRunningAgents` methods delegate to `GetRunningAgents`
- Maps `AgentProcessInstance` → `RunningAgent(Plugin, AgentProcessInstance)`

### ProcessLauncher State Management

**File:** `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:14-51`

```csharp
public sealed class ProcessLauncher
{
    private RuntimeKind runtime = RuntimeKind.PowerShell;

    public void SetRuntime(RuntimeKind runtimeKind) => this.runtime = runtimeKind;
    
    public RuntimeKind GetRuntime() => this.runtime;
}
```

- **Stateful singleton** — holds mutable `runtime` field
- `SetRuntime` called by `ProcessRuntimePreferenceTarget` and `StartupPreferencesApplier`
- `LaunchProcess` and `CaptureOutputAsync` use `this.runtime`
- No thread synchronization (single-threaded WPF UI assumptions)

### IProcessOrchestrator API (Phase 1+2)

**File:** `src/CLIHub.Core/Processes/IProcessOrchestrator.cs:8-43`

```csharp
public interface IProcessOrchestrator
{
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria);
    Task<RuntimeExecutable?> ResolveRuntimeAsync(RuntimeKind kind, CancellationToken cancellationToken = default);
}
```

- `LaunchRequest` requires `RuntimeKind` per call (not stateful)
- `ProcessResult` replaces `ProcessCaptureResult`
- `AgentProcess` replaces `AgentProcessInstance`

## Architecture Decisions

### Decision 1: Runtime Preference from Store, Not Orchestrator State

**Rationale:** `ProcessLauncher` held mutable runtime state and provided `SetRuntime`/`GetRuntime`. The orchestrator is stateless and takes `RuntimeKind` per `LaunchRequest`. Consumers must read runtime from `IPreferencesStore` when constructing requests.

**Migration:**
- `AgentCommandService` — inject `IPreferencesStore`, read `DefaultRuntime` in `ExecuteAsync`
- `AgentVersionService` — already injects `IPreferencesStore`, read `DefaultRuntime` in `ProbeAndCacheAsync`
- `ProcessRuntimePreferenceTarget` — write directly to `IPreferencesStore` (no orchestrator call)
- `StartupPreferencesApplier` — write directly to `IPreferencesStore` (no orchestrator call)

**Alternatives Rejected:**
- **Add stateful `SetRuntime` to orchestrator** — violates stateless design
- **Global mutable runtime store** — orchestrator already takes `RuntimeKind` per request

### Decision 2: AgentProcessMonitor Uses FindRunningAgentsAsync

**Rationale:** `AgentProcessMonitor` currently orchestrates `IAgentProcessInspector` + `AgentProcessMatcher` + `IPluginCatalog`. The orchestrator's `FindRunningAgentsAsync` already wraps this logic via `AgentProcessFinder` adapter (Phase 1+2). Delegate to orchestrator instead of duplicating.

**Migration:**
- Replace inspector + matcher with `IProcessOrchestrator`
- Keep `IPluginCatalog` for `GetRunningAgents()` overload (all plugins)
- For plugin-specific overload, construct `ProcessMatchCriteria` and call orchestrator

**Alternatives Rejected:**
- **Keep current orchestration** — duplicates `AgentProcessFinder` logic
- **Remove `IPluginCatalog` dependency** — breaks `GetRunningAgents()` API

### Decision 3: ProcessResult → AgentCommandResult Mapping

**Rationale:** `AgentCommandService` returns `AgentCommandResult(bool Success, string? Output, string? Error)`. Map:
- `ProcessResult.IsSuccess` → `Success = true`
- `ProcessResult.Failure/Timeout/Cancelled` → `Success = false`, `Error = result.Error ?? result.Kind.ToString()`
- `ProcessResult.Output` → `Output`

**Alternatives Rejected:**
- **Change return type** — breaking change to `IAgentCommandService`
- **Expose `ProcessResult` directly** — leaks domain types to public API

### Decision 4: Preserve Exact ProcessCaptureResult Behavior

**Rationale:** `ProcessLauncher.CaptureOutputAsync` returns `ProcessCaptureResult(bool Started, int ExitCode, string StdOut, string StdErr)`. `ProcessOrchestrator.CaptureOutputAsync` returns `ProcessResult`. Map:
- `ProcessResult.IsSuccess && ExitCode == 0` → `Started = true`
- `ProcessResult.Kind != Success` → `Started = false`
- `ProcessResult.Output` → `StdOut`
- `ProcessResult.Error` → `StdErr`

**Note:** `AgentVersionService` checks `!result.Started || result.ExitCode != 0` for failure. Preserve this check.

### Decision 5: Mark ProcessLauncher Obsolete, Not Remove

**Rationale:** `ProcessLauncher` is still referenced in DI registration and may have undiscovered consumers. Mark with `[Obsolete]` attribute in Phase 3; full removal deferred to Phase 4 after validation.

**Message:** `"Use IProcessOrchestrator. Will be removed in Phase 4."`

## Type Mappings

### ProcessCaptureResult → ProcessResult

| Old (`ProcessCaptureResult`) | New (`ProcessResult`) |
|---|---|
| `Started: true, ExitCode: 0` | `Kind: Success, ExitCode: 0` |
| `Started: false` | `Kind: Failure, Error: "..."` |
| `StdOut` | `Output` |
| `StdErr` | `Error` |

### PluginCommand + RuntimeKind → LaunchRequest

```csharp
// Old
processLauncher.LaunchProcess(command, workingDirectory);
processLauncher.CaptureOutputAsync(command.Executable, command.Arguments, workingDirectory, cancellationToken);

// New
var runtime = this.preferencesStore.Load().DefaultRuntime;
var request = LaunchRequest.Create(workingDirectory, command, runtime, cancellationToken);
await this.orchestrator.LaunchInteractiveAsync(request);
await this.orchestrator.CaptureOutputAsync(request);
```

### AgentProcessInstance → AgentProcess (via Orchestrator)

| Old (`AgentProcessInstance`) | New (`AgentProcess`) |
|---|---|
| `ProcessId` | `ProcessId` |
| `CommandLine` | `CommandLine` |
| `ExecutablePath` | (not mapped; use `Runtime.ExecutablePath`) |
| `StartTime` | `StartTime` |
| (none) | `Runtime: RuntimeExecutable?` |

**Note:** Orchestrator's `FindRunningAgentsAsync` already returns `AgentProcess` list.

## Consumer-by-Consumer Migration Plan

### Consumer 1: AgentCommandService

**Current Dependencies:**
- `ProcessLauncher processLauncher`
- `ILogger<AgentCommandService> logger`

**New Dependencies:**
- `IProcessOrchestrator orchestrator`
- `IPreferencesStore preferencesStore`
- `ILogger<AgentCommandService> logger`

**Changes:**
1. Constructor: inject `IProcessOrchestrator` + `IPreferencesStore`
2. `ExecuteAsync`:
   - Read runtime: `var runtime = this.preferencesStore.Load().DefaultRuntime;`
   - Create request: `var request = LaunchRequest.Create(projectPath, command, runtime, cancellationToken);`
   - Version command: `var result = await this.orchestrator.CaptureOutputAsync(request);`
   - Interactive command: `var result = await this.orchestrator.LaunchInteractiveAsync(request);`
   - Map: `ProcessResult.IsSuccess` → `AgentCommandResult(true, result.Output, null)`
   - Map failure: `ProcessResult.Failure` → `AgentCommandResult(false, null, result.Error ?? "Failed")`
3. Remove `RunVersionAsync` inlining (merge into `ExecuteAsync` version branch)

**Preserved Behavior:**
- Version command returns output
- Interactive command returns success/failure (no output)
- Failure messages unchanged

### Consumer 2: AgentVersionService

**Current Dependencies:**
- `ProcessLauncher processLauncher`
- `IPreferencesStore preferencesStore`
- `ILogger<AgentVersionService> logger`
- `TimeProvider timeProvider`

**New Dependencies:**
- `IProcessOrchestrator orchestrator`
- `IPreferencesStore preferencesStore`
- `ILogger<AgentVersionService> logger`
- `TimeProvider timeProvider`

**Changes:**
1. Constructor: replace `ProcessLauncher` with `IProcessOrchestrator`
2. `ProbeAndCacheAsync`:
   - Read runtime: `var runtime = this.preferencesStore.Load().DefaultRuntime;`
   - Create request: `var request = LaunchRequest.Create(workingDirectory, command, runtime, CancellationToken.None);`
   - Call: `var result = await this.orchestrator.CaptureOutputAsync(request);`
   - Map: `result.IsSuccess && result.ExitCode == 0` → extract version from `result.Output`
   - Map failure: log `result.Error`

**Preserved Behavior:**
- Timeout from preferences
- Cache invalidation unchanged
- Version extraction logic unchanged

### Consumer 3: ProcessRuntimePreferenceTarget

**Current Dependencies:**
- `ProcessLauncher processLauncher`

**New Dependencies:**
- `IPreferencesStore preferencesStore`

**Changes:**
1. Constructor: replace `ProcessLauncher` with `IPreferencesStore`
2. `SetRuntime`:
   - Read current preferences
   - Update `DefaultRuntime`
   - Write back to store

**Preserved Behavior:**
- Called from Settings UI
- Synchronous operation
- Immediate effect

**Note:** This adapter no longer talks to process infrastructure; it's a pure preference writer.

### Consumer 4: StartupPreferencesApplier

**Current Dependencies:**
- `ProcessLauncher processLauncher`
- `IStartupService startupService`

**New Dependencies:**
- `IPreferencesStore preferencesStore`
- `IStartupService startupService`

**Changes:**
1. Constructor: replace `ProcessLauncher` with `IPreferencesStore`
2. `Apply`:
   - Remove `processLauncher.SetRuntime` call (preferences already loaded by caller)
   - Keep `startupService.SetEnabled` call

**Preserved Behavior:**
- Called on startup
- Applies Windows startup setting
- Runtime preference already in store (no write needed)

**Note:** Runtime preference is **read** by orchestrator consumers; applier just validates it exists.

### Consumer 5: AgentProcessMonitor

**Current Dependencies:**
- `IAgentProcessInspector processInspector`
- `AgentProcessMatcher processMatcher`
- `IPluginCatalog pluginCatalog`
- `ILogger<AgentProcessMonitor> logger`

**New Dependencies:**
- `IProcessOrchestrator orchestrator`
- `IPluginCatalog pluginCatalog`
- `ILogger<AgentProcessMonitor> logger`

**Changes:**
1. Constructor: replace `IAgentProcessInspector` + `AgentProcessMatcher` with `IProcessOrchestrator`
2. `GetRunningAgents()` (all plugins):
   - Get all plugins
   - For each plugin:
     - Create criteria: `ProcessMatchCriteria.Create(plugin, detail)`
     - Call: `var processes = await this.orchestrator.FindRunningAgentsAsync(criteria);`
     - Map: `processes.Select(p => new RunningAgent(plugin, MapToInstance(p)))`
   - Aggregate results
3. `GetRunningAgents(Plugin plugin, AgentProcessMatchDetail detail)`:
   - Create criteria: `ProcessMatchCriteria.Create(plugin, detail)`
   - Call: `var processes = await this.orchestrator.FindRunningAgentsAsync(criteria);`
   - Map: `processes.Select(p => new RunningAgent(plugin, MapToInstance(p)))`

**Helper Method:**
```csharp
private static AgentProcessInstance MapToInstance(AgentProcess process)
    => new()
    {
        ProcessId = process.ProcessId,
        CommandLine = process.CommandLine,
        ExecutablePath = process.Runtime?.ExecutablePath ?? string.Empty,
        StartTime = process.StartTime
    };
```

**Preserved Behavior:**
- Same candidate filtering (now internal to orchestrator)
- Same WMI timeout/degradation (now internal to orchestrator)
- Same `RunningAgent` result type

## Open Questions

1. **ProcessRuntimePreferenceTarget write-back** — Should `SetRuntime` trigger a preferences save, or rely on caller? Current implementation assumes `ProcessLauncher` is in-memory state; preferences store requires explicit save.
   - **Resolution needed:** Verify Settings UI save behavior.

2. **StartupPreferencesApplier runtime application** — If runtime preference is already in store on startup, does applier need to do anything?
   - **Resolution needed:** Verify startup sequence — is applier just validating, or actively applying?

3. **AgentProcessMonitor async conversion** — Current methods are synchronous; orchestrator is async. Should we:
   - Keep sync signatures and block on async calls (`.GetAwaiter().GetResult()`)?
   - Change signatures to async and update all callers?
   - **Recommendation:** Keep sync signatures for Phase 3; defer async conversion to later.

4. **ProcessLauncher removal timeline** — Phase 4 is optional. Should Phase 3 include a removal plan?
   - **Recommendation:** Mark obsolete in Phase 3; defer removal decision.

5. **Test coverage gaps** — Do all five consumers have existing unit tests?
   - **Resolution needed:** Verify test file existence before implementation.
