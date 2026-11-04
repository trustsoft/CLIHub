# Curated Remote Plugin Feed — Design

## Verified current state

### Existing HTTP client and timeout pattern

`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:130-142` registers a shared `HttpClient` singleton:

```csharp
services.AddSingleton(_ =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CLIHub");
    return client;
});
services.AddSingleton<IAgentReleaseClient, AgentReleaseClient>();
```

`src/CLIHub.Core/Agents/AgentReleaseClient.cs:49-91` demonstrates the existing HTTP pattern:

- Shared `HttpClient` injected via DI
- User-Agent header: `"CLIHub"` (line 18)
- Timeout bound by agent probe preference via `CancellationTokenSource.CreateLinkedTokenSource` (line 62-63)
- Graceful degradation on timeout/network errors: returns `null`, logs warning, never throws unhandled exceptions (line 81-90)
- Tolerant JSON parsing with `JsonDocument` (line 137)

The probe timeout preference is read from `IPreferencesStore` (line 33, `preferencesStore` field).

### Plugin installer and state repository

`src/CLIHub.Core/Plugins/PluginInstaller.cs:1-100`:

- `IPluginInstaller.Inspect(packagePath)` validates a package and returns `PluginInstallPreview` without writing files (line 67-80)
- `IPluginInstaller.Install(packagePath, replaceExisting)` stages to `.staging-<guid>`, moves atomically, updates state, triggers catalog reload (line 83)
- Reserved built-in IDs: the set of IDs embedded in the current build, enumerated from the same resources `PluginSeeder` uses (no hardcoded list)
- Checksum verification: SHA-256 hashes of `plugin.json` and `logo.png` stored in `plugin-state.json` for drift detection

`src/CLIHub.Core/Plugins/PluginStateRepository.cs` (not read in detail, but referenced in installer):

- Manages `%APPDATA%\CLIHub\plugin-state.json`
- Atomic writes via temp-file-then-rename pattern (same as `ConfigurationRepository`)

### Current application version

`src/CLIHub.Core/Updates/UpdateService.cs:118-125`:

```csharp
public string GetCurrentVersion()
{
    if (this.Manager?.CurrentVersion is { } version)
    {
        return version.ToString();
    }
    return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
}
```

This provides the current CLIHub version for `minAppVersion` gating.

### UI organization and dialog services

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:1-100`:

- Owns CLI Agents management surface (line 15-18)
- Commands: `DetectCardCommand`, `CheckCardUpdatesCommand`, `UpdateCommand`, `EditConfigCommand` (line 97-100)
- Extends with plugin lifecycle commands: `InstallPluginCommand`, `DisablePluginCommand`, `EnablePluginCommand`, `OpenPluginsFolderCommand`, `ReloadCatalogCommand` (not shown, added by `installable-plugins`)
- Injected services: `IPluginCatalog`, `IPluginInstaller`, `IPluginDialogService`, `IPluginStateRepository` (line 31-42)

ADR-0005 (`docs/adr/0005-ui-dialog-boundaries.md:14-16`):

> Dialog seam: `IProjectDialogService` wraps the folder picker; `IUserNotificationService` wraps message boxes. View models depend on the interfaces; WPF implementations live beside the views.

The feed dialog will follow this pattern: an `IPluginDialogService` extension (or new interface) for the Browse Plugins modal, or integrate into the existing CLI Agents page as a panel.

### Configuration document boundary

ADR-0002 (`docs/adr/0002-single-configuration-document.md:17`):

> One document, one file: `AppConfigDocument` at `%APPDATA%\CLIHub\config.json`, camelCase JSON, is the only persisted configuration.

The feed URL preference (`pluginFeedUrl`) belongs in `config.json` because it is a user preference (like probe timeout or update behavior), not operational plugin state.

## Design decisions

### 1. Feed index schema

**Decision:** Versioned JSON schema served over HTTPS from a curated repository:

```jsonc
{
  "schemaVersion": 1,
  "lastUpdated": "2026-11-04T12:00:00Z",  // Informational, ISO 8601 UTC
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
      "sha256": "abc123...",  // Hex-encoded SHA-256 of the package file
      "releaseNotesUrl": "https://example.com/agent/releases/1.2.3",  // Optional
      "signature": null  // Reserved for future signing; always null in this change
    }
  ]
}
```

**Field requirements:**

- `schemaVersion` (int, required): feed format version; reject if absent or > 1
- `lastUpdated` (string, optional): informational timestamp, not validated or used for logic
- `plugins` (array, required): list of available plugins; reject index if absent or not an array
- Per entry:
  - `id`, `name`, `description`, `version`, `publisher`, `homepage`, `packageUrl`, `sha256`, `schemaVersion`, `minAppVersion` (all required)
  - `releaseNotesUrl` (optional)
  - `signature` (reserved, always null, no verification logic in this change)

**Validation rules:**

- Unknown fields tolerated (forward compatibility)
- Missing required fields: warn, skip entry, continue loading rest of index
- Duplicate plugin IDs: keep first, warn, skip later duplicates
- Index size limit: 2 MB (reasonable for hundreds of plugins with metadata)
- `packageUrl` and feed URL: HTTPS only, reject `http://` scheme

