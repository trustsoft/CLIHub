# Plugin Feed Implementation Tasks

## Stage 1: Feed client and core infrastructure

- [x] 1.1 Add `IPluginFeedClient` interface in `CLIHub.Core/Plugins/` with `GetFeedAsync` and `DownloadPackageAsync` methods
- [x] 1.2 Add result types: `PluginFeedResult`, `PluginFeedIndex`, `PluginFeedEntry`, `PluginDownloadResult` in `CLIHub.Core/Models/` or `CLIHub.Core/Plugins/`
- [x] 1.3 Implement `PluginFeedClient` in `CLIHub.Core/Plugins/`:
  - [x] Create dedicated `SocketsHttpHandler` with `AllowAutoRedirect = false`, manual redirect following (max 5 hops), HTTPS validation on every hop
  - [x] Constructor with the dedicated handler, `IPreferencesStore`, `IUpdateVersionProvider`, `ILogger<PluginFeedClient>`
  - [x] `GetFeedAsync`: fetch index, HTTPS-only enforcement, 2 MB limit, timeout via `AgentProbePolicy.ResolveTimeout`, tolerant JSON parse, duplicate ID handling, warn and skip invalid entries
  - [x] `DownloadPackageAsync`: download to temp file, SHA-256 checksum verification, 10 MB limit, delete temp file on mismatch/exception
- [x] 1.4 Add `AppVersionComparator` utility class in `CLIHub.Core/Plugins/` with `IsSatisfied(currentVersion, minAppVersion)` method (basic major.minor.patch numeric comparison)
- [x] 1.5 Add `PluginFeedVersionComparator` utility class in `CLIHub.Core/Plugins/` with `Compare(installedVersion, feedVersion)` method (numeric comparison with fallback to string equality, returns `Current`/`UpdateAvailable`/`DifferentVersionAvailable`)
- [x] 1.6 Register `IPluginFeedClient` as singleton in `ServiceCollectionExtensions.AddPluginServices`

**Gate:** Build passes, no compilation errors.

## Stage 2: Configuration and preferences

- [x] 2.1 Add `PluginFeedUrl` property (string?, nullable) to `AppPreferences` in `src/CLIHub.Core/Models/Configuration/AppConfig.cs`
- [x] 2.2 Add `PluginFeedUrl` property (string?, nullable) to `PreferencesDocument` in `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`
- [x] 2.3 Update `PreferencesDocument.From(AppPreferences)` to map `PluginFeedUrl` field
- [x] 2.4 Update `PreferencesDocument.ToPreferences()` to map `PluginFeedUrl` field
- [x] 2.5 Update `ConfigurationSnapshot` in `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` to include `PluginFeedUrl` field and conversions
- [x] 2.6 Add `FeedUrlProvider` utility in `CLIHub.Core/Plugins/` with default URL constant `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json` and `GetFeedUrl(IPreferencesStore)` method (absent/null → default official URL, empty string → feed disabled, non-empty → custom HTTPS URL)
- [x] 2.7 Add configuration round-trip test in `tests/CLIHub.Core.Tests/Configuration/` verifying `PluginFeedUrl` field persists correctly
- [x] 2.8 Update `docs/architecture/configuration.md` with `PluginFeedUrl` preference description (nullable, absent/null → default, empty → disabled, non-empty → custom URL)

**Gate:** Configuration loads and saves with `PluginFeedUrl` field; round-trip test passes.

## Stage 3: Feed client tests

- [x] 3.1 Create `PluginFeedClientTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [x] Valid index parsing with all required fields
  - [x] Invalid JSON rejected with structured error
  - [x] Missing `schemaVersion` rejected
  - [x] `schemaVersion` > 1 rejected
  - [x] Unknown fields tolerated
  - [x] Entry with missing required fields: entry skipped with warning, rest of index loaded
  - [x] Duplicate plugin IDs: first kept, later skipped with warning
  - [x] Index > 2 MB rejected
  - [x] `http://` feed URL rejected before request
  - [x] Redirect to `http://` rejected (Location header validation during manual redirect following)
  - [x] Network timeout returns error (mock or test timeout)
  - [x] Offline scenario returns error, no throw
  - [x] Package download with checksum match returns temp file path
  - [x] Package download with checksum mismatch deletes temp file and returns error
  - [x] Package > 10 MB rejected
  - [x] `http://` package URL rejected
- [x] 3.2 Create `AppVersionComparatorTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [x] Current >= required returns true ("1.2.0" >= "1.1.0", "1.1.0" >= "1.1.0")
  - [x] Current < required returns false ("1.0.0" < "1.1.0")
  - [x] Major version precedence ("2.0.0" >= "1.99.99")
- [x] 3.3 Create `PluginFeedVersionComparatorTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [x] Numeric versions: installed == feed returns `Current`
  - [x] Numeric versions: installed < feed returns `UpdateAvailable`
  - [x] Numeric versions: installed > feed returns `DifferentVersionAvailable` (downgrade case)
  - [x] Non-numeric versions: string match returns `Current`
  - [x] Non-numeric versions: string differ returns `DifferentVersionAvailable`
  - [x] Installed version null returns `UpdateAvailable`

