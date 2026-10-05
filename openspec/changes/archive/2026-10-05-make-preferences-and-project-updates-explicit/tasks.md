## 1. Update Store Contracts

- [x] 1.1 Replace mutable store `Load/Save` pairs with explicit read and update operations for preferences and project state.
- [x] 1.2 Document detached callback values, successful-completion persistence, and exception behavior.

## 2. Migrate Store Implementations and Callers

- [x] 2.1 Implement detached explicit update transactions in `PreferencesStore` and `ProjectStateStore`.
- [x] 2.2 Migrate Core/application callers, test fakes, settings, release notes, agent services, project service, and legacy UI callers.
- [x] 2.3 Preserve unrelated configuration sections and existing `config.json` persistence behavior.

## 3. Verify Explicit Ownership

- [x] 3.1 Add tests for explicit updates, detached reads, callback failures, and unrelated-state preservation.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.4 Run `openspec validate make-preferences-and-project-updates-explicit` and verify the delta spec.
