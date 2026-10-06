## Context

`docs/architecture.md` is the single largest documentation file and the AGENTS.md designates it the single source of truth for architecture topics. Sections already exist for startup, configuration persistence, plugin descriptor format, process/runtime behavior (inside project responsibilities and error handling), and UI conventions. The repository rule is "link, don't duplicate" — the split must preserve that by moving content and leaving anchors/links behind.

## Goals / Non-Goals

**Goals:**

- One topical document per bounded area: startup, configuration, plugins, processes, UI.
- `docs/architecture.md` remains the entry point with overview content and links.
- All existing inbound links keep working (repo-structure.md, AGENTS.md, releasing.md, specs).

**Non-Goals:**

- Rewriting or updating technical content — this is a move, not an edit pass.
- Splitting README, CHANGELOG, or `docs/vision.md`.
- Creating an ADR system (that is the separate `add-architecture-decision-records` change).

## Decisions

- **New directory `docs/architecture/`** rather than flat `docs/*.md`: groups the split documents under one parent and keeps the `docs/` root readable as it grows. Alternative (flat files) rejected — more top-level clutter.
- **File names**: `startup.md`, `configuration.md`, `plugins.md`, `processes.md`, `ui.md` — short, matching the glossary terms and the improvement item wording.
- **Move, don't summarize**: sections move verbatim (with link-path fixes); `architecture.md` keeps a one-line pointer per moved topic so readers coming for the old anchors find the new location. No content duplication.
- **Anchors over redirects**: there is no server-side redirect mechanism for markdown; inbound references are updated to point at the new files instead of leaving stale section anchors.

## Risks / Trade-offs

- [Risk] Stale inbound links to `architecture.md#section` anchors. → Mitigation: grep all `architecture.md#` references repo-wide and update them; verify no remaining deep anchors point at moved sections.
- [Risk] Drift between entry-point summaries and topical docs. → Mitigation: entry point keeps only links plus unchanged overview content, no restated details.

## Migration Plan

Pure documentation change; no deploy or rollback concerns beyond git revert.

## Open Questions

- None.
