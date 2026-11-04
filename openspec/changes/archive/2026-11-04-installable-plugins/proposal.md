# Plugin Installation and Management

## Why

Users currently cannot add, update, or remove agent plugins without waiting for a CLIHub release. Built-in descriptors seed once on first run and never refresh, so corrected descriptors and new built-in agents require a full reinstall of CLIHub. Invalid or obsolete plugins accumulate with no removal path, and users cannot inspect where a descriptor came from or whether it has been modified.

This prevents users from:

- Installing community or proprietary agent descriptors from a local package file
- Fixing broken built-in descriptors when an app update ships corrected ones
- Removing or temporarily disabling unwanted plugins
- Understanding plugin provenance and integrity

The existing all-or-nothing `HasAnyPlugin` seeding gate means a single user-created descriptor prevents correcting built-in descriptors forever.

## What changes

### 1. Descriptor metadata extensions (backward compatible)

Add optional fields to `Plugin` and the JSON schema while preserving existing schemaVersion 1 descriptors:

- `version` (string, semantic version)
- `publisher` (string)
- `homepage` (string, URL)

Existing descriptors without these fields continue to load. Built-in seed descriptors gain these fields. The validator accepts both old and new shapes.

### 2. Plugin lifecycle state (separate from config.json)

A new `plugin-state.json` under `%APPDATA%\CLIHub\` records per-plugin lifecycle data:

```jsonc
{
  "schemaVersion": 1,
  "plugins": {
    "opencode": {
      "origin": "builtin",         // "builtin" | "installed" | "unknown"
      "contentHash": "sha256:...",  // SHA-256 of plugin.json content
      "logoHash": "sha256:...",     // SHA-256 of logo.png content, or null
      "installedVersion": "1.2.3",  // from descriptor "version" field
      "disabled": false
    }
  }
}
```

This state:

- Lives outside `config.json` per ADR-0002 (plugin lifecycle is not user preference)
- Migrates gracefully: missing state file at startup means unknown origin for existing plugins
- Atomic writes through a temp-file-then-rename path (reuse the existing pattern from `ConfigurationRepository`)
- Records when a descriptor was modified (hash mismatch) or originated from a local install

### 3. Installer capability

A new `IPluginInstaller` interface and `PluginInstaller` implementation in `CLIHub.Core/Plugins/`:

- Validates a `*.clihub-plugin.zip` archive structure
- Enforces security: rejects zip-slip paths, symlinks, absolute paths, and disallowed files (only `plugin.json` and `logo.png` permitted)
- Enforces size limits (10 MB archive, 100 files) and id rules (id = folder name, no reserved built-in ids for installed plugins)
- Stages to a temporary directory under `%APPDATA%\CLIHub\plugins\.staging-<guid>\`
- Re-validates through `PluginDescriptorReader` + `PluginDescriptorValidator` after extraction
- Atomically moves into the plugins directory with backup of any existing plugin with the same id
- Updates `plugin-state.json` with origin = "installed", hash, and version
- Triggers catalog reload
- Rollback on failure: removes staging directory, restores backup if atomic move succeeded

Duplicate id policy: an installed plugin cannot replace a built-in id; built-ins have reserved namespace.

### 4. Fixed seeding policy (three-hash logic)

Replace the `HasAnyPlugin` gate with three-hash refresh logic that distinguishes app updates from user modifications:

**Hash definitions:**
- `embeddedHash` — hash of built-in descriptor embedded in current CLIHub build
- `storedHash` — hash recorded in state file of content CLIHub last wrote
- `diskHash` — hash of current on-disk file

**Refresh rules:**
- No state entry + directory missing → write embedded, record `embeddedHash`, origin=`builtin`
- No state entry + disk matches embedded → adopt as builtin
- No state entry + disk differs from embedded → preserve as `unknown`, log warning
- origin=`installed` → never touch
- origin=`builtin`, disabled → do not write files
- Directory missing → re-seed
- `diskHash == storedHash` and `embeddedHash != storedHash` → overwrite (app update)
- `diskHash != storedHash` → preserve user modification, log warning

Logo refresh follows the same three-hash logic.

Deleted built-in behavior: re-seed on next start unless marked disabled in state.

### 5. UI additions on CLI Agents settings page

New commands and UI elements:

- **Install Plugin…** button: opens file picker for `*.clihub-plugin.zip`, validates, installs, shows confirmation or actionable error
- **Disable** per-card action (reversible): marks plugin disabled in state file, triggers reload (no file deletion)
- **Enable** action for disabled plugins: re-enables a previously disabled plugin
- **Open Plugins Folder** button: opens `%APPDATA%\CLIHub\plugins\` in Explorer
- **Reload Catalog** button: calls `IPluginCatalog.ReloadPlugins()` explicitly
- Per-card origin badge: "Built-in", "Installed", or "Unknown" (neutral) with tooltip showing version, publisher, homepage
- Separate "Disabled" section showing disabled plugins with Enable action
- Enhanced `InvalidAgentCardItem` surface for validation errors (already exists, now shows installer errors too)

The existing `PluginsChanged` event and catalog refresh path already support UI updates.

Confirmation dialog for install shows:

- Plugin name, version, publisher
- Exact executable and arguments that will run
- Homepage link
- "Install" / "Cancel" buttons

### 6. Tests

- Core unit tests:
  - `PluginInstallerTests`: validation, zip-slip rejection, size/count limits, staging/rollback, state updates
  - `PluginStateRepositoryTests`: atomic writes, migration from missing state, hash validation
  - `PluginSeederTests` (modified): new seeding policy, hash-based refresh, modified-descriptor preservation
- ViewModel tests:
  - `AgentManagementViewModelTests` (extended): install command, remove/disable command, confirmation workflow
- Manual smoke task:
  - Install a test package, verify card appears with "Installed" badge
  - Remove plugin, verify it disappears from UI
  - Modify a built-in descriptor, restart, verify it is not overwritten
  - Delete a built-in descriptor, restart, verify it re-seeds
  - Update CLIHub with a corrected built-in, verify unmodified built-in refreshes

### 7. Documentation

- `docs/architecture/plugins.md`: add installation, lifecycle state, seeding policy sections
- `docs/project-context.md`: update runtime facts if plugins/ layout changes
- Spec deltas under `openspec/changes/installable-plugins/specs/`:
  - `plugin-installation/spec.md` (new capability)
  - `plugin-seeding/spec.md` (modified behavior)

## Impact

### Breaking changes

None. Existing schemaVersion 1 descriptors without the new fields continue to load. Existing installations see "unknown" origin until the state file is written on the next plugin action.

### Migration

On startup with no `plugin-state.json`:

- Treat all existing plugins as origin = "unknown"
- Built-in seeding writes state for newly seeded descriptors
- First install or remove action writes the state file and populates existing plugin hashes

### Rollout

Single-phase release: all changes deploy together. The state file writes atomically so partial writes cannot corrupt it.

## Explicit non-goals

- Remote plugin feed or marketplace (deferred to a separate change)
- Network calls, HTTPS fetching, or feed clients
- Index signing, publisher verification, or cryptographic signatures beyond SHA-256 integrity
- Editing the agent CLI installation itself (CLIHub only manages descriptors, per `docs/vision.md`)
- In-app plugin code execution or DLL loading (already excluded by the existing "plugins are data only" rule)
- Extending command kinds beyond launch/resume/version/update
- Automatic update checking for installed plugins (deferred)

## Dependencies

None. This change is self-contained and does not block or depend on other proposed changes.

## Follow-ups

Candidates for `docs/backlog.md`:

- Curated remote plugin feed with HTTPS integrity checks
- Automatic update detection for installed plugins
- Export installed plugin to a shareable `.clihub-plugin.zip`
- Plugin usage analytics (which agents are actually launched)
- Per-project plugin overrides (project-specific agent versions)
