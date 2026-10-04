## Context

See `proposal.md` for motivation. The current Core runtime has already encountered a DI startup failure because `AgentCommandService` exposes both a legacy aggregate-process constructor and a new split-process constructor. Similar internal compatibility constructors remain in `AgentVersionService`, `AgentDetectionService`, and `ProjectService`; test-only manager/text injection constructors remain in update and release-note services.

The project uses `InternalsVisibleTo` for `CLIHub.Tests`, so test seams can remain internal without being public DI candidates.

## Goals / Non-Goals

**Goals:**

- Give each production Core service exactly one public constructor intended for DI.
- Keep test isolation and external dependency injection through explicit named factories or narrow test adapters.
- Preserve the existing public service interfaces and runtime behavior.
- Add a composition regression test that exercises the same resolution path used by application startup.

**Non-Goals:**

- Changing service lifetimes or the DI library.
- Removing useful optional path parameters from single constructors when they do not create ambiguity.
- Refactoring all test fakes into a new testing framework.
- Changing plugin, configuration, update, or release-note behavior.

## Decisions

### 1. One public production constructor per Core service

The constructor used by application DI remains public and uses the narrowest production contracts:

- `AgentCommandService` uses `IInteractiveProcessRunner` and `IProcessOutputRunner`.
- `AgentVersionService` uses `IProcessOutputRunner` and `IPreferencesStore`.
- `AgentDetectionService` uses `IPreferencesStore`.
- `ProjectService` uses `IProjectStateStore` and `ILogoCacheService`.

Legacy constructors accepting aggregate or persistence contracts are removed rather than left internal. This prevents future test seams from becoming accidental service-construction alternatives.

### 2. Use named internal factories for external test dependencies

`UpdateService` and `ReleaseNotesService` need injected manager/text seams that are not production dependencies. Replace their overloads with explicitly named internal factories such as `CreateForTesting(...)`. The factory creates the same production type while making the non-production path visible at every call site.

**Alternative rejected:** keep optional constructor parameters. They hide test setup in the constructor surface and can make future overloads ambiguous.

### 3. Adapt tests explicitly at the boundary

Tests that currently pass `FakeConfigService` directly to service constructors will construct `PreferencesStore` or `ProjectStateStore` explicitly. Tests that use `FakeProcessLauncher` can pass the fake to both narrow process interfaces when a service requires both.

This keeps the production design honest: tests exercise the same contracts selected by DI.

### 4. Verify the full composition graph

The composition test will resolve every Core service used during startup, including `IAgentCommandService`, `IAgentDetectionService`, and `IAgentVersionService`. A build alone is insufficient because DI constructor selection occurs at runtime.

## Risks / Trade-offs

- **[Risk] Tests require more setup after constructor cleanup.** → Use explicit store construction and named factories; this makes each test dependency visible.
- **[Risk] A future service adds another public constructor.** → Keep the composition test broad and document the one-public-production-constructor rule in the architecture guidance.
- **[Risk] An internal factory is accidentally used in production.** → Keep factories internal and use production registration only with the public constructor.

## Migration Plan

1. Remove compatibility constructors and update direct Core tests to use production contracts.
2. Replace test-only constructors with named internal factories and update their tests.
3. Expand the DI composition test to resolve all Core service interfaces.
4. Run a Debug build, Release test suite, and direct startup smoke test.
5. Update architecture documentation and archive the change after validation.

Rollback is a source revert; no persisted data or external contract migration is needed.
