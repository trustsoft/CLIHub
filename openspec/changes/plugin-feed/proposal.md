# Curated Remote Plugin Feed

## Why

Users with local plugin installation (the `installable-plugins` change) can now install community or proprietary agent descriptors from `.clihub-plugin.zip` packages, but discovering and downloading those packages remains manual. Users must locate packages on third-party sites, verify integrity themselves, and manually download each package. There is no in-app browsing, no update detection for installed plugins, and no trusted distribution channel for maintainer-published updates.

This prevents users from:

- Browsing available plugins from a curated feed within CLIHub
- Installing plugins with one click after reviewing metadata and commands
- Discovering updates to installed plugins without visiting external sites
- Trusting that downloaded packages have not been tampered with (HTTPS integrity)

## What changes

### 1. Feed index contract

A curated `index.json` served over HTTPS from a maintainer-controlled repository:

```jsonc
{
  "schemaVersion": 1,
  "lastUpdated": "2026-11-04T12:00:00Z",
  "plugins": [
    {
      "id": "example-agent",
      "name": "Example Agent",
      "description": "AI coding assistant",
      "version": "1.2.3",
      "schemaVersion": 1,
      "minAppVersion": "1.1.0",
      "publisher": "Community",
      "homepage": "https://example.com/agent",
      "packageUrl": "https://github.com/trustsoft/CLIHub-plugins/releases/download/example-agent-1.2.3/example-agent-1.2.3.clihub-plugin.zip",
      "sha256": "abc123...",
      "releaseNotesUrl": "https://example.com/agent/releases/1.2.3",
      "signature": null  // Reserved for future signing; always null in this change
    }
  ]
}
```

**Index schema details:**
- `schemaVersion` (int): feed format version (1 in this change)
- `lastUpdated` (string, ISO 8601 UTC): informational timestamp of the index build
- `plugins` (array): list of available plugins
  - `id` (string, required): plugin identifier matching the package descriptor
  - `name`, `description`, `version`, `publisher`, `homepage` (strings, required): display metadata
  - `schemaVersion` (int, required): descriptor schema version the package uses
  - `minAppVersion` (string, required): minimum CLIHub version required to install (e.g., "1.1.0")
  - `packageUrl` (string, required): HTTPS URL to download the `.clihub-plugin.zip`
  - `sha256` (string, required): hex-encoded SHA-256 checksum of the package file
  - `releaseNotesUrl` (string, optional): URL to release notes or changelog
  - `signature` (string or null, reserved): reserved for a future signature verification change; always null, no verification logic in this change

**Validation:**
- Reject index if `schemaVersion` is absent or greater than 1
- Reject index if `plugins` array is absent or not an array
- Tolerate unknown fields (forward compatibility)
- Warn and skip entries with missing required fields; do not fail the entire index
- Reject index larger than 2 MB (reasonable upper bound for hundreds of plugins)
- Duplicate plugin IDs in the index: keep the first occurrence, warn and skip later duplicates

### 2. Feed client in Core

A new `IPluginFeedClient` interface and `PluginFeedClient` implementation in `CLIHub.Core/Plugins/`:

```csharp
public interface IPluginFeedClient
{
    /// <summary>
    /// Fetches and parses the plugin feed index from the configured URL.
    /// </summary>
    /// <returns>Feed result with index or errors.</returns>
    Task<PluginFeedResult> GetFeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a plugin package to a temporary file and verifies its checksum.
    /// </summary>
    /// <returns>Download result with temporary file path or errors.</returns>
    Task<PluginDownloadResult> DownloadPackageAsync(string packageUrl, string expectedSha256, CancellationToken cancellationToken = default);
}
```

**HTTP client behavior:**
- Create a dedicated `HttpClient` for the feed with a `SocketsHttpHandler` configured with `AllowAutoRedirect = false` (manual redirect handling to enforce HTTPS at every hop)
- Manual redirect following: up to 5 hops, validate HTTPS scheme on every `Location` header before following
- Apply the agent probe timeout preference (`AgentProbeTimeoutSeconds`) resolved through `AgentProbePolicy.ResolveTimeout`
- User-Agent: `CLIHub`
- HTTPS only: reject `packageUrl` or feed URL with `http://` scheme before sending request; log error and fail
- Reject HTTP redirects to non-HTTPS URLs: if a `Location` header has `http://` scheme, reject the request with an error "Redirect to non-HTTPS URL rejected"
- Response size limit: 2 MB for index, 10 MB for package (consistent with local package installer)
- Timeout: use existing agent probe timeout preference from `IPreferencesStore` (via linked `CancellationTokenSource`)
- Offline degradation: network failures return errors, never throw unhandled exceptions; log warnings

