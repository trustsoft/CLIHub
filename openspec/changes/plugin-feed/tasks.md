# Plugin Feed Implementation Tasks

## Stage 1: Feed client and core infrastructure

- [ ] 1.1 Add `IPluginFeedClient` interface in `CLIHub.Core/Plugins/` with `GetFeedAsync` and `DownloadPackageAsync` methods
- [ ] 1.2 Add result types: `PluginFeedResult`, `PluginFeedIndex`, `PluginFeedEntry`, `PluginDownloadResult` in `CLIHub.Core/Models/` or `CLIHub.Core/Plugins/`
- [ ] 1.3 Implement `PluginFeedClient` in `CLIHub.Core/Plugins/`:
  - [ ] Create dedicated `SocketsHttpHandler` with `AllowAutoRedirect = false`, manual redirect following (max 5 hops), HTTPS validation on every hop
  - [ ] Constructor with the dedicated handler, `IPreferencesStore`, `IUpdateVersionProvider`, `ILogger<PluginFeedClient>`
  - [ ] `GetFeedAsync`: fetch index, HTTPS-only enforcement, 2 MB limit, timeout via `AgentProbePolicy.ResolveTimeout`, tolerant JSON parse, duplicate ID handling, warn and skip invalid entries
  - [ ] `DownloadPackageAsync`: download to temp file, SHA-256 checksum verification, 10 MB limit, delete temp file on mismatch/exception
- [ ] 1.4 Add `AppVersionComparator` utility class in `CLIHub.Core/Plugins/` with `IsSatisfied(currentVersion, minAppVersion)` method (basic major.minor.patch numeric comparison)
- [ ] 1.5 Add `PluginFeedVersionComparator` utility class in `CLIHub.Core/Plugins/` with `Compare(installedVersion, feedVersion)` method (numeric comparison with fallback to string equality, returns `Current`/`UpdateAvailable`/`DifferentVersionAvailable`)
- [ ] 1.6 Register `IPluginFeedClient` as singleton in `ServiceCollectionExtensions.AddPluginServices`

**Gate:** Build passes, no compilation errors.

## Stage 2: Configuration and preferences

- [ ] 2.1 Add `PluginFeedUrl` property (string?, nullable) to `AppPreferences` in `src/CLIHub.Core/Models/Configuration/AppConfig.cs`
- [ ] 2.2 Add `PluginFeedUrl` property (string?, nullable) to `PreferencesDocument` in `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`
- [ ] 2.3 Update `PreferencesDocument.From(AppPreferences)` to map `PluginFeedUrl` field
- [ ] 2.4 Update `PreferencesDocument.ToPreferences()` to map `PluginFeedUrl` field
- [ ] 2.5 Update `ConfigurationSnapshot` in `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` to include `PluginFeedUrl` field and conversions
- [ ] 2.6 Add `FeedUrlProvider` utility in `CLIHub.Core/Plugins/` with default URL constant `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json` and `GetFeedUrl(IPreferencesStore)` method (absent/null → default official URL, empty string → feed disabled, non-empty → custom HTTPS URL)
- [ ] 2.7 Add configuration round-trip test in `tests/CLIHub.Core.Tests/Configuration/` verifying `PluginFeedUrl` field persists correctly
- [ ] 2.8 Update `docs/architecture/configuration.md` with `PluginFeedUrl` preference description (nullable, absent/null → default, empty → disabled, non-empty → custom URL)

**Gate:** Configuration loads and saves with `PluginFeedUrl` field; round-trip test passes.

## Stage 3: Feed client tests

