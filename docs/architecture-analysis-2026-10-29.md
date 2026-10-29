# CLIHub Architectural Analysis and Improvement Proposals
**Date:** 2026-10-29  
**Scope:** High-level architectural patterns, structural quality, and evolution roadmap  
**Baseline:** Commit `e141732` (2026-10-29)

## Executive Summary

CLIHub demonstrates a well-structured, maintainable codebase with strong adherence to clean architecture principles. The solution successfully isolates UI-independent Core logic from platform-specific adapters and WPF presentation. The codebase is:

- **286 C# source files** (~922 KB)
- **1,221 tests** (638 Core + 60 Windows + 523 UI) — **all passing**
- **Zero build warnings**
- **Well-documented** with 6 ADRs, comprehensive architecture docs, and an actively maintained backlog

### Health Score: 8.5/10

**Strengths:**
- Clean dependency flow (Core ← Windows ← UI)
- Strong test coverage with explicit boundaries
- Consistent MVVM and service patterns
- Excellent documentation and decision records
- Active technical debt management (backlog tracking)

**Areas for Improvement:**
- Cross-cutting concerns span multiple subsystems
- Some service interfaces are role-based rather than capability-based
- Coordinator proliferation suggests missing domain concepts
- Configuration model couples unrelated concerns

---

## 1. Current Architecture Assessment

### 1.1 Layer Structure ✅ Strong

```
┌─────────────────────────────────────────────┐
│          CLIHub (WPF Application)           │
│   ViewModels, Views, Coordinators, Tray    │
└─────────────────────────────────────────────┘
                    ↓ depends on
┌─────────────────────────────────────────────┐
│       CLIHub.Core.Windows (Adapters)        │
│  Registry, Process Runners, Theme Detection │
└─────────────────────────────────────────────┘
                    ↓ depends on
┌─────────────────────────────────────────────┐
│         CLIHub.Core (Domain + Infra)        │
│  Projects, Plugins, Agents, Configuration   │
└─────────────────────────────────────────────┘
```

**Verdict:** The three-layer boundary is well-enforced. Core has no WPF or Windows API dependencies, Windows adapters implement Core ports, and the UI layer owns presentation coordination.

### 1.2 Subsystem Organization ✅ Good

Core subsystems are organized by ownership:
- `Configuration/` — single JSON document, migrations, snapshots
- `Projects/` — project state, selection, enrichment, logos
- `Scanning/` — watched-root discovery, background watcher
- `Plugins/` — descriptor discovery, validation, catalog
- `Agents/` — commands, detection, versions, updates, process monitoring
- `Updates/` — Velopack integration, release notes
- `Infrastructure/` — process launching, paths, logging

**Strengths:**
- Clear ownership boundaries
- Colocated contracts (interfaces near implementations)
- Consistent naming conventions

**Concerns:**
- Some cross-cutting concerns (logging, persistence) leak into multiple subsystems
- `Infrastructure/` is a catch-all with no cohesive theme

### 1.3 Dependency Injection ✅ Strong

- Production services use single public constructors
- Registration split across `CLIHub.Core.Composition`, `CLIHub.Core.Windows`, and `CLIHub.Composition`
- Lifetime boundaries respected (scoped vs transient)
- One known issue: transient IDisposables retained by root provider (documented in backlog)

### 1.4 Testing Strategy ✅ Excellent

- 1,221 tests with 100% pass rate
- Test projects mirror production structure
- Builders for complex ViewModels (`LaunchWindowViewModelBuilder`)
- Fakes for infrastructure (`FakeInteractiveProcessRunner`, `FakeProcessOutputRunner`)
- Integration tests for real-process capture (Windows.Tests)

---

## 2. Architectural Patterns and Quality Indicators

### 2.1 MVVM Implementation ✅ Good

**Strengths:**
- ViewModels own state and commands
- Thin code-behinds (Views delegate to ViewModels)
- Clear data-binding contracts

**Concerns:**
- Some ViewModels accumulate multiple responsibilities (e.g., `LaunchWindowViewModel` composes project/agent panes, commands, update state, and application services)
- Coordinators (`LaunchCommandCoordinator`, `WindowActionCoordinator`, `StatusMessageCoordinator`) suggest missing domain concepts that could be promoted to first-class services

