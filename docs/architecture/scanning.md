# Project Scanning

`CLIHub.Core.Scanning` discovers repositories under user-configured root folders, keeps the project list
in sync, and exposes a background watcher with an observable status. It is UI-independent (uses `System.IO`
only) and never starts an agent process.

## Settings

Scanning is driven by five preferences (see [Configuration persistence](configuration.md)):

- `watchedDirectories` — root folders to scan. Empty means nothing is watched.
- `watchedDirectorySettings` — per-root `{path, scanDepth, discoveryMode}` overrides. A root without
  an entry uses the global `scanDepth` and `discoveryMode`.
- `scanDepth` — the default number of directory levels below a root inspected (clamped to `1`–`8`, default `2`).
- `ignoredPatterns` — case-insensitive directory-name patterns (`*` wildcard) skipped during the walk. When
  absent, the built-in set is used; an explicit empty list ignores nothing.
- `discoveryMode` — the default rule for which directories count as repositories: `git` (a `.git` entry),
  `agents` (a plugin `projectIndicators` marker), or `both` (default).

`.git` is never descended into. `openspec` is deliberately not treated as an agent marker.

`ProjectScanningOptions.ForRoot(path)` resolves the effective depth and discovery mode for one root: the
stored per-root entry when present, otherwise the global values. Ignored patterns remain global. The full scan,
the watcher-driven passes, and a single-root rescan all use this effective options value, so changing one
row's settings never changes another root.

## Discovery and reconciliation

`RepositoryDiscovery.Enumerate` walks each existing root to the configured depth and returns the discovered
paths, the roots that were scanned successfully, and the number of folders the walk examined under each of
those roots. A directory is discovered when it matches the mode, and discovery stops descending at a
repository (the repository itself is the project). Missing or unreadable roots are skipped with a warning and
do not affect the others.

`IProjectScanner` (implemented by `ProjectScanningService`) reconciles the discovered set through
`IProjectService.ReconcileDiscoveredProjects`:

- discovered folders not yet registered are added as projects with `source = discovered`;
- manually registered projects are never reclassified or removed;
- a discovered project is removed when it is no longer under any watched root, or when it is under a root that
  was scanned this pass but was not discovered.

Scanner-driven project mutations are posted to the owner (UI) thread captured at `Start`, so bound collections
update safely.

## Single-root rescan

`IProjectScanner.Rescan(root)` refreshes one watched root from the Project Scanning page. It enumerates only that
root with the stored scan settings and reconciles only projects beneath it:

- discovered projects under the other roots are left untouched (this also protects overlapping nested roots that
  were not part of the pass);
- manual projects are kept;
- a missing or unreadable root scans nothing, so it removes nothing.

The pass reports `Scanning` while it runs and, afterwards, the last scan time, the total discovered-project
count, and the scan coverage — how many folders the walk examined. The discovered-project count is always the
number of registered projects with discovered provenance, so a project registered manually is not counted twice
and a single-root rescan reports the same number as a full pass. The page-header "Scan Now" remains the full
pass through `Reload()`.

`IProjectScanner.CountDiscoveredProjects(root)` reports how many discovered projects sit at or below a watched
root. The Project Scanning page uses it for the secondary line of each watched-directory row, where the count
is shown next to the discovery mode and the scan depth.

## Watcher and status

While at least one watched directory is configured, a recursive `FileSystemWatcher` observes the roots. Change
events only arm a debounced timer (~750 ms); a single coalesced reconcile pass runs afterwards. Passes never
overlap, and a watcher buffer overflow triggers a full rescan and watcher re-creation.

`IProjectScanner.Status` (a `ProjectScanStatus` value) reports the state (`NotConfigured`, `Idle`, `Scanning`,
`Error`), whether it is watching, whether a scan is running, the last scan time, the discovered count, the
scanned-folder count, and the last error. `StatusChanged` raises the same value.

The scanned-folder count aggregates the coverage the walk recorded per watched root. A full pass replaces every
root's coverage, a single-root rescan replaces only that root's coverage (so the other roots keep their last
known number), a reload drops coverage for roots that are no longer configured, and `Stop` clears it. The
watcher summary card appends this folder count to its last scan time, so a scan that stops at the first level
is visible at a glance.

The Project Scanning summary cards read project provenance through `IProjectScanner` as well: the PROJECTS card shows the
total number of registered projects (discovered plus manual) and the "N discovered · M manual" split beneath it,
with `CountDiscoveredProjects(root)` feeding each watched-directory row and `CountManualProjects()` feeding the
split.

## Lifecycle

`ApplicationBootstrapper` starts the scanner after startup state is loaded; the DI container disposes it on
shutdown. Scanning settings edited on the Project Scanning page apply immediately: the page persists them
through a scanning-only boundary and calls `IProjectScanner.Reload()`, which re-reads the preferences,
reconfigures the watcher, and triggers a fresh scan — no restart and no footer Save required. Saving the
Settings window writes the same scanning values again and reloads the scanner, but the page is already in sync
with what it shows. Cancel does not revert scanning changes that were already applied; it discards the
remaining unsaved settings.
