## 1. Define Snapshot Contract

- [x] 1.1 Add `ConfigurationSnapshot` and defensive-copy/conversion helpers for projects, preferences, and current project state.
- [x] 1.2 Update `IConfigService` to expose mutation-isolated reads and explicit snapshot writes while documenting ownership semantics.

## 2. Migrate Configuration Runtime

- [x] 2.1 Update `ConfigService` to own its internal state, return detached snapshots, and preserve existing JSON/debounce/atomic-write/flush behavior.
- [x] 2.2 Migrate `PreferencesStore`, `ProjectStateStore`, all Core callers, and test fakes to the snapshot contract.
- [x] 2.3 Preserve the existing `AppConfigDocument` JSON shape and add compatibility coverage for current config files.

## 3. Verify Snapshot Boundaries

- [x] 3.1 Add tests proving returned snapshots and nested values cannot mutate service-owned state implicitly.
- [x] 3.2 Add tests for explicit updates, unrelated-state preservation, and store round-trips.
- [x] 3.3 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.4 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.5 Run `openspec validate introduce-configuration-snapshot` and verify the delta spec is valid.
