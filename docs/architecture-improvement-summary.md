# CLIHub Architectural Improvement Plan — Executive Summary
**Date:** 2026-10-29  
**Status:** Proposal  
**Target Audience:** Project maintainers, contributors, technical leadership

## Quick Facts

- **Current Health Score:** 8.5/10
- **Codebase Size:** 286 C# files (~922 KB)
- **Test Coverage:** 1,221 tests (100% pass rate)
- **Build Status:** Clean (0 warnings)
- **Technical Debt:** Well-tracked (35+ documented backlog items, 14 resolved)

## What This Analysis Covers

1. **Architectural Assessment** — Layer structure, subsystem organization, testing strategy
2. **Pattern Analysis** — MVVM, service design, dependency injection
3. **Improvement Proposals** — 6 concrete proposals with effort estimates
4. **Evolution Roadmap** — 3-phase plan (Q1-Q3 2027)
5. **Immediate Actions** — 3 quick wins for the next 30 days

## Key Findings

### ✅ Strengths

1. **Clean Architecture** — Three-layer boundary (Core ← Windows ← UI) is well-enforced
2. **Strong Testing** — 4.3 tests per file, comprehensive coverage including real-process integration
3. **Excellent Documentation** — 6 ADRs, architecture docs, active backlog tracking
4. **Consistent Patterns** — MVVM, DI, service layer all follow conventions
5. **Zero Unresolved Blockers** — Production-ready state

### ⚠️ Areas for Improvement

1. **Coordinator Proliferation** — 5 coordinators suggest missing domain concepts
2. **Large Service Interfaces** — Role-based services combine multiple responsibilities
3. **Cross-Cutting Concerns** — Logging, persistence, caching scattered across subsystems
4. **Configuration Coupling** — Single `AppConfig` couples unrelated concerns
5. **UI-Heavy Orchestration** — Workflows live in coordinators rather than Core services

## Six Core Improvement Proposals

| # | Proposal | Benefit | Effort | Risk | ROI |
|---|----------|---------|--------|------|-----|
| 1 | **Extract Cross-Cutting Services** | Reduce duplication, easier testing | Medium (2-3 days) | Low | High |
| 2 | **Introduce Domain Events** | Decouple components, add observability | Medium (3-4 days) | Low | High |
| 3 | **Capability-Based Service Decomposition** | Clear dependencies, focused tests | High (4-6 days) | Medium | Medium |
| 4 | **Process Domain Model** | Unified process operations | High (5-7 days) | Medium | Medium |
| 5 | **Plugin Lifecycle Management** | Explicit lifecycle, easier hot-reload | Medium (3-4 days) | Low | Medium |
| 6 | **Configuration Decomposition (Internal)** | Isolate domain concerns | Medium (2-3 days) | Low | High |

## Evolution Roadmap (Summary)

### Phase 1: Foundation (Q1 2027) — 2-3 weeks
**Focus:** Cross-cutting services, domain events, documentation  
**Risk:** Low (additive changes)  
**Deliverables:** ADR-0007, ADR-0008, 3-5 core events

### Phase 2: Domain Refinement (Q2 2027) — 3-4 weeks
**Focus:** Service decomposition, process domain, plugin lifecycle  
**Risk:** Medium (wide refactoring, tests provide safety)  
**Deliverables:** ADR-0009, ADR-0010, unified orchestrators

### Phase 3: Configuration Evolution (Q3 2027) — 1-2 weeks
**Focus:** Internal decomposition, evaluate split-file migration  
**Risk:** Low (internal refactoring)  
**Deliverables:** ADR-0011, domain-aligned config models

## Immediate Actions (Next 30 Days)

### 🎯 Priority 1: Document Architecture Principles
**What:** Create `docs/architecture-principles.md`  
**Why:** Onboarding, preventing anti-patterns  
**Effort:** 1 day  
**Owner:** Any senior contributor

**Contents:**
- Dependency rules (Core ← Windows ← UI)
- Service design patterns (capability-based > role-based)
- Testing strategies (builders, fakes, integration)
- Anti-patterns (magic strings, static state, God objects)

### 🎯 Priority 2: Extract ILoggingContextProvider
**What:** Introduce structured logging with correlation IDs  
**Why:** Easier debugging of async workflows, foundation for telemetry  
**Effort:** 2 days  
**Owner:** Core maintainer

**Interface:**
```csharp
interface ILoggingContextProvider
{
    IDisposable BeginScope(string operationName);
    ILogger<T> GetLogger<T>();
}
```

**Usage Example:**
```csharp
using (loggingContext.BeginScope("DetectAgents"))
{
    var logger = loggingContext.GetLogger<AgentManagementService>();
    // All logs in this scope share a correlation ID
}
```

### 🎯 Priority 3: First Domain Event (Proof of Concept)
**What:** Implement `ProjectSelected` event  
**Why:** Proves the pattern, decouples project selection from side effects  
**Effort:** 2 days  
**Owner:** Any contributor

**Event:**
```csharp
record ProjectSelected(string Path, DateTime SelectedAt) : IDomainEvent;
```

**Publisher:**
```csharp
// ProjectService.SetCurrentProject
eventPublisher.Publish(new ProjectSelected(path, DateTime.UtcNow));
```

