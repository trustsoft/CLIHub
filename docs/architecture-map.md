# Architecture map: OpenSpec capability → Scryer model node

This is the bridge between the two governed views of the project:

- **OpenSpec capabilities** (`openspec/specs/<capability>/spec.md`) — *what* behavior exists.
- **Scryer model** (`.scryer/model.scry`, over MCP) — *where* that behavior lives, which
  claims own it, and which tests back it.

Use it to answer "which model node does this capability belong to?" before planning or
editing. Node ids and breadcrumbs match `.scryer/model.scry` at the time of writing; the
model is authoritative — when in doubt, re-read it with `orient`/`read_model`.

| Capability | Model node (breadcrumb — id) | Code | Backing tests |
| --- | --- | --- | --- |
| `agent-detection` | Agent Runtime / AgentDetector — `node-6fc644` | `src/CLIHub.Core/Services/AgentDetector.cs` | `AgentDetectorTests.cs` |
| `agent-launch` | Agent Runtime / LauncherCore — `node-m30anq`; RuntimeResolver — `node-7xpmzf`; CommandBuilder — `node-crxzv9` | `src/CLIHub.Core/Services/{LauncherCore,RuntimeResolver,CommandBuilder}.cs` | `LauncherCoreTests.cs`, `RuntimeResolverTests.cs`, `CommandBuilderTests.cs` |
| `agent-plugins` | Agent Runtime / PluginLoader — `node-vaf1g8` | `src/CLIHub.Core/Services/PluginLoader.cs` | `PluginLoaderTests.cs` |
| `app-config` | Configuration / JsonDocumentStore — `node-q1353h` | `src/CLIHub.Core/Services/JsonDocumentStore.cs` | `JsonDocumentStoreTests.cs` |
| `app-settings` | Configuration / SettingsStore — `node-08e31p` | `src/CLIHub.Core/Services/SettingsStore.cs` | `SettingsStoreTests.cs` |
| `app-update` | Application Updates / UpdateService — `node-82j637`; Platform Adapters / VelopackUpdateClient — `node-ykkm7c` | `src/CLIHub.Core/Services/UpdateService.cs`, `src/CLIHub.App/Platform/VelopackUpdateClient.cs` | `UpdateServiceTests.cs` |
| `global-hotkey` | Windows Integration / HotkeyManager — `node-74mzwk`; Application Composition / App — `node-x9pp0t` | `src/CLIHub.App/Interop/HotkeyManager.cs`, `src/CLIHub.App/App.xaml.cs` | manual (UI/interop) |
| `logo-resolution` | Project Management / LogoResolver — `node-zttn7c`; Platform Adapters / LogoImageService — `node-8dhbk2` | `src/CLIHub.Core/Services/LogoResolver.cs`, `src/CLIHub.App/Platform/LogoImageService.cs` | `LogoResolverTests.cs` |
| `popup` | Popup UI / PopupWindow — `node-c5b27e`, PopupViewModel — `node-15efmk`, AgentItemViewModel — `node-karbe5`, ProjectItemViewModel — `node-mvd4fd`; Windows Integration / PopupPositioner — `node-5jcnqa` | `src/CLIHub.App/Views/**`, `src/CLIHub.App/ViewModels/**`, `src/CLIHub.App/Interop/PopupPositioner.cs` | manual (UI/interop) |
| `project-registry` | Project Management / ProjectRegistry — `node-tpnrqf` | `src/CLIHub.Core/Services/ProjectRegistry.cs` | `ProjectRegistryTests.cs` |

## Container boundaries

The model carries directory/file globs per node (`boundaries` in the source map). A change
to a file that falls outside every boundary is a signal that the work is **outside the
modeled architecture** — stop and plan the node/claim first, or get a decision.

- `CLIHub / Core` — `src/CLIHub.Core/**`
  - Agent Runtime — the five `Services/*.cs` above
  - Project Management — `ProjectRegistry.cs`, `LogoResolver.cs`
  - Configuration — `JsonDocumentStore.cs`, `SettingsStore.cs`
  - Application Updates — `UpdateService.cs`
- `CLIHub / Desktop App` — `src/CLIHub.App/**`
  - Application Composition — `App.xaml*`; Popup UI — `ViewModels/**`, `Views/**`; Windows Integration — `Interop/**`; Platform Adapters — `Platform/**`
- `CLIHub / Core Tests` — `tests/CLIHub.Core.Tests/**`
  - Core Service Tests, Persistence Tests, Resolver Tests (per test file)

## Known gaps (not yet modeled)

- `PhysicalFileSystem` (`src/CLIHub.Core/Abstractions/PhysicalFileSystem.cs`) and its test
  `PhysicalFileSystemTests.cs` have no node — an unmapped abstraction.
- `HotkeyParser.cs` (`src/CLIHub.App/Interop/`) has no node.
- `src/CLIHub.Core/Models/**` (data shapes) and `CoreJson.cs` are owned by the `Core`
  container but not decomposed — data shapes would be schema nodes (`properties`), none
  modeled yet.
- Desktop App UI and interop are verified manually (see `AGENTS.md`), so their claims stay
  untested by design.

## Using it

```
scryer orient { task, files }     # governing nodes, claims, directives, phase
scryer locate { file, symbol }    # reverse lookup for one file
scryer get_health                 # coverage, untested claims, silent anchors
```

Keep this table in sync when a capability's owning node changes — the model is the source
of truth, this file is the index.