**Rationale:** The schema is simple, extensible, and mirrors the existing plugin descriptor shape (`id`, `name`, `version`, `publisher`, `homepage`). The `signature` field is reserved for a future signing change; including it now avoids a schema version bump later.

**Rejected alternative:** Separate metadata file per plugin — rejected because it would require multiple HTTP requests and complicate versioning. A single index file is simpler and faster to fetch.

### 2. Feed client interface and implementation

**Decision:** New `IPluginFeedClient` interface in `CLIHub.Core/Plugins/`:

```csharp
namespace CLIHub.Core.Plugins;

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
    /// <param name="packageUrl">HTTPS URL to the .clihub-plugin.zip file.</param>
    /// <param name="expectedSha256">Hex-encoded SHA-256 checksum from the feed index.</param>
    /// <returns>Download result with temporary file path or errors.</returns>
    Task<PluginDownloadResult> DownloadPackageAsync(string packageUrl, string expectedSha256, CancellationToken cancellationToken = default);
}

public record PluginFeedResult(
    bool Success,
    PluginFeedIndex? Index,
    IReadOnlyList<string> Errors);

public record PluginFeedIndex(
    int SchemaVersion,
    string? LastUpdated,
    IReadOnlyList<PluginFeedEntry> Plugins);

public record PluginFeedEntry(
    string Id,
    string Name,
    string Description,
    string Version,
    int SchemaVersion,
    string MinAppVersion,
    string Publisher,
    string Homepage,
    string PackageUrl,
    string Sha256,
    string? ReleaseNotesUrl,
    string? Signature);

public record PluginDownloadResult(
    bool Success,
    string? TempFilePath,
    IReadOnlyList<string> Errors);
```

**Implementation: `PluginFeedClient` in `CLIHub.Core/Plugins/`**

Constructor dependencies:
- `SocketsHttpHandler handler` (created with `AllowAutoRedirect = false` for manual redirect handling)
- `IPreferencesStore preferencesStore` (for probe timeout and feed URL preference)
- `ILogger<PluginFeedClient> logger`

The client creates its own `HttpClient` with the configured handler (not reusing the shared auto-redirecting client).

**HTTP behavior:**

- HTTPS-only enforcement:
  - Reject feed URL or `packageUrl` with `http://` scheme before making request (log error, return failure)
  - Manual redirect handling: detect 3xx responses, extract `Location` header, validate HTTPS scheme before following (up to 5 hops)
  - Reject redirects to non-HTTPS: if `Location` header starts with `http://`, reject (log error, return failure: "Redirect to non-HTTPS URL rejected")
- Timeout: reuse agent probe timeout from `AgentProbePolicy.ResolveTimeout(preferencesStore.Load().AgentProbeTimeoutSeconds)` (same pattern as `AgentReleaseClient`)
- User-Agent: `"CLIHub"` (already set on shared client, but guard to avoid duplication in tests like `AgentReleaseClient:43-46`)
- Response size limits:
  - Feed index: 2 MB max (`Content-Length` header or incremental read with threshold)
  - Package download: 10 MB max (consistent with `PluginInstaller.MaxArchiveSizeBytes`)
