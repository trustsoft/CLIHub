# Plugin Installation and Management — Design

## Verified current state

### Plugin descriptor schema (schemaVersion 1)

`src/CLIHub.Core/Models/Plugins/Plugin.cs:1-130` defines the current shape:

- `SchemaVersion` (int?, optional, defaults to 1 if absent)
- `Id`, `Name`, `Description` (string)
- `Commands` (AgentCommands: launch, resume, version, update)
- `Detection` (SystemPaths, ProjectIndicators)
- `UpdateCheck` (npm or githubReleases source)
- `LogoPath`, `PluginDirectory` (runtime-populated)

No `version`, `publisher`, or `homepage` fields exist today.

### Plugin catalog loading

`src/CLIHub.Core/Plugins/PluginCatalog.cs:69-130` (`LoadPluginsCore`):

1. Enumerates descriptors from `IPluginDescriptorSource` in deterministic order (ADR-0004: OrdinalIgnoreCase by directory path)
2. Validates through `IPluginDescriptorValidator`
3. First-wins duplicate-id resolution (line 102-111)
4. Populates `LogoPath` via `PluginLogoResolver`
5. Returns detached copies via `Plugin.Copy()` (line 133)
6. Raises `PluginsChanged` event on manual reload (line 66)

Invalid plugins are retained in `invalidPlugins` collection (line 72, 82, 94, 122) and exposed via `GetInvalidPlugins()` (line 136).

### Current seeding policy

`src/CLIHub.Core/Plugins/PluginSeeder.cs:33-134` (`SeedIfEmpty`):

- Line 37-40: `HasAnyPlugin` gate — if any plugin subdirectory with `plugin.json` exists, return 0 (do nothing)
- Line 42-125: seed all built-in descriptors and logos from embedded resources
- Line 83-86, 105-108: skip writing if `plugin.json` or `logo.png` already exists

This is all-or-nothing: one user plugin prevents correcting built-in descriptors forever.

### Startup integration

`src/CLIHub/Startup/PluginInitializationService.cs:25-29`:

```csharp
public void Initialize()
{
    this.pluginSeeder.SeedIfEmpty();
    this.pluginCatalog.LoadPlugins();
}
```

Called from `StartupStateLoader.Load()` before UI initialization.

### File system layout

`src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs:28-50`:

- `Root`: `%APPDATA%\CLIHub`
- `PluginsDirectory`: `%APPDATA%\CLIHub\plugins`
- `ConfigFile`: `%APPDATA%\CLIHub\config.json`
- `CacheDirectory`: `%APPDATA%\CLIHub\cache`

No `plugin-state.json` path exists today.

### UI surface

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:92-97`:

- `Cards` (ObservableCollection<AgentCardItem>): valid plugins
- `InvalidCards` (ObservableCollection<InvalidAgentCardItem>): invalid plugins with diagnostics

The CLI Agents page (`src/CLIHub/Views/SettingsAgentsPage.xaml`) displays cards and invalid cards. No install/disable/reload commands exist today.

`src/CLIHub/ViewModels/Agents/AgentCardItem.cs` has per-card commands: `DetectCommand`, `CheckUpdatesCommand`, `UpdateCommand`, `EditConfigCommand`. No disable/enable command exists.

### Agent Management ViewModel commands

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:83-88`:

```csharp
this.DetectCardCommand = new RelayCommand<AgentCardItem>(...);
this.CheckCardUpdatesCommand = new RelayCommand<AgentCardItem>(...);
this.UpdateCommand = new RelayCommand<AgentCardItem>(...);
this.EditConfigCommand = new RelayCommand<AgentCardItem>(...);
this.DetectCommand = new RelayCommand(() => ...);
this.CheckUpdatesCommand = new RelayCommand(() => ...);
```

No `InstallPluginCommand`, `DisablePluginCommand`, `EnablePluginCommand`, `OpenPluginsFolderCommand`, or `ReloadCatalogCommand` exists.

### Configuration document boundary

ADR-0002 (`docs/adr/0002-single-configuration-document.md:17-19`):

> One document, one file: `AppConfigDocument` at `%APPDATA%\CLIHub\config.json`, camelCase JSON, is the only persisted configuration. Unknown fields are ignored and missing fields fall back to defaults.