**Subscribers:**
- Logo cache warmup (preload project logo)
- Recency tracking (update lastUsed timestamp)
- Analytics stub (log project usage)

## Decision Criteria for Adopting Proposals

### Accept a Proposal If:
- ✅ It reduces coupling or improves testability
- ✅ Tests provide safety net for refactoring
- ✅ Benefits outweigh migration cost
- ✅ It aligns with ADRs and existing patterns

### Defer a Proposal If:
- ❌ No immediate pain point (speculative architecture)
- ❌ Test coverage is insufficient for safe refactoring
- ❌ Effort exceeds available bandwidth
- ❌ It introduces new dependencies or frameworks

### Reject a Proposal If:
- ❌ It breaks layer boundaries (Core → UI)
- ❌ It introduces breaking changes to stable APIs
- ❌ It requires rewriting large portions of the codebase
- ❌ It conflicts with ADRs or project goals

## Metrics to Track Progress

| Metric | Current | Target (Q3 2027) | How to Measure |
|--------|---------|------------------|----------------|
| **Coordinator Count** | 5 | 2-3 | Count classes named `*Coordinator` |
| **Avg Service Methods** | ~6-8 | ~3-4 | Methods per service interface |
| **Cross-Cutting Duplication** | High | Low | Lines of similar logging/persistence code |
| **Domain Event Coverage** | 0% | 50%+ | % of state changes with events |
| **Test Execution Time** | ~10s | <5s | `dotnet test` duration |
| **ADR Count** | 6 | 11+ | Files in `docs/adr/` |

## Risk Mitigation Strategy

### For All Refactorings:
1. **Incremental Changes** — One subsystem at a time
2. **Test-Driven** — Add tests before refactoring, verify after
3. **Feature Flags** — Use conditional compilation for experimental paths
4. **Smoke Tests** — Manual validation of critical paths (launch, settings, tray)
5. **Rollback Plan** — Every change is a separate commit/PR, easy to revert

### For High-Risk Changes (Process Domain, Service Decomposition):
1. **Facade Pattern** — Introduce new interface alongside old implementation
2. **Dual Implementation** — Run both paths in parallel, compare results
3. **Beta Testing** — Real users on different terminal flavors
4. **Performance Benchmarks** — Before/after comparison (BenchmarkDotNet)

## Success Criteria (By Q3 2027)

### Must Have:
- ✅ All 1,221 tests still passing
- ✅ Zero new build warnings
- ✅ No performance regressions (launch time, command latency)
- ✅ ADR-0007 through ADR-0011 accepted
- ✅ Domain events for 3+ state changes

### Nice to Have:
- 🎯 Coordinator count reduced from 5 to 2-3
- 🎯 Cross-cutting services extracted (logging, persistence, caching)
- 🎯 Service interfaces averaged to 3-4 methods
- 🎯 Configuration decomposed into domain-aligned models

### Stretch Goals:
- 🚀 Plugin hot-reload prototype
- 🚀 Telemetry integration (OpenTelemetry)
- 🚀 Performance improvement (test execution <5s)
- 🚀 Multi-UI prototype (Avalonia or CLI)

## Communication Plan

### Stakeholders:
- **Core Maintainers** — Review proposals, approve ADRs, merge PRs
- **Contributors** — Implement proposals, write tests, update docs
- **Users** — Beta test refactored features, provide feedback

### Updates:
- **Weekly:** Progress report in team channel/Slack
- **Monthly:** Architecture review meeting, demo new patterns
- **Quarterly:** Retrospective, metrics review, roadmap adjustment

### Documentation:
- **ADRs:** Why decisions were made, alternatives considered
- **Architecture Docs:** How the system is structured, patterns used
- **Backlog:** What's deferred, triggers for revisiting
- **CHANGELOG:** User-facing changes, migration notes

## Questions and Next Steps

### Open Questions:
1. Should we split `AppConfig` into separate files eventually, or keep the single-file invariant?
2. Do we need a formal versioning strategy for service interfaces?
3. Should domain events be synchronous or async by default?
4. How do we handle breaking changes in a plugin ecosystem?

### Recommended Next Steps:
1. **Review this analysis** with core maintainers (1 meeting, ~1 hour)
2. **Accept/defer/reject** each of the 6 proposals (async, 1 week)
3. **Assign owners** for accepted proposals
4. **Schedule kick-off** for Phase 1 (Q1 2027 target)
5. **Create tracking board** (GitHub Projects or similar)

---

## Appendix: Related Documents

- **Full Analysis:** `docs/architecture-analysis-2026-10-29.md`
- **Visualization:** `docs/architecture-visualization.md`
- **Current Architecture:** `docs/architecture.md`
- **ADRs:** `docs/adr/README.md`
- **Backlog:** `docs/backlog.md`
- **Project Context:** `docs/project-context.md`

## Contact

For questions or feedback on this analysis, open an issue or discussion in the repository.

---

**Conclusion:** CLIHub has a solid foundation with clear improvement paths. The proposed changes are incremental, low-risk, and build on existing strengths. Starting with the three immediate actions will validate the approach before committing to larger refactorings.
