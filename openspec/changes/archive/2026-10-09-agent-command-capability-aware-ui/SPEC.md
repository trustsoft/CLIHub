# Agent Command Capability-Aware UI - Specification

## Requirements

### Functional Requirements

**FR1: Capability Detection**
- System MUST detect which commands each agent supports from plugin.json
- Detection MUST check for presence of command in `Plugin.Commands` collection
- Detection MUST support all five AgentCommandKind values: Launch, Resume, Init, Update, Version

**FR2: UI Reflection**
- Actions menu MUST enable commands only when selected agent supports them
- Launch command MUST always be enabled when agent is selected (all agents have launch)
- Resume command MUST be enabled only when agent defines resume command
- Init command MUST be enabled only when agent defines init command
- Update command MUST be enabled only when agent defines update command
- Version command MUST be enabled only when agent defines version command

**FR3: Selection State**
- All agent commands MUST be disabled when no agent is selected
- Commands MUST update immediately when agent selection changes
- Command state MUST reflect current selected agent, not previous selection

**FR4: Visual Feedback**
- Disabled menu items MUST be visually distinguishable (grayed out)
- Disabled menu items MUST remain visible in the menu
- User MUST be able to see all available command types even when disabled

### Non-Functional Requirements

**NFR1: Performance**
- Capability checks MUST complete in <1ms per agent
- Selection change MUST update command states in <50ms
- No noticeable UI lag or flicker

**NFR2: Reliability**
- System MUST handle null/missing Commands collection gracefully
- System MUST handle malformed plugin.json without crashes
- System MUST degrade gracefully for unknown command kinds

**NFR3: Maintainability**
- Capability properties MUST follow existing `CanLaunch`/`CanResume` pattern
- No duplication of capability-checking logic
- Clear separation between capability detection and UI binding

**NFR4: Compatibility**
- MUST NOT change plugin.json schema
- MUST NOT break existing plugins
- MUST NOT change public API contracts

## Acceptance Criteria

### AC1: AgentItem Capability Properties
```
GIVEN an AgentItem with a plugin
WHEN the plugin defines an init command in plugin.json
THEN item.CanInit MUST return true
AND when the plugin does NOT define an init command
THEN item.CanInit MUST return false

(Same pattern for CanUpdate, CanVersion)
```

### AC2: Menu Command States - Init
```
GIVEN no agent is selected
WHEN user opens Actions menu
THEN Init menu item MUST be disabled

GIVEN agent "github-copilot" is selected (has init command)
WHEN user opens Actions menu
THEN Init menu item MUST be enabled

GIVEN agent "openclaude" is selected (no init command)
WHEN user opens Actions menu
THEN Init menu item MUST be disabled
```

### AC3: Menu Command States - Update
```
GIVEN agent "opencode" is selected (has update command)
WHEN user opens Actions menu
THEN Update menu item MUST be enabled

GIVEN agent "cline-cli" is selected (no update command)
WHEN user opens Actions menu
THEN Update menu item MUST be disabled
```

### AC4: Menu Command States - Version
```
GIVEN agent "qwen-code" is selected (has version command)
WHEN user opens Actions menu
THEN Show Version menu item MUST be enabled

(Note: Most agents have version, so this should rarely be disabled)
```

### AC5: Menu Command States - Resume
```
GIVEN agent "pi" is selected (has resume command)
WHEN user opens Actions menu
THEN Resume Session menu item MUST be enabled

GIVEN agent "github-copilot" is selected (no resume command)
WHEN user opens Actions menu
THEN Resume Session menu item MUST be disabled
```

### AC6: Selection Change Responsiveness
```
GIVEN "openclaude" is selected (has resume, no init)
AND Actions menu is open
WHEN user selects "github-copilot" (no resume, has init)
THEN Resume MUST become disabled
AND Init MUST become enabled
AND change MUST occur within 50ms
```

### AC7: Null Safety
```
GIVEN an agent with null Commands collection
WHEN UI checks CanInit/CanUpdate/CanVersion
THEN no exception MUST be thrown
AND all capability properties MUST return false
```

### AC8: Launch Always Available
```
GIVEN any agent is selected
WHEN user opens Actions menu
THEN Launch menu item MUST always be enabled
BECAUSE all agents define launch command by convention
```

## User Scenarios

### Scenario 1: Working with Limited Agent
```
User: Selects "cline-cli" agent
System: Shows only Launch and Version as enabled
User: Clicks Actions > Update
System: Menu item is grayed out and unclickable
Outcome: User understands cline-cli doesn't support updates
```

### Scenario 2: Agent with Full Capabilities
```
User: Selects "opencode" agent
System: Shows Launch, Resume, Update, Version as enabled (Init disabled)
User: Can use all four supported commands
Outcome: Full functionality for feature-rich agent
```

### Scenario 3: Quick Agent Switching
```
User: Rapidly switches between agents
System: Commands enable/disable smoothly without lag
User: Opens menu on any agent
System: Correct command states immediately
Outcome: Responsive, accurate UI
```

## Implementation Specification

### Component 1: AgentItem (Model)

**File:** `src/CLIHub/ViewModels/AgentItem.cs`

**Changes:**
Add three properties after line 73 (after CanResume):

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

**Validation:**
- Properties MUST be public
- Properties MUST return bool
- Properties MUST use null-conditional operator (`?.`)
- Properties MUST match existing CanLaunch/CanResume pattern exactly

