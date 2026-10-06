## 1. Deterministic Loading

- [x] 1.1 Sort plugin directories with the documented stable comparer and verify valid plugins are loaded in that order.
- [x] 1.2 Preserve the first deterministic duplicate-ID winner, include winner and skipped directories in the warning, and verify the behavior with focused tests.

## 2. Verification

- [x] 2.1 Run focused plugin manager tests and the complete Core test project, verifying all tests pass.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release`, verifying the solution builds without warnings or errors.
- [x] 2.3 Run `dotnet test CLIHub.sln -c Release`, verifying Core and UI test projects pass.
- [x] 2.4 Run `openspec validate make-plugin-loading-deterministic`, verifying the delta spec is valid.
