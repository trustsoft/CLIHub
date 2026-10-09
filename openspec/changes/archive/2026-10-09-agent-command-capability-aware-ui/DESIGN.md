# Agent Command Capability-Aware UI - Design

## Overview

Make the Agents pane Actions menu capability-aware by adding properties to AgentItem that check command support, updating command predicates in WindowActionCoordinator, and dynamically filtering menu items based on selected agent capabilities.

## Architecture

### Current State

```
LaunchWindow (XAML)
  ├─ Binds to: LaunchWindowViewModel.AgentsActions
  │   └─ Returns: MenuActionCoordinator.AgentsActions
  │       └─ Built by: LaunchWindowActionBuilder.Build()
  │           └─ Creates 5 static menu items (Launch, Resume, Init, Update, Version)
  │
  └─ Commands from WindowActionCoordinator:
      ├─ LaunchCommand: checks HasSelectedAgent() ✓
      ├─ ResumeCommand: checks HasSelectedAgent() ✗ (should check CanResume)
      ├─ InitCommand: checks HasSelectedAgent() ✗ (should check CanInit)
      ├─ UpdateCommand: checks HasSelectedAgent() ✗ (should check CanUpdate)
      └─ VersionCommand: checks HasSelectedAgent() ✗ (should check CanVersion)

AgentItem (per-row model)
  ├─ CanLaunch: Plugin.Commands?.Get(AgentCommandKind.Launch) != null ✓
  └─ CanResume: Plugin.Commands?.Get(AgentCommandKind.Resume) != null ✓
      (Missing: CanInit, CanUpdate, CanVersion)
```

### Target State

```
LaunchWindow (XAML)
  └─ Binds to: LaunchWindowViewModel.AgentsActions
      └─ Returns: MenuActionCoordinator.AgentsActions
          └─ Rebuilt on agent selection change
              └─ Includes only commands supported by selected agent

AgentItem (per-row model)
  ├─ CanLaunch ✓
  ├─ CanResume ✓
  ├─ CanInit (NEW)
  ├─ CanUpdate (NEW)
  └─ CanVersion (NEW)

WindowActionCoordinator
  ├─ ResumeCommand: () => SelectedAgent?.CanResume == true
  ├─ InitCommand: () => SelectedAgent?.CanInit == true
  ├─ UpdateCommand: () => SelectedAgent?.CanUpdate == true
  └─ VersionCommand: () => SelectedAgent?.CanVersion == true
```

## Component Changes

### 1. AgentItem - Add Capability Properties

**File:** `src/CLIHub/ViewModels/AgentItem.cs`

Add three new properties after `CanResume`:

```csharp
/// <summary>
///   Whether the agent defines the init command.
/// </summary>
public bool CanInit => Plugin.Commands?.Get(AgentCommandKind.Init) != null;

/// <summary>
///   Whether the agent defines the update command.
/// </summary>
public bool CanUpdate => Plugin.Commands?.Get(AgentCommandKind.Update) != null;

/// <summary>
///   Whether the agent defines the version command.
/// </summary>
public bool CanVersion => Plugin.Commands?.Get(AgentCommandKind.Version) != null;
```

**Rationale:** Consistent with existing `CanLaunch` and `CanResume` pattern. Each property checks for command presence in plugin.json.

### 2. WindowActionCoordinator - Update Command Predicates

**File:** `src/CLIHub/ViewModels/WindowActionCoordinator.cs`

Change command construction (lines 59-70) from:

```csharp
ResumeCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
    HasSelectedAgent);
InitCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Init),
    HasSelectedAgent);
UpdateCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Update),
    HasSelectedAgent);
VersionCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Version),
    HasSelectedAgent);
```

To:

```csharp
ResumeCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
    () => _getSelectedAgent()?.CanResume == true);
InitCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Init),
    () => _getSelectedAgent()?.CanInit == true);
UpdateCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Update),
    () => _getSelectedAgent()?.CanUpdate == true);
VersionCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Version),
    () => _getSelectedAgent()?.CanVersion == true);
```

**Rationale:** Commands are disabled when no agent is selected OR when the selected agent doesn't support that command.

### 3. LaunchWindowViewModel - Trigger Command Re-evaluation

**File:** `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`

The `SelectedAgent` property (line ~90) already calls `UpdateCommandStates()` on change. Verify that method calls `CommandManager.InvalidateRequerySuggested()` to refresh command CanExecute checks.

**Current code:**
```csharp
private void UpdateCommandStates()
{
    CommandManager.InvalidateRequerySuggested();
}
```

**Action:** Verify this exists and is called on SelectedAgent change. If missing, add it.

### 4. Menu Action Filtering (Optional Enhancement)

**Alternative approach:** Instead of changing command predicates, dynamically rebuild the menu to only show supported commands.

**File:** `src/CLIHub/ViewModels/LaunchWindowActionBuilder.cs`

Add a new overload:

