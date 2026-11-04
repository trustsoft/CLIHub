# Plugin Installation and Management — Tasks

## Stage 1: Core infrastructure and state management

### 1.1 Plugin descriptor metadata additions

- [x] 1.1.1 Add `Version`, `Publisher`, `Homepage` properties to `Plugin` model (`src/CLIHub.Core/Models/Plugins/Plugin.cs`)
- [x] 1.1.2 Update `Plugin.Copy()` to include new fields
- [x] 1.1.3 Update built-in seed descriptors under `src/CLIHub.Core/SeedPlugins/` with version/publisher/homepage
- [x] 1.1.4 Verify existing tests pass (backward compatibility: optional fields)
- [x] 1.1.5 Add test: `PluginDescriptorReaderTests.Read_OptionalMetadataFields_PopulatesPlugin`

**Gate:** `dotnet build CLIHub.sln` succeeds, `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginDescriptorReaderTests"` passes.

### 1.2 Plugin state file repository

- [x] 1.2.1 Add `PluginStateEntry` record to `src/CLIHub.Core/Plugins/PluginStateEntry.cs` (origin, contentHash, logoHash, installedVersion, disabled fields)
- [x] 1.2.2 Add `PluginStateDocument` record to `src/CLIHub.Core/Plugins/PluginStateDocument.cs` (schemaVersion, plugins dictionary)
- [x] 1.2.3 Create `IPluginStateRepository` interface in `src/CLIHub.Core/Plugins/IPluginStateRepository.cs` (Load, Save methods)
- [x] 1.2.4 Implement `PluginStateRepository` in `src/CLIHub.Core/Plugins/PluginStateRepository.cs` (atomic write, tolerant read)
- [x] 1.2.5 Add `PluginStatePath` constant to `AppPaths.cs`: `Path.Combine(Root, "plugin-state.json")`
- [x] 1.2.6 Add unit tests: `tests/CLIHub.Core.Tests/Plugins/PluginStateRepositoryTests.cs`
  - Load missing file returns empty state
  - Load corrupt JSON returns empty state, logs warning
  - Save writes atomically via temp-file-then-rename
  - Save/Load round-trip preserves data

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginStateRepositoryTests"` passes.

### 1.3 Hash computation utility

- [x] 1.3.1 Add `PluginHasher` static class to `src/CLIHub.Core/Plugins/PluginHasher.cs` (ComputeHash method, SHA-256, hex-encoded)
- [x] 1.3.2 Add unit tests: `tests/CLIHub.Core.Tests/Plugins/PluginHasherTests.cs`
  - Deterministic hash for same content
  - Different content produces different hash

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginHasherTests"` passes.

## Stage 2: Installer implementation

### 2.1 Installer interface and result types

- [x] 2.1.1 Add `PluginInstallPreview` record to `src/CLIHub.Core/Plugins/PluginInstallPreview.cs` (IsValid, PluginId, PluginName, Version, Publisher, Homepage, DeclaredCommands list, ReplacesExistingPlugin, ExistingPluginVersion, Errors)
- [x] 2.1.2 Add `CommandPreview` record to `src/CLIHub.Core/Plugins/CommandPreview.cs` (CommandKind, Executable, Arguments)
- [x] 2.1.3 Add `PluginInstallResult` record to `src/CLIHub.Core/Plugins/PluginInstallResult.cs` (Success, InstalledPluginId, Errors)
- [x] 2.1.4 Add `IPluginInstaller` interface to `src/CLIHub.Core/Plugins/IPluginInstaller.cs` (Inspect and Install methods)

### 2.2 Installer validation and inspection

- [x] 2.2.1 Implement `PluginInstaller` class in `src/CLIHub.Core/Plugins/PluginInstaller.cs`
  - `Inspect` method: validate archive structure (one top-level directory, required plugin.json, optional logo.png only)
  - Enforce size limits (10 MB archive, 100 files)
  - Reject zip-slip paths, symlinks, absolute paths, hidden files
  - Check id rules (id == directory name, not a built-in id from embedded resources)
  - Parse descriptor in-memory and extract all declared commands (launch, resume, version, update) with their executable/arguments
  - Return preview with metadata, all declared commands, validation errors
  - Check if replaces existing installed plugin