- Offline degradation: catch `HttpRequestException`, `OperationCanceledException`, `TaskCanceledException`, log warnings, return structured errors

**Checksum verification (`DownloadPackageAsync`):**

1. Download package to temp file: `Path.Combine(Path.GetTempPath(), $"clihub-plugin-{Guid.NewGuid():N}.zip")`
2. Compute SHA-256 hash: `SHA256.HashData(File.ReadAllBytes(tempFilePath))`, convert to hex string
3. Compare case-insensitive with `expectedSha256`
4. Match: return `PluginDownloadResult(Success: true, TempFilePath: tempFilePath, Errors: [])`
5. Mismatch: delete temp file, return `PluginDownloadResult(Success: false, TempFilePath: null, Errors: ["Package checksum mismatch..."])`
6. Exception: delete temp file in `finally`, return failure with error message

**Rationale:** The interface separates feed fetching from package downloading, allowing independent testing and error handling. The checksum verification is mandatory and happens in the feed client, not the installer, so tampered packages never reach the confirmation dialog.

**Rejected alternative:** Integrate feed client into `PluginInstaller` — rejected because the installer already has a clear single responsibility (local package installation). The feed client is a distinct HTTP/network concern.

### 3. Download and install workflow

**Decision:** User-initiated workflow from Browse Plugins UI:

**Sequence:**

1. User clicks "Browse Plugins…" button on CLI Agents page
2. ViewModel (`AgentManagementViewModel`) calls `IPluginFeedClient.GetFeedAsync()`
3. On success: populate feed list with Available / Installed / Update available states
4. On failure: show error message "Could not load plugin feed. Check your network connection." with Retry button
5. User selects a plugin, clicks Install or Update
6. ViewModel calls `IPluginFeedClient.DownloadPackageAsync(packageUrl, sha256)`
7. On checksum mismatch: show error dialog "Package checksum mismatch. The downloaded file may be corrupted or tampered with. Installation cancelled.", delete temp file, stop
8. On download success: ViewModel calls `IPluginInstaller.Inspect(tempFilePath)` (existing local installation flow)
9. Show confirmation dialog (existing: metadata, all declared commands with executables/arguments)
10. On confirm: ViewModel calls `IPluginInstaller.Install(tempFilePath, replaceExisting)` (existing local installation flow)
11. Delete temp file after install completes (success or failure, in `finally` block)

**Exception safety:**

```csharp
string? tempFilePath = null;
try
{
    var downloadResult = await this.feedClient.DownloadPackageAsync(packageUrl, sha256, ct);
    if (!downloadResult.Success)
    {
        ShowError(downloadResult.Errors);
        return;
    }
    tempFilePath = downloadResult.TempFilePath;
    
    // Inspect, confirm, install (existing flow)
    var preview = this.pluginInstaller.Inspect(tempFilePath);
    // ... confirmation dialog, install
}
finally
{
    if (tempFilePath != null && File.Exists(tempFilePath))
    {
        try { File.Delete(tempFilePath); }
        catch { /* Log but don't throw */ }
    }
}
```

**Rationale:** Reusing the existing `IPluginInstaller.Inspect` and `Install` methods leverages all existing validation, staging, rollback, and state management logic. The feed client adds HTTPS download and checksum verification as a pre-step.

**Rejected alternative:** Add feed-specific install path in installer — rejected because the installer's validation and atomicity are already comprehensive. Duplicating that logic for feed installs would create maintenance burden.

### 4. Update detection and version comparison

**Decision:** Numeric version comparison with fallback to opaque string equality:

