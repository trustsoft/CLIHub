## 1. Configuration and model

- [x] 1.1 Add scanning preferences to `AppPreferences` (`WatchedDirectories`, `ScanDepth`, `IgnoredPatterns`, `DiscoveryMode`) and mirror them in `PreferencesDocument` and `ConfigurationSnapshot.ClonePreferences`; verify by serialization round-trip tests in `tests/CLIHub.Core.Tests`.
- [x] 1.2 Add project provenance (`ProjectSource` enum on `Project`, `source` field on `ProjectDocument`) and thread it through `ProjectState` and `ConfigurationSnapshot` cloning; verify round-trip and legacy-default (`manual`) tests.
- [x] 1.3 Add the scanning defaults (scan depth `2`, built-in ignored set, `.git` never descended) as a single constants/options type shared by the scanner and the settings defaults; verify default and out-of-range normalization tests.

## 2. Project service provenance

- [x] 2.1 Extend `IProjectService` with provenance-aware registration (manual vs discovered) and a remove-discovered-under-roots operation, keeping existing registration behavior unchanged; verify `ProjectServiceTests` cover both paths.
- [x] 2.2 Ensure manual `AddProject` marks provenance manual, and re-encountering a manual path during discovery never reclassifies it; verify with focused tests.
- [x] 2.3 Confirm removal of a discovered project leaves files on disk and clears the current-project id when applicable; verify with tests.

## 3. Scanning engine (Core)

- [x] 3.1 Implement the bounded directory walk (scan-depth limit, ignored-pattern matching with `*`, never descending into `.git`, skip missing/unreadable roots with a warning) as a testable enumerator; verify unit tests.
- [x] 3.2 Implement the three-mode discovery criterion (`git`/`agents`/`both`, `openspec` excluded) using `IPluginCatalog`; verify unit tests cover each mode.
- [x] 3.3 Implement `IProjectScanner` reconcile (set-based, `ProjectPathPolicy`-deduplicated, add discovered with provenance, remove discovered projects no longer under any watched root) over `IProjectService` and `IPreferencesStore`; verify reconcile tests including root removal and manual-project preservation.

## 4. Watcher and status

- [x] 4.1 Implement the debounced `FileSystemWatcher` loop (coalesces bursts, never overlaps passes, and on buffer overflow performs a full rescan and re-creates the watcher); verify debounce and overflow tests using an injected watcher/time provider.
- [x] 4.2 Expose an immutable `Status` (watching, scanning, last-scan time, discovered count) plus `StatusChanged` on `IProjectScanner`; verify status transitions with tests.
- [x] 4.3 Make scanner start/stop idempotent and driven by the watched-directories list (empty list stops watching); verify start/stop/restart tests.

## 5. Composition and lifecycle

- [x] 5.1 Register the scanner and watcher in `CLIHub.Core` `ServiceCollectionExtensions`; verify the solution builds and `ServiceCollectionExtensionsTests` pass.
- [x] 5.2 Start the scanner after startup load and stop it on application shutdown; verify the startup/shutdown wiring tests.

## 6. Settings UI

- [x] 6.1 Add the scanning fields and watcher status to `SettingsViewModel` (load, save, validation, options, status marshalled to the UI thread); verify the view-model tests.
- [x] 6.2 Add `SettingsProjectsPage` (watched-directories list with add/remove, scan-depth field, ignored-patterns list, three-option "Repository discovery" selector, read-only status indicator) and route the `projects` key to it instead of the placeholder; verify the navigation test asserts the page is shown and implemented.
- [x] 6.3 Route "add watched directory" through the existing folder-picker dialog service and apply changes on Save (restart watcher/reconcile); verify behavior tests.
- [x] 6.4 Verify the architecture boundary test still passes (no `System.Windows` outside the allowed application files).

## 7. Verification

- [x] 7.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 7.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 7.3 Run `openspec validate settings-projects-scanning --strict` and verify the change is valid.
- [x] 7.4 Smoke-test: add a watched root; discovered repos appear as projects; creating/removing a repo updates the list; removing the root removes only its discovered projects; each discovery mode surfaces the expected folders; status indicator reflects watching/scanning.