- [x] 2.2.2 Enumerate built-in IDs from embedded resources (same resource enumeration `PluginSeeder` uses) for reserved ID checking
- [x] 2.2.3 Add unit tests: `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs`
  - Inspect: valid package returns preview with all declared commands
  - Inspect: missing plugin.json rejected
  - Inspect: extra files rejected
  - Inspect: zip-slip path rejected
  - Inspect: symlink rejected
  - Inspect: absolute path rejected
  - Inspect: archive too large rejected
  - Inspect: too many files rejected
  - Inspect: id mismatch rejected
  - Inspect: built-in id rejected for installed plugin (verify against embedded resources)
  - Inspect: identifies replacement scenario

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginInstallerTests"` passes (inspection tests).

### 2.3 Installer staging and atomic move

- [x] 2.3.1 Add `Install` method: re-validate package, extract to `.staging-<guid>` under plugins directory
- [x] 2.3.2 Add descriptor validation: read through `PluginDescriptorReader`, validate through `PluginDescriptorValidator`
- [x] 2.3.3 Add atomic move with backup: rename existing plugin to `.backup-<guid>`, move staging to target, delete backup on success
- [x] 2.3.4 Add rollback on failure: delete staging directory, restore backup if move completed
- [x] 2.3.5 Add `replaceExisting` enforcement: reject if false and installed plugin exists
- [x] 2.3.6 Extend unit tests:
  - Install: staging directory created and removed on success
  - Install: backup created and removed on success
  - Install: rollback restores backup on failure
  - Install: invalid descriptor rejected after extraction
  - Install: replaceExisting=false rejects duplicate
  - Install: replaceExisting=true replaces existing plugin

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginInstallerTests"` passes (all tests).

### 2.4 Installer state and catalog integration

- [x] 2.4.1 Inject `IPluginStateRepository` into `PluginInstaller`
- [x] 2.4.2 Update state file after successful install (origin = "installed", contentHash, logoHash, version)
- [x] 2.4.3 Inject `IPluginCatalog` into `PluginInstaller`
- [x] 2.4.4 Trigger `ReloadPlugins()` after successful install
- [x] 2.4.5 Extend unit tests:
  - State file updated after install with contentHash and logoHash
  - Catalog reload triggered

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginInstallerTests"` passes, `dotnet build CLIHub.sln` succeeds.

## Stage 3: Fixed seeding policy (three-hash logic)

### 3.1 Seeding refactor

- [x] 3.1.1 Inject `IPluginStateRepository` into `PluginSeeder` constructor
- [x] 3.1.2 Add `EnsureBuiltinsExist()` method to `PluginSeeder` implementing three-hash refresh:
  - For each built-in, compute `embeddedHash` of descriptor and logo
  - Load state entry for plugin id
  - Apply decision table from design.md §4:
    - No state + no directory → write embedded, record embeddedHash, origin=builtin
    - No state + diskHash == embeddedHash → adopt as builtin
    - No state + diskHash != embeddedHash → preserve as unknown, log warning
    - origin=installed → never touch
    - origin=builtin, disabled=true → skip writing files
    - origin=builtin, directory missing → write embedded, record embeddedHash
    - origin=builtin, diskHash == storedHash and embeddedHash != storedHash → overwrite (app update), record embeddedHash
    - origin=builtin, diskHash == storedHash == embeddedHash → no-op
    - origin=builtin, diskHash != storedHash → preserve user modification, log warning, do NOT update storedHash
    - origin=unknown, directory missing → write embedded, change origin to builtin, record embeddedHash
    - origin=unknown, diskHash == embeddedHash → adopt as builtin, change origin
    - origin=unknown, diskHash != embeddedHash → preserve user content
  - Apply same three-hash logic to logo.png using logoHash fields
- [x] 3.1.3 Update `PluginInitializationService.Initialize()` to call `EnsureBuiltinsExist()` instead of `SeedIfEmpty()`
- [x] 3.1.4 Modify existing `PluginSeederTests`:
  - Update test names and expectations for new policy
  - Add test: no state, directory missing, writes embedded
  - Add test: no state, disk matches embedded, adopts as builtin
  - Add test: no state, disk differs from embedded, preserves as unknown
  - Add test: origin=builtin, unmodified, app update (embeddedHash changed), overwrites
  - Add test: origin=builtin, user modified (diskHash != storedHash), preserves
  - Add test: origin=installed, never touched
  - Add test: disabled built-in not re-seeded
  - Add test: deleted built-in re-seeded
  - Add test: origin=unknown, directory missing, writes embedded and changes origin to builtin
  - Add test: origin=unknown, disk matches embedded, adopts as builtin
  - Add test: logo refresh follows same three-hash logic

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginSeederTests"` passes, `dotnet build CLIHub.sln` succeeds.