Plugin lifecycle state (origin, hash, disabled flag) does not belong in the user preferences/project-state document. A separate small state file is justified here because:

1. It is not user preference (not changed from Settings)
2. It is not project state (not per-project)
3. It is operational metadata for the plugin subsystem, analogous to `cache/logos.json` (line 55)

### Persistence pattern

`src/CLIHub.Core/Infrastructure/Persistence/ConfigurationRepository.cs` writes atomically via temp-file-then-rename. The same pattern applies to plugin state.

### Dialog service organization

The existing `IProjectDialogService` interface lives in `src/CLIHub/Launching/ProjectDialogService.cs`. User-notification and folder-picker services follow the same pattern: interface and implementation colocated under `Launching/`.

## Design decisions

### 1. Descriptor metadata additions

**Decision:** Add optional fields to `Plugin` model without bumping `schemaVersion`:

```csharp
public string? Version { get; set; }
public string? Publisher { get; set; }
public string? Homepage { get; set; }
```

**Rationale:** These are advisory metadata, not behavioral. A descriptor without them is still valid. Keeping `schemaVersion` at 1 preserves backward compatibility: old CLIHub builds ignore unknown fields (JSON deserialization), new builds accept both shapes.

**Rejected alternative:** Bump to `schemaVersion: 2` — rejected because the existing validator treats absent `schemaVersion` as 1 (spec line 159-162), and a version bump would reject all existing descriptors in older builds.

### 2. Plugin state file schema

**Decision:** Separate `plugin-state.json` under `%APPDATA%\CLIHub\`:

```jsonc
{
  "schemaVersion": 1,
  "plugins": {
    "<plugin-id>": {
      "origin": "builtin" | "installed" | "unknown",
      "contentHash": "sha256:...",       // SHA-256 of plugin.json content
      "logoHash": "sha256:..." | null,   // SHA-256 of logo.png content, or null if absent
      "installedVersion": null | "1.2.3", // from descriptor "version" field
      "disabled": false
    }
  }
}
```

**Rationale:**

- Not a preference or project-state field, so does not belong in `config.json` (ADR-0002)
- Precedent: `cache/logos.json` is a similar operational metadata file
- Atomic writes via temp-file-then-rename prevent corruption
- Missing state file at startup = unknown origin, graceful migration

**Hash fields:**

- `contentHash`: SHA-256 of the raw `plugin.json` file content (UTF-8 bytes), hex-encoded; detects drift from what CLIHub last wrote
- `logoHash`: SHA-256 of the raw `logo.png` file content, hex-encoded, or `null` if no logo; allows built-in logo updates to propagate

**Note:** Hashes stored in a user-writable state file detect **content drift and modification**, not tampering or authenticity (no signature verification).

**Migration from missing state:**

1. On startup, if `plugin-state.json` does not exist, all existing plugins are evaluated per the seeding decision table below
2. Built-in seeding writes state for newly seeded or adopted descriptors with origin = "builtin"
3. First install/disable action writes the state file and backfills hashes/origins for existing plugins

**Rejected alternative:** Fold state into `config.json` — rejected per ADR-0002; this is not user preference.

**Rejected alternative:** Store state inside each `plugin.json` — rejected because it would corrupt the descriptor content and break hash validation.

### 3. Installer atomicity and security

**Decision:** `IPluginInstaller` interface with separate inspect and install methods in `CLIHub.Core/Plugins/`:

```csharp
public interface IPluginInstaller
{
    /// <summary>
    /// Inspects a plugin package without writing files.
    /// </summary>
    /// <param name="packagePath">Path to the .clihub-plugin.zip file.</param>
    /// <returns>Preview with metadata, validation errors, or replacement info.</returns>
    PluginInstallPreview Inspect(string packagePath);

    /// <summary>
    /// Installs a plugin package after confirmation.
    /// </summary>
    /// <param name="packagePath">Path to the .clihub-plugin.zip file.</param>
    /// <param name="replaceExisting">Whether to replace an existing installed plugin with the same id.</param>
    /// <returns>Result with installed plugin id or errors.</returns>
    PluginInstallResult Install(string packagePath, bool replaceExisting);
}