```csharp
public class PluginFeedVersionComparator
{
    public static UpdateStatus Compare(string? installedVersion, string feedVersion)
    {
        if (installedVersion == null)
        {
            return UpdateStatus.Available; // Not installed. The caller distinguishes an installed
                                           // plugin whose version is null (UpdateAvailable) from a
                                           // plugin with no state entry at all (Available).
        }
        
        // Attempt numeric major.minor.patch comparison
        var installedParsed = TryParseVersion(installedVersion);
        var feedParsed = TryParseVersion(feedVersion);
        
        if (installedParsed.HasValue && feedParsed.HasValue)
        {
            var cmp = CompareVersions(installedParsed.Value, feedParsed.Value);
            if (cmp > 0)
            {
                return UpdateStatus.DifferentVersionAvailable; // Installed > feed (downgrade)
            }
            else if (cmp == 0)
            {
                return UpdateStatus.Current;
            }
            else
            {
                return UpdateStatus.UpdateAvailable; // Feed > installed (upgrade)
            }
        }
        
        // Fall back to string equality for non-numeric versions
        return string.Equals(installedVersion, feedVersion, StringComparison.Ordinal)
            ? UpdateStatus.Current
            : UpdateStatus.DifferentVersionAvailable;
    }
    
    private static (int Major, int Minor, int Patch)? TryParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length < 3)
            return null;
        
        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return null;
        }
        
        return (major, minor, patch);
    }
    
    private static int CompareVersions((int Major, int Minor, int Patch) a, (int Major, int Minor, int Patch) b)
    {
        if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
        return a.Patch.CompareTo(b.Patch);
    }
}

public enum UpdateStatus
{
    Available,                  // Not installed
    Current,                    // Installed, matches feed (up to date)
    UpdateAvailable,            // Installed < feed (upgrade available)
    DifferentVersionAvailable   // Installed > feed (downgrade) or non-comparable versions differ
}
```

**UI display:**

- Available: "Install" button, no installed version shown
- Current: "Installed" badge (green), installed version matches feed version
- UpdateAvailable: "Update" button, show installed version and feed version ("Installed: 1.0.0 → Available: 1.2.3")
- DifferentVersionAvailable: "Install" button (not "Update"), show both versions ("Installed: 2.0.0, Available: 1.0.0"), warning message "This is a different version (may be older)"

**Rationale:** Numeric comparison prevents offering "1.0.0" as an update when "2.0.0" is installed. Fallback to string equality handles non-numeric versions (e.g., "v1.2.3-beta", date-based versions). Labeling downgrades as "Different version available" avoids misleading users into thinking they are upgrading when they would be downgrading.

**Rejected alternative:** Parse as full SemVer with pre-release/build metadata precedence — rejected because not all agents use full SemVer (e.g., date-based versions like "2026-11-04"), and imposing full SemVer would break non-conforming feeds. Basic numeric comparison + fallback is the most general and least surprising behavior.

### 5. `minAppVersion` gating

**Decision:** Basic numeric version comparison (major.minor.patch):

```csharp
public class AppVersionComparator
{
    public static bool IsSatisfied(string currentVersion, string minAppVersion)
    {
        // Parse versions as "major.minor.patch" (ignore pre-release/build metadata)
        var current = ParseVersion(currentVersion);
        var required = ParseVersion(minAppVersion);
        
        if (current.Major != required.Major)
            return current.Major > required.Major;
        if (current.Minor != required.Minor)
            return current.Minor > required.Minor;
        return current.Patch >= required.Patch;
    }
    
    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        return (major, minor, patch);
    }
}
```

**UI gating:**

- Get current CLIHub version via `IUpdateVersionProvider.GetCurrentVersion()` (injected, same interface used by `UpdateService`)
- If `IsSatisfied(currentVersion, minAppVersion)` is false:
  - Show message: "Requires CLIHub {minAppVersion} or newer. Update CLIHub to install this plugin."
  - Disable Install button
  - Never attempt download or install

**Current version provider:** `IUpdateVersionProvider` (interface at `src/CLIHub.Core/Updates/IUpdateVersionProvider.cs`, implemented by `UpdateService` at `src/CLIHub.Core/Updates/UpdateService.cs:118`)

**Rationale:** CLIHub uses semantic versioning (major.minor.patch), so basic numeric comparison is sufficient. Parsing only the first three segments and ignoring pre-release/build metadata keeps the logic simple and predictable.