- [ ] 3.1 Create `PluginFeedClientTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [ ] Valid index parsing with all required fields
  - [ ] Invalid JSON rejected with structured error
  - [ ] Missing `schemaVersion` rejected
  - [ ] `schemaVersion` > 1 rejected
  - [ ] Unknown fields tolerated
  - [ ] Entry with missing required fields: entry skipped with warning, rest of index loaded
  - [ ] Duplicate plugin IDs: first kept, later skipped with warning
  - [ ] Index > 2 MB rejected
  - [ ] `http://` feed URL rejected before request
  - [ ] Redirect to `http://` rejected (Location header validation during manual redirect following)
  - [ ] Network timeout returns error (mock or test timeout)
  - [ ] Offline scenario returns error, no throw
  - [ ] Package download with checksum match returns temp file path
  - [ ] Package download with checksum mismatch deletes temp file and returns error
  - [ ] Package > 10 MB rejected
  - [ ] `http://` package URL rejected
- [ ] 3.2 Create `AppVersionComparatorTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [ ] Current >= required returns true ("1.2.0" >= "1.1.0", "1.1.0" >= "1.1.0")
  - [ ] Current < required returns false ("1.0.0" < "1.1.0")
  - [ ] Major version precedence ("2.0.0" >= "1.99.99")
- [ ] 3.3 Create `PluginFeedVersionComparatorTests` in `tests/CLIHub.Core.Tests/Plugins/`:
  - [ ] Numeric versions: installed == feed returns `Current`
  - [ ] Numeric versions: installed < feed returns `UpdateAvailable`
  - [ ] Numeric versions: installed > feed returns `DifferentVersionAvailable` (downgrade case)
  - [ ] Non-numeric versions: string match returns `Current`
  - [ ] Non-numeric versions: string differ returns `DifferentVersionAvailable`
  - [ ] Installed version null returns `UpdateAvailable`

**Gate:** All feed client tests pass (`dotnet test CLIHub.sln --filter "FullyQualifiedName~PluginFeed"`).

## Stage 4: UI integration - Browse Plugins dialog

- [ ] 4.1 Create `BrowsePluginsDialogViewModel` in `CLIHub/ViewModels/Agents/`:
  - [ ] Dependencies: `IPluginFeedClient`, `IPluginInstaller`, `IPluginStateRepository`, `IUpdateVersionProvider`, `ILogger`
  - [ ] Properties: `ObservableCollection<FeedPluginItem> Plugins`, `bool IsLoading`, `string? ErrorMessage`, `bool HasError`
  - [ ] Commands: `RefreshCommand`, `InstallCommand`, `UpdateCommand`, `CloseCommand`
  - [ ] `LoadFeedAsync()`: fetch feed, populate Plugins with Available/Installed/UpdateAvailable/DifferentVersionAvailable status
  - [ ] `InstallPluginAsync(FeedPluginItem)`: download, inspect, confirm, install workflow with temp file cleanup in `finally`
  - [ ] `UpdatePluginAsync(FeedPluginItem)`: download, inspect, confirm, install with `replaceExisting=true`, temp file cleanup
- [ ] 4.2 Create `FeedPluginItem` model in `CLIHub/ViewModels/Agents/`:
  - [ ] Properties: Id, Name, Version, Publisher, Homepage, Description, PackageUrl, Sha256, Status (enum: Available/Installed/UpdateAvailable/DifferentVersionAvailable), InstalledVersion (nullable), MinAppVersion, CanInstall (bool, false if `minAppVersion` unsatisfied), IsDowngrade (bool, true if status == DifferentVersionAvailable and numeric comparison shows installed > feed)
- [ ] 4.3 Create `BrowsePluginsDialog.xaml` and code-behind in `CLIHub/Views/`:
  - [ ] Title: "Browse Plugins"
  - [ ] Feed status banner (loading/loaded/error with Retry button)
  - [ ] Plugin list (ListView or DataGrid) with columns: Name, Version, Publisher, Status, Actions
  - [ ] Install/Update buttons per row (disabled if `CanInstall` is false; Update button only shown for UpdateAvailable status)
  - [ ] For DifferentVersionAvailable status: show "Install" button with warning text "Different version (may be older)" and both versions
  - [ ] Details link or tooltip showing description, homepage, release notes URL
  - [ ] Refresh button
  - [ ] Close button
- [ ] 4.4 Extend `IPluginDialogService` in `CLIHub/Launching/` with `ShowBrowsePluginsDialog()` method
- [ ] 4.5 Implement `ShowBrowsePluginsDialog()` in `PluginDialogService` (open `BrowsePluginsDialog` as modal per ADR-0005)
- [ ] 4.6 Add `BrowsePluginsCommand` to `AgentManagementViewModel` in `CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` (calls `pluginDialogs.ShowBrowsePluginsDialog()`)
- [ ] 4.7 Add "Browse Plugins…" button to `SettingsAgentsPage.xaml` bound to `BrowsePluginsCommand`
- [ ] 4.8 Register `BrowsePluginsDialogViewModel` in DI if needed (or construct in dialog code-behind)
- [ ] 4.9 Add XAML binding test in `tests/CLIHub.Tests/Views/` for Browse Plugins entry point (matching `SettingsAgentsPageBindingTests` pattern)

**Gate:** Browse Plugins dialog opens from CLI Agents page (may show error if feed not reachable yet); binding test passes.

## Stage 5: ViewModel tests

- [ ] 5.1 Create `BrowsePluginsDialogViewModelTests` in `tests/CLIHub.Tests/ViewModels/Agents/`:
  - [ ] Feed load success populates Plugins list
  - [ ] Feed load failure shows error message
  - [ ] Refresh command re-fetches feed
  - [ ] Install command: download, inspect, confirm, install (mock dependencies)
  - [ ] Update command: download, inspect, replace (mock dependencies)
  - [ ] Checksum mismatch shows error, no install
  - [ ] `minAppVersion` unsatisfied: `CanInstall` false, Install button disabled
  - [ ] DifferentVersionAvailable status: downgrade warning shown, Install (not Update) button enabled
- [ ] 5.2 Extend `AgentManagementViewModelTests` in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`:
  - [ ] `BrowsePluginsCommand` opens dialog (verify `IPluginDialogService.ShowBrowsePluginsDialog` called)