public record PluginInstallPreview(
    bool IsValid,
    string? PluginId,
    string? PluginName,
    string? Version,
    string? Publisher,
    string? Homepage,
    IReadOnlyList<CommandPreview> DeclaredCommands,
    bool ReplacesExistingPlugin,
    string? ExistingPluginVersion,
    IReadOnlyList<string> Errors);

public record CommandPreview(
    string CommandKind,  // "launch", "resume", "version", "update"
    string Executable,
    string? Arguments);

public record PluginInstallResult(
    bool Success,
    string? InstalledPluginId,
    IReadOnlyList<string> Errors);
```

**Inspect phase (no writes):**

1. Validate archive structure:
   - Must contain exactly one top-level directory (the plugin id)
   - Required: `<id>/plugin.json`
   - Optional: `<id>/logo.png`
   - No other files permitted
   - Max archive size: 10 MB
   - Max file count: 100
   - Reject: zip-slip paths (`..`, absolute paths), symlinks, hidden files

2. Extract to in-memory buffer and parse `plugin.json` through `PluginDescriptorReader` + `PluginDescriptorValidator`

3. Extract all declared commands (launch, resume, version, update) with their executable and arguments from the descriptor

4. Return preview with metadata, all declared commands (each with its exact executable and arguments), validation errors, and whether it would replace an existing installed plugin

**Security rationale:** All four command kinds execute the descriptor's specified executable/arguments. A package could hide its actual payload in `resume`, `version`, or `update` while showing a harmless `launch` command. The confirmation dialog MUST list every declared command so the user can inspect all execution paths before approving installation.

**Install phase (after user confirmation):**

1. Re-validate package (defense in depth; user could have swapped the file)

2. Extract to temporary staging directory: `%APPDATA%\CLIHub\plugins\.staging-<guid>\`

3. Check id rules:
   - Plugin `id` must equal the directory name
   - Reject if `id` matches a built-in plugin id (reserved namespace: the set of ids embedded in the current build, enumerated from the same resources `PluginSeeder` uses)
   - If `replaceExisting` is false and an installed plugin with same id exists, return error

4. Atomic move:
   - Backup existing plugin directory to `<id>.backup-<guid>` if replacing
   - Move staging directory to `<id>`
   - On success: delete backup
   - On failure: restore backup, delete staging directory

5. Update `plugin-state.json`:
   - Set origin = "installed"
   - Compute and store `contentHash` and `logoHash`
   - Store `installedVersion` from descriptor `version` field (null if absent)
   - Set `disabled = false`

6. Trigger `IPluginCatalog.ReloadPlugins()`

**Rollback on failure:** Delete staging directory, restore backup if atomic move completed.

**Security notes:**

- Zip-slip protection: reject any entry with `..` or absolute path
- File type restriction: only `plugin.json` and `logo.png` allowed
- Size limits prevent DoS via large archives
- No DLL loading, no code execution — only JSON and PNG files

**Rejected alternative:** Allow arbitrary files in plugin directory — rejected for security (only descriptor and logo are needed, other files are attack surface).

**Rejected alternative:** Replace built-in plugins — rejected to preserve the known-good baseline; built-in ids are reserved. Reserved IDs are defined as the set of ids embedded in the current build (enumerated from the same embedded resources `PluginSeeder` uses), so future built-ins are automatically reserved without hardcoding a list.

### 4. Fixed seeding policy (three-hash decision table)

**Decision:** Replace `HasAnyPlugin` gate with explicit three-hash refresh logic.

**Hash definitions:**

- **`embeddedHash`** — SHA-256 hash of the built-in descriptor embedded in the current CLIHub build
- **`storedHash`** — hash recorded in `plugin-state.json` of the content CLIHub last wrote (absent if no state entry)
- **`diskHash`** — hash of the current on-disk `plugin.json` file (absent if directory missing)

**Decision table for each built-in plugin:**

| State entry | Directory | Condition | Action | Update state |
|-------------|-----------|-----------|--------|--------------|
| None | Missing | — | Write embedded descriptor and logo | Record `embeddedHash`, `logoHash`, origin=`builtin` |
| None | Exists | `diskHash == embeddedHash` | Adopt as built-in | Record `diskHash` as `storedHash`, origin=`builtin` |
| None | Exists | `diskHash != embeddedHash` | Preserve user content, log warning | Record `diskHash` as `storedHash`, origin=`unknown` |
| origin=`installed` | Any | — | Never touch | No change |
| origin=`builtin`, disabled=`true` | Any | — | Do not write files | No change (keep state) |
| origin=`builtin` | Missing | — | Write embedded descriptor and logo | Record `embeddedHash`, `logoHash` |
| origin=`builtin` | Exists | `diskHash == storedHash` and `embeddedHash == storedHash` | No-op (already current) | No change |
| origin=`builtin` | Exists | `diskHash == storedHash` and `embeddedHash != storedHash` | Overwrite with embedded (app update) | Record `embeddedHash` as new `storedHash` |
| origin=`builtin` | Exists | `diskHash != storedHash` | User modified; preserve, log warning | No change (keep `storedHash`) |
| origin=`unknown` | Missing | — | Write embedded descriptor and logo (no user content remains) | Change origin to `builtin`, record `embeddedHash`, `logoHash` |
| origin=`unknown` | Exists | `diskHash == embeddedHash` | Adopt as built-in | Change origin to `builtin`, record `embeddedHash` as `storedHash` |
| origin=`unknown` | Exists | `diskHash != embeddedHash` | Preserve user content | No change |

**Logo refresh logic (same three-hash pattern):**

- `embeddedLogoHash` — hash of built-in logo embedded in current build
- `storedLogoHash` — hash recorded in state file
- `diskLogoHash` — hash of current `logo.png` (null if missing)

Logo refresh follows the same rules as descriptor refresh: unmodified logos are overwritten on app update, modified logos are preserved.

**Rationale:** The three-hash comparison distinguishes:
1. **App update** (unmodified on-disk content differs from new embedded version) → overwrite
2. **User modification** (on-disk content differs from what CLIHub last wrote) → preserve

**Rejected alternative:** Always overwrite built-ins — rejected because it destroys user edits and testing changes.

### 5. Catalog API for disabled plugins

**Decision:** Add `GetDisabledPlugins()` method to `IPluginCatalog`:

```csharp
/// <summary>
///   Gets disabled plugin entries that were skipped during load.
/// </summary>
/// <returns> The disabled plugins and their state metadata. </returns>
IReadOnlyList<DisabledPluginEntry> GetDisabledPlugins();

