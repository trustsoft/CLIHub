## Context

LaunchWindowViewModel currently manages status messages through direct property assignment in multiple locations:

**Selection changes:**
- Line 159: `StatusMessage = $"Current project: {value.Name}";`
- Line 178: `StatusMessage = $"Selected agent: {value.Name}";`

**Preference changes:**
- Line 222: `StatusMessage = value ? "Window pinned open" : "Window unpinned";`

**Agent command results:**
- Line 326: Status updated via callback `message => StatusMessage = message`

**Update outcomes:**
- Line 434: `StatusMessage = message` (from UpdateControl.OutcomeReported)

**Errors:**
- Line 373: `StatusMessage = $"Opened {root}";`
- Line 377: `StatusMessage = $"Could not open {root}: {ex.Message}";`
- Line 402: `StatusMessage = "No agents found. Add plugin.json files...";`

This pattern scatters status coordination logic and makes the ViewModel harder to test and reason about.

Existing coordinators in the codebase: `ProjectPaneController`, `AgentPaneController`, `LaunchCommandCoordinator`, `ShellCoordinator` — all follow the pattern of owning a workflow boundary and exposing observable state.

See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Extract status message coordination into a focused, testable coordinator
- Centralize all status message sources (events, commands, user actions)
- Reduce LaunchWindowViewModel responsibility (~10% line reduction)
- Zero behavior changes — preserve exact message content, timing, and triggers
- Follow established coordinator pattern in codebase

**Non-Goals:**
- Changing status message content or formatting
- Adding new status message sources
- Modifying XAML bindings (preserve StatusMessage property name)
- Changing coordinator registration lifetime (singleton like other coordinators)

## Decisions

### Decision 1: Coordinator subscribes to event sources, not ViewModel

**Rationale:** The coordinator should own status orchestration completely. Having ViewModel forward events to coordinator creates unnecessary coupling.

**Chosen:** 
- Coordinator receives controllers, update control, and external launcher as constructor dependencies
- Coordinator subscribes directly to: `ProjectsChanged`, `OutcomeReported`, agent refresh callbacks
- ViewModel only exposes the coordinator for binding

**Alternatives considered:**
- ViewModel forwards events to coordinator → More coupling, defeats purpose
- Shared event bus → Over-engineering for single subscriber

### Decision 2: Status message property becomes pass-through

**Rationale:** XAML bindings currently target `LaunchWindowViewModel.StatusMessage`. We can preserve this binding contract while delegating to coordinator.

**Chosen:** 
```csharp
public string StatusMessage => _statusCoordinator.CurrentMessage;
```
- ViewModel property forwards to coordinator
- Coordinator raises PropertyChanged, ViewModel forwards it
- XAML bindings unchanged

**Alternatives considered:**
- Change XAML to bind to `StatusMessageCoordinator.CurrentMessage` → Breaking change, unnecessary
- ViewModel stores copy → Duplication, sync issues

### Decision 3: Coordinator is a singleton registered in DI

**Rationale:** Status messages are global UI state, not per-window. Matches other coordinator lifetimes.

**Chosen:** Register as singleton in `ServiceRegistration.cs`, same as `LaunchCommandCoordinator`, `ShellCoordinator`.

**Alternatives considered:**
- Transient → Multiple coordinators would conflict
- Scoped → No scoping needed for desktop app

### Decision 4: Message priorities and transient vs. persistent messages

**Rationale:** Some messages are transient (command results), others persist (selection). Current behavior treats all equally — last write wins.

**Chosen:** Preserve current "last write wins" behavior. No message priorities or timeouts in this refactor.

**Alternatives considered:**
- Add message priorities → Out of scope, behavior change
- Add message timeouts → Out of scope, behavior change

### Decision 5: Constructor initialization order

**Rationale:** Coordinator needs to be ready before ViewModel triggers initial refreshes that generate status messages.

**Chosen:**
- Coordinator constructed and subscribed in its constructor
- ViewModel receives fully initialized coordinator
- Initial status set by coordinator responding to first events

**Alternatives considered:**
- Lazy initialization → Complexity, potential race conditions
- Manual Initialize() call → Easy to forget, error-prone

## Detailed Design

### StatusMessageCoordinator class structure

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Coordinates status messages from multiple sources: controller events, 
///   command outcomes, preference changes, and error conditions.
/// </summary>
public sealed class StatusMessageCoordinator : ObservableObject, IDisposable
{
    private readonly ProjectPaneController _projectPane;
    private readonly AgentPaneController _agentPane;
    private readonly UpdateControlViewModel _updateControl;
    private string _currentMessage = "CLIHub ready";
    private bool _disposed;

