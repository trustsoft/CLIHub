## Context

See `proposal.md`. The WPF layer has several `_ = SomeAsyncOperation()` call sites. `UpdateControlViewModel` and version population already demonstrate local handling patterns, but there is no shared boundary for unexpected exceptions and host status reporting.

## Goals / Non-Goals

**Goals:**

- Make fire-and-forget operations observable and exception-safe.
- Keep operation-specific success/failure behavior unchanged.
- Provide a consistent logging and user-status hook.
- Keep the boundary in the UI/application layer rather than adding UI concerns to Core services.

**Non-Goals:**

- Converting every command to `async void`.
- Changing `IAgentCommandService`, `IUpdateService`, or Core result contracts.
- Designing a general background-job scheduler.
- Hiding expected service-level failures that are already represented by result objects.

## Decisions

### 1. Use a small host-level runner

Create an application-layer helper that accepts an operation name, an async delegate, a logger, and an optional status callback. It awaits the delegate, catches unexpected exceptions, logs them with context, and reports a concise safe message when a callback is provided.

### 2. Keep expected results in the operation

The runner handles exceptions only. Operations continue to interpret `AgentCommandResult`, update results, and validation outcomes themselves. This prevents the boundary from duplicating product behavior.

### 3. Route only relevant fire-and-forget call sites

Start with launch-window agent commands, update-control operations, and settings/application actions that can escape through a synchronous command/event handler. Leave intentionally handled operations unchanged when they already have a complete try/catch boundary.

## Risks / Trade-offs

- **[Risk] Duplicate status messages for an operation that already catches exceptions.** → Do not wrap operations with an existing complete boundary unless the wrapper is the sole owner of exception reporting.
- **[Risk] A generic message hides useful context.** → Include the operation name in logs and use concise user-facing text while retaining exception details in logs.
- **[Risk] Runner becomes a dumping ground for workflow logic.** → Keep it limited to await, log, and optional status reporting.

## Migration Plan

1. Add runner tests for successful completion and exception capture.
2. Integrate the launch-window agent command call sites.
3. Integrate update-control and other applicable WPF async call sites.
4. Run full build/test and manually exercise launch, version, update, and settings actions.
5. Update `improvements.md` and archive the change.