public record DisabledPluginEntry(
    string PluginId,
    string PluginDirectory,
    string? Origin,  // "builtin", "installed", "unknown"
    string? Version);
```

**Rationale:** Disabled plugins are not launchable, so `GetAllPlugins()` excludes them. But the UI must show them with an Enable action, so the catalog must expose them separately.

**UI rendering:**

- CLI Agents page shows a separate "Disabled" section below active plugins
- Disabled cards show plugin id, origin badge, version (if known), and an "Enable" button
- Origin badges: "Built-in", "Installed", "Unknown" (neutral badge for origin=`unknown`)

### 6. UI additions

**Decision:** Add commands to `AgentManagementViewModel`:

- `InstallPluginCommand`: opens file picker, calls `Inspect`, shows confirmation with metadata and all declared commands, then calls `Install` if confirmed
- `DisablePluginCommand` (per-card): sets `disabled = true` in state, triggers reload
- `EnablePluginCommand` (per-disabled-card): sets `disabled = false` in state, triggers reload
- `OpenPluginsFolderCommand`: calls `Process.Start` with `%APPDATA%\CLIHub\plugins\`
- `ReloadCatalogCommand`: calls `IPluginCatalog.ReloadPlugins()` explicitly

**Confirmation dialog workflow:**

1. User clicks "Install Plugin…", file picker opens
2. User selects `.clihub-plugin.zip`
3. ViewModel calls `Inspect(packagePath)`
4. If invalid, show error dialog and stop
5. If valid, show confirmation dialog:

```
Install Agent Plugin

Name:      OpenClaude
Version:   1.0.0
Publisher: Community
Homepage:  https://example.com/openclaude

This plugin declares the following commands:

  launch:  claude
  resume:  claude --continue
  version: claude --version

[Replaces installed version 0.9.0]  (if applicable)

