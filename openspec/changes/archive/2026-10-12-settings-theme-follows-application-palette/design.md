## Context

This is a documentation/spec correction with no behavior change: the Settings window already resolves the active palette through `DynamicResource`, and `App.xaml.cs` merges `LightTheme.xaml` when the effective theme is light. The remaining defect is wording in `settings-theme` and several repository documents. The OpenSpec CLI is not available in this environment, so the delta-to-main-spec merge and the archive move are done manually.

## Goals / Non-Goals

**Goals:** make every remaining `settings-theme` statement and every affected document true of the shipped code, keeping requirement titles stable.

**Non-Goals:** code changes, schema changes, requirement renames.

## Decisions

### D1 - Correct the contract, not the code

The implementation already supports the light palette; only the spec and docs are adjusted.

### D2 - Keep requirement titles

`openspec validate` matches `MODIFIED` requirements by title, so bodies and scenarios are corrected while titles (including "Dark input controls") are left unchanged.

### D3 - Documents in the same change

The glossary, README, startup, releasing, and ADR corrections are part of the same documentation defect and are done here.

## Risks / Trade-offs

- Manual merge without `openspec validate` - mitigated by a final scan for remaining dark-only wording.

## Migration Plan

- None. Documentation-only.

## Open Questions

- None.