**Rejected alternative:** Full SemVer comparison with pre-release precedence — deferred because the current CLIHub versioning does not use pre-release tags in production builds. If pre-release tags become common, a future change can refine this.

### 6. UI integration: Browse Plugins dialog

**Decision:** Add "Browse Plugins…" button to CLI Agents settings page, opening a modal dialog or embedded panel.

**Entry point on CLI Agents page (XAML, next to existing commands):**

```xml
<Button Content="Browse Plugins..." Command="{Binding BrowsePluginsCommand}" />
```

**Dialog or panel structure (per ADR-0005: modal dialog with `IPluginDialogService` extension):**

- Title: "Browse Plugins"
- Feed status banner:
  - Loading: "Loading plugin feed…" with spinner
  - Loaded: "X plugins available"
  - Error: "Could not load plugin feed. Check your network connection." with Retry button
  - Empty: "No plugins available."
- Plugin list (ItemsControl or ListView):
  - Columns: Name, Version, Publisher, Status, Actions
  - Status badges: "Available", "Installed", "Update available"
  - Actions: Install button (available), Update button (update available), Details link (optional)
- Refresh button (re-fetch feed index)
- Close button

**ViewModel command (new in `AgentManagementViewModel`):**

```csharp
public ICommand BrowsePluginsCommand { get; }

// In constructor:
this.BrowsePluginsCommand = new RelayCommand(() => this.OpenBrowsePlugins());

private void OpenBrowsePlugins()
{
    // Open modal dialog via IPluginDialogService or embedded panel
    // Dialog ViewModel calls IPluginFeedClient.GetFeedAsync(), populates list
}
```

**Dialog ViewModel:**

```csharp
public class BrowsePluginsDialogViewModel : ObservableObject
{
    private readonly IPluginFeedClient feedClient;
    private readonly IPluginInstaller installer;
    private readonly IPluginStateRepository stateRepository;
    
    public ObservableCollection<FeedPluginItem> Plugins { get; }
    public ICommand RefreshCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand UpdateCommand { get; }
    
    // On open: await feedClient.GetFeedAsync(), populate Plugins with FeedPluginItem
    // FeedPluginItem: { Id, Name, Version, Publisher, Status, PackageUrl, Sha256 }
}
```

**Rationale:** A modal dialog (per ADR-0005) keeps the feed browsing workflow separate from the main CLI Agents list, which already shows installed plugins. The dialog can be dismissed without affecting the main settings page.

**Rejected alternative:** Embed feed list inline on CLI Agents page — rejected because the page is already dense with installed plugins, invalid plugins, disabled plugins, and commands. A separate dialog avoids clutter.

### 7. Feed URL configuration

**Decision:** Add optional `PluginFeedUrl` preference to `AppPreferences` and `PreferencesDocument`:

```csharp
// In AppPreferences (src/CLIHub.Core/Models/Configuration/AppConfig.cs):
public string? PluginFeedUrl { get; set; }

// In PreferencesDocument (src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs):
public string? PluginFeedUrl { get; set; }
```

**Configuration semantics:**
- Absent field or JSON `null` → use default official URL
- Explicit empty string `""` → feed disabled (Browse Plugins button grayed out or hidden)
- Non-empty string → use that custom URL (must be HTTPS)

**Default value provider:**

```csharp
public class FeedUrlProvider
{
    private const string DefaultFeedUrl = "https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json";
    
    public static string? GetFeedUrl(IPreferencesStore preferencesStore)
    {
        var config = preferencesStore.Load();
        var pluginFeedUrl = config.PluginFeedUrl;
        
        // Absent or null: default official URL
        if (pluginFeedUrl is null)
        {
            return DefaultFeedUrl;
        }
        
        // Explicit empty string: disabled
        if (string.IsNullOrWhiteSpace(pluginFeedUrl))
        {
            return null;
        }
        
        // Non-empty: custom URL
        return pluginFeedUrl;
    }
}
```

**Note:** The exact repository name (`CLIHub-plugins`) remains a low-risk open question for user confirmation before implementation.

**Rationale:** The feed URL is a preference (like probe timeout or update source), so it belongs in `config.json` per ADR-0002. Advanced users can override for testing or private feeds.

