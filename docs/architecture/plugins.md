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
  - An unrecognized `source` is ignored with a logged warning; the agent is treated as having no update source, so its availability is `Unknown` and the Settings CLI Agents card Update action stays disabled. The launch-window Update action remains declaration-based.
- Built-in descriptors for OpenCode, Pi, OMP, Qwen Code, and Devin are embedded
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
   - On success: deletes backup, updates `registry.json`, triggers catalog reload
   - On failure: deletes staging directory, restores backup if move completed

### Plugin Lifecycle State

CLIHub tracks plugin origin, content integrity, and lifecycle state in one registry document, `%APPDATA%\CLIHub\registry.json` — the single local source of truth for that state:

```jsonc
{
  "plugins": {
    "opencode": {
      "origin": "builtin",             // "builtin", "installed", or "unknown"
      "contentHash": "sha256...",      // SHA-256 of plugin.json content, or null if not recorded
      "logoHash": "sha256..." | null,  // SHA-256 of logo.png content, or null if absent
      "installedVersion": "1.0.0",     // from descriptor "version" field, or null
      "enabled": true,                 // absent field defaults to true (fail-safe)
      "installedAt": "2026-11-14T..." | null // UTC install time; null for built-ins and adopted entries
    }
  }
}
```

Reads are tolerant: missing fields are defaulted (`origin` degrades to `unknown`, `enabled` defaults to `true`), unknown fields are ignored, and a corrupt file degrades to an empty registry without throwing. Writes are atomic (temp file + rename). There is no schema-version field.

**One-time legacy adoption:** when `registry.json` is absent and the pre-registry `plugin-state.json` exists, CLIHub adopts it: origin, content hashes, and installed version are preserved, the inverted `disabled` flag becomes `enabled`, and `installedAt` stays null (not fabricated). A successful adoption writes `registry.json` once and deletes `plugin-state.json`; a failed write keeps the legacy file and retries on the next start. Once a registry exists, the legacy file is never read again. A corrupt registry with a legacy file present recovers by adopting the legacy content.

**Origin values:**

- `builtin`: written by this CLIHub build from embedded built-in descriptors
- `installed`: installed from a `.clihub-plugin.zip` package or a descriptor-only feed install
- `unknown`: origin not recognized (predates the registry or content differs from what CLIHub wrote)

**Hash semantics:** `contentHash` and `logoHash` are SHA-256 hashes (hex-encoded) of the raw file bytes. They detect **content drift and user modifications** relative to what CLIHub last wrote. They are NOT cryptographic signatures and do NOT provide authenticity verification or tamper-evidence against motivated attackers.

**Disabled plugins:** Plugins marked `enabled: false` remain on disk but are not loaded by the catalog, are not launchable, and are displayed in a separate "Disabled" section of the CLI Agents page. They can be re-enabled without reinstallation.

### Scan Verification and Reconciliation

During each catalog load, every processed plugin's stored `contentHash` is verified against the descriptor on disk — uniformly for every origin (`builtin`, `installed`, `unknown`), including disabled plugins in the skip path:

- **Mismatch:** the descriptor was modified after CLIHub last wrote it. A warning is logged and the plugin is exposed as user-modified through `IPluginCatalog.GetUserModifiedPlugins()` (id, directory, origin, stored hash, disk hash). The plugin still loads normally, and the stored hash is never rewritten — the modification stays detectable on every later scan. A null stored hash means "cannot verify", not "modified".
- **Folder without a registry entry:** adopted with origin `unknown`, the disk hash, the descriptor's version, and `enabled: true`; the registry is saved at most once per scan. A clean scan that adopts nothing does not rewrite the registry.
- **Registry entry whose folder is gone:** the entry is kept (logged, never pruned) — built-in re-seeding and disabled-state preservation depend on entries surviving missing folders.

### Plugin Seeding (Three-Hash Refresh Logic)

