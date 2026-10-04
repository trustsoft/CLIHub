## Why

The Core boundary refactor is complete, but runtime review found reliability gaps in the code behind those boundaries. Version probes can stampede into duplicate processes, stale asynchronous results can update obsolete UI items, shell command construction is fragile for quoting and special characters, and timed-out update checks can leave background tasks running without observation.

These issues affect responsiveness, command correctness, and failure handling in ordinary Windows usage. They should be fixed as a focused runtime-stability change before adding more Core functionality.

## What Changes

- Prevent duplicate in-flight version probes for the same agent.
- Cancel or supersede stale version-population runs when the agent list is refreshed.
- Remove the ambiguous public compatibility constructor from `AgentVersionService` while preserving the intended process-output contract.
- Make interactive Windows Terminal, Command Prompt, and PowerShell command construction correct for paths, arguments, and embedded special characters.
- Make captured command execution use a safe, tested command-line construction path for executable shims and arguments.
- Ensure update-check timeouts observe and terminate the check lifecycle without leaving unobserved failures.
- Add focused regression tests for concurrency, cancellation, quoting, special characters, timeout, and failure paths.
- Keep the existing plugin descriptor format and public user workflows compatible.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-version`: Version lookup and caching must coalesce concurrent probes and supersede stale presentation requests while retaining TTL and timeout behavior.
- `agent-commands`: Interactive and captured command execution must preserve argument boundaries and work with Windows paths, shell shims, and supported runtimes.
- `update-checking`: A timed-out update check must cancel or observe its underlying operation and leave the service in a stable failed state.

## Impact

- Affects `CLIHub.Core` process execution, agent version lookup, update checking, and their interfaces only where required by the existing behavior contracts.
- Affects WPF launch-window version population through cancellation and generation handling.
- Adds and updates unit tests under `tests/CLIHub.Tests`.
- Does not change plugin JSON, persisted configuration, update endpoints, or the visible update/agent workflows except to make their failure and concurrency behavior reliable.
- Requires an implementation change and a full solution build/test verification before archival.
