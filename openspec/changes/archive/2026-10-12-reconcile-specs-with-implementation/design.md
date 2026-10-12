## Context

The code is the source of truth for every affected behavior; this change corrects the contract to match it and does not change behavior. The OpenSpec CLI is not available in this environment, so the delta-to-main-spec merge and the archive move are performed manually following the documented formats.

## Goals / Non-Goals

**Goals:** make every affected durable spec statement and every affected repository document true of the shipped code, while keeping requirement titles stable so the sync tooling can match them.

**Non-Goals:** code changes, configuration-schema changes, capability renames, or spec-set restructuring.

## Decisions

### D1 - Correct the contract, not the code

Each finding is a spec or document defect; the implementation already behaves as intended. No source change is made.

### D2 - Keep requirement titles and modify bodies

`openspec validate` matches `MODIFIED` requirements by title. Where a title is now imprecise (for example "Dark launch window theme"), the requirement body and scenarios are corrected rather than renaming the requirement, because the archive uses `MODIFIED` exclusively and never `RENAMED`.

### D3 - Purpose text corrected in place

OpenSpec deltas do not model a capability's `## Purpose`. Where the Purpose is stale (`launch-window-theme`, `settings-theme`, `agent-commands`, `preferences-ui`), it is corrected directly in the main spec.

### D4 - Documents fixed in the same change

The `CHANGELOG.md` capability references and the architecture documents are part of the same defect and are corrected here rather than left as follow-up.

## Risks / Trade-offs

- Manual merge without `openspec validate` - mitigated by re-reading each main spec after editing and diffing it against the delta.

## Migration Plan

- None. Documentation-only.

## Open Questions

- None.