### Component 2: WindowActionCoordinator (ViewModel)

**File:** `src/CLIHub/ViewModels/WindowActionCoordinator.cs`

**Changes:**
Modify command construction (lines 59-70):

**Before:**
```csharp
ResumeCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
    HasSelectedAgent);
```

**After:**
```csharp
ResumeCommand = new RelayCommand(
    () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
    () => _getSelectedAgent()?.CanResume == true);
```

Apply same pattern to InitCommand, UpdateCommand, VersionCommand.

**Validation:**
- Lambda MUST use null-conditional operator (`?.`)
- Lambda MUST compare to `== true` (not just truthiness)
- Lambda MUST call `_getSelectedAgent()` (not cache the agent)
- LaunchCommand predicate remains `HasSelectedAgent` (no capability check needed)

### Component 3: LaunchWindowViewModel (Orchestration)

**File:** `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`

**Verification Required:**
Confirm `UpdateCommandStates()` is called when `SelectedAgent` changes.

**Expected code:**
```csharp
public AgentItem? SelectedAgent
{
    get => _selectedAgent;
    set
    {
        if (SetProperty(ref _selectedAgent, value))
        {
            UpdateCommandStates();
        }
    }
}

private void UpdateCommandStates()
{
    CommandManager.InvalidateRequerySuggested();
}
```

If missing, ADD the UpdateCommandStates call.

## Testing Requirements

### Unit Tests - AgentItem

**File:** `tests/CLIHub.Tests/ViewModels/AgentItemTests.cs` (create new)

Required test cases:
1. `CanInit_ReturnsTrueWhenInitCommandExists`
2. `CanInit_ReturnsFalseWhenInitCommandMissing`
3. `CanInit_ReturnsFalseWhenCommandsNull`
4. `CanUpdate_ReturnsTrueWhenUpdateCommandExists`
5. `CanUpdate_ReturnsFalseWhenUpdateCommandMissing`
6. `CanUpdate_ReturnsFalseWhenCommandsNull`
7. `CanVersion_ReturnsTrueWhenVersionCommandExists`
8. `CanVersion_ReturnsFalseWhenVersionCommandMissing`
9. `CanVersion_ReturnsFalseWhenCommandsNull`

### Unit Tests - WindowActionCoordinator

**File:** `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs` (create new)

Required test cases:
1. `InitCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
2. `InitCommand_CanExecute_ReturnsFalseWhenAgentCannotInit`
3. `InitCommand_CanExecute_ReturnsTrueWhenAgentCanInit`
4. `UpdateCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
5. `UpdateCommand_CanExecute_ReturnsFalseWhenAgentCannotUpdate`
6. `UpdateCommand_CanExecute_ReturnsTrueWhenAgentCanUpdate`
7. `VersionCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
8. `VersionCommand_CanExecute_ReturnsFalseWhenAgentCannotShowVersion`
9. `VersionCommand_CanExecute_ReturnsTrueWhenAgentCanShowVersion`
10. `ResumeCommand_CanExecute_ReturnsFalseWhenAgentCannotResume`
11. `ResumeCommand_CanExecute_ReturnsTrueWhenAgentCanResume`
12. `LaunchCommand_CanExecute_ReturnsTrueWhenAgentSelected` (verify unchanged)

### Manual Test Plan

1. **Verify github-copilot (launch, version, update, init; no resume)**
   - Select agent → Init, Update, Version enabled; Resume disabled
   - Open Actions menu → Verify states
   - Click enabled items → Commands execute
   - Click disabled Resume → Nothing happens (grayed out)

2. **Verify openclaude (launch, resume, version; no update, init)**
   - Select agent → Resume, Version enabled; Init, Update disabled
   - Open Actions menu → Verify states

3. **Verify cline-cli (launch, version; no resume, update, init)**
   - Select agent → Only Version enabled beyond Launch
   - Open Actions menu → Verify minimal enabled items

4. **Verify opencode (launch, resume, version, update; no init)**
   - Select agent → Most commands enabled except Init
   - Open Actions menu → Verify Init disabled

5. **Verify selection change responsiveness**
   - Select openclaude → Note Resume enabled, Init disabled
   - Switch to github-copilot → Resume disables, Init enables immediately

6. **Verify no agent selected**
   - Deselect all → All agent commands disabled
   - Open Actions menu → All items grayed out

## Out of Scope

- Dynamic menu content (hiding unsupported commands entirely)
- Tooltips explaining why commands are disabled
- Runtime command discovery
- Command execution behavior changes
- Error message improvements
- Plugin.json validation

## Dependencies

None - uses existing infrastructure.

## Risks & Mitigations

**Risk:** WPF command re-evaluation might not trigger on agent selection change
**Mitigation:** Verify UpdateCommandStates() calls CommandManager.InvalidateRequerySuggested()

**Risk:** Null Commands collection causes NullReferenceException
**Mitigation:** Use null-conditional operator (`?.`) in all capability checks

**Risk:** Performance degradation with frequent selection changes
**Mitigation:** Capability checks are O(1) dictionary lookups; no concern

## Success Metrics

- 100% of unit tests pass
- Manual test plan passes for all agents
- No exceptions thrown during agent selection/menu opening
- Command state changes occur within 50ms of selection change
- Zero regression in existing functionality