Built-in descriptors are seeded at startup using three-hash comparison to distinguish application updates from user modifications:

- **`embeddedHash`** — SHA-256 hash of the built-in descriptor embedded in the current CLIHub build
- **`storedHash`** — hash recorded in `registry.json` of the content CLIHub last wrote
- **`diskHash`** — hash of the current on-disk `plugin.json` file

**Decision table for each built-in plugin:**

| Registry entry | Directory | Condition | Action | Update registry |
|-------------|-----------|-----------|--------|--------------|
| None | Missing | — | Write embedded descriptor and logo | Record `embeddedHash`, `logoHash`, origin=`builtin` |
| None | Exists | `diskHash == embeddedHash` | Adopt as built-in | Record `diskHash` as `storedHash`, origin=`builtin` |
| None | Exists | `diskHash != embeddedHash` | Preserve user content, log warning | Record `diskHash` as `storedHash`, origin=`unknown` |
| origin=`installed` | Any | — | Never touch | No change |
| origin=`builtin`, `enabled: false` | Any | — | Do not write files | No change (keep state) |
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

- **Disable** an enabled plugin: sets `enabled: false` in the registry, reloads catalog, plugin removed from active list
- **Enable** a disabled plugin: sets `enabled: true` in the registry, reloads catalog, plugin returned to active list

Disabled plugins stay on disk and can be re-enabled without reinstallation. Disabled built-in plugins are not re-seeded (files not overwritten).

### Plugin Identity Display

The CLI Agents page does not render a separate origin badge. Each card carries the identity in the
agent name's tooltip (`IdentityTooltip`): the recorded origin as a labelled line ("Origin: Built-in",
"Origin: Installed", or "Origin: Unknown"), followed by the descriptor's version, publisher, and
homepage when declared. Disabled plugins show the same tooltip with origin and version only.

The installed version or state is a chip on the name row. When an update is available, the
accent-styled update chip (`<installed> → <latest>`) replaces the state chip in the same slot, so one
chip at a time carries version information; the state chip returns when the update clears.

### Open Plugins Folder

The "Open Plugins Folder" command opens `%APPDATA%\CLIHub\plugins\` in Windows Explorer.

### Manual Catalog Reload

The "Reload Catalog" command triggers `IPluginCatalog.ReloadPlugins()`, rescanning the plugins directory and updating the UI without restarting the application.

## Remote Plugin Feed

CLIHub can browse and install plugins from a remote HTTPS index. By default, the official v2 feed at `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json` is used. The feed URL can be customized or disabled in Preferences; a custom URL pointing at a v1-style index keeps working end-to-end.

### Index Schema

The feed index is JSON with `schemaVersion` 1 or 2. CLIHub rejects indexes with a `schemaVersion` greater than 2.

Schema version 1 (the frozen v1 registry; still served to older clients and custom feeds):

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
      "signature": null  // reserved for future signing; never verified
    }
  ]
}
```

Schema version 2 (the default registry, CI-generated; ADR-0015):

```jsonc
{
  "schemaVersion": 2,
  "lastUpdated": "2026-11-14T01:55:08Z",  // optional
  "plugins": [
    {
      "id": "example-plugin",
      "name": "Example Plugin",
      "description": "An example plugin",
      "version": "1.0.0",
      "compatibility": ">=1.1.0 <2.0.0",   // constraint list: >= <= > < =, separated by commas and/or whitespace
      "channel": "stable",                  // only "stable" is listed; anything else skips the entry
      "publisher": "Community",
      "homepage": "https://example.com",
      "descriptor": {                        // inline plugin.json descriptor (required JSON object; id must match the entry id)
        "schemaVersion": 1,
        "id": "example-plugin",
        "name": "Example Plugin",
        "commands": { "launch": { "executable": "example" } }
      },
      "package": {                           // optional; without it the plugin installs descriptor-only
        "url": "https://example.com/example-plugin-1.0.0.clihub-plugin.zip",
        "sha256": "abc123..."
      },
      "logoUrl": "https://example.com/logo.png",  // optional
      "releaseNotesUrl": null,               // optional
      "signature": null                      // reserved for future signing; never verified
    }
  ]
}
```

