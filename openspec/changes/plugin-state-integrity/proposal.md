# Plugin state integrity: visible write failures, install warnings, and accepted modification overwrite

## Why

Two linked gaps make plugin state changes silently unreliable:

1. **Registry write failures are invisible** (`docs/backlog.md:122-134`). `IPluginRegistry.Save` returns `void` and swallows failures with a log line (`PluginRegistryRepository.cs:97-105`). Disable and enable save-then-reload the catalog — the reload reads the old persisted state back, so a failed write leaves the previous state in effect while the caller's `catch` cannot fire. The user sees only a log entry.

2. **Local modification overwritten on update is accepted behavior, but unstated** (`docs/backlog.md:108-120`). The code already replaces the descriptor and rewrites the stored hash on every install/update, but this is documented nowhere, and the user-modified collection has no contract stating that its members are cleared by the next install.

## What Changes

### Part A — Make write failures visible

- **A1 — Contract.** Replace `void Save(PluginRegistryDocument)` with `bool TrySave(PluginRegistryDocument)` on `IPluginRegistry`. The private `TrySave` in `PluginRegistryRepository` becomes the public implementation.
- **A2 — Disable/Enable.** `DisablePlugin` and `EnablePlugin` inspect the `TrySave` return. On failure: surface "Could not save the plugin state" on the card (`AgentCardItem.Message` / a new `DisabledAgentCardItem.Message`) and present the persisted truth (reload from disk). The existing try/catch stays for unexpected throws.
- **A3 — Install.** `PluginInstallResult` gains a `Warnings` channel. `RecordInstalledState` writes are post-file-commit — the files are installed and the backup is deleted, so a state-write failure is non-fatal. The result reports `Success = true` with a warning that the plugin appears as Unknown origin on the next scan; the UI (`ShowInstallOutcome`) surfaces warnings. The stale doc comment "installed and activated" is fixed (plugins are never activated; ADR-0004/ADR-0016).
- **A4 — Seed and catalog adoption.** Stay fail-soft: log with their own context ("seeded built-ins will be re-applied next start" / "folder adopted in memory only"). No UI change.

### Part B — Record accepted behavior (no implementation)

- **B1 — Doc.** Add one sentence to `docs/architecture/plugins.md`: an install/update replaces the descriptor and the stored hash by design; the user-modified flag is a diagnostic of drift since the last CLIHub write, not a protection mechanism, and is cleared by the next install.
- **B2 — Spec.** Add one scenario to the `plugin-installation` capability: when a plugin is installed over a locally modified descriptor, the installed content and hash replace the modified ones and the plugin is no longer reported as user-modified.

### Backlog resolution

Two entries resolve with this change:

- "Locally modified plugins are reported but never consulted" → `**Status: Resolved by plugin-state-integrity (Part B — documented as accepted behavior).**`
- "Registry save failures are invisible" → `**Status: Resolved by plugin-state-integrity (Part A — TrySave returns bool, callers surface failure).**`

## Capabilities

### Modified Capability: `plugin-registry`

Delta in `specs/plugin-registry/spec.md` (this change):

- **MODIFIED "Single local registry document"** — write result is reported to the caller and never presented as persisted.
- **MODIFIED "Verify content and reconcile on catalog load"** — catalog-adoption write failure is logged with context, not surfaced.

### Modified Capability: `plugin-installation`

Delta in `specs/plugin-installation/spec.md` (this change):

- **MODIFIED "Disable and enable plugins"** — failed save surfaces "Could not save the plugin state" and reloads to the persisted truth.
- **MODIFIED "Plugin lifecycle state tracking"** — state-write failure after a committed install adds a warning, not a failure.
- **MODIFIED "Atomic installation with rollback"** — a state-write failure after the atomic move is a warning, not a rollback trigger.
- **ADDED scenario to "SHA-256 content integrity"** — local modification is overwritten by install and the plugin is no longer reported as user-modified.

## Impact

**Modified code:**
- `src/CLIHub.Core/Plugins/IPluginRegistry.cs` — `Save` → `TrySave` (return type `bool`).
- `src/CLIHub.Core/Plugins/PluginRegistryRepository.cs` — public `TrySave` replaces `Save`; private `TrySave` becomes the body; adoption and seed log messages tightened.
- `src/CLIHub.Core/Plugins/PluginInstallResult.cs` — `Warnings` property added; doc comment fixed.
- `src/CLIHub.Core/Plugins/PluginInstaller.cs` — `RecordInstalledState` checks `TrySave` return and populates warnings.
- `src/CLIHub.Core/Plugins/PluginCatalog.cs` — adoption save uses `TrySave`, logs failure context.
- `src/CLIHub.Core/Plugins/PluginSeeder.cs` — seed save uses `TrySave`, logs failure context.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — `UpdatePluginRegistryEntry` returns `bool`; `DisablePlugin` and `EnablePlugin` inspect it, set card message on failure.
- `src/CLIHub/ViewModels/Agents/DisabledAgentCardItem.cs` — `Message` and `HasMessage` properties added.
- `src/CLIHub/Launching/PluginDialogService.cs` — `ShowInstallOutcome` renders warnings.
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — bind `DisabledAgentCardItem.Message`.
- `docs/architecture/plugins.md` — one sentence on accepted overwrite behavior.
- `docs/backlog.md` — two entries resolved.
- Tests: `PluginRegistryRepositoryTests`, `PluginInstallerTests`, `PluginCatalogTests`, `PluginSeederTests`, `AgentManagementViewModelTests.Plugins`, `PluginDialogServiceTests`, test fakes (`InMemoryPluginRegistry`).

**Unchanged:** installer staging/backup/rollback ordering; seeder three-hash decision table; `GetUserModifiedPlugins` API and its diagnostic role; the replaced-plugin backup/restore path; catalog reload semantics; `BrowsePluginsDialogViewModel` (inspects, but its save path is through the installer); the built-in plugin reserve list; descriptor format; feed integration.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage; `openspec validate --all`; `graphify update .` at the end; `node .gitnexus/run.cjs detect-changes --scope all --repo .` at the end.

## Non-Goals

- No confirmation dialog for overwriting locally modified descriptors (Part B records accepted behavior; no new UI).
- No removal of `GetUserModifiedPlugins` (it stays the diagnostic surface).
- No change to the seeder's three-hash preservation rule for built-ins.
- No pruning of registry entries whose folder disappeared.
- No new UI surfaces beyond the existing card-message and install-outcome patterns.
- No change to `BrowsePluginsDialogViewModel` save paths (they go through the installer).

## Dependencies

- Base: HEAD `04f0666` (working tree clean). No other active changes.
- Consumes, does not modify: `AtomicFileWriter`, `PluginHasher`, `PluginRegistryDocument`, `PluginRegistryEntry`, `PluginOrigin`.
- No file overlap with concurrent work.