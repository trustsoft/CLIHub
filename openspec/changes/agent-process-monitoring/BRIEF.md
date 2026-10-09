# Agent Process Monitoring

## Problem
Update command doesn't check if agents are currently running, which could cause crashes or data loss when updating while agents are active. Users have no visibility into which agent processes are running.

## Goal
Add monitoring of running agent processes to prevent updates while agents are active, with clear user feedback.

## Solution Approach
Three-phase implementation:
1. **Core Process Detection** - Create process inspection infrastructure
2. **Agent Matching Logic** - Match running processes to registered agents
3. **Update Command Integration** - Block updates when agents are running

## Non-Goals
- Graceful shutdown of agents (future enhancement)
- Real-time process monitoring UI (future enhancement)
- Cross-platform support (Windows-only for now)

## Success Criteria
- UpdateCommand.CanExecute returns false when any agent process is running
- UI shows clear message listing which agents are blocking the update
- No false positives (non-agent processes don't block updates)
- No false negatives (running agents always detected)

## Constraints
- Windows 10/11 only (existing constraint)
- No breaking changes to public APIs
- All existing tests must continue to pass
- New functionality must have comprehensive unit tests