**Recommendation:** Consider extracting cross-cutting coordination into domain services rather than UI-layer coordinators. For example, `LaunchCommandCoordinator.ExecuteAsync` orchestrates agent command execution — this could be a Core-level `IAgentLaunchOrchestrator` with the UI coordinator reduced to a ViewModel adapter.

### 2.2 Service Interfaces: Role-Based vs. Capability-Based ⚠️ Mixed

**Current Pattern:**
- `IAgentCommandService` — capability-based ✅
- `IAgentManagementService` — role-based ⚠️ (combines detection, availability, updates)
- `IUpdateService` — role-based ⚠️ (combines checking, downloading, installing, state management)

**Issue:** Role-based services accumulate responsibilities and make testing harder. For example, a test needing only agent version detection must stub the entire `IAgentManagementService`.

**Recommendation:** Refactor toward smaller, capability-based interfaces. For example:
```csharp
// Instead of IAgentManagementService doing 5 things:
IAgentDetector
IAgentAvailabilityService
IAgentUpdateService
IAgentProcessMonitor
```

This improves testability, reduces coupling, and makes dependencies explicit.

### 2.3 Configuration Model ⚠️ Monolithic

**Current Design:**
- Single `config.json` with all application state
- `AppConfig` aggregates preferences, projects, scanning settings
- Atomic persistence boundary (ADR-0002)

**Strengths:**
- Simple persistence contract
- Transactional updates
- Migration infrastructure

**Concerns:**
- Preferences, projects, and scanning settings have different change frequencies and lifetimes
- Every subsystem change risks breaking unrelated areas
- The single-document invariant creates coupling between unrelated concerns

**Recommendation:** Maintain the single-file contract externally, but decompose `AppConfig` internally into domain-aligned value objects:
```csharp
record AppConfig(
    PreferencesConfig Preferences,
    ProjectsConfig Projects,
    ScanningConfig Scanning,
    int Version
);
```

This preserves the atomic boundary while isolating domain changes. Migrations would operate on the decomposed structure.

### 2.4 Coordinator Proliferation ⚠️ Pattern Smell

**Observed Coordinators:**
- `LaunchCommandCoordinator` — agent command execution
- `WindowActionCoordinator` — window-level actions (update, agent commands)
- `StatusMessageCoordinator` — status message orchestration
- `MenuActionCoordinator` — menu action dispatch
- `TrayIconController` — tray menu state and actions

**Analysis:** Five coordinators suggest missing domain concepts. Many coordinators are thin adapters between ViewModels and Core services, indicating that the Core layer may not expose sufficiently high-level operations.

**Recommendation:** Promote common orchestration patterns to Core services. For example:
- `LaunchCommandCoordinator.ExecuteAsync` → `IAgentLaunchWorkflow` in Core
- `WindowActionCoordinator.RunAgentCommandAsync` → same as above
- `StatusMessageCoordinator` → consider an event-driven notification service in Core

This reduces UI-layer orchestration and makes workflows testable without WPF dependencies.

---

## 3. Structural Improvement Proposals

### Proposal 1: Extract Cross-Cutting Services

**Problem:** Logging, persistence, and caching logic are scattered across subsystems.

**Solution:** Introduce explicit cross-cutting service contracts:
- `ILoggingContextProvider` — structured logging with correlation IDs
- `IPersistenceService<T>` — generic persistence boundary
- `ICacheService<TKey, TValue>` — pluggable caching strategy

**Impact:**
- Reduced duplication (e.g., file-write patterns in configuration, logos, cache)
- Easier to swap implementations (e.g., migrate from JSON to SQLite)
- Clearer contract for testing

**Effort:** Medium (2-3 days)

---

### Proposal 2: Introduce Domain Events

**Problem:** Components rely on explicit coupling to notify others of state changes. For example, `ProjectService` updates state and callers must poll or subscribe to specific events.

**Solution:** Introduce a lightweight domain event bus:
```csharp
interface IDomainEventPublisher
{
    void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent;
}

interface IDomainEventSubscriber
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent;
}

// Example events:
record ProjectSelected(string Path) : IDomainEvent;
record AgentDetected(string PluginId, AgentInstallState State) : IDomainEvent;
record ThemeChanged(AppTheme Theme, string AccentHex) : IDomainEvent;
```

