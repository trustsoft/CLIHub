# Spec wording cosmetics: "launch window" noun in app-lifecycle/preferences-ui, sentence-style requirement names in agent-ui

## Why

Two Cosmetic items in `docs/backlog.md` were approved by the user on 2026-10-24 for one spec-only change:

1. **`app-lifecycle`/`preferences-ui` say "main window" for the launch window** (`docs/backlog.md:262-271`).
   Five occurrences call the launch window "the main window": `openspec/specs/app-lifecycle/spec.md:23,99,103,107`
   and `openspec/specs/preferences-ui/spec.md:96` (re-verified byte-accurate on 2026-10-24). This is the same
   drift the `launch-window-layout-rename` change (2026-10-24, archived) fixed inside its own capability and
   explicitly left out of scope there (`openspec/changes/archive/2026-10-24-launch-window-layout-rename/proposal.md:30`,
   design "Decision 3"). The sibling capabilities `launch-window-layout` and `launch-window-theme` say
   "the launch window" throughout, and both edited specs already use "launch window" everywhere else, so the
   five lines are the only remaining inconsistency.

2. **`agent-ui` requirement naming style drift** (`docs/backlog.md:273-277`). Ten requirement headers carry
   `REQ-AGENT-CAP-NNN:` ID prefixes (`openspec/specs/agent-ui/spec.md:9,29,49,69,89,109,132,146,160,174`)
   inside a capability whose other three requirements use sentence-style names (`:194,219,289`). No other
   capability in `openspec/specs/` uses ID-style requirement names.

Neither item changes any behavior: requirement bodies (except the five window-noun phrases), scenarios, and
assertions stay byte-identical, and no source or test file is touched.

## What Changes

1. **Normalize the window noun to "launch window"** in the five occurrences, via three MODIFIED requirement
   blocks in two delta files (scenario names preserved byte-identical, per repo convention):
   - `app-lifecycle` — "Single instance enforcement" (one scenario THEN, `:23`) and "Startup window visibility"
     (requirement prose `:99` plus two scenario THENs `:103,107`);
   - `preferences-ui` — "Save applies preferences" (one scenario THEN, `:96`).
   Exact before → after wording is in design "Decision 1".
2. **Rename the ten ID-style `agent-ui` requirement headers to sentence-style names** derived from each
   requirement's body, via `## RENAMED Requirements` FROM/TO pairs — the CLI operation that renames a
   requirement within a capability while rewriting only the header line. Requirement bodies are not carried
   in the delta at all, so they cannot change. The proposed names are in design "Decision 2" (table);
   two of them are flagged for user confirmation in design "Open questions".

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-lifecycle` — MODIFIED "Single instance enforcement" and "Startup window visibility"; all five scenario
  names preserved; only the window noun changes.
- `preferences-ui` — MODIFIED "Save applies preferences"; all eight scenario names preserved; only the window
  noun changes.
- `agent-ui` — RENAMED pairs for the ten ID-style requirement headers; bodies and scenario blocks untouched.

No `.openspec.yaml` is included: the change carries deltas, so `skip_specs` does not apply, and no capability is
retired (following `openspec/changes/archive/2026-10-24-low-sweep-2/` and
`.../2026-10-24-commandline-agent-blocking/`, which also shipped deltas with no `.openspec.yaml` and validated
clean).

## Impact

- Specs: the three deltas above; `openspec archive` applies them to the main specs. No Purpose changes (both
  Purposes are "main window"-free; verified).
- Code: none. No `src/`, `tests/`, or configuration edits.
- Docs: `docs/backlog.md` — the two swept entries are rewritten to the resolved format at commit/archive time
  (orchestrator stage, per current policy).
- Risk: minimal. Wording-only; `openspec validate --all` guards scenario-name preservation (MODIFIED) and
  rename collisions (RENAMED) at authoring time.

## Non-goals

- No Purpose rewrites (not needed — verified no "main window" in either Purpose; and a delta `## Purpose`
  cannot replace an existing capability's Purpose anyway).
- No rewording of "main window" prose outside the two named specs; archived changes, `CHANGELOG.md`, and
  historical plans keep the wording of their time.
- No changes to `agent-ui` requirement bodies, scenario names, or the code-fenced GIVEN/WHEN/THEN scenario
  style; no renaming of the three already sentence-style requirements.
- No renaming of capabilities or scenario names anywhere.
- No deletion or registration of the legacy `MainWindow` class (parked backlog option).
- No source, test, or build-configuration changes.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change; working tree clean).
- Terminology follows the archived `2026-10-24-launch-window-layout-rename` change, which established "the
  launch window" as the spec noun and logged this sibling drift as the follow-up.
