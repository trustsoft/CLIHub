## 1. Define Repository Contract

- [x] 1.1 Add `IConfigurationRepository` with detached `Read`, serialized `Update`, and synchronous `Flush`; verify its contract with focused interface-level tests.
- [x] 1.2 Move snapshot ownership and the single writer into `ConfigurationRepository`, remove `IConfigService`/`ConfigService`, and verify no old registrations or references remain.

## 2. Integrate Stores and Migration Lifecycle

- [x] 2.1 Migrate `PreferencesStore` and `ProjectStateStore` to repository transactions; verify concurrent updates to separate sections are both retained.
- [x] 2.2 Integrate schema validation and `ConfigMigrationRunner`; verify successful migration queues only the fully migrated current-version snapshot and failures leave the source untouched.
- [x] 2.3 Update Core DI, application callers, test fakes, and direct configuration tests; verify all consumers resolve/use the repository singleton.
- [x] 2.4 Preserve the single JSON document, debounce, atomic replacement, migration failure behavior, and Flush contract; verify legacy/current round-trips and pending-write durability.

## 3. Verify Repository Guarantees

- [x] 3.1 Add tests for concurrent updates, callback failure isolation, migration normalization/failure, debounce, atomic writes, and Flush; run the focused repository test fixture.
- [x] 3.2 Run focused Core configuration tests and the complete Core test project.
- [x] 3.3 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.4 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.5 Run `openspec validate introduce-configuration-repository` and verify the delta spec.
