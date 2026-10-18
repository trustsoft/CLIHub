## 1. Persistence and core model

- [x] 1.1 Add a `WatchedDirectoryScanSettings` record and a case-insensitive per-root map on `AppPreferences`, defaulting to empty
- [x] 1.2 Persist the map as an optional `watchedDirectorySettings` array of path/depth/discovery entries in `AppConfigDocument`; load absent entries as the global fallback and ignore entries for paths that are not watched
- [x] 1.3 Deep-copy the map in `ConfigurationSnapshot.ClonePreferences`

## 2. Per-root scanning

- [x] 2.1 Extend `ProjectScanningOptions` with the per-root map and a `ForRoot(path)` effective-options method (normalizing out-of-range depths)
- [x] 2.2 Make `RepositoryDiscovery.Enumerate` walk each root with its effective options; keep ignored patterns global
- [x] 2.3 Use the effective options in the full scan and in the single-root rescan

## 3. Settings rows

- [x] 3.1 Replace the watched-directory string list with rows that carry the path, selected depth, selected discovery mode, and discovered count; seed new rows from the global defaults
- [x] 3.2 Build the saved path list and per-root map from the rows; drop entries for removed rows
- [x] 3.3 Point the scan/remove commands and the per-root count refresh at the row objects

## 4. Row chip UI

- [x] 4.1 Add chip toggle and menu styles for settings values, modelled on the launch window's action popover
- [x] 4.2 Render depth and discovery chips in each row and apply a chosen value to that row only
- [x] 4.3 Keep the repository count on the row's secondary line and keep the global cards labelled as defaults for new directories

## 5. Verification and docs

- [x] 5.1 Update/add core tests: options fallback, per-root discovery, configuration round-trip, out-of-range per-root depth
- [x] 5.2 Update/add view-model tests: row seeding, per-row edits, save/load round-trip, removed row drops its settings
- [x] 5.3 Update `docs/architecture/scanning.md` and `docs/architecture/configuration.md` for the per-root entries
- [x] 5.4 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
- [x] 5.5 Verify in the app: chips open menus, only the edited row changes, and Scan uses the row's settings