**Rejected alternative:** Hardcode feed URL in source — rejected because it prevents testing with local/staging feeds and makes switching to a different official feed require a code change.

### 8. Built-in descriptors and feed relationship

**Decision:** Feed lists only **non-reserved plugin IDs**. Built-in plugins (origin = "builtin" in state) are excluded from the feed and remain under application update control.

**Feed publishing rule:**

- Feed maintainers must not include plugin IDs that match any built-in descriptor embedded in CLIHub releases
- The set of reserved IDs is defined by the embedded resources `PluginSeeder` uses (`EmbeddedPluginResources.BuiltinPluginIds`, same source of truth as `PluginInstaller`)
- If a feed entry has a built-in ID, `PluginInstaller.InspectCore` will reject it at install time (existing reserved-ID check at `src/CLIHub.Core/Plugins/PluginInstaller.cs:342`)

**No override workflow:**

A reserved built-in plugin ID cannot be installed from the feed, even when the built-in is disabled. `PluginInstaller.InspectCore` rejects any package whose `id` matches a reserved ID, regardless of the built-in's `disabled` state in `plugin-state.json`. Users cannot replace built-ins with feed-sourced versions in this change.

**Rationale:** The three-hash seeding logic for built-ins is already robust for app updates and user modifications. Mixing feed updates with built-in seeding would require reconciling two update sources, checksums, and version precedence, adding complexity. Keeping them separate is cleaner. Allowing feed installation of reserved IDs would require changing `PluginInstaller.InspectCore` behavior (a MODIFIED `plugin-installation` requirement), which is out of scope for this change.

**Rejected alternative:** Allow feed updates for built-ins — rejected because it would require:
- Reconciling feed `packageUrl`/SHA-256 with embedded descriptor hashes
- Defining precedence (does feed override app update? does user modification block feed updates?)
- Extending three-hash logic to account for feed-sourced versions

This complexity is not justified for the initial feed release. If users want feed-sourced versions, they can disable the built-in and install from feed.

### 9. Publishing contract and curated repository

**Decision:** Specify a separate curated repository with immutable versioned packages and a manually/CI-maintained index.

**Repository details:**

- Repository: `trustsoft/CLIHub-plugins` (owner matches the existing CLIHub application repository `trustsoft/CLIHub`)
- Branch: `main` (hosts `index.json`)
- Feed index URL: `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`
- Package URLs: `https://github.com/trustsoft/CLIHub-plugins/releases/download/<tag>/<plugin-id>-<version>.clihub-plugin.zip`
- **Note:** The exact repository name (`CLIHub-plugins`) remains a low-risk open question for user confirmation before implementation

**Repository structure (not created in this change):**

```
CLIHub-plugins/
├── index.json              # Feed index on main branch
├── plugins/                # Source descriptors and logos (optional, for CI)
│   ├── example-agent/
│   │   ├── plugin.json
│   │   └── logo.png
└── README.md               # Publishing guide
```

**Releases:**

- Each plugin version is a GitHub Release asset: `<plugin-id>-<version>.clihub-plugin.zip`
- Release tag: `<plugin-id>-<version>` (e.g., `example-agent-1.2.3`)
- Immutable: once published, a version is never changed (republish with new version instead)
- SHA-256 checksums computed during CI build and written into `index.json`

**Publishing workflow (conceptual, not implemented in this change):**

1. Contributor submits PR with plugin descriptor and logo under `plugins/<id>/`
2. Maintainer reviews and approves
3. CI or manual script:
   - Creates `.clihub-plugin.zip` from `plugins/<id>/`
   - Computes SHA-256 checksum
   - Updates `index.json` on `main` branch with new entry or version bump (adds/updates plugin entry with id, name, version, packageUrl, sha256, etc.)
   - Creates GitHub Release with tag `<plugin-id>-<version>` and attaches package as release asset
   - Commits updated `index.json` to `main` branch
4. CLIHub users fetch updated `index.json` and see new plugin or update