**Gate:** All ViewModel tests pass (`dotnet test CLIHub.sln --filter "FullyQualifiedName~BrowsePlugins"`).

## Stage 6: Documentation

- [ ] 6.1. Update `docs/architecture/plugins.md`:
  - [ ] Add "Remote Plugin Feed" section after "Plugin Installation"
  - [ ] Describe feed index schema, download/install flow, update detection, `minAppVersion` gating
  - [ ] Document publishing contract: repository structure, release workflow, SHA-256 generation
  - [ ] Note: repository name `trustsoft/CLIHub-plugins` is documented default, exact name subject to confirmation
- [ ] 6.2. Update `docs/project-context.md` if new runtime paths introduced (temp download location, feed cache if applicable; likely no changes needed)
- [ ] 6.3. Document publishing contract as separate task or note: specify `trustsoft/CLIHub-plugins` repository structure, `index.json` on `main` branch, release asset naming `<plugin-id>-<version>.clihub-plugin.zip`, tag naming `<plugin-id>-<version>`, SHA-256 generation on publish, immutable packages

**Gate:** Documentation builds and renders correctly.

## Stage 7: Spec delta

- [ ] 7.1 Keep the existing `openspec/changes/plugin-feed/specs/plugin-feed/spec.md` delta in sync with implementation if deviations occur during build/test

**Gate:** Spec validates with `openspec validate openspec/changes/plugin-feed/`.

## Stage 8: Manual smoke testing