### 3.2 Catalog integration with disabled flag

- [x] 3.2.1 Modify `PluginCatalog.LoadPluginsCore()` to read state file at start
- [x] 3.2.2 Skip loading plugins with `disabled = true` in state
- [x] 3.2.3 Collect disabled plugins into separate `disabledPlugins` collection
- [x] 3.2.4 Add `GetDisabledPlugins()` method returning `IReadOnlyList<DisabledPluginEntry>`
- [x] 3.2.5 Add `DisabledPluginEntry` record (PluginId, PluginDirectory, Origin, Version)
- [x] 3.2.6 Add unit tests:
  - Disabled plugin not loaded by GetAllPlugins()
  - Disabled plugin returned by GetDisabledPlugins()
  - Enabled plugin loaded normally

**Gate:** `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~PluginCatalogTests"` passes.

## Stage 4: UI commands and ViewModel

### 4.1 Dialog service for plugin actions

- [x] 4.1.1 Add `IPluginDialogService` interface to `src/CLIHub/Launching/IPluginDialogService.cs`
  - `PickPluginPackage()` method (returns selected file path or null)
  - `ShowInstallConfirmation(PluginInstallPreview preview)` method (returns bool)
- [x] 4.1.2 Implement `PluginDialogService` in `src/CLIHub/Launching/PluginDialogService.cs`
  - File picker with `*.clihub-plugin.zip` filter
  - Confirmation dialog with plugin metadata, ALL declared commands (launch, resume, version, update) with their exact executable and arguments, and replacement info
- [x] 4.1.3 Register service in `ServiceCollectionExtensions.cs`

### 4.2 AgentManagementViewModel commands

- [x] 4.2.1 Inject `IPluginInstaller` and `IPluginDialogService` into `AgentManagementViewModel`
- [x] 4.2.2 Add `InstallPluginCommand` (opens file picker, calls Inspect, shows confirmation with metadata and ALL declared commands with their exact executable/arguments, then calls Install if confirmed)
- [x] 4.2.3 Add `DisablePluginCommand` per-card (updates state, reloads catalog)
- [x] 4.2.4 Add `EnablePluginCommand` per-disabled-card (updates state, reloads catalog)
- [x] 4.2.5 Add `OpenPluginsFolderCommand` (Process.Start with plugins directory)
- [x] 4.2.6 Add `ReloadCatalogCommand` (calls IPluginCatalog.ReloadPlugins)
- [x] 4.2.7 Extend `AgentCardItem` with `Origin`, `InstalledVersion`, `OriginBadgeText`, `OriginBadgeTooltip` properties
- [x] 4.2.8 Add `DisabledCards` collection populated from `GetDisabledPlugins()`
- [x] 4.2.9 Populate origin metadata from state file during card synchronization

**Gate:** `dotnet build CLIHub.sln` succeeds (compilation only, UI tests next).

### 4.3 ViewModel tests

- [x] 4.3.1 Add or extend `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`:
  - Install command calls Inspect, shows confirmation, calls Install with correct replaceExisting flag
  - Install confirmation workflow (valid preview shows dialog, invalid shows error)
  - Disable command updates state and reloads catalog
  - Enable command updates state and reloads catalog
  - Open plugins folder command
  - Reload catalog command
  - Disabled plugins rendered in separate collection
- [x] 4.3.2 Add mock `FakePluginDialogService` in test support if needed

**Gate:** `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~AgentManagementViewModelTests"` passes.

## Stage 5: UI XAML and visual elements

### 5.1 CLI Agents page UI additions

