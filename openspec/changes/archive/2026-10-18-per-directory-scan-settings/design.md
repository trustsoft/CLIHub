## Context

`ProjectScanningOptions.From` currently turns four global preferences into one options value, and
`RepositoryDiscovery.Enumerate` walks every root with it. `AppPreferences.WatchedDirectories` is a list of
normalized paths, so nothing ties a depth or mode to a path. The Settings page binds the list directly to strings
and renders one global summary line.

## Goals / Non-Goals

**Goals**

- Per-root depth and discovery mode, persisted and applied by every scan path.
- Global depth/mode stay as the defaults for newly added rows.
- Row controls that are compact, discoverable, and consistent with the launch window's action menus.
- Backward-compatible configuration: existing `config.json` files need no rewrite.

**Non-Goals**

- Per-root ignored patterns.
- Per-root watcher behaviour or per-root enable flags.
- A schema-version bump or migration that rewrites existing documents.

## Decisions

### D1. Additive persistence keyed by path

`PreferencesDocument` keeps `watchedDirectories` as the ordered list of paths and gains an optional
`watchedDirectorySettings` array of `{ path, scanDepth, discoveryMode }`. `AppPreferences` exposes the same data as
a case-insensitive dictionary keyed by the normalized path.

- Absent entries fall back to the global `scanDepth` / `discoveryMode`, so existing files load unchanged.
- Writing a document also writes the entries for current rows only, so removing a root drops its entry.
- Kept as an array, not a JSON object keyed by path, so paths with separators stay readable and ordered.

### D2. Effective options per root

`ProjectScanningOptions` gains an optional per-root settings dictionary and a `ForRoot(path)` method that returns
the same options with the root's depth and mode (or the global values when absent). `RepositoryDiscovery.Enumerate`
calls it for each root before walking. `ProjectScanningService` uses it for the single-root rescan, so a row's Scan
action uses the row's settings.

### D3. Row model in the settings view model

The watched list becomes a collection of `WatchedDirectoryRow` objects: path, selected `ScanDepthOption`, selected
`DiscoveryModeOption`, and the discovered count. Global cards seed the selected values when a row is created and
when a stored path has no entry. Editing a row does not touch the global defaults, and editing the defaults does not
change existing rows. Saving writes the path list plus the row settings.

### D4. Chip menus

Each row renders two chips (`Depth N`, discovery label) using a toggle plus a popup card styled like the launch
window's actions popover. The depth chip lists the allowed depth values; the discovery chip lists the three modes.
Choosing a value updates the row and closes the menu. The chips are not a `ComboBox` because the settings window
styling (chip surface + action-menu card) is not the stock combo look.

### D5. Count line

The row keeps its secondary line for the per-root repository count. The discovery and depth values move into the
chips, so the line no longer repeats them.

## Risks / Trade-offs

- A stale per-root entry for a path that is no longer listed is dropped on save; until then it is ignored because
  the scanner only iterates the configured roots.
- Two controls per row make rows taller; the page is already a scrolling list, and typical watched-root counts are
  small.
- The settings contract now has two sources for the same value (defaults and row); the fallback rule (row entry,
  then global) is applied in one place (`ProjectScanningOptions.ForRoot`).

## Migration Plan

No schema change. On load, a root without an entry uses the global values. The first save writes an entry for every
row so later global edits do not silently move existing roots.
