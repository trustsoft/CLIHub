## Why

Several Core services retain internal compatibility constructors alongside their production constructors. This has already caused a real startup failure: Microsoft DI found two equally applicable `AgentCommandService` constructors and terminated the application before the tray was created.

The service construction contract should be explicit so production DI has exactly one constructor per service, while tests retain deliberate seams through named factories or adapters.

## What Changes

- Remove compatibility constructor overloads from `AgentCommandService`, `AgentVersionService`, `AgentDetectionService`, and `ProjectService`.
- Replace test-only constructor seams in `UpdateService` and `ReleaseNotesService` with named internal test factories.
- Update tests to construct narrow stores and process contracts explicitly.
- Add composition coverage that resolves every Core service used by the application, including agent command, detection, and version services.
- Define and document the convention that production Core services expose one public DI constructor.
- Preserve all production behavior, persisted data, plugin descriptors, and public service interfaces.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-lifecycle`: The startup DI container SHALL resolve all registered Core services without constructor ambiguity and the application SHALL continue startup normally.

## Impact

- Affects Core service constructors and Core unit-test setup.
- Affects `ServiceCollectionExtensionsTests` and composition behavior at application startup.
- Does not add runtime dependencies or change user-facing workflows.
- Test-only construction remains available through explicit internal factories/adapters rather than constructor overloads.