**Gate:** All feed client tests pass (`dotnet test CLIHub.sln --filter "FullyQualifiedName~PluginFeed"`).

## Stage 4: UI integration - Browse Plugins dialog

- [x] 4.1 Create `BrowsePluginsDialogViewModel` in `CLIHub/ViewModels/Agents/`:
  - [x] Dependencies: `IPluginFeedClient`, `IPluginInstaller`, `IPluginStateRepository`, `IUpdateVersionProvider`, `ILogger`
  - [x] Properties: `ObservableCollection<FeedPluginItem> Plugins`, `bool IsLoading`, `string? ErrorMessage`, `bool HasError`
  - [x] Commands: `RefreshCommand`, `InstallCommand`, `UpdateCommand`, `CloseCommand`
  - [x] `LoadFeedAsync()`: fetch feed, populate Plugins with Available/Installed/UpdateAvailable/DifferentVersionAvailable status
  - [x] `InstallPluginAsync(FeedPluginItem)`: download, inspect, confirm, install workflow with temp file cleanup in `finally`
  - [x] `UpdatePluginAsync(FeedPluginItem)`: download, inspect, confirm, install with `replaceExisting=true`, temp file cleanup
- [x] 4.2 Create `FeedPluginItem` model in `CLIHub/ViewModels/Agents/`:
  - [x] Properties: Id, Name, Version, Publisher, Homepage, Description, PackageUrl, Sha256, Status (enum: Available/Installed/UpdateAvailable/DifferentVersionAvailable), InstalledVersion (nullable), MinAppVersion, CanInstall (bool, false if `minAppVersion` unsatisfied), IsDowngrade (bool, true if status == DifferentVersionAvailable and numeric comparison shows installed > feed)
- [x] 4.3 Create `BrowsePluginsDialog.xaml` and code-behind in `CLIHub/Views/`:
  - [x] Title: "Browse Plugins"
  - [x] Feed status banner (loading/loaded/error with Retry button)
  - [x] Plugin list (ListView or DataGrid) with columns: Name, Version, Publisher, Status, Actions
  - [x] Install/Update buttons per row (disabled if `CanInstall` is false; Update button only shown for UpdateAvailable status)
  - [x] For DifferentVersionAvailable status: show "Install" button with warning text "Different version (may be older)" and both versions
  - [x] Details link or tooltip showing description, homepage, release notes URL
  - [x] Refresh button
  - [x] Close button
- [x] 4.4 Extend `IPluginDialogService` in `CLIHub/Launching/` with `ShowBrowsePluginsDialog()` method
- [x] 4.5 Implement `ShowBrowsePluginsDialog()` in `PluginDialogService` (open `BrowsePluginsDialog` as modal per ADR-0005)
- [x] 4.6 Add `BrowsePluginsCommand` to `AgentManagementViewModel` in `CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` (calls `pluginDialogs.ShowBrowsePluginsDialog()`)
- [x] 4.7 Add "Browse Plugins…" button to `SettingsAgentsPage.xaml` bound to `BrowsePluginsCommand`
- [x] 4.8 Register `BrowsePluginsDialogViewModel` in DI if needed (or construct in dialog code-behind)
- [x] 4.9 Add XAML binding test in `tests/CLIHub.Tests/Views/` for Browse Plugins entry point (matching `SettingsAgentsPageBindingTests` pattern)

**Gate:** Browse Plugins dialog opens from CLI Agents page (may show error if feed not reachable yet); binding test passes.

## Stage 5: ViewModel tests

- [x] 5.1 Create `BrowsePluginsDialogViewModelTests` in `tests/CLIHub.Tests/ViewModels/Agents/`:
  - [x] Feed load success populates Plugins list
  - [x] Feed load failure shows error message
  - [x] Refresh command re-fetches feed
  - [x] Install command: download, inspect, confirm, install (mock dependencies)
  - [x] Update command: download, inspect, replace (mock dependencies)
  - [x] Checksum mismatch shows error, no install
  - [x] `minAppVersion` unsatisfied: `CanInstall` false, Install button disabled
  - [x] DifferentVersionAvailable status: downgrade warning shown, Install (not Update) button enabled
