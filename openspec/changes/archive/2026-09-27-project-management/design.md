## Context

See proposal.md - Why. The MVP (archived change `application-scaffold`) already provides `AppConfig.Projects`, `AppConfig.CurrentProjectId`, the `Project` model, `ConfigService` with atomic JSON persistence, `PluginManager`, `ProcessLauncher`, and a tray icon with a Launch/Exit menu. This change adds the missing project layer on top of those pieces. No in-memory state exists yet for projects; `ConfigService` is the single source of truth.

## Goals / Non-Goals

**Goals:**
- A small, testable `IProjectService` in `CLIHub.Core` with no WPF dependencies
- Reuse `AppConfig`/`ConfigService` rather than introducing a second store
- Deterministic, ordered project lists (recent, favorites)
- Logo resolution that is pure file-system logic (unit-testable)
- Wire the service into the tray menu and MainWindow

**Non-Goals:**
- Recursive project discovery/scanning of the file system
- Watching project folders for changes
- Project groups/tags or custom sort orders
- Editing project names or paths after registration (name is derived from folder)
- Validating that a project still contains a given AI agent

## Decisions

### Decision 1: `IProjectService` wraps `IConfigService`, no separate store

**Chosen:** Implement `ProjectService` as an in-memory cache backed by `ConfigService.Save/Load`.

**Rationale:**
- `AppConfig` already models projects and current selection; duplicating storage risks divergence
- Single persisted file keeps backup/portability simple
- Tests can substitute a fake `IConfigService` without touching the file system

**Alternatives considered:**
- Dedicated `projects.json`: Rejected — splits related state across files with no benefit
- SQLite: Rejected — overkill for a small list; JSON already chosen and implemented

### Decision 2: Name derived from folder, ID is a GUID

**Chosen:** `Name = Path.GetFileName(path)`, `Id = Guid.NewGuid().ToString("N")`.

**Rationale:**
- Users recognize projects by folder name; no extra input dialog
- GUID avoids collisions when two folders share a name
- Editing names is a non-goal, so derived names stay consistent

**Alternatives considered:**
- Name entered by user: Rejected for MVP friction
- Path as ID: Rejected — paths change and contain characters unfit for IDs

### Decision 3: Repository method set (interface shape)

```csharp
public interface IProjectService
{
    IReadOnlyList<Project> GetAllProjects();
    IReadOnlyList<Project> GetRecentProjects(int limit);
    IReadOnlyList<Project> GetFavorites();
    Project? GetCurrentProject();
    Project AddProject(string folderPath);
    void RemoveProject(string projectId);
    void SetCurrentProject(string projectId);
    void ToggleFavorite(string projectId);
    void TouchProject(string projectId);   // updates LastUsed
    string ResolveLogo(string projectPath); // logo detection
}
```

**Rationale:** Covers every scenario in the spec with a flat, easily mockable surface. `TouchProject` is separate from `SetCurrentProject` so "launched an agent" can bump recency without changing selection semantics.

### Decision 4: Logo detection by ordered filename probe

**Chosen:** Probe a fixed, ordered list of names in the project root and return the first hit.

```csharp
private static readonly string[] CandidateLogoNames =
{
    "logo.png", "logo.jpg", "logo.jpeg", "logo.svg",
    "icon.png", "icon.jpg", "favicon.png"
};
```

**Rationale:**
- Predictable, deterministic, trivially unit-testable against a temp directory
- Matches the vision's "first match wins" rule
- No image decoding needed — only existence checks, so no extra dependency

**Alternatives considered:**
- Enumerate all images and pick one: Rejected — non-deterministic
- Read image metadata/branding: Rejected — unnecessary complexity

### Decision 5: Default logo location

**Chosen:** Default logo resolved from the CLIHub application's embedded resources (`Resources`), falling back to `assets`.

**Rationale:** A project with no logo should still show something consistent; the placeholder belongs to the app, not the project folder. (Note: MVP wrote a generated placeholder into each plugin folder; for projects we keep the default inside the app.)

**Alternatives considered:**
- Write a placeholder file into each project folder: Rejected — mutating user projects is undesirable

### Decision 6: Tray and window integration stay in the UI project

**Chosen:** `ProjectService` lives in `CLIHub.Core`; tray menu wiring and the MainWindow list live in `CLIHub`.

**Rationale:** Preserves the documented dependency rule (Core has no WPF references). The tray reads `GetCurrentProject()` and `GetRecentProjects(n)` and rebuilds its menu when selection changes.

**Alternatives considered:**
- Service raising WPF-bound events: Rejected — couples Core to UI. Use a plain `event EventHandler? ProjectsChanged;` instead.

## Risks / Trade-offs

**[Risk] Concurrent writes to `config.json`** → Mitigation: `ConfigService` already does atomic temp-file + rename; `ProjectService` is a singleton and mutations are serialized on the UI thread for MVP.

**[Risk] Stale `currentProjectId` (folder deleted or entry removed)** → Mitigation: On load, if `currentProjectId` does not match a registered project, clear it and continue (spec: "Clear current project").

**[Risk] Duplicate registration with different casing/trailing slash** → Mitigation: Normalize paths (`Path.GetFullPath`, trim trailing separators) and compare case-insensitively before adding.

**[Trade-off] Names derived from folder** → Benefit: zero-input UX. Cost: two same-named folders are indistinguishable in the list beyond their path; acceptable for MVP.

**[Trade-off] No folder-existence check at launch time beyond current validation** → Benefit: simpler. Cost: user can launch into a project whose folder was moved; `ProcessLauncher` already validates the working directory and fails gracefully.

## Migration Plan

No data migration required. `config.json` already contains `projects: []` and `currentProjectId: null` from `AppConfig` defaults, so the new service reads existing files unchanged. First use simply adds entries.

Rollback: reverting the code leaves an extra populated `projects` array in `config.json`, which the previous version ignores safely.

## Open Questions

- **Should agents be filtered per project?** Deferred — the vision mentions per-agent project indicators, but this change treats all loaded plugins as launchable in any project.
- **Should recent list cap be configurable?** Deferred — hardcode a sensible default (e.g. 10) for now.
