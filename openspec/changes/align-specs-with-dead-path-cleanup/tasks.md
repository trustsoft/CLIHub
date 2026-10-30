# Tasks: align-specs-with-dead-path-cleanup

## Stage 1: Spec deltas

- [x] 1.1 Add `specs/app-lifecycle/spec.md` that removes Requirement "Single instance resource ownership" and adds "Single instance ownership and disposal": keep the requirement text and the scenarios "Production registration has one owner", "Duplicate launch exits before environment setup", and "First instance releases the mutex through provider disposal"; the "Second instance exits through the provider lifecycle" scenario does not reappear.
- [x] 1.2 Add `specs/preferences-ui/spec.md` that removes Requirement "Settings sections" and adds "Settings sections and pages": remove the sentence "A page whose settings are not yet implemented SHALL show a neutral empty state instead of controls." and do not re-add the "Page without implemented settings" scenario; keep every other scenario name and its text unchanged.
- [x] 1.3 Add `specs/settings-theme/spec.md` that removes Requirement "Sidebar navigation styling" and adds "Sidebar navigation and page styling": keep the requirement text and the scenarios "Group labels and items", "Each item shows a leading icon", "Active page highlighted", and "Page header"; do not re-add the "Placeholder page readable" scenario.
- [x] 1.4 Confirm no file under `src/`, `tests/`, or `docs/architecture/` is modified by this change.

## Stage 2: Validation

- [x] 2.1 Run `openspec validate --all` and confirm the change validates with a spec delta (no `skip_specs`).
- [x] 2.2 Re-read the three delta files and confirm every retained scenario name matches the original spec verbatim.

## Stage 3: Archive and close-out (needs explicit user approval)

- [ ] 3.1 Run `openspec archive align-specs-with-dead-path-cleanup --yes` and confirm the three scenarios are gone from `openspec/specs/` and the change moved under `openspec/changes/archive/`.
- [ ] 3.2 Mark the "Spec text still describes the removed placeholder and defensive instance paths" entry in `docs/backlog.md` resolved, with the change id and commit shas.
- [ ] 3.3 Commit the archive move and the backlog close-out separately, only with explicit approval.
