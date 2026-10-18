## Why

Repository scan depth and discovery mode are one global pair applied to every watched directory. A folder of small Git repositories, a deep monorepo tree, and an agent-only workspace all get the same depth and the same repository test. Users who scan different kinds of trees must change the global values and rescan everything, then change them back.

The page already lists each watched directory as a row. The settings belong on that row. The global cards become the defaults a newly added directory starts with, rather than a setting that keeps overriding every row.

## What Changes

- Store a scan depth and a discovery mode per watched directory, keyed by its normalized path, while keeping the existing global values as defaults for rows without a stored entry.
- Apply each root's own settings in the full scan, the watcher-driven rescans, and the per-directory scan; ignored patterns remain global.
- Turn the per-row scan depth and discovery mode into chip controls that open a menu with the allowed values (modelled on the launch window's agent/project action menus).
- Keep the existing "Repository scan depth" and "Repository discovery" cards as defaults used when a directory is added; the row values do not change when the defaults change.
- Drop a root's stored settings when the root is removed; persist the remaining entries on save.
- Existing configurations keep working: roots without a stored entry use the global values, so no data rewrite is needed.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `project-scanning`: Adds per-directory scan settings, applied by the full scan and by a single-root rescan, with the global settings as the fallback.
- `preferences-ui`: The "Projects and Paths page options" requirement describes per-row chip controls and the global cards as defaults for newly added directories.
- `configuration-snapshot`: The watched-directories preference persists per-root settings without changing the existing roots or global defaults.

## Impact

- `src/CLIHub.Core/Models/Configuration/AppConfig.cs` — per-root settings on `AppPreferences`.
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` — persistence of the per-root entries.
- `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` — detached copy of the per-root settings.
- `src/CLIHub.Core/Scanning/ProjectScanningOptions.cs` — effective options per root.
- `src/CLIHub.Core/Scanning/RepositoryDiscovery.cs` — walk each root with its effective options.
- `src/CLIHub.Core/Scanning/ProjectScanningService.cs` — per-root scan for the row and full passes.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — watched-directory rows carrying depth and discovery.
- `src/CLIHub/Views/SettingsProjectsPage.xaml` + controls/styles — chip menus in each row.
- Tests: options, discovery, scanner, configuration round-trip, view-model rows and save.
- No schema-version bump: the new field is additive and absent entries fall back to the global values.
