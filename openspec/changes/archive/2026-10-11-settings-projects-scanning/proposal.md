## Why

Users keep many repositories under a few parent folders, and today each one must be added to CLIHub by hand, one folder picker at a time. Watching a small set of root directories and discovering the repositories beneath them keeps the project list current automatically and lets agent usage be seen without manual registration. This is the first slice of Phase 2 (`settings-projects-scanning`) and unblocks `settings-agents-management`.

## What Changes

- Add a **Projects & Paths** feature: users configure **watched root directories**, a **repository scan depth**, **ignored directory patterns**, and a **repository discovery mode**, and see a read-only **background watcher status** indicator.
- Scanning walks each watched root up to the configured depth, skipping ignored patterns, and never descends into `.git`.
- The **discovery mode** chooses what counts as a repository: **Git repositories** (a `.git` entry), **Agent folders** (at least one plugin **project indicator**, for example `.claude` or `.opencode`), or **Both**. OpenSpec is **not** treated as an agent marker.
- Discovered repositories are **auto-registered** as projects, deduplicated by normalized path. Manually registered projects are never duplicated or reclassified.
- A **background watcher** observes the watched roots and triggers a debounced re-scan on change; a status surface exposes watcher state (idle/scanning), the last scan time, and the discovered-project count.
- **Removing a watched root** removes the projects that were discovered under it (unless they also fall under another watched root or were registered manually).
- New persisted preferences (`watchedDirectories`, `scanDepth`, `ignoredPatterns`, `discoveryMode`) and a per-project provenance marker (`source`: `manual`/`discovered`) are **additive**; the configuration schema version is **not** bumped.
- This change is **non-breaking**: absent fields fall back to defaults that preserve current behavior (nothing is watched until the user configures a root).

## Capabilities

### New Capabilities
- `project-scanning`: watched-root configuration, bounded three-mode repository discovery, and the background watcher with an observable status.

### Modified Capabilities
- `project-management`: projects gain a provenance distinction between manually registered and scanner-discovered; discovery registers and removes discovered projects while leaving manual projects intact.
- `preferences-ui`: the Projects & Paths page offers the watched-directories list, scan-depth field, ignored-patterns list, discovery-mode selector, and watcher-status indicator.
- `configuration-snapshot`: the new scanning preferences and the project provenance marker default safely and round-trip through the single `config.json` document.

## Impact

- Core: new `CLIHub.Core.Scanning` services (repository scanner, watcher, status surface) over `IPreferencesStore`, `IProjectService`, `IPluginCatalog`, and `IAgentDetectionService`; `ProjectScanningPreferences` on `AppPreferences`; `ProjectSource` on `Project`/`ProjectDocument`; composition in `ServiceCollectionExtensions`.
- App: `SettingsViewModel` + new `SettingsProjectsPage` (replacing the placeholder for the `projects` key); page routing in `SettingsWindow.xaml`; watcher start/stop in the application lifecycle; project-list refresh wiring.
- Config: additive `preferences.watchedDirectories`, `preferences.scanDepth`, `preferences.ignoredPatterns`, `preferences.discoveryMode`, and `projects[].source`; no `schemaVersion` change.
- Tests: scanning/discovery unit tests, watcher debounce tests, settings view-model tests, persistence round-trip tests, and the architecture-boundary test (scanning stays in Core).

## Non-goals

- Detecting or marking OpenSpec in a repository.
- Persisting detected agent IDs per project (agent usage is derived on demand through the existing `IAgentDetectionService`).
- A per-machine global index, cross-root project merging, or scanning network/UNC locations beyond what `Directory` enumeration supports.
- Editing or renaming discovered projects, and any "suggest before adding" confirmation flow (discovery is automatic).
