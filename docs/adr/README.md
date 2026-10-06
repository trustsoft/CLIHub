# Architecture Decision Records

Short-lived context is easy to lose; this folder records the architectural decisions that shape CLIHub and the trade-offs that were weighed when making them. Each record is self-contained and names the change that implemented the decision.

## Format

Each ADR is a numbered markdown file using a lightweight Nygard-style structure:

- `# NNNN. Title` — the decision in one line.
- `## Status` — `Accepted` or `Superseded by NNNN`, with the date.
- `## Context` — the forces at play and the problem being solved.
- `## Decision` — what was decided and why, including the alternatives that were rejected.
- `## Consequences` — what becomes easier, what becomes harder, and what this commits the project to.

## Conventions

- **Naming:** `NNNN-short-title.md` with a zero-padded, sequential number (`0001`, `0002`, …). Take the next free number; never reuse or renumber existing records.
- **Immutability:** an accepted ADR is not edited to change its meaning. When a decision changes, write a new ADR and mark the old one `Superseded by NNNN`.
- **Scope:** one decision per record. Implementation detail belongs in `docs/architecture.md`; ADRs capture the *why*.

## Index

| ADR   | Decision                                                              | Status   |
| ----- | --------------------------------------------------------------------- | -------- |
| 0001  | [Core/UI boundaries](0001-core-ui-boundaries.md)                       | Accepted |
| 0002  | [Single configuration document](0002-single-configuration-document.md) | Accepted |
| 0003  | [Startup orchestration](0003-startup-orchestration.md)                 | Accepted |
| 0004  | [Plugin catalog and precedence](0004-plugin-catalog-and-precedence.md) | Accepted |
| 0005  | [UI dialog boundaries](0005-ui-dialog-boundaries.md)                   | Accepted |
