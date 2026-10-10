## Context

See `proposal.md` — Why. The existing building blocks this design reuses:

- `CLIHub.Core.Projects` — `IProjectService` (register/remove/select, path normalization via `ProjectPathPolicy`) and `IProjectStateStore` over the single `config.json` document.
- `CLIHub.Core.Plugins` — `IPluginCatalog.GetAllPlugins()` exposes each plugin's `Detection.ProjectIndicators`.
- `CLIHub.Core.Agents` — `IAgentDetectionService.IsAvailableInProject` already derives per-project agent usage on demand (file-system only, TTL-cached); this change does not add per-project agent storage.
- `CLIHub.Core.Configuration` — `IPreferencesStore` and the additive `AppPreferences`/`PreferencesDocument` boundary; `CLIHub.Core` is UI-independent but Windows-aware and may use `System.IO`.
- `CLIHub` — `SettingsViewModel`/`SettingsWindow` route pages by the `SelectedPage.Key`, with `SettingsPlaceholderPage` shown while `IsProjected` is `false` for the `projects` key.

## Goals / Non-Goals

**Goals:**

- A Core-owned scanning service that turns watched roots into a continuously current, deduplicated set of projects.
- A background watcher with a debounced re-scan and an observable status.
- Additive configuration so existing installs keep working untouched (nothing watched until configured).
- A Projects & Paths page that edits the preferences and shows status, following the existing Settings Save/Cancel model.

**Non-Goals:**

- OpenSpec detection/marking; persisted per-project agent IDs; a confirmation/suggestion flow before auto-registering; network/UNC scanning guarantees; editing or renaming discovered projects; a manual "rescan" button (the roadmap scopes the status to an indicator only).

## Decisions

### D1 — Scanning lives in `CLIHub.Core.Scanning`, UI-independent

Add `IProjectScanner` (reconcile + status) and a watcher built on `System.IO.FileSystemWatcher`. **Why:** `CLIHub.Core` is already Windows-aware and owns domain workflows and persistence; keeping the scanner in Core makes debounce/reconcile logic unit-testable without WPF and keeps `System.Windows` out. **Alternative rejected:** an app-layer watcher — splits domain state, harder to test, and duplicates the "owned by Core" pattern used by project/agent services.

### D2 — Discovery mode: `git`, `agents`, or `both`

A single `discoveryMode` preference selects the criterion: `git` (a `.git` entry exists), `agents` (at least one plugin `ProjectIndicators` entry exists), or `both` (either). `openspec` is deliberately excluded in every mode. **Why:** one explicit three-way choice lets a user narrow discovery (only real repos, or only agent-instrumented folders) instead of a boolean that can only widen it. **Alternative rejected:** a boolean auto-detect toggle — cannot express "repositories only" or "agent folders only".

### D3 — Reconcile is set-based and path-deduplicated

The scanner computes the desired discovered-path set per pass, then reconciles against project state: add missing discovered projects, set provenance to `discovered` on add, and remove projects marked `discovered` that are no longer under any watched root. Manual projects are never touched. Paths compare with the existing case-insensitive `ProjectPathPolicy`. **Why:** set reconciliation makes root removal and "moved repo" cases correct and idempotent. **Alternative rejected:** event-driven add-only — cannot express removals and drifts on missed events.

### D4 — Provenance stored on the project (`source`: `manual`/`discovered`)

Add a `ProjectSource` to `Project` and `ProjectDocument`, defaulting to `manual` for missing values. Registration distinguishes `AddProject` (manual) from a scanner path. **Why:** provenance must persist with the project to drive root-removal semantics; it also lets the launch window distinguish discovered rows later without new storage. **Alternative rejected:** keeping a side list of discovered paths — risks divergence from persisted projects and complicates atomic writes.

### D5 — Watcher debounce with a coalescing rescan loop

`FileSystemWatcher` events (created/renamed/deleted, recursive) only mark a "dirty" flag; a single debounced trigger (~750 ms) runs one reconcile pass. Passes never overlap; a watcher buffer overflow forces a full rescan and watcher re-creation. **Why:** large trees emit bursts of events; immediate per-event scanning thrashes disk and the project store. **Alternative rejected:** immediate scanning — unacceptable churn; polling — adds latency and constant I/O.

### D6 — Status is a pushed value object

`IProjectScanner.StatusChanged` carries an immutable status record (`IsWatching`, `IsScanning`, `LastScanUtc`, `DiscoveredCount`, `State`). The app view-model subscribes and marshals to the UI thread. **Why:** the page needs live status without polling; a value object stays testable. **Alternative rejected:** polling the scanner from a timer — extra coupling and UI-thread churn.

### D7 — Preferences are additive; `ignoredPatterns` uses an absent-vs-empty distinction

Add `watchedDirectories` (list), `scanDepth` (int, default 2), `ignoredPatterns` (nullable list: absent ⇒ built-in defaults, present ⇒ exact list), `discoveryMode` (token `git`/`agents`/`both`, default `both`). No `schemaVersion` bump. **Why:** the roadmap requires additive change with backward-compatible defaults; the nullable list lets a user deliberately clear the ignore set. **Alternative rejected:** seeding defaults into every existing config — a needless rewrite of user files and no way to express "ignore nothing".

### D8 — Applied without restart via lifecycle-managed restart

Saving scanning preferences restarts the watcher and triggers a reconcile using the new settings; the watcher starts after startup load when roots exist and stops on shutdown/empty list. **Why:** matches the "applied without restart" behavior of other Settings pages. **Alternative rejected:** restart-required — inconsistent with the rest of the Settings window.

## Risks / Trade-offs

- **Watcher buffer overflow on huge trees** → increase internal buffer, and on `Error` perform a full reconcile and re-create the watcher.
- **Scan cost on very wide trees** → depth cap, built-in ignored defaults, background execution, and coalesced passes; a root that is slow does not block UI.
- **Concurrent project mutation (UI add/remove vs. scan)** → serialize all scanner-driven project mutations through one scan loop and serialize the store update; the project service contract stays single-writer.
- **Debounce latency** → ~750 ms is imperceptible for project discovery; keep it a single constant, tunable later.
- **Case-insensitive path equality across drives** → reuse `ProjectPathPolicy.Normalize`/`AreEqual` for deduplication and "under root" checks.

## Migration Plan

- Ship additive fields and the project `source` marker; ordinary load applies defaults (empty watched list, depth 2, built-in ignores, discovery mode `both`, provenance manual). No schema migration or bump.
- Rollback: older builds ignore unknown JSON fields and treat missing `source` as manual, so a downgrade is non-destructive (an ignored watched-root list simply stops being used).

## Open Questions

- Whether discovered projects should carry a visible "discovered" badge in the launch window — deferred to `settings-agents-management` / launch-window polish.
- Whether to add an explicit "Rescan now" action — deferred; the roadmap scopes the status to a read-only indicator.
