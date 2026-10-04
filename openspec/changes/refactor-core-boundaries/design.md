## Context

See `proposal.md` for the motivation and scope. The current Core project is a single `net8.0` library without WPF references, but its `Services/` directory combines application behavior, persistence, process execution, Windows integration, update delivery, and asset loading.

The current dependency graph is mostly acyclic and shallow. The main hidden coupling comes from the mutable `AppConfig` returned by `IConfigService`, the broad responsibility of `ProjectService`, and the use of `IProcessLauncher` for both interactive launches and captured command execution.

The application and test projects already depend on Core through interfaces and DI. The migration therefore needs to preserve runtime behavior and keep public contracts stable unless an explicit decision records a breaking change.

## Goals / Non-Goals

**Goals:**

- Make subsystem ownership visible from the directory and namespace structure.
- Establish dependency rules that can be applied during review and verified through build and tests.
- Keep domain and application policies independent from replaceable infrastructure where practical.
- Reduce the number of Core classes that need to understand the complete `AppConfig` shape.
- Preserve behavior, persistence format, plugin descriptor compatibility, and existing UI workflows.
- Provide a phased migration that can be stopped after any safe boundary.

**Non-Goals:**

- Splitting Core into multiple assemblies during the first migration.
- Changing user-facing functionality, plugin JSON semantics, configuration JSON schema, or update behavior.
- Replacing the dependency injection library, logging stack, Velopack, or the process runtime strategy.
- Introducing a general-purpose domain-driven design framework or a new abstraction for every method.
- Removing Windows-specific functionality from the application; it will be isolated and named accurately.

## Decisions

### 1. Use subsystem-oriented folders inside `CLIHub.Core` first

The first structural target is one project with explicit folders and namespaces:

```text
Agents/          agent commands, detection, versions, availability policies
Projects/        project lifecycle and project-specific policies
Plugins/         plugin descriptors, catalog loading, and seeding
Configuration/   configuration models and persistence
Updates/         update and release-note behavior
Infrastructure/  processes, Windows integration, filesystem persistence
Contracts/       cross-subsystem interfaces where a shared location improves discovery
Composition/     Core DI registration
Models/          stable data contracts and value types
Hotkeys/         hotkey parsing models and policies
Logging/         logging setup and preference parsing
Formatting/      display-only formatting helpers
```

The names are an organizational map, not a requirement to move every interface into `Contracts/`. An interface stays next to its subsystem when that makes ownership clearer. A move must not be performed solely for symmetry.

**Alternative considered:** split immediately into `CLIHub.Domain`, `CLIHub.Application`, and `CLIHub.Infrastructure.Windows`. This would provide stronger compile-time boundaries, but it would increase project and DI churn before the current responsibilities are understood. The multi-project split remains a later option if the folder-level rules prove insufficient.

### 2. Treat Core as UI-independent but Windows-aware

Core must not reference WPF or UI types. It may contain Windows application infrastructure because CLIHub targets Windows and several capabilities require Registry, named mutexes, named pipes, Windows Terminal, and Velopack. Such code belongs under `Infrastructure/` and is exposed through interfaces when a policy or application service needs to call it.

Documentation should describe this accurately as “UI-independent application core with Windows infrastructure,” rather than “platform-agnostic business logic.”

**Alternative considered:** move all Windows code into the WPF project. This would make Core smaller but would move testable application behavior into the UI host and make reuse and testing harder.

### 3. Keep subsystem services as application-facing facades

Existing public services such as `IProjectService`, `IAgentCommandService`, `IAgentDetectionService`, and `IPluginManager` remain the stable entry points during migration. Internal helpers may be extracted behind them:

- project path normalization and logo resolution from `ProjectService`;
- plugin catalog and seeding concerns from agent behavior;
- process construction/capture mechanics from agent command policy.

The facade owns the use-case boundary and coordinates narrower policies. UI code should not need to know the extracted implementation types.

**Alternative considered:** expose every extracted class publicly immediately. This would increase API surface and make temporary migration structure permanent.

### 4. Introduce narrower configuration access incrementally

`AppConfig` is split conceptually into a persistence document and owned state objects. The migration will preserve the current `config.json` shape and the single atomic persistence path.

The target internal model is:

```text
AppConfigDocument
    persistence representation of the existing config.json shape

ProjectState
    Projects and CurrentProjectId

AppPreferences
    application preferences, initially retaining the current flat JSON fields
```

`AppConfigDocument` owns serialization compatibility. `ProjectState` owns project state and its mutations. `AppPreferences` owns preference values. `IConfigService` or an internal document store remains responsible for loading, serialization, and atomic writes; those responsibilities must not be duplicated in subsystem services.

The first migration does not introduce multiple physical configuration files, nested preference JSON, a database, or a new schema version. It first adds compatibility coverage, then introduces narrower access contracts or snapshots where they remove real coupling.

Initial ownership:

```text
Projects subsystem     -> Projects, CurrentProjectId
Launch preferences     -> DefaultRuntime, path display, hotkey, window behavior
Agent probing          -> AgentProbeTtlMinutes, AgentProbeTimeoutSeconds
Updates                -> CheckForUpdatesOnStartup, release-note marker
Logging                -> LogLevel
```

