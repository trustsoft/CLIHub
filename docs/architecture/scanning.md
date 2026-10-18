# Project Scanning

`CLIHub.Core.Scanning` discovers repositories under user-configured root folders, keeps the project list
in sync, and exposes a background watcher with an observable status. It is UI-independent (uses `System.IO`
only) and never starts an agent process.

## Settings

Scanning is driven by four preferences (see [Configuration persistence](configuration.md)):

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
paths plus the roots that were scanned successfully. A directory is discovered when it matches the mode, and
discovery stops descending at a repository (the repository itself is the project). Missing or unreadable roots
are skipped with a warning and do not affect the others.

`IProjectScanner` (implemented by `ProjectScanningService`) reconciles the discovered set through
`IProjectService.ReconcileDiscoveredProjects`:

- discovered folders not yet registered are added as projects with `source = discovered`;
- manually registered projects are never reclassified or removed;
- a discovered project is removed when it is no longer under any watched root, or when it is under a root that
  was scanned this pass but was not discovered.

Scanner-driven project mutations are posted to the owner (UI) thread captured at `Start`, so bound collections
update safely.

## Single-root rescan

`IProjectScanner.Rescan(root)` refreshes one watched root from the Projects & Paths page. It enumerates only that
root with the stored scan settings and reconciles only projects beneath it:

- discovered projects under the other roots are left untouched (this also protects overlapping nested roots that
  were not part of the pass);
- manual projects are kept;
- a missing or unreadable root scans nothing, so it removes nothing.

The pass reports `Scanning` while it runs and, afterwards, the last scan time and the total discovered-project
count. The page-header "Scan Now" remains the full pass through `Reload()`.

`IProjectScanner.CountDiscoveredProjects(root)` reports how many discovered projects sit at or below a watched
root. The Settings page uses it for the secondary line of each watched-directory row, where the count is shown
next to the discovery mode and the scan depth.

## Watcher and status

While at least one watched directory is configured, a recursive `FileSystemWatcher` observes the roots. Change
events only arm a debounced timer (~750 ms); a single coalesced reconcile pass runs afterwards. Passes never
overlap, and a watcher buffer overflow triggers a full rescan and watcher re-creation.

`IProjectScanner.Status` (a `ProjectScanStatus` value) reports the state (`NotConfigured`, `Idle`, `Scanning`,
`Error`), whether it is watching, whether a scan is running, the last scan time, the discovered count, and the
last error. `StatusChanged` raises the same value.

## Lifecycle

`ApplicationBootstrapper` starts the scanner after startup state is loaded; the DI container disposes it on
shutdown. Saving the scanning preferences calls `IProjectScanner.Reload()`, which re-reads the preferences,
reconfigures the watcher, and triggers a fresh scan — no restart required.
