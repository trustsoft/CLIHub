# Plugins

Each plugin is discovered from its own subdirectory of `%APPDATA%\CLIHub\plugins\`, containing `plugin.json`
and an optional `logo.png`. The descriptor's `id` is authoritative; the built-in seeded folders are named by
plugin id:

```jsonc
{
  "schemaVersion": 1,
  "id": "opencode",
  "name": "OpenCode",
  "description": "AI coding agent CLI",
  "version": "1.0.0",           // optional
  "publisher": "Community",     // optional
  "homepage": "https://...",    // optional
  "commands": {
    "launch":  { "executable": "opencode" },
    "resume":  { "executable": "opencode", "arguments": "--continue" },
    "version": { "executable": "opencode", "arguments": "--version" },
    "update":  { "executable": "opencode", "arguments": "upgrade" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.opencode" ],
    "projectIndicators": [ ".opencode", "openspec" ]
  },
  "updateCheck": {
    "source": "npm",
    "package": "@opencode/cli"
  }
}
```

- `schemaVersion` is optional; when the field is absent the descriptor is accepted as schema version 1, the
  supported maximum. Values greater than 1 or non-integer values are invalid.

- `version`, `publisher`, and `homepage` are optional metadata fields added for installable plugins. They do not affect behavior but are displayed in the UI and installation confirmation dialog.

- `commands` is a named set; only `launch` is required.
- `detection.systemPaths` are host install markers (env vars expanded); `detection.projectIndicators` are project-relative markers.
- `updateCheck` is optional and names the distribution channel the agent publishes to, so CLIHub can tell when a newer version exists. Two source kinds are supported:
  - `"source": "npm"` with `"package": "<name>"` — the latest version is read from `https://registry.npmjs.org/<package>/latest`.
  - `"source": "githubReleases"` with `"repo": "<owner>/<repository>"` — the latest version is read from that repository's latest GitHub release (`tag_name`, leading `v` stripped).
  - An unrecognized `source` is ignored with a logged warning; the agent is treated as having no update source. The result never gates or alters the Update action.
- Built-in descriptors for OpenCode, Pi, GitHub Copilot, and Qwen Code are embedded
  and seeded using the three-hash refresh logic described below.

## Plugin Installation

Users can install third-party plugins from local `.clihub-plugin.zip` packages via the CLI Agents settings page.

### Package Format

A valid package is a ZIP archive with:

- Exactly one top-level directory named `<plugin-id>/`
- Required: `<plugin-id>/plugin.json` (valid descriptor)
- Optional: `<plugin-id>/logo.png` (plugin logo image)
- No other files permitted

Security constraints:

- Maximum archive size: 10 MB
- Maximum entry count: 100 files
- No zip-slip paths (entries with `..` or absolute paths)
- No symlinks
- No hidden files
- Plugin `id` must match the directory name

### Installation Workflow

1. **Inspect:** User selects a package file. `IPluginInstaller.Inspect` validates the structure and parses the descriptor without writing files. If valid, a preview is shown with plugin metadata and every declared command (launch, resume, version, update) with its exact executable and arguments. All command kinds execute arbitrary binaries, so the confirmation dialog lists every declared command to allow full inspection before approval.

2. **Confirm:** A confirmation dialog shows:
   - Plugin name, version, publisher, homepage
   - Every declared command kind with its executable and arguments
   - Whether the package replaces an existing installed plugin (and its version)

3. **Install:** If confirmed, `IPluginInstaller.Install` executes:
   - Re-validates the package (defense in depth)
   - Rejects if the plugin `id` matches a built-in plugin id (reserved namespace: the set of ids embedded in the current build)
   - Rejects if `replaceExisting=false` and an installed plugin with the same id exists
   - Extracts to a staging directory: `%APPDATA%\CLIHub\plugins\.staging-<guid>\`
   - Validates the extracted descriptor through `PluginDescriptorReader` and `PluginDescriptorValidator`
   - For replacement: backs up the existing plugin directory to `<id>.backup-<guid>`
   - Atomically moves staging directory to `<id>`
   - On success: deletes backup, updates `plugin-state.json`, triggers catalog reload
   - On failure: deletes staging directory, restores backup if move completed

### Plugin Lifecycle State

CLIHub tracks plugin origin, content integrity, and lifecycle state in `%APPDATA%\CLIHub\plugin-state.json`:

```jsonc
{
  "schemaVersion": 1,
  "plugins": {
    "opencode": {
      "origin": "builtin",             // "builtin", "installed", or "unknown"
      "contentHash": "sha256:...",     // SHA-256 of plugin.json content
      "logoHash": "sha256:..." | null, // SHA-256 of logo.png content, or null if absent
      "installedVersion": "1.0.0",     // from descriptor "version" field, or null
      "disabled": false
    }
  }
}
```

**Origin values:**

- `builtin`: written by this CLIHub build from embedded built-in descriptors
- `installed`: installed from a `.clihub-plugin.zip` package
- `unknown`: origin not recognized (predates state file or content differs from what CLIHub wrote)

**Hash semantics:** `contentHash` and `logoHash` are SHA-256 hashes (hex-encoded) of the raw file bytes. They detect **content drift and user modifications** relative to what CLIHub last wrote. They are NOT cryptographic signatures and do NOT provide authenticity verification or tamper-evidence against motivated attackers.

**Disabled plugins:** Plugins marked `disabled: true` remain on disk but are not loaded by the catalog, are not launchable, and are displayed in a separate "Disabled" section of the CLI Agents page. They can be re-enabled without reinstallation.

### Plugin Seeding (Three-Hash Refresh Logic)

Built-in descriptors are seeded at startup using three-hash comparison to distinguish application updates from user modifications:

- **`embeddedHash`** — SHA-256 hash of the built-in descriptor embedded in the current CLIHub build
- **`storedHash`** — hash recorded in `plugin-state.json` of the content CLIHub last wrote
- **`diskHash`** — hash of the current on-disk `plugin.json` file

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
| origin=`unknown` | Missing | — | Write embedded descriptor and logo | Change origin to `builtin`, record `embeddedHash`, `logoHash` |
| origin=`unknown` | Exists | `diskHash == embeddedHash` | Adopt as built-in | Change origin to `builtin`, record `embeddedHash` as `storedHash` |
| origin=`unknown` | Exists | `diskHash != embeddedHash` | Preserve user content | No change |

**Logo refresh:** `logo.png` files are refreshed using the same three-hash logic with `embeddedLogoHash`, `storedLogoHash`, and `diskLogoHash`. Unmodified logos are overwritten on app update; modified logos are preserved.

**Reserved IDs:** Built-in plugin IDs are reserved and cannot be used by installed plugins. The set of reserved IDs is the set of IDs embedded in the current build (enumerated from the same resources `PluginSeeder` uses), so future built-ins are automatically reserved without hardcoding a list.

### Disable and Enable Plugins

From the CLI Agents settings page, users can:

- **Disable** an enabled plugin: sets `disabled: true` in state, reloads catalog, plugin removed from active list
- **Enable** a disabled plugin: sets `disabled: false` in state, reloads catalog, plugin returned to active list

Disabled plugins stay on disk and can be re-enabled without reinstallation. Disabled built-in plugins are not re-seeded (files not overwritten).

### Plugin Origin Display

The CLI Agents page displays an origin badge on each plugin card:

- **Built-in** badge: origin = `builtin` in state
- **Installed** badge: origin = `installed` in state
- **Unknown** badge: origin = `unknown` in state (neutral styling)

Origin tooltips show plugin version, publisher, and homepage if available.

### Open Plugins Folder

The "Open Plugins Folder" command opens `%APPDATA%\CLIHub\plugins\` in Windows Explorer.

### Manual Catalog Reload

The "Reload Catalog" command triggers `IPluginCatalog.ReloadPlugins()`, rescanning the plugins directory and updating the UI without restarting the application.

## Remote Plugin Feed

CLIHub can browse and install plugins from a remote HTTPS index. By default, the official feed at `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json` is used. The feed URL can be customized or disabled in Preferences.

### Index Schema

The feed index is JSON with `schemaVersion: 1`:

```jsonc
{
  "schemaVersion": 1,
  "lastUpdated": "2024-11-04T12:00:00Z",  // optional
  "plugins": [
    {
      "id": "example-plugin",
      "name": "Example Plugin",
      "description": "An example plugin",
      "version": "1.0.0",
      "schemaVersion": 1,
      "minAppVersion": "1.0.0",
      "publisher": "Community",
      "homepage": "https://example.com",
      "packageUrl": "https://example.com/example-plugin-1.0.0.clihub-plugin.zip",
      "sha256": "abc123...",  // hex-encoded SHA-256 of the package
      "releaseNotesUrl": "https://example.com/release-notes",  // optional
      "signature": null  // reserved for future signing; always null in this change
    }
  ]
}
```

All URLs must be HTTPS. Manual redirects (3xx responses) are followed up to 5 hops; every redirected URL must also be HTTPS.

### Package Verification

Downloaded packages are verified:

1. **SHA-256 checksum:** The downloaded package's SHA-256 hash must match the `sha256` value in the index entry. Mismatches abort installation with an error.
2. **Package structure:** The ZIP structure is validated according to the same rules as local installation (one top-level directory, required `plugin.json`, optional `logo.png`, no hidden files or symlinks).

### Installation Flow

1. **Browse:** User opens "Browse Plugins" from the CLI Agents page. The feed is fetched over HTTPS.
2. **Status classification:** Each feed entry is classified by comparing its `version` with the installed version (if any):
   - **Available:** Not installed
   - **Installed (Current):** Installed, versions match
   - **Update available:** Installed, feed version newer
   - **Different version available:** Installed, versions differ (downgrade or different build)
3. **minAppVersion gating:** If `minAppVersion` is present and the current CLIHub version does not satisfy it (numeric major.minor.patch comparison), the plugin is shown but the Install/Update button is disabled with a message.
4. **Download → Inspect → Confirm → Install:** User clicks Install/Update. CLIHub downloads the package to a temporary file, verifies the SHA-256 checksum, inspects the package structure, and shows the same confirmation dialog as local installation (plugin name, version, publisher, homepage, every declared command). On confirmation, the package is installed through the same `IPluginInstaller` workflow as local packages.
5. **Install outcome:** Success or failure is shown in a dialog. On success, the catalog reloads and the plugin card reflects the new state.

### Feed URL Preference

The `PluginFeedUrl` preference in `AppPreferences` controls the feed URL:

- **Absent or JSON `null`:** Use the default official URL (`https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`).
- **Explicit empty string (`""`):** Feed is disabled. The "Browse Plugins" button is grayed out (disabled) on the CLI Agents page.
- **Non-empty HTTPS URL:** Use the custom feed URL. Non-HTTPS URLs are rejected at validation.

### Built-In ID Filtering

Feed entries whose `id` matches a built-in plugin ID (the set of IDs embedded in the current CLIHub build) are filtered out at load time and never shown in the Browse Plugins dialog. This prevents the feed from attempting to replace built-in plugins, which are managed by the seeding logic.

### Publishing Contract

The official feed at `trustsoft/CLIHub-plugins` is a curated index of community plugins. To publish a plugin:

1. Package your plugin as a `.clihub-plugin.zip` following the format rules.
2. Host the package at a public HTTPS URL (GitHub Releases, CDN, etc.).
3. Compute its SHA-256 hash (`certutil -hashfile <file> SHA256` on Windows, `shasum -a 256 <file>` on Unix).
4. Open a pull request to `trustsoft/CLIHub-plugins` adding your entry to `index.json`, including the package URL and SHA-256 hash.

The maintainers review the package for safety and quality, and merge if approved. The updated index is immediately available to all CLIHub users who use the default feed URL.

### Error Handling

- **Network failure:** If the feed cannot be fetched, the Browse Plugins dialog shows an error message and an empty plugin list.
- **Invalid index JSON:** If the index is malformed or has an unsupported `schemaVersion`, an error is shown.
- **Download failure:** If a package download fails, an error dialog is shown and installation is aborted.
- **Checksum mismatch:** If the downloaded package's SHA-256 does not match the index entry, an error is shown and the package is discarded without extraction.

## Plugin Errors

- **Invalid plugin.json:** skip during discovery, log a warning (with the missing field)
- **Duplicate plugin IDs:** load the first, skip duplicates, log a warning
- **Missing logo:** fall back to the default project logo
- **Disabled plugin:** not loaded by catalog, exposed via `GetDisabledPlugins()`, displayed in separate "Disabled" section
