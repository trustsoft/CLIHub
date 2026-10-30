# Proposal: align-specs-with-dead-path-cleanup

## Why

`composition-and-dead-path-cleanup` (archived 2026-10-30) deleted three dead paths:
the unreachable `InstanceCoordinator` second-instance check, the Settings placeholder page with
`IsImplemented`, and the `EmptyStateText` style that page used. Three durable spec statements still
describe those paths:

- `openspec/specs/app-lifecycle/spec.md` — Requirement "Single instance resource ownership",
  scenario "Second instance exits through the provider lifecycle".
- `openspec/specs/preferences-ui/spec.md` — Requirement "Settings sections", the sentence
  "A page whose settings are not yet implemented SHALL show a neutral empty state instead of
  controls." and scenario "Page without implemented settings".
- `openspec/specs/settings-theme/spec.md` — scenario "Placeholder page readable".

The code they describe no longer exists, so the specs misdescribe the current contract and invite a
future agent to restore removed code as if it were a regression. This change aligns the text with
the implementation.

## What changes

OpenSpec 1.15 refuses to drop a scenario from a MODIFIED requirement (a MODIFIED block replaces
the whole requirement and must keep every existing scenario name). A requirement that loses a
scenario is therefore replaced: the old requirement is removed and a renamed equivalent is added
without the stale scenario. All retained scenario names and their text are copied verbatim.

- `app-lifecycle`: remove Requirement "Single instance resource ownership"; add Requirement
  "Single instance ownership and disposal" with the same text and the scenarios "Production
  registration has one owner", "Duplicate launch exits before environment setup", and "First
  instance releases the mutex through provider disposal". "Second instance exits through the
  provider lifecycle" is gone.
- `preferences-ui`: remove Requirement "Settings sections"; add Requirement "Settings sections and
  pages" with the placeholder sentence removed and the "Page without implemented settings" scenario
  gone. Every other scenario name and its text is unchanged.
- `settings-theme`: remove Requirement "Sidebar navigation styling"; add Requirement "Sidebar
  navigation and page styling" with the "Placeholder page readable" scenario gone. The requirement
  text and the other four scenario names are unchanged.

No code, test, or runtime change. `docs/backlog.md` gets its spec-drift entry marked resolved after
the change is archived.

## Impact

- Specs only: three capability spec files receive deltas when the change is archived.
- No production or test files change.
- No new durable behavior; this removes descriptions of behavior that was already removed.

## Non-goals

- Do not restore the placeholder page, `IsImplemented`, `InstanceCoordinator`, or `EmptyStateText`.
- Do not change any other requirement or scenario text.
- Do not change application behavior, tests, or architecture documents.