- [x] 5.1.1 Add "Install Plugin…" button to `src/CLIHub/Views/SettingsAgentsPage.xaml` (bound to `InstallPluginCommand`)
- [x] 5.1.2 Add "Open Plugins Folder" button (bound to `OpenPluginsFolderCommand`)
- [x] 5.1.3 Add "Reload Catalog" button (bound to `ReloadCatalogCommand`)
- [x] 5.1.4 Add per-card "Disable" button (bound to `DisablePluginCommand`, visible for enabled plugins)
- [x] 5.1.5 Add separate "Disabled" section with per-card "Enable" button (bound to `EnablePluginCommand`)
- [x] 5.1.6 Add origin badge to agent card template (displays `OriginBadgeText`, tooltip shows full metadata)
- [x] 5.1.7 Update summary card to show installed vs. built-in counts

**Gate:** `dotnet build CLIHub.sln` succeeds, `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~SettingsPageMetadataTests"` passes (page metadata validation).

## Stage 6: Integration and verification

### 6.1 End-to-end integration

- [x] 6.1.1 Full build: `dotnet build CLIHub.sln`
- [x] 6.1.2 Full test suite: `dotnet test CLIHub.sln`
- [x] 6.1.3 OpenSpec validation: `openspec validate --all`

**Gate:** All three commands succeed with zero errors.

### 6.2 Manual smoke test

- [x] 6.2.1 Create a test `.clihub-plugin.zip` package (e.g., a copy of an existing descriptor with test metadata)
- [x] 6.2.2 Launch CLIHub, navigate to CLI Agents page
- [x] 6.2.3 Click "Install Plugin…", select test package, verify confirmation shows metadata and launch command
- [x] 6.2.4 Confirm installation, verify plugin appears in agent list with "Installed" badge and correct metadata
- [x] 6.2.5 Click "Disable" on installed plugin, verify it moves to "Disabled" section
- [x] 6.2.6 Click "Enable" on disabled plugin, verify it returns to active list
- [x] 6.2.7 Click "Open Plugins Folder", verify `%APPDATA%\CLIHub\plugins\` opens in Explorer with plugin directories visible
- [x] 6.2.8 Manually edit a built-in `plugin.json` (e.g., change name), restart CLIHub, verify modification preserved and warning logged
- [x] 6.2.9 Manually delete a built-in `plugin.json` directory, restart, verify it re-seeds
- [x] 6.2.10 Prepare a modified built-in descriptor with different hash (simulate app update), replace embedded resource, rebuild, run, verify unmodified built-in refreshes and modified one preserves edits
- [x] 6.2.11 Replace a built-in `logo.png` with different content, restart, verify logo preserved (user modification)
- [x] 6.2.12 Click "Reload Catalog", verify UI updates without restart
- [x] 6.2.13 Verify origin=unknown badge shown for plugin with no state entry

**Gate:** All smoke test steps complete successfully, no crashes or unexpected behavior.

## Stage 7: Documentation

### 7.1 Architecture documentation

- [x] 7.1.1 Update `docs/architecture/plugins.md`:
  - Add "Plugin Installation" section (installer, package format, Inspect/Install workflow, security model)
  - Add "Plugin Lifecycle State" section (state file, origin tracking, three-hash refresh logic, disabled flag)
  - Update "Plugin Seeding" section (new three-hash decision table, logo refresh)
- [x] 7.1.2 Update `docs/project-context.md` runtime facts if plugin layout changed (likely no change, but verify)

### 7.2 Spec delta maintenance

- [x] 7.2.1 Keep `openspec/changes/installable-plugins/specs/plugin-installation/spec.md` in sync if implementation deviates from the delta
- [x] 7.2.2 Keep `openspec/changes/installable-plugins/specs/plugin-seeding/spec.md` in sync if implementation deviates from the delta

### 7.3 Final validation

- [x] 7.3.1 Run `openspec validate --all` from repository root
- [x] 7.3.2 Verify zero errors
- [x] 7.3.3 Run `graphify update .` to refresh knowledge graph

**Gate:** `openspec validate --all` reports 0 errors, `graphify update .` succeeds.

## Completion checklist

- [x] All tasks marked complete
- [x] `dotnet build CLIHub.sln` succeeds with zero warnings in changed files
- [x] `dotnet test CLIHub.sln` passes all tests
- [x] `openspec validate --all` reports 0 errors
- [x] Manual smoke test completed and documented
- [x] Documentation updated and reviewed
- [x] Change ready for user approval and archiving