[Install]  [Cancel]
```

6. If user clicks Install, call `Install(packagePath, replaceExisting=true/false)`
7. Show success or error message

**Per-card origin badge:** Add `Origin`, `OriginBadgeText`, `OriginBadgeTooltip` properties to `AgentCardItem`, populated from state file.

**Invalid card enhancements:** Existing `InvalidAgentCardItem` already renders errors; installer validation errors fit the same surface.

### 7. Dialog service location

**Decision:** Follow the existing pattern in `src/CLIHub/Launching/`:

- Add `IPluginDialogService` interface
- Add `PluginDialogService` implementation
- Colocate with `ProjectDialogService` and `IProjectDialogService`

**Rationale:** The existing dialog services live under `Launching/`, not a separate `Dialogs/` folder. Consistency with the existing organization avoids creating a new folder for a single interface.

### 8. Testing strategy

**Unit tests (Core):**

- `PluginInstallerTests`:
  - Inspect: valid package returns preview with correct metadata
  - Inspect: invalid packages rejected (zip-slip, symlinks, disallowed files, size/count limits, id mismatch, built-in id)
  - Install: valid package installs successfully
  - Install: staging/rollback on failure
  - Install: state file updated correctly
  - Install: replaceExisting honored

- `PluginStateRepositoryTests`:
  - Atomic writes via temp-file-then-rename
  - Migration from missing state file
  - Corrupt JSON returns empty state, logs warning

- `PluginHasherTests`:
  - Deterministic hash for same content
  - Different content produces different hash

- `PluginSeederTests` (modified):
  - New seeding policy: three-hash decision table
  - No state entry, directory missing: writes embedded
  - No state entry, disk == embedded: adopts as builtin
  - No state entry, disk != embedded: preserves as unknown
  - origin=builtin, unmodified, app update: overwrites with embedded
  - origin=builtin, modified: preserves user edit
  - origin=installed: never touched
  - Disabled built-in not re-seeded
  - Logo refresh follows same logic

**ViewModel tests:**

- `AgentManagementViewModelTests` (extended):
  - Install command: inspect → confirm → install workflow
  - Disable command updates state and reloads catalog
  - Enable command updates state and reloads catalog
  - Open plugins folder command
  - Reload catalog command
  - Disabled plugins rendered in separate section

**Manual smoke task:**

1. Install a test `.clihub-plugin.zip` (create one from an existing descriptor)
2. Verify plugin appears with "Installed" badge and correct metadata
3. Disable the plugin, verify it moves to "Disabled" section
4. Enable the plugin, verify it returns to active list
5. Open plugins folder, verify directory exists
6. Modify a built-in `plugin.json` (e.g., change name), restart CLIHub, verify modification preserved and logged
7. Delete a built-in `plugin.json` directory, restart, verify it re-seeds
8. Prepare a test CLIHub build with a corrected built-in descriptor (different embedded hash), install over existing, verify unmodified built-in refreshes and modified one preserves edits
9. Replace a built-in `logo.png` with different content, restart, verify logo preserved (user modification)
10. Click "Reload Catalog", verify UI updates without restart

## Security model

**Trust boundary:** The `.clihub-plugin.zip` package is untrusted input. The installer enforces:

1. Structural validation (no zip-slip, symlinks, absolute paths)
2. File type restriction (only `plugin.json` and `logo.png`)
3. Size and count limits
4. Descriptor schema validation (existing `PluginDescriptorValidator`)
5. SHA-256 content hash for **integrity and drift detection** (not tampering or authenticity — no signature verification in this change)

**Hash semantics:** The `contentHash` and `logoHash` fields stored in the user-writable `plugin-state.json` detect **content drift and user modifications** relative to what CLIHub last wrote. They do NOT provide cryptographic authenticity (who created the content) or tamper-evidence against a motivated attacker with write access to `%APPDATA%\CLIHub\`. The hashes answer: "Did the user or another process modify this file since CLIHub wrote it?"

**No signature verification:** This change does NOT add cryptographic signatures or publisher verification. The user explicitly chooses a local file, and the hash provides integrity within the trust boundary (detect corruption/drift), not authenticity (who created it).

**Future trust model (out of scope):** A remote feed would add HTTPS transport security and optionally index signing. That is a separate change.

## Deviations from brief

None. The design aligns with the brief's decisions and scope. The three-hash table and logo refresh logic clarify the brief's intent (distinguish app update from user modification) without changing scope.

## Open questions

None remaining. All implementation questions are answered in the design or deferred to tasks.md with explicit answers.