**Checksum verification:**
- Compute SHA-256 hash of the downloaded package file
- Compare hex-encoded hash (case-insensitive) with `expectedSha256` from index
- Mismatch: delete temp file, return error describing mismatch
- Match: return temp file path for caller to install

**Error handling:**
- Network failures, timeouts, HTTP errors: structured error messages returned in results (never thrown)
- Invalid JSON: parse error returned
- Checksum mismatch: hard error (user notification), temp file deleted

### 3. Download and install flow

User workflow from feed UI:

1. User opens "Browse Plugins" dialog (new entry point on CLI Agents page or dedicated button per ADR-0005)
2. ViewModel calls `GetFeedAsync()` to fetch the index
3. Display available/installed/update states in a list:
   - **Available**: plugin in feed but not installed
   - **Installed**: installed version matches feed version
   - **Update available**: installed version differs from feed version
4. User selects a plugin and clicks Install/Update
5. ViewModel calls `DownloadPackageAsync(packageUrl, sha256)`
6. On success: ViewModel calls `IPluginInstaller.Inspect(tempFilePath)` to show confirmation dialog
7. User confirms installation
8. ViewModel calls `IPluginInstaller.Install(tempFilePath, replaceExisting=true/false)`
9. Temp file is deleted after install completes (success or failure)

**Checksum mismatch handling:**
- Display error dialog: "Package checksum mismatch. The downloaded file may be corrupted or tampered with. Installation cancelled."
- Delete temp file immediately
- Do not proceed to confirmation or installation

**Exception safety:**
- Delete temp file in a `finally` block after download, regardless of success/failure/exception

### 4. Update detection

Compare installed plugin versions with feed versions to show "Update available" or "Different version available" state:

**Version comparison:**
- Attempt to parse both installed and feed versions as major.minor.patch numeric segments (split on `.`, parse each segment as integer)
- If both versions parse successfully:
  - Compare major, then minor, then patch numerically (e.g., "1.10.0" > "1.9.5", "2.0.0" > "1.99.99")
  - If feed version > installed version: "Update available"
  - If feed version == installed version: "Installed (up to date)"
  - If feed version < installed version: "Different version available" (show both versions; do not offer "Update" button, offer "Install" with warning that it is a downgrade)
- If either version does not parse as major.minor.patch (contains non-numeric segments, missing dots, etc.):
  - Fall back to case-sensitive string equality
  - If strings match: "Installed (up to date)"
  - If strings differ: "Different version available" (show both versions)
- If installed plugin has no `version` field (null): treat as "unknown version", show feed version as available
- If feed entry has no matching installed plugin id: "Available" for install
- If local installed plugin has no feed entry: no update detection (installed-only plugin)

**Rationale:** Numeric comparison prevents offering "1.0.0" as an update when "2.0.0" is installed. Fall back to string equality for non-numeric versions (e.g., "v1.2.3-beta", custom version schemes). Label downgrades as "Different version available" to avoid misleading users.

**SemVer precedence not required:** Full SemVer pre-release and build metadata precedence is out of scope. A future change may add it if needed.

### 5. `minAppVersion` gating

Before allowing installation from feed:

- Read current CLIHub application version (existing `UpdateWorkflow.CurrentVersion` or equivalent)
- Compare with feed entry's `minAppVersion` using basic string-prefix comparison (e.g., "1.1.0" requires CLIHub >= "1.1.0")
- If current version is older than `minAppVersion`: show "Requires CLIHub [minAppVersion] or newer. Update CLIHub to install this plugin." in the feed UI; disable Install button
- Never attempt to download or install a plugin with unsatisfied `minAppVersion`

**Version comparison for gating:**
- Split on `.` and compare numeric segments left-to-right (major, minor, patch)
- If current version >= required version: allow install
- Otherwise: gate with message

### 6. UI integration

**New "Browse Plugins" dialog or panel (per ADR-0005):**

- Entry point: a new button "Browse Plugins…" on the CLI Agents settings page, beside "Install Plugin…" and "Open Plugins Folder"
- Modal dialog or side panel showing:
  - Feed status: loading, loaded (X plugins available), offline/error
  - Plugin list with columns/rows:
    - Name, version, publisher, status badge (Available / Installed / Update available), Install/Update button
  - Per-row actions:
    - **Install** (available plugins): download, confirm, install
    - **Update** (installed with feed update): download, confirm, install with replaceExisting=true
    - **Details** (optional): show description, homepage link, release notes link
  - Refresh button: re-fetch feed index
  - Close button

