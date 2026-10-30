# Design: align-specs-with-dead-path-cleanup

## Verified current state

- `composition-and-dead-path-cleanup` is archived at
  `openspec/changes/archive/2026-10-30-composition-and-dead-path-cleanup/` (implementation commit
  `3133dd0`, archive commit `e663d79`).
- That change deleted `IInstanceCoordinator`, `InstanceCoordinator`, and `InstanceStatus`; the
  bootstrapper no longer performs a second single-instance check. `ApplicationHost` remains the only
  decision point.
- That change deleted `SettingsPlaceholderPage.xaml/.xaml.cs`, `SettingsViewModel.IsPlaceholderPageVisible`,
  `SettingsPage.IsImplemented`, and the `EmptyStateText` style. Every production Settings page is
  implemented.
- A repo-wide search confirms no residual references in `src` or `tests` to any of those symbols.
- The three scenarios named in `proposal.md` therefore describe code that no longer exists.

## Decisions

**Replace the requirement rather than edit it.** OpenSpec 1.15 rejects a MODIFIED requirement that
omits an existing scenario: the block replaces the whole requirement, so every original scenario
name must remain. Because the stale scenarios assert behavior that no longer exists, the requirement
is removed and re-added under a slightly broader name without that scenario. Retained scenarios are
copied verbatim, so their names and text are unchanged.

**Rename only as much as the removal requires.** "Single instance resource ownership" becomes
"Single instance ownership and disposal"; "Settings sections" becomes "Settings sections and pages";
"Sidebar navigation styling" becomes "Sidebar navigation and page styling". Each new name still
describes the retained contract.

**Do not add new requirements or scenarios.** The change is descriptive alignment, not a capability
change.

## Alternatives rejected

- **Leave the specs stale.** Rejected: the durable contract would claim capabilities the app does not
  have, and a future agent could "restore" removed code as a regression fix.
- **Restore the removed code.** Rejected: the archived change proves the paths were unreachable, and
  the user smoke confirmed the app works without them.
- **Amend `app-lifecycle` to assert the provider-owned path never occurs.** Rejected: a negative
  scenario adds no user-observable contract and duplicates the absence proof in the archived design.

## Risks

- Low. Spec-text only. `openspec validate --all` must pass, and the other scenario names in each
  modified requirement must be unchanged. There is no runtime, test, or build impact.
