## Context

See proposal.md - Why. Today `Plugin.Commands` is a `List<PluginCommand>` and `MainWindow` launches the first entry; `PluginCommand` has `Name`, `Executable`, `Arguments`. `ProcessLauncher` only spawns Windows Terminal sessions. `PluginManager` validates "at least one command". The plugin format is our own and pre-1.0, so a schema change is acceptable if handled explicitly.

## Goals / Non-Goals

**Goals:**
- A named, typed command model matching the vision (launch, resume last, version, update, init)
- Execute each command kind correctly (terminal vs captured output)
- Detect host install and per-project availability from declared markers, with no execution
- Clear validation errors for malformed plugins
- Keep all logic in `CLIHub.Core` and unit-testable

**Non-Goals:**
- A plugin-authoring UI or schema editor
- Automatic plugin discovery/download for the six named agents (sample descriptors only)
- Polling or watching for install/availability changes (checked on demand)
- Parallel or streaming output capture (version returns final stdout)

## Decisions

### Decision 1: Structured `AgentCommands` object (BREAKING schema change)

**Chosen:**
```jsonc
{
  "id": "opencode",
  "name": "OpenCode",
  "logo": "logo.png",
  "commands": {
    "launch":  { "executable": "opencode" },
    "resume":  { "executable": "opencode", "arguments": "--continue" },
    "version": { "executable": "opencode", "arguments": "--version" },
    "update":  { "executable": "npm", "arguments": "update -g opencode-ai" },
    "init":    { "executable": "opencode", "arguments": "init" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.opencode", "%USERPROFILE%\\.config\\opencode" ],
    "projectIndicators": [ ".opencode", "openspec" ]
  }
}
```

C# model:
```csharp
public enum AgentCommandKind { Launch, Resume, Version, Update, Init }

public sealed class AgentCommands
{
    public PluginCommand? Launch { get; set; }
    public PluginCommand? Resume { get; set; }
    public PluginCommand? Version { get; set; }
    public PluginCommand? Update { get; set; }
    public PluginCommand? Init { get; set; }
    public PluginCommand? Get(AgentCommandKind kind) => kind switch { ... };
}

public sealed class AgentDetection
{
    public List<string> SystemPaths { get; set; } = new();
    public List<string> ProjectIndicators { get; set; } = new();
}
```

**Rationale:**
- The vision names five specific commands; a list of arbitrary `{name, ...}` entries cannot express "this is the resume command"
- `Get(kind)` gives call sites type-safe access and a single place to add kinds
- Matches the plugin.json example already used while planning this project

**Alternatives considered:**
- Keep a list and match on `Name`: Rejected — stringly-typed, error-prone, and breaks if a name changes
- Separate top-level fields on `Plugin`: Rejected — pollutes the entity and complicates the descriptor

**Migration:** update the sample `plugin.json` files; document the new shape in the change. Plugins without `commands.launch` are rejected with a warning (they simply fail to load, as before).

### Decision 2: `PluginCommand` stays the per-command unit

**Chosen:** Reuse `PluginCommand { Name, Executable, Arguments }` as the value type for each kind; `Name` becomes an optional display label (the kind supplies identity).

**Rationale:** Avoids churn in `ProcessLauncher` and the descriptor; `Name` is still useful for toasts/labels.

**Alternatives considered:** Replace with a lighter record — unnecessary churn.

### Decision 3: `IAgentCommandService` routes execution by kind

```csharp
public interface IAgentCommandService
{
    Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin, AgentCommandKind kind, string projectPath, CancellationToken ct = default);
}

public sealed record AgentCommandResult(bool Success, string? Output, string? Error);
```

Rules: `Launch`, `Resume`, `Init`, `Update` → `IProcessLauncher.LaunchProcess` (terminal). `Version` → `IProcessLauncher.CaptureOutputAsync` (no terminal). Validate: plugin defines the kind; `projectPath` exists.

**Rationale:** One entry point for the UI and tray; keeps terminal-vs-capture policy in one place; returns a uniform result for reporting.

**Alternatives considered:**
- Call `IProcessLauncher` directly from the UI: Rejected — duplicates the kind→mode routing and validation
- Async for all commands: Chosen — `Version` must await output; terminal commands complete when spawned