**Status badges reuse existing pattern:**
- Leverage `AgentCardItem` / `DisabledAgentCardItem` display patterns where applicable
- Feed-specific states: "Available", "Installed (up to date)", "Update available"

**Loading and error states:**
- Loading: "Loading plugin feed…" with spinner
- Offline/error: "Could not load plugin feed. Check your network connection." with Retry button
- Empty feed: "No plugins available."

**Confirmation dialog reuse:**
- Reuse the existing `IPluginInstaller.Inspect` confirmation workflow (show all declared commands with executables/arguments, metadata, replacement status)
- After download and checksum verification, pass the temp file path to `Inspect`, then to `Install`

### 7. Feed URL configuration

**Default official feed URL:**
- Default constant in code: `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`
- Separate repository: `trustsoft/CLIHub-plugins` (owner matches the existing CLIHub application repository owner)
- **Note:** The exact repository name remains a low-risk open question for user confirmation before implementation

**Configuration:**
- Add optional preference field in `config.json`: `pluginFeedUrl` (string, nullable)
- If absent or JSON `null`: use default official URL
- If present and non-empty: use custom URL (allows local testing or private feeds)
- If present and empty string `""`: disable feed (Browse Plugins button grayed out or hidden, no feed fetching)
- UI for override: not required in this change; advanced users edit `config.json` directly

**Configuration semantics:**
- Absent field or explicit JSON `null` → default official feed URL
- Explicit empty string `""` → feed disabled
- Non-empty string → custom feed URL (must be HTTPS)

### 8. Built-in descriptors and feed relationship

**Decision:** The feed installs/updates **non-reserved plugin IDs only**. Built-in plugins (origin = "builtin" in state file) remain under application update control and are **not** listed in the feed or updated via feed.

**Rationale:**
- Built-in seeding uses three-hash logic that already handles app updates, user modifications, and disabled state
- Allowing feed updates for built-ins would require reconciling feed `packageUrl`/SHA-256 with embedded three-hash policy, adding complexity and potential conflicts
- Reserved built-in IDs cannot be installed from the feed: `PluginInstaller.InspectCore` rejects any package whose `id` matches a reserved ID (defined by `EmbeddedPluginResources.BuiltinPluginIds`), regardless of the built-in's disabled state

**Scope:**
- Feed lists only non-built-in agent plugins
- Built-in plugin IDs remain reserved and cannot be installed from the feed per existing `PluginInstaller` logic (`PluginInstaller.cs:342`)
- No workflow exists to replace a built-in with a feed-sourced version in this change; such a feature would require a MODIFIED `plugin-installation` requirement and is out of scope

### 9. Publishing contract and curated repository

**Curated repository design (separate from CLIHub application repo):**

- Repository: `trustsoft/CLIHub-plugins` (owner matches the existing CLIHub application repository `trustsoft/CLIHub`)
- Branch: `main` (hosts `index.json`)
- Feed index URL: `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`
- Immutable versioned packages: release assets on GitHub Releases, named `<plugin-id>-<version>.clihub-plugin.zip`
- Package URLs: `https://github.com/trustsoft/CLIHub-plugins/releases/download/<tag>/<plugin-id>-<version>.clihub-plugin.zip`
- SHA-256 checksums: generated and written into `index.json` during publish (manual or CI)
- **Note:** The exact repository name (`CLIHub-plugins`) remains a low-risk open question for user confirmation before implementation

**Publishing process (conceptual, not implemented in this change):**

1. Maintainer reviews and approves a plugin submission (PR with descriptor + logo)
2. CI or manual script builds the `.clihub-plugin.zip` from approved descriptor
3. CI computes SHA-256 checksum of the package
4. CI updates `index.json` on `main` branch with new entry or version bump (adds/updates plugin entry with id, name, version, packageUrl, sha256, etc.)
5. CI creates GitHub Release with tag `<plugin-id>-<version>` and attaches package as release asset
6. Updated `index.json` is committed to `main` branch

**This change specifies the contract and documents the publishing workflow as a task; creating the repository and CI workflow is a follow-up outside this change.**

### 10. Tests

**Core unit tests:**