Invalid v2 entries (missing required fields, malformed compatibility range, non-`stable` channel, invalid inline descriptor, descriptor/entry id mismatch, incomplete `package`) are skipped with a logged warning, like invalid v1 entries. The entry shape follows the index `schemaVersion`; the two schemas are never mixed within one index.

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
3. **Compatibility gating:** The current CLIHub version must satisfy the entry's compatibility range (v1 entries synthesize the floor `>=minAppVersion`; v2 entries carry the range directly; both are evaluated by the same numeric major.minor.patch constraint evaluator). Unsatisfied entries are shown with the Install/Update button disabled and a message — the v1 floor keeps the verbatim "Requires CLIHub <min> or newer." message; v2 entries show the raw range (e.g. "Requires CLIHub >=1.1.0 <2.0.0.").
4. **Package-backed install — Download → Inspect → Confirm → Install:** User clicks Install/Update. CLIHub downloads the package to a temporary file, verifies the SHA-256 checksum, inspects the package structure, and shows the same confirmation dialog as local installation (plugin name, version, publisher, homepage, every declared command). On confirmation, the package is installed through the same `IPluginInstaller` workflow as local packages.
5. **Descriptor-only install — Inspect → Confirm → Install:** For a v2 entry without a `package`, CLIHub builds the confirmation dialog directly from the inline descriptor — no download and no checksum step (the feed index in the git repository is the integrity anchor). On confirmation, the verbatim descriptor JSON (plus the `logoUrl` logo when it downloads successfully) is staged, re-validated from disk, and installed through the same backup/atomic-move/rollback/state/catalog-reload path as a package install. A failed or non-HTTPS logo fetch is logged as a warning and the install proceeds without `logo.png` (the default logo is shown).
6. **Install outcome:** Success or failure is shown in a dialog. On success, the catalog reloads and the plugin card reflects the new state.

### Background Feed Sync and Conditional Fetch

A background service (`IPluginFeedSyncService`, registered in Core) refreshes the feed once at application start and then hourly. It starts from `ApplicationBootstrapper` after the optional startup work and before the project scanner; `Start()` returns immediately and the first refresh runs on a background task, so startup never waits for the network. Shutdown cancels the in-flight refresh and waits at most the shutdown budget (2 seconds). The interval is fixed in this slice; a preference is future work.

Each refresh re-checks the `PluginFeedUrl` preference, so a disabled feed (`""`) is skipped without a request.

`PluginFeedClient.GetFeedAsync` performs conditional requests. The response `ETag` (or `Last-Modified` when there is no ETag) is stored in `%APPDATA%\CLIHub\cache\feed-sync.json` and sent back as `If-None-Match` (or `If-Modified-Since`) on the next fetch — including on every followed redirect hop. A `304 Not Modified` reports success from the cached index without re-parsing, without writing either cache file, and without publishing a change event.

