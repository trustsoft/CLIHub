## 1. Extend Configuration Persistence Tests

- [x] 1.1 Review and refine the existing `ConfigServiceTests` helpers for unique paths, bounded polling, and deterministic cleanup.
- [x] 1.2 Add tests for concurrent/consecutive `Save` calls and latest-JSON persistence.
- [x] 1.3 Add tests for `Flush` during pending/active background writes.
- [x] 1.4 Add a worker-restart test for a save arriving while the worker is exiting.
- [x] 1.5 Add write-failure and recovery tests, including preservation of the last valid JSON after an atomic replacement failure.

## 2. Add Minimal Test Seam Only If Required

- [x] 2.1 Add the minimal internal `BeforeWorkerExitForTests` seam needed to deterministically cover the worker-exit race without changing public APIs.

## 3. Verify Preserved Behavior

- [x] 3.1 Run Core configuration tests and the complete Core test project.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.4 Run `openspec validate add-configuration-concurrency-tests` and verify the change remains explicitly spec-free.
