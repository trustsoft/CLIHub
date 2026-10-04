## 1. Synchronization tests and helper

- [x] 1.1 Add focused tests for existing rows, additions, removals, property updates, ordering, and selected-plugin preservation; verify the tests fail against clear/add behavior or cover the helper contract.
- [x] 1.2 Implement a keyed agent-list synchronizer over plugin ID that updates existing `AgentItem` instances and applies membership/order changes; verify focused synchronizer tests pass.

## 2. Launch-window integration

- [x] 2.1 Replace `LaunchWindowViewModel.RefreshAgents` clear/add rebuilding with keyed synchronization while preserving the current composer policy and selection behavior; verify launch-window tests pass.
- [x] 2.2 Keep version population generation/cancellation aligned with synchronized row identity; verify stale probes do not update current rows and current probes do.
- [x] 2.3 Verify command completion, project changes, filter changes, and manual refresh preserve expected agent selection and row properties in a Debug run.

## 3. Verification and backlog

- [x] 3.1 Run focused agent-availability and view-model tests plus `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 3.2 Update `improvements.md` to close the list-refresh item and retain only verified follow-up work; verify the current file paths and status counts.
- [x] 3.3 Run `openspec validate "incremental-agent-list-refresh"` and archive the change after all tasks pass.