**Benefits:**
- Decouples components (publishers don't know subscribers)
- Makes workflows observable for debugging and telemetry
- Easier to add new behaviors (e.g., analytics, logging) without touching existing code

**Effort:** Medium (3-4 days)  
**Risk:** Low (additive change, no breaking changes)

---

### Proposal 3: Capability-Based Service Decomposition

**Problem:** Large service interfaces (`IAgentManagementService`, `IUpdateService`) make dependencies unclear and testing verbose.

**Solution:** Extract focused interfaces per capability:

**Example: Agent Management**
```csharp
// Before:
interface IAgentManagementService
{
    Task<IReadOnlyList<AgentAvailability>> ComputeAvailabilityAsync(...);
    Task<AgentProbeResult> ProbeAsync(...);
    Task<AgentCommandResult> UpdateAsync(...);
    Task<AgentCommandResult> DetectAsync(...);
}

// After:
interface IAgentAvailabilityService
{
    Task<IReadOnlyList<AgentAvailability>> ComputeAsync(IReadOnlyList<Plugin> plugins, CancellationToken ct);
}

interface IAgentProbeService
{
    Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken ct);
}

interface IAgentUpdateService
{
    Task<AgentCommandResult> UpdateAsync(Plugin plugin, CancellationToken ct);
}
```

**Benefits:**
- Tests inject only what they need
- Clear dependency graphs (GraphViz/gitnexus shows exact usage)
- Easier to evolve (small interfaces have fewer reasons to change)

**Effort:** High (4-6 days)  
**Risk:** Medium (requires wide refactoring, but tests provide safety net)

---

### Proposal 4: Process Domain Model

**Problem:** Process launching, command building, runtime resolution, and process matching are scattered across multiple services.

**Solution:** Introduce a unified Process Domain with explicit value objects:

```csharp
// Domain models
record RuntimeExecutable(RuntimeKind Kind, string Path);
record AgentProcess(int Pid, string CommandLine, RuntimeExecutable Runtime);
record LaunchRequest(
    string WorkingDirectory,
    AgentCommand Command,
    RuntimePreference Preference
);

// Unified orchestration
interface IProcessOrchestrator
{
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request, CancellationToken ct);
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request, CancellationToken ct);
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(Plugin plugin, CancellationToken ct);
}
```

**Benefits:**
- Single source of truth for process operations
- Testable without Windows APIs (fake implementation)
- Clearer boundary for future enhancements (e.g., containerized agents, remote execution)

**Effort:** High (5-7 days)  
**Risk:** Medium (touches many integration points)

---

### Proposal 5: Plugin Lifecycle Management

**Problem:** Plugin discovery, validation, loading, and seeding are separate concerns with no explicit lifecycle model.

**Solution:** Introduce a `PluginLifecycleManager`:

```csharp
interface IPluginLifecycleManager
{
    Task<PluginCatalogResult> LoadCatalogAsync(CancellationToken ct);
    Task<PluginValidationResult> ValidateAsync(PluginDescriptor descriptor);
    Task SeedDefaultPluginsAsync();
    Task ReloadCatalogAsync(CancellationToken ct);
}

record PluginCatalogResult(
    IReadOnlyList<Plugin> Plugins,
    IReadOnlyList<PluginLoadError> Errors
);
```

**Benefits:**
- Explicit lifecycle phases (discovery → validation → loading → activation)
- Easier to add plugin hot-reload or plugin marketplace features
- Centralized error handling and reporting

**Effort:** Medium (3-4 days)  
**Risk:** Low (mostly organizational refactoring)

---

### Proposal 6: Configuration Decomposition (Internal)

**Problem:** The single `AppConfig` couples unrelated concerns (preferences, projects, scanning).

**Solution:** Decompose internally while maintaining the single-file external contract:

```csharp
// External contract (unchanged)
class AppConfig
{
    public PreferencesDocument Preferences { get; set; }
    public ProjectStateDocument ProjectState { get; set; }
    public ScanningSettingsDocument Scanning { get; set; }
    public int Version { get; set; }
}

// Internal domain models (new)
record PreferencesConfig(...);
record ProjectsConfig(...);
record ScanningConfig(...);

// Mapper
class ConfigurationMapper
{
    AppConfig ToDocument(PreferencesConfig prefs, ProjectsConfig projects, ScanningConfig scanning);
    (PreferencesConfig, ProjectsConfig, ScanningConfig) FromDocument(AppConfig config);
}
```

**Benefits:**
- Changes to preferences don't risk breaking project logic
- Clearer boundaries for testing (mock only the domain slice you need)
- Foundation for splitting the file later (if needed)

**Effort:** Medium (2-3 days)  
**Risk:** Low (internal refactoring, external contract unchanged)

---

## 4. Code Quality Metrics

### 4.1 Complexity Analysis (via GitNexus)

- **Total symbols:** 7,990
- **Relationships:** 14,777
- **Execution flows:** 482
- **Communities (graphify):** 925

**Observation:** High relationship count suggests good modularity (many small collaborations), but 482 execution flows indicate complexity in orchestration paths.

**Recommendation:** Focus refactoring on high-fan-out symbols (many callers or many dependencies). Use gitnexus impact analysis before changes.

### 4.2 Test Coverage (Indirect)

- **1,221 tests** for ~286 source files ≈ **4.3 tests per file**
- No skipped tests, 100% pass rate
- Real-process integration tests included

**Verdict:** Coverage is strong. The backlog contains no "missing test coverage" entries, suggesting confidence in current tests.

### 4.3 Technical Debt Tracking ✅ Excellent

The `docs/backlog.md` file is a model of technical debt management:
- 628 lines of structured findings
- Each entry has: status, severity, evidence, proposed remedy, trigger condition
- Resolved items retained for context (14 resolved entries as of 2026-10-29)

**Recommendation:** Maintain this discipline. Consider adding a "Health Score" metric to the backlog header to track trend over time.

---

## 5. Evolution Roadmap

### Phase 1: Foundation (Q1 2027) — 2-3 weeks

**Goals:**
- Extract cross-cutting services (Proposal 1)
- Introduce domain events (Proposal 2)
- Document architectural principles (patterns, constraints, anti-patterns)

**Deliverables:**
- `ILoggingContextProvider`, `IPersistenceService<T>`, `ICacheService<TKey, TValue>`
- `IDomainEventPublisher` / `IDomainEventSubscriber` with 3-5 core events
- ADR-0007: Domain Event Architecture
- ADR-0008: Cross-Cutting Service Contracts

**Risk:** Low (additive changes, existing code unchanged)

### Phase 2: Domain Refinement (Q2 2027) — 3-4 weeks

**Goals:**
- Capability-based service decomposition (Proposal 3)
- Process domain model (Proposal 4)
- Plugin lifecycle management (Proposal 5)

**Deliverables:**
- Decomposed agent and update services
- Unified `IProcessOrchestrator`
- `IPluginLifecycleManager`
- ADR-0009: Capability-Based Service Design
- ADR-0010: Process Domain Unification

**Risk:** Medium (wide refactoring, but tests provide safety)

### Phase 3: Configuration Evolution (Q3 2027) — 1-2 weeks

**Goals:**
- Configuration decomposition (Proposal 6)
- Evaluate split-file migration (if domain models prove beneficial)

**Deliverables:**
- Internal domain-aligned config models
- `ConfigurationMapper` boundary
- ADR-0011: Configuration Domain Decomposition

**Risk:** Low (internal refactoring)

### Phase 4: Future Enhancements (Q4 2027+)

**Potential Areas:**
- Plugin hot-reload (enabled by lifecycle manager)
- Plugin marketplace (remote catalog discovery)
- Telemetry and observability (enabled by domain events)
- Remote agent execution (enabled by process domain abstraction)
- Multi-repository workspace (project grouping)

---

## 6. Immediate Actions (Next 30 Days)

### Priority 1: Document Current Architecture Principles

**Action:** Create `docs/architecture-principles.md` documenting:
- Dependency rules (Core ← Windows ← UI)
- Service design patterns (capability-based > role-based)
- Testing strategies (builders, fakes, integration)
- Anti-patterns (magic strings, static state, God objects)

**Effort:** 1 day  
**Benefit:** Onboarding new contributors, preventing anti-patterns

### Priority 2: Extract `ILoggingContextProvider`

**Action:** Introduce structured logging with correlation IDs:
```csharp
interface ILoggingContextProvider
{
    IDisposable BeginScope(string operationName);
    ILogger<T> GetLogger<T>();
}
```

**Effort:** 2 days  
**Benefit:** Easier debugging of async workflows, foundation for telemetry

### Priority 3: Introduce First Domain Event (Proof of Concept)

**Action:** Implement `ProjectSelected` event:
- Publisher: `ProjectService.SetCurrentProject`
- Subscribers: Logo cache warmup, recency tracking, analytics stub

**Effort:** 2 days  
**Benefit:** Proves the pattern, decouples project selection from side effects

---

## 7. Long-Term Vision

### 7.1 Hexagonal Architecture Evolution

**Current:** Three-layer architecture (UI → Windows → Core)

**Future:** Hexagonal (Ports & Adapters) with explicit boundaries:

```
┌────────────────────────────────────────────────┐
│            Application Layer (UI)              │
│     ViewModels, Views, Application Services    │
└────────────────────────────────────────────────┘
                    ↓ uses
┌────────────────────────────────────────────────┐
│            Domain Layer (Core)                 │
│  Projects, Agents, Plugins, Configuration      │
│           + Domain Events + Use Cases          │
└────────────────────────────────────────────────┘
                    ↓ defines ports
┌────────────────────────────────────────────────┐
│         Infrastructure Adapters                │
│  Windows APIs, FileSystem, HTTP, Velopack      │
└────────────────────────────────────────────────┘
```

**Benefits:**
- Domain logic completely isolated (no infrastructure leaks)
- Multiple UI frontends possible (WPF, Avalonia, CLI, web)
- Easier to test (ports are mockable contracts)

**Effort:** High (6-12 months, incremental migration)

### 7.2 Plugin Ecosystem Maturity

**Current:** JSON descriptors, local discovery, built-in seeding

**Future:**
- Remote plugin catalog (GitHub registry, Artifactory)
- Plugin sandboxing (run in separate processes or containers)
- Plugin API versioning and compatibility checks
- Community plugin marketplace

**Foundation:** Proposal 5 (Plugin Lifecycle Management)

### 7.3 Observability and Telemetry

**Current:** Serilog file logging

**Future:**
- Structured telemetry (application events, user flows, error rates)
- Performance metrics (launch time, command latency, memory usage)
- Health checks (disk space, permission issues, runtime availability)
- OpenTelemetry integration (distributed tracing)

**Foundation:** Proposal 2 (Domain Events), Priority 2 (Logging Context)

---

## 8. Risks and Mitigations

### Risk 1: Coordinator Refactoring Breaks UI Flows

**Mitigation:**
- Refactor incrementally (one coordinator at a time)
- Maintain full test suite (1,221 tests are the safety net)
- Use smoke tests for UI-critical paths (launch, settings, tray)

### Risk 2: Configuration Changes Break Migrations

**Mitigation:**
- Keep external `AppConfig` contract unchanged during Phase 1-2
- Add round-trip tests for new domain models (follow existing pattern in `PersistenceCloneRoundTripTests`)
- Validate migrations against real user data (include test fixtures from old versions)

### Risk 3: Process Domain Refactoring Affects Stability

**Mitigation:**
- Introduce `IProcessOrchestrator` as a facade over existing implementations
- Run real-process integration tests (`WindowsInteractiveProcessRunnerTests`)
- Beta test with users running different terminal flavors (PowerShell, Git Bash, Windows Terminal)

---

## 9. Conclusion

CLIHub has a **solid architectural foundation** with clean layer boundaries, strong testing discipline, and excellent documentation. The main improvement areas are:

1. **Reduce UI-layer orchestration** by promoting workflows to Core services
2. **Decompose large service interfaces** into capability-based contracts
3. **Introduce domain events** to decouple cross-cutting concerns
4. **Extract cross-cutting infrastructure** (logging, persistence, caching)

The proposed roadmap is **incremental and low-risk**, building on existing strengths rather than requiring a rewrite. Each phase delivers value independently and maintains backward compatibility.

**Recommended Next Step:** Start with Priority 1 (document principles) and Priority 3 (first domain event POC) to validate the approach before committing to larger refactorings.

---

## Appendix: Key Metrics

| Metric | Value | Assessment |
|--------|-------|------------|
| Source files | 286 C# files (~922 KB) | Reasonable size |
| Test count | 1,221 (100% pass) | Excellent coverage |
| Build warnings | 0 | Clean |
| ADRs | 6 | Good decision tracking |
| Backlog entries | 35+ (14 resolved) | Strong debt management |
| GraphViz communities | 925 | High modularity |
| GitNexus symbols | 7,990 | Manageable complexity |
| Dependency depth | 3 layers | Clean architecture |
| Test-to-source ratio | 4.3 tests/file | Strong |
| Unresolved blockers | 0 | Production-ready |

**Overall Health Score: 8.5/10**

**Strengths:** Architecture, testing, documentation, debt tracking  
**Opportunities:** Service granularity, coordinator reduction, cross-cutting concerns
