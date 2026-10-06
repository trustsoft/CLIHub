## Context

The decisions to record are implemented and verified by earlier changes (`introduce-plugin-catalog-boundary`, `make-plugin-loading-deterministic`, `introduce-configuration-snapshot`, `add-config-schema-version`, `add-config-migration-runner`, the startup coordinator extractions, `introduce-ui-dialog-services`). The ADRs are therefore retrospective: they capture the decision as accepted, with the trade-offs that were actually weighed in those changes.

## Goals / Non-Goals

**Goals:**

- A durable, low-ceremony ADR home that future changes will actually extend.
- One ADR per decision listed in `improvements.md` item 28.
- Inbound discoverability from the architecture entry point.

**Non-Goals:**

- Re-deciding or changing any implemented behavior.
- Recording every small choice; only the five listed decisions.
- Tooling or CI enforcement of ADR presence.

## Decisions

- **Format: lightweight Nygard-style ADR** — `Title`, `Status` (Accepted), `Date`, `Context`, `Decision`, `Consequences` (including alternatives rejected). Rationale: zero tooling, matches the repository's plain-markdown docs; MADR's extra sections (option analysis tables) add ceremony the project does not need for retrospective records.
- **Location: `docs/adr/`** with files named `NNNN-short-title.md` (zero-padded, sequential). A `README.md` holds the conventions and the index table.
- **Retrospective status wording:** all five start as `Accepted` with today's date, each noting the change (or changes) that implemented it, so history stays traceable without duplicating the archives.
- **Linking:** `docs/architecture.md` gets a short "Decision Records" section pointing to the index; `docs/repo-structure.md` lists the folder in its Documentation inventory. No content duplication.

## Risks / Trade-offs

- [Risk] ADRs drift from reality as code evolves. -> Mitigation: each ADR names its implementing change; conventions instruct superseding (not editing) accepted ADRs when a decision changes.
- [Risk] Numbering collisions from parallel work. -> Mitigation: single sequential numbering and a one-line rule in the README to take the next number; acceptable for a single-maintainer cadence.

## Migration Plan

Pure documentation addition; rollback is git revert.

## Open Questions

- None.
