## Context

See `proposal.md` for motivation and scope. The current implementation has already separated process execution contracts and moved Core into subsystem folders. The remaining runtime risks are concentrated in four places:

- `AgentVersionService` has a result cache but no in-flight coalescing.
- Launch-window version population is fire-and-forget and has no generation or cancellation boundary.
- `ProcessLauncher` builds shell command strings manually from raw plugin command fields.
- `UpdateService` races an update task against a delay and returns on timeout while the update task may still complete later.

Existing plugin descriptors store `executable` and a raw `arguments` string. The persisted plugin format is part of the compatibility surface and will remain unchanged.

## Goals / Non-Goals

**Goals:**

- Ensure concurrent requests for one agent share one version probe.
- Ensure an obsolete UI population pass cannot mutate current agent items.
- Make runtime-specific command construction deterministic and covered by tests.
- Ensure update-check timeout and cancellation paths observe all task failures.
- Preserve existing public result types, plugin descriptors, runtime choices, and user-facing workflows.

**Non-Goals:**

- Introducing a new plugin command JSON schema.
- Adding progress reporting for version probes or update checks.
- Replacing Velopack or changing the update source.
- Generalizing process execution to non-Windows platforms.
- Solving every UI collection allocation issue in this change; collection batching may remain a follow-up optimization.

## Decisions

### 1. Coalesce version probes with an in-flight task map

Keep the completed-result TTL cache and add a per-agent in-flight task map. The first request creates the task; concurrent requests reuse it. The task is removed when it completes, while the completed value remains governed by the existing TTL cache.

The caller cancellation token cancels its wait using `WaitAsync` and does not cancel the shared probe for other callers. This prevents one obsolete UI population pass from cancelling a probe still needed by another consumer.

**Alternative rejected:** a global lock around the probe. It prevents duplicate work but serializes unrelated agents and increases UI latency.

### 2. Use a population generation plus cancellation token in the launch view model

Each refresh cancels the previous population scope and increments a generation number. The population captures the generation and only applies a version result when the token is current and the item still belongs to that generation. Exceptions are handled at the population boundary and reported through logging/status handling rather than becoming unobserved task failures.

The legacy `MainWindow` remains outside the normal composition path, but its equivalent population path receives the same defensive treatment where it remains compiled and testable.

**Alternative rejected:** checking only whether an item is still in the collection. An item can remain present while a newer refresh has changed its intended version request, so generation identity is required.

### 3. Centralize Windows command construction in a tested builder

Introduce an internal command-line builder responsible for:

- quoting executable paths and working directories;
- constructing the Windows Terminal, Command Prompt, and PowerShell launch forms;
- constructing the captured `cmd.exe /c` form used for `.cmd`/`.bat`/shim resolution;
- escaping or quoting command tokens consistently.

The raw `arguments` field remains compatible with existing descriptors. The builder treats it as the descriptor's argument tail and applies the existing command semantics while protecting executable boundaries and shell metacharacters. Tests cover paths, quotes, spaces, and shell characters for each runtime.

PowerShell commands use an encoded command payload where the runtime requires a compound command string. This avoids trying to apply cmd escaping rules to PowerShell syntax.

**Alternative rejected:** switching all execution to `ProcessStartInfo.ArgumentList` immediately. It is safer for tokenized arguments, but the existing descriptor stores a raw argument string and interactive shell runtimes still require deliberate shell command construction. The builder provides a compatibility layer first.

### 4. Observe update checks after timeout

Keep the current non-blocking timeout behavior, but attach an explicit continuation/observation path to the underlying Velopack task before returning a timeout result. If the API supports cancellation in the current dependency version, pass a linked token; otherwise the task is allowed to finish in the background but every terminal exception is observed and logged.

The service always clears the available-version state on timeout/cancellation and remains ready for a later check.

**Alternative rejected:** awaiting the original task after timeout. That would make the timeout ineffective when the network operation does not honor cancellation.

### 5. Make compatibility constructors non-public

The new process-output contract is the production constructor for `AgentVersionService`. The old `IProcessLauncher` constructor remains only as an internal test/migration seam, avoiding ambiguous public DI construction while preserving existing Core tests during the change.

## Risks / Trade-offs

- **[Risk] Shared probes continue running after a caller is cancelled.** → The shared task is bounded by the existing process timeout, and its completion is cached; caller cancellation only stops the obsolete wait.
- **[Risk] Raw descriptor arguments cannot express a fully typed argument vector.** → Preserve the descriptor format, centralize construction, add regression cases, and treat a typed command schema as a future breaking change.
- **[Risk] Shell quoting differs between Windows runtimes.** → Keep runtime-specific builders and test each generated command form independently.
- **[Risk] Velopack may not cancel an in-flight network task.** → Observe the task explicitly and log late failures; return control to the caller at the configured timeout.
- **[Risk] Fire-and-forget UI handlers still exist outside version population.** → Cover the targeted population path here and keep broader fire-and-forget cleanup as a follow-up if it is not required by these scenarios.

## Migration Plan

1. Add regression tests that reproduce duplicate version probes, stale population updates, command quoting failures, and late update-check failures.
2. Add in-flight probe coalescing and internalize the legacy constructor.
3. Add generation/cancellation handling to launch-window version population and the retained legacy window path.
4. Implement the runtime command builder and route interactive/captured process paths through it.
5. Fix update-check timeout observation and state reset.
6. Run the full solution build and test suite, then validate the OpenSpec change.
7. Review the generated command cases and update the durable improvement backlog with any explicitly deferred work.

Rollback is by reverting each focused commit. No persisted data or plugin descriptor migration is required.
