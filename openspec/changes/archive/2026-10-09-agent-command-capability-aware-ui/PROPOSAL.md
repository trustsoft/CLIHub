# Agent Command Capability-Aware UI - Proposal

## Why

The Actions menu in the Agents pane currently shows all commands for every agent, regardless of whether the agent actually supports those commands. This creates confusion when users see disabled commands without understanding why they're unavailable. Making the UI capability-aware will improve discoverability and reduce user confusion by clearly showing which commands each agent supports through its plugin.json definition.

## What Changes

This change adds capability detection properties to `AgentItem` (CanInit, CanUpdate, CanShowVersion) and updates command predicates in `WindowActionCoordinator` to check agent capabilities instead of just selection state. Commands will now enable/disable automatically based on whether the selected agent supports them via plugin.json.

## Problem

The Actions menu in the Agents pane currently shows all commands (Launch, Resume, Initialize, Update, Show Version) for every agent, regardless of whether the agent actually supports those commands. This creates a poor user experience:

- Users see disabled menu items without understanding why
- No clear indication of which commands an agent supports
- Inconsistent behavior across different agents
- Users might try commands that don't work, leading to confusion

**Example:** The `cline-cli` agent only supports `launch` and `version` commands, but the menu shows all five commands with three of them disabled.

## Current State

```
AgentItem has:
  ✓ CanLaunch - checks for launch command
  ✓ CanResume - checks for resume command
  ✗ No checks for Init, Update, Version commands

WindowActionCoordinator commands:
  ✓ LaunchCommand - uses HasSelectedAgent()
  ✗ ResumeCommand - uses HasSelectedAgent() (should check CanResume)
  ✗ InitCommand - uses HasSelectedAgent() (should check CanInit)
  ✗ UpdateCommand - uses HasSelectedAgent() (should check CanUpdate)
  ✗ VersionCommand - uses HasSelectedAgent() (should check CanVersion)
```

## Proposed Solution

Make the UI capability-aware by:

1. **Add capability detection** - Extend AgentItem with `CanInit`, `CanUpdate`, `CanVersion` properties that check plugin.json
2. **Update command predicates** - Change WindowActionCoordinator commands to check agent capabilities instead of just selection
3. **Automatic UI updates** - Commands enable/disable automatically based on selected agent's capabilities

## User Experience Impact

**Before:**
```
User selects "cline-cli"
Actions menu shows: Launch ✓, Resume ✗, Init ✗, Update ✗, Version ✓
User confusion: "Why are these disabled?"
```

**After:**
```
User selects "cline-cli"
Actions menu shows: Launch ✓, Resume ✗, Init ✗, Update ✗, Version ✓
System: Commands reflect actual agent capabilities from plugin.json
User understanding: "This agent doesn't support those features"
```

The key improvement: commands correctly reflect what each agent can actually do.

## Implementation Approach

### 1. Model Layer (AgentItem)
Add three boolean properties following the existing pattern:
```csharp
public bool CanInit => Plugin.Commands?.Get(AgentCommandKind.Init) != null;
public bool CanUpdate => Plugin.Commands?.Get(AgentCommandKind.Update) != null;
public bool CanVersion => Plugin.Commands?.Get(AgentCommandKind.Version) != null;
```

### 2. ViewModel Layer (WindowActionCoordinator)
Update command predicates from:
```csharp
InitCommand = new RelayCommand(..., HasSelectedAgent);
```
To:
```csharp
InitCommand = new RelayCommand(..., () => _getSelectedAgent()?.CanInit == true);
```

### 3. UI Layer (Automatic)
WPF CommandManager automatically re-evaluates predicates when:
- Agent selection changes
- LaunchWindowViewModel calls `UpdateCommandStates()`
- CommandManager.InvalidateRequerySuggested() is invoked

## Benefits

1. **Clear capability visibility** - Users immediately see what each agent supports
2. **Better UX** - No confusion about disabled commands
3. **Maintainable** - Follows existing CanLaunch/CanResume pattern
4. **Zero breaking changes** - No plugin.json schema changes
5. **Performant** - O(1) dictionary lookups, <1ms per check
6. **Testable** - Capability logic is pure and easily unit tested

## Alternatives Considered

### Alternative 1: Dynamic Menu (Hide unsupported commands)
Instead of disabling, completely hide commands that aren't supported.

**Pros:** Cleaner menu, no disabled items
**Cons:** Users can't see what commands exist; harder to discover agent differences
**Decision:** Rejected - disabled items provide better discoverability

### Alternative 2: Tooltips on Disabled Commands
Show tooltip explaining why a command is disabled.

**Pros:** Better user education
**Cons:** Extra complexity; tooltips on disabled items are hard in WPF
**Decision:** Deferred to future enhancement

### Alternative 3: Runtime Command Discovery
Dynamically probe which commands work instead of trusting plugin.json.

**Pros:** Always accurate
**Cons:** Slow, complex, unnecessary (plugin.json is source of truth)
**Decision:** Rejected - plugin.json is the contract

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Null Commands collection causes crash | High | Use null-conditional operator (`?.`) |
| Command states don't update on selection | High | Verify UpdateCommandStates() integration |
| Performance degradation | Low | Capability checks are O(1) lookups |
| Regression in existing functionality | Medium | Comprehensive unit and manual testing |

## Testing Strategy

1. **Unit tests** - AgentItem capability properties (9 tests)
2. **Unit tests** - WindowActionCoordinator command predicates (13 tests)
3. **Manual testing** - Each agent type (5 agents × 5 commands)
4. **Edge case testing** - No selection, rapid switching, null handling

## Success Criteria

- ✅ Commands enable/disable based on plugin.json capabilities
- ✅ No exceptions thrown with any agent or selection state
- ✅ Command states update within 50ms of selection change
- ✅ All unit tests pass
- ✅ Manual testing confirms correct behavior for all agents
- ✅ Zero regression in existing functionality

## Estimated Effort

**Implementation:** 3-4 hours
- Model changes: 15 min
- ViewModel changes: 20 min
- Unit tests: 75 min
- Manual testing: 45 min
- Documentation: 20 min

**Risk:** Low - straightforward extension of existing pattern

## Dependencies

None - uses existing infrastructure (AgentItem, WindowActionCoordinator, plugin.json schema).

## Future Enhancements

After this change, we can consider:
1. Dynamic menu content (Phase 2)
2. Tooltips explaining disabled commands
3. Command discovery and validation
4. Visual indicators in agent list showing capabilities

## Recommendation

**Proceed with implementation.** This change:
- Solves a real UX problem
- Uses proven patterns from the codebase
- Has no breaking changes
- Is low risk and high value
- Requires minimal effort (3-4 hours)

The capability-aware UI will make CLIHub's multi-agent support clearer and more intuitive.
