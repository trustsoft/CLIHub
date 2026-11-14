# 0019. Logging operation scopes

## Status

Accepted — 2026-10-29 (recorded 2026-11-14). Implements the accepted half of Proposal 1 of the
2026-10-29 architecture analysis. Implementing commit: `7d92bf3` "introduce ILoggingContextProvider
with operation scopes".

## Context

Proposal 1 proposed cross-cutting infrastructure services. Reconnaissance split it in two: the
persistence and cache halves had no shared shape and were rejected with reopen triggers in
ADR-0013 (`docs/adr/0013-no-generic-persistence-cache.md:34-41`); the logging half had a real,
repeated need. Several long-running operations — process launch, output capture, process
enumeration, project scanning, catalog load, startup work — emitted log lines that could not be
stitched back into one operation, because the logger had no ambient operation identity and the
project has no ambient static logger to attach one to.

The remaining forces:

- Log lines must stay structured, not prefixed strings: the sinks already consume
  `Microsoft.Extensions.Logging` state, and a `"[op=...] "` prefix is unsearchable in a structured
  sink and undiscoverable in code.
- The operation identity must be disposable and nesting-safe at the call site, not something the
  caller must remember to clear.
- It must not introduce an ambient flow: the project explicitly keeps services injected and avoids
  static state.

## Decision

**The contract is a disposable scope factory.**
`ILoggingContextProvider.BeginOperation(string operationName)` returns an `IDisposable` scope
(`src/CLIHub.Core/Infrastructure/Logging/ILoggingContextProvider.cs:6-17`). The implementation
validates the name, generates the correlation id, and opens a `BeginScope` scope whose dictionary
carries `OperationName` and `CorrelationId` as structured properties
(`src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs:22-34`). When the logger returns
no scope, a shared no-op disposable keeps call sites uniform (`:33`, `:36-40`). The provider is a DI
singleton (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:36`).

**Scopes open at operation entry points, not inside helpers.**
The scope is created once, at the boundary of the operation the user or the scheduler triggered:

- `ProcessOrchestrator.LaunchInteractiveAsync` (`src/CLIHub.Core/Processes/ProcessOrchestrator.cs:53`),
  `CaptureOutputAsync` (`:116`), and both `FindRunningAgentsAsync` overloads (`:187`, `:199`).
- `AgentProcessMonitor.GetRunningAgents`, both overloads, under the operation name
  `"EnumerateAgentProcesses"` (`src/CLIHub.Core/Agents/AgentProcessMonitor.cs:84`, `:123`).
- `ApplicationOperationLifetime.RunObservedAsync`, using the operation name it is given
  (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:103`).
- `ProjectScanningService.RunPassAsync`, one scope per scan pass
  (`src/CLIHub.Core/Scanning/ProjectScanningService.cs:406`).
- `PluginCatalog.ReloadPlugins` (`src/CLIHub.Core/Plugins/PluginCatalog.cs:73`) and
  `PluginCatalog.LoadPluginsCore`, which serves both the initial load and reload
  (`:91`; callers at `:65-68`, `:71-76`).
- The two domain-event subscribers, which open a scope named after the event so an audit line
  carries its own operation identity
  (`src/CLIHub.Core/Events/ProjectSelectionLoggingSubscriber.cs:42`,
  `src/CLIHub.Core/Events/AgentLaunchAuditSubscriber.cs:42`).

**No ambient logger, no per-subsystem wrapper.**
The provider is injected into the types that need it; nothing reads a static current-context. There
is no generic `ILogger` wrapper per subsystem and no generic persistence/cache abstraction — that
half was rejected in ADR-0013 (`docs/adr/0013-no-generic-persistence-cache.md:34-41`).

**Known limitation: ids are per scope.**
The correlation id is generated per scope from a random 8-hex value
(`src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs:42-46`). It is not inherited by
child scopes and does not cross into a domain event: the event bus generates its own id per publish
(`src/CLIHub.Core/Events/DomainEventBus.cs:33,120-124`). A launch that triggers a probe and then an
event therefore produces three unrelated ids.

Rejected alternatives:

- *An `AsyncLocal`-based ambient logger* — it would make the current operation reachable without
  injection, which is exactly the hidden global the project avoids, and it would silently do the
  wrong thing across thread-pool continuations.
- *A generic `ILogger` wrapper per subsystem* — a wrapper per subsystem multiplies types without
  adding structure; `Microsoft.Extensions.Logging` already carries the structured state.
- *Log-message prefixes instead of structured properties* — prefixes break structured search and
  put the formatting burden on every call site.

## Consequences

**Easier:** every log line inside an operation carries `OperationName` and `CorrelationId` without
call-site changes; an operation's lines can be filtered as one group; adding a new operation is one
`using var scope = ...` line at its entry point.

**Harder:** the scope must be opened at the true entry point or the grouping is wrong; a long-lived
scope around a loop silently stamps unrelated work with one id; and because the id does not flow,
joining a parent operation to its children and to any event it publishes is still manual.

**Committed to:** operation identity stays a structured scope; the contract stays a disposable
factory rather than ambient state.

## Reopen triggers

Correlation must span a parent operation and its children — for example launch → probe → event —
where a single id has to survive crossing a method boundary, a thread hop, or a publish. That needs
a flowing/ambient context (or explicit id passing through every contract), not a per-call scope
factory, and it would supersede this record. The same applies if the sinks need operation duration
or nesting depth, which a bare `BeginScope` dictionary cannot express.