```csharp
public LaunchWindowActions BuildForAgent(
    ICommand addProject,
    ICommand removeProject,
    ICommand toggleFavorite,
    ICommand refresh,
    ICommand launch,
    ICommand resume,
    ICommand initialize,
    ICommand update,
    ICommand showVersion,
    bool showOnlyProjectAgents,
    AgentItem? selectedAgent)
{
    // ... existing project actions ...

    var agentActions = new List<MenuAction>
    {
        new() { Label = "Launch", Glyph = IconGlyphs.Play, Command = launch }
    };

    if (selectedAgent?.CanResume == true)
        agentActions.Add(new() { Label = "Resume Session", Glyph = IconGlyphs.Refresh, Command = resume });

    if (selectedAgent?.CanInit == true)
        agentActions.Add(new() { Label = "Initialize", Glyph = IconGlyphs.Initialize, Command = initialize });

    if (selectedAgent?.CanUpdate == true)
        agentActions.Add(new() { Label = "Update", Glyph = IconGlyphs.Update, Command = update });

    if (selectedAgent?.CanVersion == true)
        agentActions.Add(new() { Label = "Show Version", Glyph = IconGlyphs.Version, Command = showVersion });

    agentActions.Add(MenuAction.Separator());
    agentActions.Add(filterAction);
    agentActions.Add(MenuAction.Separator());
    agentActions.Add(new() { Label = "Refresh", Glyph = IconGlyphs.Refresh, Command = refresh });

    return new LaunchWindowActions(projectActions, agentActions.ToArray(), filterAction);
}
```

**Decision:** Start with approach #2 (command predicates). It's simpler and provides clear visual feedback (disabled menu items). The dynamic menu approach can be added later if desired.

## Data Flow

1. User selects an agent row
2. `LaunchWindowViewModel.SelectedAgent` changes
3. `UpdateCommandStates()` calls `CommandManager.InvalidateRequerySuggested()`
4. WPF re-evaluates all command `CanExecute` predicates
5. Menu items in Actions popup enable/disable based on `CanResume`, `CanInit`, `CanUpdate`, `CanVersion`

## Testing Strategy

### Unit Tests

**File:** `tests/CLIHub.Tests/ViewModels/AgentItemTests.cs` (new)

```csharp
[Fact]
public void CanInit_ReturnsTrueWhenInitCommandExists()
{
    var plugin = CreatePluginWithCommand(AgentCommandKind.Init);
    var item = new AgentItem { Plugin = plugin, Name = "test" };
    
    Assert.True(item.CanInit);
}

[Fact]
public void CanInit_ReturnsFalseWhenInitCommandMissing()
{
    var plugin = CreatePluginWithCommand(AgentCommandKind.Launch);
    var item = new AgentItem { Plugin = plugin, Name = "test" };
    
    Assert.False(item.CanInit);
}

// Similar tests for CanUpdate, CanVersion
```

**File:** `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs` (new)

```csharp
[Fact]
public void InitCommand_DisabledWhenNoAgentSelected()
{
    var coordinator = CreateCoordinator(getSelectedAgent: () => null);
    
    Assert.False(coordinator.InitCommand.CanExecute(null));
}

[Fact]
public void InitCommand_DisabledWhenAgentCannotInit()
{
    var agent = CreateAgentWithoutInit();
    var coordinator = CreateCoordinator(getSelectedAgent: () => agent);
    
    Assert.False(coordinator.InitCommand.CanExecute(null));
}

[Fact]
public void InitCommand_EnabledWhenAgentCanInit()
{
    var agent = CreateAgentWithInit();
    var coordinator = CreateCoordinator(getSelectedAgent: () => agent);
    
    Assert.True(coordinator.InitCommand.CanExecute(null));
}

// Similar tests for UpdateCommand, VersionCommand, ResumeCommand
```

### Manual Testing

1. Select `github-copilot` → Init, Update, Version enabled; Resume disabled
2. Select `openclaude` → Resume, Version enabled; Init, Update disabled
3. Select `cline-cli` → Version enabled; Resume, Init, Update disabled
4. Deselect agent → All commands disabled
5. Switch between agents → Commands update immediately

## Edge Cases

- **No agent selected:** All agent commands disabled
- **Agent with no commands (malformed plugin):** All commands disabled
- **Agent with only Launch:** Only Launch enabled (all others disabled)
- **Commands property null:** Handled by `?.Get()` null-conditional

## Backward Compatibility

- No changes to plugin.json format
- No changes to public APIs
- Existing plugins work without modification
- UI gracefully handles missing commands

## Performance

- Capability checks are O(1) dictionary lookups
- No dynamic menu rebuilding on every selection change
- WPF command re-evaluation is efficient and standard practice
- No noticeable performance impact

## Future Enhancements

1. **Dynamic menu (Phase 2):** Completely hide unsupported commands instead of disabling them
2. **Tooltips:** Show why a command is disabled ("This agent doesn't support updates")
3. **Command discovery:** Runtime detection of new commands in plugin.json
4. **Command aliases:** Support alternative command names in UI

## References

- Phase 13 in `docs/improvements.md`
- Existing `CanLaunch` / `CanResume` pattern in `AgentItem.cs`
- WPF commanding: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/commanding-overview
