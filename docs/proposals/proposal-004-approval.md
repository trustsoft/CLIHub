# Proposal 4 Approval Summary

**Date:** 2026-10-29  
**Proposal:** Process Domain Model  
**Status:** ✅ **APPROVED**

## What Was Approved

**Unified Process Domain** with single orchestrator replacing 5 fragmented services.

### Core Change

Replace:
```
ProcessLauncher + WindowsInteractiveProcessRunner + 
WindowsProcessOutputRunner + AgentProcessMatcher + 
WindowsAgentProcessInspector (5 services)
```

With:
```
IProcessOrchestrator (1 unified interface)
```

### Key Benefits

| Metric | Before | After |
|--------|--------|-------|
| Service dependencies | 5 | 1 |
| Test mocks needed | 5+ | 1 |
| Runtime resolution logic | 2 places (duplicated) | 1 place |
| Process model types | Scattered | Unified (`AgentProcess`, `RuntimeExecutable`) |

## Documents Created

1. **Full Proposal (English):** `docs/proposals/proposal-004-process-domain-model.md` (23 KB)
2. **Full Proposal (Russian):** `docs/proposals/proposal-004-process-domain-model-ru.md` (23 KB)
3. **Quick Summary (Russian):** `docs/proposals/proposal-004-summary-ru.md` (6 KB)
4. **ADR-0012:** `docs/adr/ADR-0012-process-domain-unification.md`

## Implementation Plan

### Phase 1: Domain Models (2 days)
- Create `CLIHub.Core/Processes/` namespace
- Add value objects: `RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`
- ✅ **No breaking changes** — new types coexist with old

### Phase 2: Orchestrator (2 days)
- Implement `ProcessOrchestrator`
- Wire up existing runners as dependencies
- Write comprehensive unit tests
- Register in DI alongside existing services

### Phase 3: Migrate Consumers (2-3 days)
- Update `AgentCommandService`
- Update `LaunchCommandCoordinator`
- Update `WindowActionCoordinator`
- Update `AgentProcessMonitor`
- ✅ Full test suite after each migration

### Phase 4: Deprecate (Optional, later)
- Mark old interfaces `[Obsolete]`
- Remove in next major version

## Success Criteria

- ✅ All 1,221 tests still passing
- ✅ Zero new build warnings
- ✅ Performance within ±5% of baseline
- ✅ All consumers migrated to `IProcessOrchestrator`
- ✅ Integration tests for all runtimes (pwsh, bash, cmd, Windows Terminal)
- ✅ ADR-0012 accepted and published

## Timeline

- **Target:** Q2 2027 (Phase 2 of architecture roadmap)
- **Effort:** 5-7 days
- **Risk:** Medium (mitigated by incremental migration)
- **Dependencies:** Proposal 1 (Cross-Cutting Services) — optional but recommended

## Owner Assignment

- **Proposal Owner:** TBD
- **Implementation Lead:** TBD
- **Reviewer:** TBD

## Next Steps

1. ✅ **Approved** — proposal accepted
2. ⏳ **Assign owner** — who will lead implementation
3. ⏳ **Schedule kick-off** — when to start Phase 1
4. ⏳ **Communicate** — inform team of approved changes
5. ⏳ **Track progress** — create GitHub issues/milestone

## References

- **Analysis Index:** `docs/architecture-analysis-index.md`
- **Full Analysis:** `docs/architecture-analysis-2026-10-29.md` (Section 3.4)
- **Visualization:** `docs/architecture-visualization.md`
- **Backlog Entry:** `docs/backlog.md` (Architecture and design section)

---

**Approval Authority:** Project maintainers  
**Approval Date:** 2026-10-29  
**Recorded by:** Kiro (architectural analysis agent)