    public StatusMessageCoordinator(
        ProjectPaneController projectPane,
        AgentPaneController agentPane,
        UpdateControlViewModel updateControl)
    {
        _projectPane = projectPane;
        _agentPane = agentPane;
        _updateControl = updateControl;

        // Subscribe to event sources
        _projectPane.ProjectsChanged += OnProjectsChanged;
        _updateControl.OutcomeReported += OnUpdateOutcomeReported;
    }

    /// <summary>
    ///   Current status message displayed in the footer.
    /// </summary>
    public string CurrentMessage
    {
        get => _currentMessage;
        private set => SetProperty(ref _currentMessage, value);
    }

    /// <summary>
    ///   Reports a project selection status.
    /// </summary>
    public void ReportProjectSelected(string projectName) => 
        CurrentMessage = $"Current project: {projectName}";

    /// <summary>
    ///   Reports an agent selection status.
    /// </summary>
    public void ReportAgentSelected(string agentName) => 
        CurrentMessage = $"Selected agent: {agentName}";

    /// <summary>
    ///   Reports a window pin status change.
    /// </summary>
    public void ReportWindowPinChanged(bool isPinned) => 
        CurrentMessage = isPinned ? "Window pinned open" : "Window unpinned";

    /// <summary>
    ///   Reports a command execution outcome.
    /// </summary>
    public void ReportCommandOutcome(string message) => 
        CurrentMessage = message;

    /// <summary>
    ///   Reports a folder open success or error.
    /// </summary>
    public void ReportFolderOpen(string path, Exception? error = null) =>
        CurrentMessage = error is null 
            ? $"Opened {path}"
            : $"Could not open {path}: {error.Message}";

    /// <summary>
    ///   Reports when no agents are found.
    /// </summary>
    public void ReportNoAgentsFound() =>
        CurrentMessage = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";

    private void OnProjectsChanged(object? sender, EventArgs e) =>
        CurrentMessage = "Projects refreshed";

    private void OnUpdateOutcomeReported(object? sender, string message) =>
        CurrentMessage = message;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projectPane.ProjectsChanged -= OnProjectsChanged;
        _updateControl.OutcomeReported -= OnUpdateOutcomeReported;
    }
}
```

### LaunchWindowViewModel changes

1. **Constructor:**
   - Add `StatusMessageCoordinator statusCoordinator` parameter
   - Remove `_statusMessage` field
   - Remove `UpdateControl.OutcomeReported` subscription (moved to coordinator)

2. **StatusMessage property:**
   ```csharp
   public string StatusMessage => _statusCoordinator.CurrentMessage;
   ```
   - Forward coordinator's PropertyChanged to own PropertyChanged for XAML binding

3. **Replace direct assignments:**
   - `StatusMessage = $"Current project: {value.Name}"` → `_statusCoordinator.ReportProjectSelected(value.Name)`
   - `StatusMessage = $"Selected agent: {value.Name}"` → `_statusCoordinator.ReportAgentSelected(value.Name)`
   - `StatusMessage = value ? "..." : "..."` → `_statusCoordinator.ReportWindowPinChanged(value)`
   - Command callback → `_statusCoordinator.ReportCommandOutcome`
   - OpenDataFolder → `_statusCoordinator.ReportFolderOpen`
   - RefreshAgents → `_statusCoordinator.ReportNoAgentsFound`

4. **Disposal:**
   - Remove `UpdateControl.OutcomeReported` unsubscription (coordinator owns it)
   - Coordinator disposal handled by DI container

### ServiceRegistration.cs changes

```csharp
services.AddSingleton<StatusMessageCoordinator>();
```

Register before `LaunchWindowViewModel` since ViewModel depends on it.

## Risks / Trade-offs

**[Risk] PropertyChanged forwarding complexity** → Mitigation: Simple one-liner, well-tested pattern in WPF

**[Risk] Coordinator subscription timing** → Mitigation: Coordinator subscribes in constructor, guaranteed ready before first use

**[Trade-off] Additional indirection for status updates** → Accepted: Coordinator provides clear API, better than scattered assignments

**[Trade-off] One more class to maintain** → Accepted: ~80 lines with single responsibility vs. scattered logic across 435-line ViewModel

## Open Questions

None — design is fully specified and follows established patterns.
