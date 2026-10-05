## Context

See `proposal.md` for the motivation and scope. `CLIHub.Core` already has subsystem-oriented folders, but the namespace declarations have not followed the folder migration. The current broad namespaces are used by implementations, public contracts, internal helpers, the WPF host, and tests, so a namespace change must be treated as a coordinated source migration.

The existing public composition entry point remains `CLIHub.Core.AddClIHubCoreServices`. Models and utility namespace families already communicate stable cross-cutting ownership and are outside this migration unless a compile-time conflict requires otherwise.

## Goals / Non-Goals

**Goals:**

- Make each Core type discoverable through the namespace of the subsystem that owns it.
- Keep interfaces next to their owning subsystem rather than preserving a second global contract bucket.
- Align the two legacy files still under `Core/Services/` with their physical subsystem folders.
- Preserve runtime behavior, public members, dependency lifetimes, and serialized formats.
- Make the source-breaking namespace change explicit and mechanically verifiable.

**Non-Goals:**

- Splitting `CLIHub.Core` into multiple assemblies.
- Changing service APIs, constructor shapes, DI lifetimes, persistence, plugin descriptors, or UI behavior.
- Introducing namespace compatibility aliases or duplicate forwarding types; those would preserve the old ownership ambiguity.
- Moving `Models/`, `Hotkeys/`, `Logging/`, `Formatting/`, `SeedPlugins/`, or `Composition/` into new namespace families without a concrete ownership reason.

## Decisions

### 1. Use subsystem namespaces as the source of truth

The target namespace map is:

| Current area | Target namespace | Types |
| --- | --- | --- |
| Configuration | `CLIHub.Core.Configuration` | `ConfigService`, `IConfigService`, `PreferencesStore`, `IPreferencesStore`, `CoreJson` |
| Projects | `CLIHub.Core.Projects` | `ProjectService`, `IProjectService`, `ProjectStateStore`, `IProjectStateStore`, `ProjectPathPolicy`, `ProjectLogoResolver` |
| Plugins | `CLIHub.Core.Plugins` | `PluginManager`, `IPluginManager`, `PluginSeeder`, `IPluginSeeder` |
| Agents | `CLIHub.Core.Agents` | `AgentCommandService`, `IAgentCommandService`, `AgentDetectionService`, `IAgentDetectionService`, `AgentVersionService`, `IAgentVersionService`, `AgentListComposer` |
| Updates | `CLIHub.Core.Updates` | `UpdateService`, `IUpdateService`, `ReleaseNotesService`, `IReleaseNotesService`, `UpdateControlLogic` |
| Process infrastructure | `CLIHub.Core.Infrastructure.Processes` | `ProcessLauncher`, `IProcessLauncher`, `IInteractiveProcessRunner`, `IProcessOutputRunner`, `WindowsCommandLineBuilder` |
| Persistence infrastructure | `CLIHub.Core.Infrastructure.Persistence` | `LogoCacheService`, `ILogoCacheService` |
| File-system infrastructure | `CLIHub.Core.Infrastructure.FileSystem` | `AppPaths`, `DirectoryInitializer` |
| Windows infrastructure | `CLIHub.Core.Infrastructure.Windows` | `StartupService`, `IStartupService`, `StartupRegistry`, `IStartupRegistry`, `SingleInstanceGuard` |

The existing `CLIHub.Core` composition namespace remains unchanged. Existing `Models`, `Hotkeys`, `Logging`, and `Formatting` namespaces also remain unchanged.

### 2. Colocate public contracts with their owners

Interfaces move to the namespace of the implementation or subsystem they describe. This removes `CLIHub.Core.Interfaces` as a public catch-all and makes dependency direction visible in imports. Cross-subsystem consumers reference the owning namespace explicitly; no new `Contracts` namespace is introduced.

### 3. Align legacy file locations during the migration

`UpdateControlLogic.cs` moves from `Core/Services/` to `Core/Updates/`, and `StartupRegistry.cs` moves from `Core/Services/` to `Core/Infrastructure/Windows/`. Other files are already located under their target subsystem folders. The file moves are organizational only and carry no logic changes.

### 4. Migrate in dependency-aware waves

The implementation updates one subsystem group at a time, starting with leaf infrastructure and configuration, then domain/application subsystems, and finally composition, host, tests, and documentation. Each wave must compile before the next wave begins. Mechanical namespace updates are preferred over opportunistic refactoring.

### 5. Accept the source-level breaking change without compatibility aliases

The old namespaces are public source names, so external consumers must update imports. Runtime and binary compatibility with the old names is not a goal for this internal application repository. Keeping aliases would leave two valid ownership paths and undermine the purpose of the migration.

## Risks / Trade-offs

- **[Risk] Broad compile break after a namespace declaration changes.** → Update consumers in the same wave and build after every subsystem group.
- **[Risk] A type is assigned to the wrong subsystem namespace.** → Use the target map above, verify each moved file against its folder, and review the final namespace inventory.
- **[Risk] Hidden global using or fully qualified references retain the old namespaces.** → Search the full repository for `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` after migration; allow only historical documentation where explicitly intended.
- **[Risk] Mechanical edits accidentally change behavior.** → Keep the implementation diff limited to namespace declarations, using directives, file moves, and documentation; run the existing full test suite.
- **[Risk] Archived historical artifacts become misleading if rewritten.** → Update current architecture and repository documentation only; leave archived change records as historical snapshots.

## Migration Plan

1. Capture the current namespace inventory and confirm the target map against the source tree and project references.
2. Migrate `Infrastructure.FileSystem`, `Infrastructure.Persistence`, and `Infrastructure.Processes`; update their consumers and run a Core/application build.
3. Migrate `Configuration` and `Projects`; verify configuration serialization and project-state tests.
4. Migrate `Plugins` and `Agents`; verify plugin, detection, command, availability, and version tests.
5. Migrate `Updates` and `Infrastructure.Windows`, moving the two legacy files into their owning folders; verify update, release-notes, startup, and single-instance tests.
6. Update `Composition`, the WPF host, and all tests to use owner namespaces; verify DI composition resolves every required service.
7. Search for obsolete namespace references, update current documentation, and inspect the final file/namespace map.
8. Run `dotnet build CLIHub.sln -c Release`, `dotnet test`, `openspec validate --specs`, and the change validation before implementation is considered complete.

Rollback is a source revert of the migration commit or wave. No persisted-data or deployment migration is required because namespaces do not affect serialized data or runtime storage.