Two cache files hold the last-good state, both under `%APPDATA%\CLIHub\cache\`:

- `feed-index.json`: the raw index bytes of the last successful fetch (bounded by the same 2 MB index limit), written atomically.
- `feed-sync.json`: a flat record `{ etag, lastModified, timestamp, indexHash }`. `indexHash` is the SHA-256 hash of the sorted `{ id, version, compatibility }` tuples and drives change detection; it is written with the validators and timestamp on every fresh 200 (so a cache populated by Browse alone is self-consistent) and rewritten by the sync service when it publishes a change.

Reads are tolerant: a missing cache means an unconditional fetch, a corrupt file is deleted with a logged warning, and sync metadata without its index is dropped. A cached index larger than 2 MB is never served.

When the feed changes, the service publishes `PluginFeedUpdatedEvent` carrying the new index hash (never the index itself). A 304, an unchanged index, a fetch served from the cache, and a failed fetch publish nothing. `AgentManagementViewModel` subscribes, reloads the catalog, and re-resolves the per-card plugin-update chip from the refreshed index; an agent version update takes the chip slot when both are available. Failures are logged at Warning and never thrown.

On a failed live fetch (network error, timeout, HTTP error status, parse error) the client serves the cached index with `FromCache: true` and `CachedAt`, so Browse degrades to stale-but-usable data instead of a hard error. Redirect-policy rejections and the 2 MB bound never fall back to stale data.

### Feed URL Preference

The `PluginFeedUrl` preference in `AppPreferences` controls the feed URL:

- **Absent or JSON `null`:** Use the default official URL (`https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json`).
- **Explicit empty string (`""`):** Feed is disabled. The "Browse Plugins" button is grayed out (disabled) on the CLI Agents page.
- **Non-empty HTTPS URL:** Use the custom feed URL. Non-HTTPS URLs are rejected at validation.

### Built-In ID Filtering

Feed entries whose `id` matches a built-in plugin ID (the set of IDs embedded in the current CLIHub build) are filtered out at load time and never shown in the Browse Plugins dialog. This prevents the feed from attempting to replace built-in plugins, which are managed by the seeding logic.

### Publishing Contract

The official v2 feed at `trustsoft/CLIHub-plugins-v2` is a CI-generated index of community plugins (ADR-0015). To publish a plugin there:

1. Add a per-plugin folder with the plugin's descriptor (and logo) under `plugins/<plugin-id>/`.
2. If the plugin ships files beyond the descriptor and logo, package them as a `.clihub-plugin.zip` following the format rules, host it at a public HTTPS URL, and record its SHA-256 hash in the entry's `package` object. Descriptor-only plugins omit `package` and install directly from the inline descriptor.
3. Declare the entry metadata: `id`, `name`, `description`, `version`, `compatibility` range, `channel: "stable"`, `publisher`, `homepage`, and optional `logoUrl`/`releaseNotesUrl`.
4. Open a pull request to `trustsoft/CLIHub-plugins-v2`. The maintainers review the plugin for safety and quality, and merge if approved; CI regenerates `index.json`.

The older `trustsoft/CLIHub-plugins` registry is frozen: it keeps serving already-shipped clients and historical custom feeds but receives no new plugins. The v1 publishing flow below is kept for those users:

1. Package your plugin as a `.clihub-plugin.zip` following the format rules.
2. Host the package at a public HTTPS URL (GitHub Releases, CDN, etc.).
3. Compute its SHA-256 hash (`certutil -hashfile <file> SHA256` on Windows, `shasum -a 256 <file>` on Unix).
4. Open a pull request to `trustsoft/CLIHub-plugins` adding your entry to `index.json`, including the package URL and SHA-256 hash.

### Error Handling

- **Network failure:** If the feed cannot be fetched and no cached index exists, the Browse Plugins dialog shows an error message and an empty plugin list. When a cached index exists, the list is shown with an inline stale-data notice ("Using cached plugin list (last updated …). Check your internet connection to refresh.") instead of the hard error.
- **Invalid index JSON:** If the index is malformed or has an unsupported `schemaVersion`, an error is shown (or the cached list with the stale-data notice when a cache exists).
- **Download failure:** If a package download fails, an error dialog is shown and installation is aborted.
- **Checksum mismatch:** If the downloaded package's SHA-256 does not match the index entry, an error is shown and the package is discarded without extraction.

## Plugin Errors

- **Invalid plugin.json:** skip during discovery, log a warning (with the missing field)
- **Duplicate plugin IDs:** load the first, skip duplicates, log a warning
- **Missing logo:** fall back to the default project logo
- **Disabled plugin:** not loaded by catalog, exposed via `GetDisabledPlugins()`, displayed in separate "Disabled" section