- `PluginFeedClientTests`:
  - Valid index parsing
  - Invalid JSON rejected
  - Unknown fields tolerated
  - `schemaVersion` > 1 rejected
  - Duplicate plugin IDs: first kept, later skipped
  - Index size limit enforced (> 2 MB rejected)
  - HTTPS-only enforcement (http:// URLs rejected)
  - Redirect to http:// rejected
  - Network timeout (via mock or timeout config)
  - Offline degradation (returns error, no throw)
  - Package download with checksum match succeeds
  - Package download with checksum mismatch deletes temp file and returns error

- `PluginFeedVersionComparatorTests`:
  - String equality comparison for versions
  - `minAppVersion` gating logic (basic version comparison)

**ViewModel tests:**

- `AgentManagementViewModelTests` (extended):
  - Browse plugins command opens feed dialog
  - Feed load success populates list
  - Feed load failure shows error state
  - Install from feed: download, inspect, confirm, install workflow
  - Update from feed: download, inspect, confirm, replace workflow
  - Refresh command re-fetches index

**Manual smoke task:**

1. Start CLIHub, open CLI Agents page, click "Browse Plugins"
2. Verify feed loads (or shows offline error if no network)
3. Select an available plugin, click Install, verify confirmation dialog shows metadata and all declared commands
4. Confirm install, verify plugin appears in CLI Agents list with "Installed" badge
5. Modify `index.json` (bump plugin version), refresh feed, verify "Update available" status
6. Click Update, verify confirmation, verify updated version installed
7. Set feed to older version than installed, verify "Different version available" status with downgrade warning
8. Tamper with a package file (change one byte), attempt install, verify checksum mismatch error and no installation
9. Set `config.json` `PluginFeedUrl` to empty string `""`, restart, verify Browse Plugins button grayed out or hidden
10. Test offline: disconnect network, attempt Browse, verify graceful "Could not load plugin feed" error (no crash)
11. Test `minAppVersion` gating: create a feed entry with `minAppVersion` higher than current CLIHub version, verify Install button disabled with "Requires CLIHub X.Y.Z or newer" message

### 11. Documentation

- `docs/architecture/plugins.md`: add "Remote Plugin Feed" section describing feed index schema, download/install flow, update detection, `minAppVersion` gating, and publishing contract
- `docs/project-context.md`: update runtime facts if new paths/files are introduced (temp download location, feed cache if applicable)
- Spec delta under `openspec/changes/plugin-feed/specs/plugin-feed/spec.md` (new capability)
- No MODIFIED requirements for `plugin-installation` or `plugin-seeding` (feed is additive, does not change local installation or seeding behavior)

## Impact

### Breaking changes

None. The feed is an additive feature. Existing local installation, built-in seeding, and plugin management continue to work unchanged. The feed is opt-in (users must click Browse Plugins to interact with it).

### Migration

No migration needed. Feed URL configuration is optional and defaults to the official URL. If feed is unreachable, the application degrades gracefully (error message in Browse Plugins dialog).

### Rollout

Single-phase release: all feed features deploy together. The feed client, UI, and version comparison are cohesive and tested as one unit.

## Explicit non-goals

- **Open marketplace:** This is a curated feed where maintainers publish plugins, not a self-service marketplace. Users cannot publish directly; plugins are reviewed and approved by maintainers.
- **Index signatures / certificate pinning:** SHA-256 checksums over HTTPS provide transport security and integrity verification. Cryptographic signatures on `index.json` or individual packages are reserved for a future change (the `signature` field is present in the schema but always `null` and never verified in this change).
- **Automatic background updates:** Update detection is explicit (user opens Browse Plugins and sees "Update available"). No silent polling or auto-install.
- **Agent CLI installation:** Feed installs descriptors only, never agent CLI binaries. The agent CLI must be installed separately by the user (per existing vision).
- **SemVer-aware version comparison:** Version comparison attempts numeric major.minor.patch parsing for both plugin versions (update detection) and `minAppVersion` (gating). When both versions parse, numeric comparison distinguishes upgrades from downgrades. When either fails to parse, fall back to string equality. Full SemVer parsing with pre-release/build metadata precedence is a future enhancement if needed.
- **Per-project plugin overrides:** Feed plugins are global (per `%APPDATA%\CLIHub\`), not per-project.
- **Plugin dependency graphs:** Plugins are independent; no dependencies or install order constraints.
- **Feed caching / offline mode:** Feed is fetched on-demand when Browse Plugins opens. No persistent cache or offline fallback to last-known index in this change.

## Dependencies

- Depends on `installable-plugins` (archived): `IPluginInstaller`, `plugin-state.json`, three-hash seeding, disable/enable, state repository
- No other proposed changes are required

## Follow-ups

Candidates for `docs/backlog.md`:

- Cryptographic signatures on `index.json` and packages (verify publisher authenticity)
- Automatic update detection on startup (background check for updates to installed plugins)
- Feed caching and offline mode (persist last-known feed, show stale data with timestamp)
- SemVer-aware version comparison for update precedence
- Export installed plugin to shareable `.clihub-plugin.zip` (reverse of install)
- In-app submission workflow for community plugins (PR creation from CLIHub UI)
- Multiple feed sources (community feed + enterprise feed, merge entries)
