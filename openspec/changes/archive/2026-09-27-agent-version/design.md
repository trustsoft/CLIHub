## Context

See proposal.md - Why. `agent-commands` provides `IProcessLauncher.CaptureOutputAsync` (shell-resolving, redirected output, timeout) and `Plugin.Commands.Version`. `MainWindow` builds an `AgentItem` list from `IPluginManager` + `IAgentDetectionService`. Detecting versions requires running each agent's CLI; running that synchronously in the constructor would freeze the window and delay the first paint.

## Goals / Non-Goals

**Goals:**
- Per-agent version shown in the list
- No UI blocking; versions appear as they resolve
- No project required
- Avoid re-running CLIs on every refresh
- Unknown handled cleanly

**Non-Goals:**
- Comparing versions / "update available" logic (that is `update-service`)
- Background polling
- A separate version column UI beyond the existing status text

## Decisions

### Decision 1: `IAgentVersionService` in Core, reuse `IProcessLauncher`

```csharp
public interface IAgentVersionService
{
    Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default);
    void Invalidate();
}
```

**Rationale:** Version retrieval is process logic, so it belongs in Core and reuses the existing capture path (no duplicated P/Invoke). Nullable return encodes "unknown".

**Alternatives considered:**
- Add a `GetVersion` to `IAgentCommandService`: Rejected — that interface is project-scoped; versions are not
- Read versions from files on disk: Rejected — not general across agents

### Decision 2: Neutral working directory

**Chosen:** Run the version command with the user profile folder (`Environment.SpecialFolder.UserProfile`) as the working directory.

**Rationale:** The version command does not touch project files, and the working directory must exist; the profile always does.

**Alternatives considered:** Requiring a project — rejected (spec: project-independent); `AppContext.BaseDirectory` — also fine but profile is more shell-like.

### Decision 3: Cache keyed by plugin id, invalidated explicitly

**Chosen:** `ConcurrentDictionary<string, string?>` keyed by plugin id; `Invalidate()` clears it. The list refresh calls `Invalidate()` so an explicit refresh re-checks.

**Rationale:** Avoids re-spawning processes on incidental redraws while still allowing a deliberate refresh.

**Alternatives considered:** No cache — rejected (visible latency); time-based expiry — overkill.

### Decision 4: Asynchronous, parallel population in the UI

**Chosen:** `MainWindow` renders `AgentItem`s immediately, then `await Task.WhenAll` over per-agent `GetVersionAsync`, updating each item. `AgentItem` implements `INotifyPropertyChanged` for `Version` so updates rebind without rebuilding the list.

**Rationale:** First paint is instant; versions stream in; `async` keeps the UI responsive.

**Alternatives considered:**
- Rebuild the whole `ItemsSource` after all versions resolve: Rejected — clears selection/flicker
- Block in the constructor: Rejected — freezes startup

### Decision 5: Timeout and failure tolerated

**Chosen:** Rely on `CaptureOutputAsync`'s timeout; on failure/empty output, the version is `unknown` and the failure is logged.

**Rationale:** A slow or broken CLI must not hang or break the list.

## Risks / Trade-offs

**[Risk] Spawning six CLIs at startup adds load** → Mitigation: lookups run off the UI thread, once, and are cached; only agents with a version command are invoked.

**[Risk] A CLI prints extra text (banners)** → Trade-off: the first non-empty line is taken as the version; descriptors can pin the exact `version` arguments.

**[Risk] `INotifyPropertyChanged` on a sealed view item adds boilerplate** → Accepted: keeps updates smooth without list rebuilds.

**[Trade-off] No periodic refresh** → Benefit: no background churn. Cost: versions refresh on demand (list refresh).

## Migration Plan

1. Add `IAgentVersionService`/`AgentVersionService` (Core), register in `AddClIHubCoreServices`
2. Make `AgentItem` carry a mutable `Version` with `INotifyPropertyChanged`
3. In `MainWindow.RefreshAgents`, render items, then populate versions asynchronously
4. Tests: service returns output, caches, handles missing/failing commands

Rollback: revert code; no persisted state changes.

## Open Questions

- **Show version in the tray menu too?** Deferred; the window is the place for detail.
- **Parse an update-available flag from the version output?** Deferred to `update-service`.