**Rationale:** A separate repository keeps the feed independent of CLIHub application releases. Maintainers can publish plugin updates without waiting for a CLIHub release. Immutable packages with checksums ensure integrity.

**Rejected alternative:** Embed feed index in CLIHub application repo — rejected because it would tie plugin updates to CLIHub releases, defeating the purpose of a remote feed.

### 10. Testing strategy

**Core unit tests (new test classes):**

`PluginFeedClientTests`:
- `GetFeedAsync_ValidIndex_ParsesCorrectly`: valid JSON with all required fields returns success
- `GetFeedAsync_InvalidJson_ReturnsError`: malformed JSON returns structured error
- `GetFeedAsync_MissingSchemaVersion_ReturnsError`: index without `schemaVersion` rejected
- `GetFeedAsync_UnsupportedSchemaVersion_ReturnsError`: `schemaVersion` > 1 rejected
- `GetFeedAsync_UnknownFields_Tolerated`: extra fields in index do not fail parsing
- `GetFeedAsync_DuplicateIds_FirstKept`: duplicate plugin IDs, first kept, later skipped with warning
- `GetFeedAsync_IndexTooLarge_ReturnsError`: index > 2 MB rejected
- `GetFeedAsync_HttpUrl_Rejected`: feed URL with `http://` scheme rejected before request
- `GetFeedAsync_RedirectToHttp_Rejected`: HTTPS request redirected to `http://` rejected
- `GetFeedAsync_Timeout_ReturnsError`: network timeout returns error (mock or test timeout)
- `GetFeedAsync_Offline_ReturnsError`: network failure returns error, no throw
- `DownloadPackageAsync_ChecksumMatch_Success`: download with matching checksum returns temp file path
- `DownloadPackageAsync_ChecksumMismatch_DeletesFileAndReturnsError`: mismatch deletes temp file, returns error
- `DownloadPackageAsync_PackageTooLarge_ReturnsError`: package > 10 MB rejected
- `DownloadPackageAsync_HttpUrl_Rejected`: `packageUrl` with `http://` scheme rejected

`AppVersionComparatorTests`:
- `IsSatisfied_CurrentGreaterThanRequired_ReturnsTrue`: "1.2.0" >= "1.1.0"
- `IsSatisfied_CurrentEqualToRequired_ReturnsTrue`: "1.1.0" >= "1.1.0"
- `IsSatisfied_CurrentLessThanRequired_ReturnsFalse`: "1.0.0" < "1.1.0"
- `IsSatisfied_MajorVersion_TakesPrecedence`: "2.0.0" >= "1.99.99"

`PluginFeedVersionComparatorTests`:
- `Compare_NumericInstalledMatchesFeed_ReturnsCurrent`: numeric versions match returns `Current`
- `Compare_NumericInstalledLessThanFeed_ReturnsUpdateAvailable`: numeric installed < feed returns `UpdateAvailable` (upgrade)
- `Compare_NumericInstalledGreaterThanFeed_ReturnsDifferentVersionAvailable`: numeric installed > feed returns `DifferentVersionAvailable` (downgrade)
- `Compare_NonNumericInstalledMatchesFeed_ReturnsCurrent`: non-numeric versions match as strings returns `Current`
- `Compare_NonNumericInstalledDiffersFeed_ReturnsDifferentVersionAvailable`: non-numeric versions differ as strings returns `DifferentVersionAvailable`
- `Compare_InstalledNull_ReturnsAvailable`: no installed version returns `Available`

**ViewModel tests (extend existing `AgentManagementViewModelTests`):**

- `BrowsePluginsCommand_OpensDialog`: command opens Browse Plugins dialog
- `FeedLoad_Success_PopulatesList`: successful feed fetch populates plugin list
- `FeedLoad_Failure_ShowsError`: failed feed fetch shows error state
- `InstallFromFeed_DownloadConfirmInstall`: download, inspect, confirm, install workflow
- `UpdateFromFeed_DownloadConfirmReplace`: update workflow with `replaceExisting=true`
- `RefreshCommand_RefetchesFeed`: refresh re-fetches index and updates list

**Manual smoke task (see tasks.md for full checklist):**