### Decision 4: `ProcessLauncher.CaptureOutputAsync` for version

```csharp
Task<(bool Started, int ExitCode, string StdOut, string StdErr)> CaptureOutputAsync(
    string executable, string? arguments, string workingDirectory, CancellationToken ct);
```

Uses `ProcessStartInfo` with `RedirectStandardOutput/Error`, `UseShellExecute = false`, `CreateNoWindow = true`, and a timeout.

**Rationale:** Version needs the text; terminal spawning cannot provide it. Keeping it on `IProcessLauncher` preserves the single process-boundary service and stays testable via the interface.

**Alternatives considered:**
- A separate `IOutputRunner` service: Rejected — splits process handling across two services for little gain

### Decision 5: `IAgentDetectionService` uses file checks with env expansion

```csharp
public interface IAgentDetectionService
{
    bool IsInstalledInSystem(Plugin plugin);
    bool IsAvailableInProject(Plugin plugin, string projectPath);
}
```

- System: expand each `systemPaths` entry with `Environment.ExpandEnvironmentVariables`, then `File.Exists || Directory.Exists`.
- Project: `Path.Combine(projectPath, indicator)` then `File.Exists || Directory.Exists`; if the project folder is missing, return false.

**Rationale:** Pure, fast, deterministic; matches the spec's "no execution" requirement.

**Alternatives considered:**
- Run `<exe> --version` to test presence: Rejected — slow and has side effects; the spec forbids execution for detection

### Decision 6: `PluginManager` validates the new schema

**Chosen:** A plugin is valid when `Id` and `Name` are set and `Commands.Launch` is present; `Detection` defaults to empty lists when absent. Invalid plugins are skipped with a warning naming the missing piece.

**Rationale:** Keeps the previous "skip invalid, keep loading" behavior and the new launch-required rule in one place.

**Alternatives considered:** Warn-and-load without launch — rejected; a plugin that cannot launch is not useful.

### Decision 7: UI surfaces availability and commands

**Chosen:** The MainWindow agent list shows each agent with system/project availability and offers actions (Launch, Resume, Init, Update, Get version). The tray menu lists launchable agents for the current project.

**Rationale:** Makes the new capabilities reachable; keeps the window as the full surface and the tray as a quick action.

**Alternatives considered:** Tray-only — rejected; the window is where users review agents.

## Risks / Trade-offs

**[Risk] Breaking `plugin.json` schema breaks existing installs** → Mitigation: it is pre-1.0 and our own format; ship updated samples and document the change; invalid plugins are skipped (not crashed on).

**[Risk] A `version` command hangs and blocks the UI** → Mitigation: `CaptureOutputAsync` runs off the UI thread with a timeout; on timeout it is killed and reported as failure.

**[Risk] `systemPaths` with unexpanded or malformed variables** → Mitigation: `ExpandEnvironmentVariables` leaves unknown variables intact; existence check then simply fails.

**[Risk] Terminating commands (`init`, `update`) have side effects** → Mitigation: they run in the user's project in a visible terminal, so the user sees and controls them; no silent execution.

**[Trade-off] Detection is check-on-demand, not reactive** → Benefit: no polling cost. Cost: status can be stale after installing an agent until refresh; a refresh action is provided.

## Migration Plan

1. Add `AgentCommandKind`, `AgentCommands`, `AgentDetection`; change `Plugin.Commands`/add `Plugin.Detection`
2. Add `CaptureOutputAsync` to `IProcessLauncher`/`ProcessLauncher`
3. Add `IAgentCommandService`/`AgentCommandService` and `IAgentDetectionService`/`AgentDetectionService`
4. Update `PluginManager` validation; register new services in `AddClIHubCoreServices`
5. Update `MainWindow` and `TrayIconController`; update sample `plugin.json`
6. Tests for command routing, capture, and detection

Rollback: revert code and restore the previous sample `plugin.json`; config is unaffected.

## Open Questions

- **Install/update the agent for the user automatically?** Deferred — `update` runs the agent's own updater; installing a missing agent is out of scope.
- **Cache detection results?** Deferred — checks are cheap file-system probes.