No subsystem may add unrelated mutations to `AppConfig` merely because it can access the object. A new configuration section must identify its owning subsystem and persistence path.

**Alternative considered:** replace `AppConfig` with a database or event-sourced state immediately. That is outside the refactor's needs and would change persistence behavior without a product requirement.

**Alternative considered:** split the configuration into several physical files immediately. A single document keeps related state atomic, preserves the current backup and recovery model, and avoids partial updates. Separate files may be reconsidered only when independent lifecycle, size, or access requirements justify them.

### 5. Separate process policies before replacing process implementations

Interactive launch and captured command execution are distinct use cases. The first step is to name and test their responsibilities separately, while preserving the existing `IProcessLauncher` compatibility seam. A later step may introduce narrower interfaces such as an interactive runner and an output runner if the extracted policies require independent replacement.

This avoids a speculative abstraction while making the current `AgentCommandService` branching explicit.

### 6. Group DI registration by subsystem

`AddClIHubCoreServices` remains the public extension method. Its implementation delegates to private or public subsystem registration methods, for example `AddConfigurationServices`, `AddProjectServices`, `AddPluginServices`, and `AddAgentServices`.

Each registration group must make its dependencies visible and must not resolve UI services. Composition is the only place where concrete Core implementations are selected by default.

### 7. Use an explicit deviation protocol

During implementation, a change is considered aligned when it satisfies all of these rules:

- no UI dependency enters Core;
- no new dependency points from a lower-level infrastructure component to an application facade;
- cross-subsystem calls use an existing contract or a deliberately named new contract;
- persisted formats and observable behavior remain compatible;
- each extracted responsibility has focused tests or an explicit reason why existing tests cover it.

If a task cannot satisfy a rule, implementation pauses at that boundary and records an entry in `deviations.md` containing: the task, the proposed deviation, the reason, alternatives considered, impact, and whether the deviation is temporary. A deviation is not silently folded into the code or task list.

## Risks / Trade-offs

- **[Risk] Namespace and file moves create broad mechanical diffs.** → Move one subsystem at a time, build after each move, and avoid behavior changes in the same commit where possible.
- **[Risk] Narrow configuration APIs duplicate persistence logic.** → Keep serialization and atomic writes inside `ConfigService`; narrower APIs express ownership and delegate persistence.
- **[Risk] Infrastructure folders become a second catch-all.** → Every new infrastructure type must name the external boundary it owns, such as `Processes`, `Windows`, or `Persistence`.
- **[Risk] Compatibility facades preserve too much coupling.** → Extract only after tests identify the responsibility and add a follow-up task when a facade remains broader than intended.
- **[Risk] The refactor drifts into product work.** → Reject behavior changes from this change unless they are required to preserve behavior after extraction; record product ideas separately.
- **[Risk] Existing OpenSpec changes are in progress.** → Avoid overlapping files with `launch-window-update-button` and verify the working tree before each implementation wave.

## Migration Plan

1. Complete and verify `centralize-app-data-paths`, which is already marked complete.
2. Complete the remaining behavior verification for `launch-window-update-button`, especially before changing configuration access used by update workflows. This change should be treated as a sequencing prerequisite for broad Core moves, but it does not need to be merged into this change.
3. Capture the current dependency map, configuration ownership map, config JSON compatibility baseline, and build/test result.
4. Introduce `AppConfigDocument` and `ProjectState` ownership while preserving the existing `config.json` shape and single atomic write path.
5. Add narrow configuration access contracts and verify that project state mutations remain owned by the project subsystem.
6. Create the subsystem folders and move files with namespace updates only; preserve behavior.
7. Extract project path and logo policies while retaining `IProjectService`.
8. Separate plugin catalog concerns from agent policies and availability composition.
9. Clarify process execution policies and add narrower seams only where tests or replacement needs justify them.
10. Group DI registration and add composition tests for the subsystem registrations.
11. Update architecture documentation and add the final dependency/governance checklist.
12. Run the full solution build and tests after each wave and perform a final review against this design.

Rollback is by reverting the completed migration commit or wave. Because persisted formats and public behavior are preserved, rollback does not require a data migration. If a task changes a public contract despite this design, it requires a separate recorded decision before proceeding.

## Governance Artifacts

This change uses the following documents as control points:

- `proposal.md` — why the refactor exists and what it must change.
- `design.md` — the target boundaries, decisions, risks, and migration rules.
- `tasks.md` — the ordered implementation checklist and verification evidence.
- `deviations.md` — the explicit record for any approved departure from this design.

The implementation should keep `deviations.md` empty unless a real deviation occurs. After completion, the durable boundary rules belong in `docs/architecture.md`; the OpenSpec change remains the historical record of the migration.

## OpenSpec Sequencing

The related changes are intentionally kept separate:

```text
centralize-app-data-paths (complete)
        ↓
launch-window-update-button (finish remaining verification)
        ↓
refactor-core-boundaries (this change)
```

`centralize-app-data-paths` provides the stable path boundary used by Core persistence and infrastructure. `launch-window-update-button` establishes the current update-control behavior before the Core configuration access is narrowed. This change consumes those completed baselines but does not alter their requirements or implementation plans.