1. Browse plugins, verify feed loads or shows offline error
2. Install a plugin from feed, verify confirmation shows all commands, verify installed
3. Bump feed version, refresh, verify "Update available" status
4. Update plugin, verify new version installed
5. Set feed to older version than installed, verify "Different version available" status with downgrade warning
6. Tamper with package (change byte), verify checksum mismatch error, no install
7. Set `PluginFeedUrl` to empty string `""`, verify Browse disabled
8. Test offline: disconnect, attempt browse, verify graceful error
9. Test `minAppVersion` gating: feed entry with high `minAppVersion`, verify Install disabled with message

## Security model

**Trust boundary:** The feed index and package URLs are served over HTTPS. The security model relies on:

1. **Transport security (HTTPS):** TLS prevents tampering in transit
2. **Checksum verification (SHA-256):** Detects corruption or tampering of downloaded packages
3. **No index signatures (this change):** The `signature` field is reserved but always `null`; no signature verification logic is implemented

**What the checksums provide:**

- **Integrity:** Detect if the downloaded package differs from what the feed index describes (corruption, network errors, or tampered package)
- **Immutability:** Once a package URL + checksum is in the feed, changing the package at that URL breaks the checksum

**What the checksums do NOT provide:**

- **Authenticity:** Checksums do not prove *who* created the package or the feed
- **Index tampering detection:** If the feed index itself is tampered with (including `packageUrl` and `sha256` fields), there is no signature to detect it in this change

**Future signing (out of scope):**

- A future change can add cryptographic signatures on the `index.json` and/or individual packages
- The `signature` field in the feed schema reserves space for this
- Signature verification would happen in `PluginFeedClient.GetFeedAsync` (verify index signature) and/or before checksum verification (verify package signature)

**Rationale:** HTTPS + SHA-256 checksums provide a baseline trust model suitable for a curated feed where the feed URL itself is trusted (official CLIHub feed or user-configured private feed). Adding signatures is valuable for defense in depth but is deferred to a future change to avoid scope creep.

## Deviations from brief

None. The design aligns with all approved product decisions in the brief.

## Open questions

1. **Exact repository name confirmation:**
   - **Question:** Confirm the exact repository name: `trustsoft/CLIHub-plugins` (with capital "CLI" and "Hub", and hyphen)?
   - **Impact:** Default feed URL constant, documentation references
   - **Current design uses:** `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`
   - **Low risk:** The name can be finalized during implementation without affecting the architecture

2. **Feed repository creation and initial population:**
   - **Question:** Who creates the repository and seeds the initial `index.json` with what plugins?
   - **Impact:** Smoke testing requires a live feed; if repo is not created before implementation, smoke task will use a local test feed
   - **Resolution path:** Implementation can use a local HTTPS test server or a fake `IPluginFeedClient`/HTTP handler for smoke testing; production feed URL is deployed post-implementation

3. **UI placement: modal dialog vs. embedded panel:**
   - **Question:** Should Browse Plugins be a modal dialog (per ADR-0005 pattern) or an embedded panel/tab on CLI Agents page?
   - **Current design:** Modal dialog (matches ADR-0005 folder picker pattern)
   - **Alternative:** Embedded panel or separate Settings tab (e.g., "Available Plugins" tab next to "Installed Plugins")
   - **Decision:** Modal dialog is simplest and matches existing dialog service pattern; embedding can be revisited in a future UX polish change

4. **Version comparison for update detection:**
   - **Question:** Should version comparison be basic numeric major.minor.patch (implemented design) or full SemVer with pre-release/build metadata precedence?
   - **Implemented design:** Attempt numeric major.minor.patch comparison when both versions parse; if either fails to parse, fall back to string equality (simple, handles both numeric and custom schemes, distinguishes upgrades from downgrades)
   - **Alternative:** Full SemVer precedence including pre-release and build metadata (e.g., "1.2.3-beta" < "1.2.3")
   - **Decision:** Basic numeric comparison + string fallback is sufficient for this change; full SemVer precedence can be added later if plugin publishers request it

## Follow-ups for backlog

Recorded in proposal; not repeated here.
