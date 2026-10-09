# Agent Command Capability-Aware UI

**Status:** Planning  
**Priority:** Medium  
**Estimated effort:** 4-6 hours

## Problem

Currently, the Agents pane Actions menu displays all five command options (Launch, Resume, Initialize, Update, Show Version) regardless of whether the selected agent actually supports those commands. While Launch and Resume buttons in the agent list correctly check `CanLaunch` and `CanResume`, the menu commands only verify that an agent is selected (`HasSelectedAgent()`), not whether the agent supports the specific command.

This creates a poor user experience:
- Users can click "Initialize" on agents like `openclaude` that don't define an init command
- Users can click "Update" on agents like `cline-cli` that don't support updates
- The command fails with a generic error instead of being disabled upfront

**Current state:**
- `github-copilot`: launch, version, update, init (no resume)
- `openclaude`: launch, resume, version (no update, init)
- `opencode`: launch, resume, version, update (no init)
- `pi`: launch, resume, version, update (no init)
- `cline-cli`: launch, version (no resume, update, init)
- `qwen-code`: launch, version, update (no resume, init)

## Solution

Make menu commands capability-aware by:

1. **Add capability checks to AgentItem** - Similar to existing `CanLaunch` and `CanResume`, add `CanInit`, `CanUpdate`, and `CanVersion` properties that check for command presence in plugin.json

2. **Update WindowActionCoordinator command predicates** - Change `InitCommand`, `UpdateCommand`, and `VersionCommand` to check both agent selection AND capability support

3. **Make menu builder dynamic** - Modify `LaunchWindowActionBuilder` to accept agent capability state and conditionally include menu items based on what the selected agent supports

4. **Wire capability state to menu** - Update `MenuActionCoordinator` and `LaunchWindowViewModel` to rebuild or filter the Agents actions menu when agent selection changes

## Success Criteria

- Menu items (Resume, Init, Update, Version) only appear or are enabled when the selected agent supports them
- Launch always appears (all agents have launch commands)
- Empty selection shows no agent-specific commands or all commands disabled
- UI updates immediately when agent selection changes
- No breaking changes to plugin.json format or public APIs
- Tests verify capability checks for all command kinds

## Scope

**In scope:**
- AgentItem capability properties
- WindowActionCoordinator command predicates
- Menu action filtering/rebuilding
- Tests for new capability checks

**Out of scope:**
- Changes to plugin.json schema
- Runtime command discovery
- Command execution logic
- Error handling improvements (separate concern)

## Dependencies

None - this is purely UI logic that reads existing plugin.json command definitions.

## Risks

- **Dynamic menu rebuilding complexity** - If menu must rebuild on selection change, ensure no flicker or performance issues
- **Alternative:** Keep all items but disable unsupported commands (simpler, clearer UX feedback)

## Notes

This aligns with Phase 13 from improvements.md: "Agent Subsystem Enhancement" - specifically the capability-aware UI portion.