- [x] 5.2 Extend `AgentManagementViewModelTests` in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`:
  - [x] `BrowsePluginsCommand` opens dialog (verify `IPluginDialogService.ShowBrowsePluginsDialog` called)

**Gate:** All ViewModel tests pass (`dotnet test CLIHub.sln --filter "FullyQualifiedName~BrowsePlugins"`).

## Stage 6: Documentation

- [x] 6.1. Update `docs/architecture/plugins.md`:
  - [x] Add "Remote Plugin Feed" section after "Plugin Installation"
  - [x] Describe feed index schema, download/install flow, update detection, `minAppVersion` gating
  - [x] Document publishing contract: repository structure, release workflow, SHA-256 generation
  - [x] Note: repository name `trustsoft/CLIHub-plugins` is documented default, exact name subject to confirmation
- [x] 6.2. Update `docs/project-context.md` if new runtime paths introduced (temp download location, feed cache if applicable; likely no changes needed)
- [x] 6.3. Document publishing contract as separate task or note: specify `trustsoft/CLIHub-plugins` repository structure, `index.json` on `main` branch, release asset naming `<plugin-id>-<version>.clihub-plugin.zip`, tag naming `<plugin-id>-<version>`, SHA-256 generation on publish, immutable packages

**Gate:** Documentation builds and renders correctly.

## Stage 7: Spec delta

- [x] 7.1 Keep the existing `openspec/changes/plugin-feed/specs/plugin-feed/spec.md` delta in sync with implementation if deviations occur during build/test

**Gate:** Spec validates with `openspec validate openspec/changes/plugin-feed/`.

## Stage 8: Manual smoke testing

- [x] 8.1 Create test feed `index.json` with sample plugins and serve via local HTTPS test server (e.g., using a self-signed cert or a fake `IPluginFeedClient` for testing)
- [x] 8.2 Set `PluginFeedUrl` to test feed URL in `config.json`
- [x] 8.3 Start CLIHub, open CLI Agents page, click "Browse Plugins"
- [x] 8.4 Verify feed loads and displays plugins with correct status (Available/Installed/UpdateAvailable/DifferentVersionAvailable)
- [x] 8.5 Select an available plugin, click Install
- [x] 8.6 Verify confirmation dialog shows name, version, publisher, homepage, and all declared commands (launch, resume, version, update) with executables/arguments
- [x] 8.7 Confirm install, verify plugin appears in CLI Agents list with "Installed" badge
- [x] 8.8 Bump test feed plugin version, click Refresh in Browse Plugins dialog
- [x] 8.9 Verify plugin shows "Update available" status with old and new versions
- [x] 8.10 Click Update, verify confirmation, verify updated version installed
- [x] 8.11 Set test feed plugin version to an older version (e.g., installed 2.0.0, feed 1.0.0)
- [x] 8.12 Verify plugin shows "Different version available" status with warning "This is a different version (may be older)", Install button (not Update button)
- [x] 8.13 Tamper with a test package (change one byte), attempt install
- [x] 8.14 Verify checksum mismatch error: "Package checksum mismatch. The downloaded file may be corrupted or tampered with. Installation cancelled."
- [x] 8.15 Verify temp file was deleted (check `%TEMP%` for `clihub-plugin-*.zip` files)
- [x] 8.16 Set `PluginFeedUrl` to empty string `""` in `config.json`, restart CLIHub
- [x] 8.17 Verify Browse Plugins button is grayed out or hidden
- [x] 8.18 Disconnect network (or set feed URL to unreachable address), attempt Browse Plugins
- [x] 8.19 Verify graceful error: "Could not load plugin feed. Check your network connection." with Retry button
- [x] 8.20 Create test feed entry with `minAppVersion` higher than current CLIHub version (e.g., "99.0.0")
- [x] 8.21 Verify plugin in feed shows "Requires CLIHub 99.0.0 or newer" message and Install button is disabled
- [x] 8.22 Set feed URL to `http://` (not HTTPS), attempt Browse Plugins
- [x] 8.23 Verify error logged and feed load fails (HTTPS-only enforcement)
- [x] 8.24 Restore working feed URL, verify Browse Plugins works again

**Gate:** All smoke tests pass. Feed browsing, installation, update, downgrade warning, error handling, and gating work as specified.

## Stage 9: Final validation and cleanup

- [x] 9.1. Run full test suite: `dotnet test CLIHub.sln`
- [x] 9.2. Run `openspec validate --all` and confirm 0 errors
- [x] 9.3. Review any temp files or test artifacts created during implementation, clean up
- [x] 9.4. Verify no unintended changes to `.opencode/`, `.pi/`, or other non-code directories
- [x] 9.5. Run `graphify update .` to refresh the knowledge graph

**Gate:** All gates passed, no validation errors, implementation complete.

## Notes

- **Feed repository creation:** The `trustsoft/CLIHub-plugins` repository is not created in this change. Document the publishing contract and workflow as a task; implementation uses a local test feed for smoke testing. Production feed URL is deployed post-implementation.
- **Exact repository name:** `trustsoft/CLIHub-plugins` (with capital "CLI" and "Hub", hyphen) is the documented default; user confirmation on exact casing/naming is a low-risk open question that can be finalized during implementation without affecting architecture.
- **Temp file cleanup:** Every download workflow must delete the temp file in a `finally` block to avoid leaking temp files on exception or cancellation.
- **HTTPS enforcement:** Reject `http://` scheme for feed URL and package URL before making any HTTP request; detect and reject redirects to `http://`.
- **Version comparison:** Numeric major.minor.patch comparison when both versions parse (upgrade = `UpdateAvailable`, downgrade = `DifferentVersionAvailable`), with string-equality fallback for non-numeric versions; the same numeric comparison drives `minAppVersion` gating.
- **Built-in plugins:** Feed lists only non-reserved plugin IDs; built-in plugins remain under application update control (three-hash seeding).