- [ ] 8.1 Create test feed `index.json` with sample plugins and serve via local HTTPS test server (e.g., using a self-signed cert or a fake `IPluginFeedClient` for testing)
- [ ] 8.2 Set `PluginFeedUrl` to test feed URL in `config.json`
- [ ] 8.3 Start CLIHub, open CLI Agents page, click "Browse Plugins"
- [ ] 8.4 Verify feed loads and displays plugins with correct status (Available/Installed/UpdateAvailable/DifferentVersionAvailable)
- [ ] 8.5 Select an available plugin, click Install
- [ ] 8.6 Verify confirmation dialog shows name, version, publisher, homepage, and all declared commands (launch, resume, version, update) with executables/arguments
- [ ] 8.7 Confirm install, verify plugin appears in CLI Agents list with "Installed" badge
- [ ] 8.8 Bump test feed plugin version, click Refresh in Browse Plugins dialog
- [ ] 8.9 Verify plugin shows "Update available" status with old and new versions
- [ ] 8.10 Click Update, verify confirmation, verify updated version installed
- [ ] 8.11 Set test feed plugin version to an older version (e.g., installed 2.0.0, feed 1.0.0)
- [ ] 8.12 Verify plugin shows "Different version available" status with warning "This is a different version (may be older)", Install button (not Update button)
- [ ] 8.13 Tamper with a test package (change one byte), attempt install
- [ ] 8.14 Verify checksum mismatch error: "Package checksum mismatch. The downloaded file may be corrupted or tampered with. Installation cancelled."
- [ ] 8.15 Verify temp file was deleted (check `%TEMP%` for `clihub-plugin-*.zip` files)
- [ ] 8.16 Set `PluginFeedUrl` to empty string `""` in `config.json`, restart CLIHub
- [ ] 8.17 Verify Browse Plugins button is grayed out or hidden
- [ ] 8.18 Disconnect network (or set feed URL to unreachable address), attempt Browse Plugins
- [ ] 8.19 Verify graceful error: "Could not load plugin feed. Check your network connection." with Retry button
- [ ] 8.20 Create test feed entry with `minAppVersion` higher than current CLIHub version (e.g., "99.0.0")
- [ ] 8.21 Verify plugin in feed shows "Requires CLIHub 99.0.0 or newer" message and Install button is disabled
- [ ] 8.22 Set feed URL to `http://` (not HTTPS), attempt Browse Plugins
- [ ] 8.23 Verify error logged and feed load fails (HTTPS-only enforcement)
- [ ] 8.24 Restore working feed URL, verify Browse Plugins works again

**Gate:** All smoke tests pass. Feed browsing, installation, update, downgrade warning, error handling, and gating work as specified.

## Stage 9: Final validation and cleanup

- [ ] 9.1. Run full test suite: `dotnet test CLIHub.sln`
- [ ] 9.2. Run `openspec validate --all` and confirm 0 errors
- [ ] 9.3. Review any temp files or test artifacts created during implementation, clean up
- [ ] 9.4. Verify no unintended changes to `.opencode/`, `.pi/`, or other non-code directories
- [ ] 9.5. Run `graphify update .` to refresh the knowledge graph

**Gate:** All gates passed, no validation errors, implementation complete.

## Notes

- **Feed repository creation:** The `trustsoft/CLIHub-plugins` repository is not created in this change. Document the publishing contract and workflow as a task; implementation uses a local test feed for smoke testing. Production feed URL is deployed post-implementation.
- **Exact repository name:** `trustsoft/CLIHub-plugins` (with capital "CLI" and "Hub", hyphen) is the documented default; user confirmation on exact casing/naming is a low-risk open question that can be finalized during implementation without affecting architecture.
- **Temp file cleanup:** Every download workflow must delete the temp file in a `finally` block to avoid leaking temp files on exception or cancellation.
- **HTTPS enforcement:** Reject `http://` scheme for feed URL and package URL before making any HTTP request; detect and reject redirects to `http://`.
- **Version comparison:** Numeric major.minor.patch comparison when both versions parse (upgrade = `UpdateAvailable`, downgrade = `DifferentVersionAvailable`), with string-equality fallback for non-numeric versions; the same numeric comparison drives `minAppVersion` gating.
- **Built-in plugins:** Feed lists only non-reserved plugin IDs; built-in plugins remain under application update control (three-hash seeding).
