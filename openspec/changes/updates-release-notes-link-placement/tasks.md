# Tasks

## Stage 1: XAML restructuring

- [x] 1.1 Move the "View release notes →" `Button` from `UpdatesHeaderAction` (currently `Grid.Row="1"`, `src/CLIHub/Views/SettingsWindow.xaml:141-148`) into `UpdatesHeaderTitleExtra` as the third child after the Channel chip's closing `</Border>` (after `:106`).
- [x] 1.2 Set the link's `Margin="8,0,0,0"` and `VerticalAlignment="Center"` to match the chips' spacing and alignment (Decision 2, Decision 3).
- [x] 1.3 Remove `Grid.Row="1"`, `VerticalAlignment="Bottom"`, and `Margin="0,4,0,0"` from the link — those attributes existed only for its bottom-right placement in the action column and are no longer applicable.
- [x] 1.4 Remove the `<Grid>` wrapper and `<Grid.RowDefinitions>` (`:112-116`) from `UpdatesHeaderAction`, leaving only the status/button `StackPanel` (`:118-139`) as the template's root element (Decision 4).
- [x] 1.5 Remove the `Grid.Row="0"` attribute from the `StackPanel` inside `UpdatesHeaderAction` (`:118`), since there is no longer a `Grid` parent.
- [x] 1.6 Set `VerticalAlignment="Center"` on the `UpdatesHeaderAction` `StackPanel` to match the `ProjectsHeaderAction` precedent (`:153-154`), so the status chip and button center within the header row rather than stretching to fill it (Decision 8).

### Gate 1.1: Build

```powershell
dotnet build CLIHub.sln
```

**Pass criterion:** Build succeeds with no errors. Warnings about unused variables or style violations are acceptable at this gate; they will be addressed if they relate to this change.

## Stage 2: Documentation and changelog

- [x] 2.1 Update `CHANGELOG.md`: add a new bullet under `### Changed` in the `## Unreleased` section (after the `settings-page-header-emblem` bullet at `:17`): "The 'View release notes' link on the Settings Updates page moved from the bottom-right of the page header to the title row, immediately after the 'Channel: Stable' chip, so the header reads as one line of metadata (title, version chip, channel chip, release-notes link) and one line of actions (status chip + check button) (`updates-release-notes-link-placement`, `preferences-ui`, `settings-theme`)."

### Gate 2.1: Build and test

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

**Pass criterion:** Build succeeds and all tests pass. No test changes are expected for this visual-only rearrangement, but the gate confirms no test broke due to an unintended side effect.

## Stage 3: Final validation

- [x] 3.1 Run the full build and test suite:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

**Pass criterion:** Build succeeds and all tests pass.

- [x] 3.2 Run OpenSpec validation for the main specs and this change:

```powershell
openspec validate --all
openspec validate updates-release-notes-link-placement --strict
```

**Pass criterion:** Both commands succeed with no errors. The `--all` validation confirms the main specs are still valid, and the `--strict` validation confirms this change's delta specs are well-formed and consistent.

- [x] 3.3 Verify that the delta specs (`openspec/changes/updates-release-notes-link-placement/specs/preferences-ui/spec.md` and `specs/settings-theme/spec.md`) match the current wording of the main specs (`openspec/specs/preferences-ui/spec.md:191-228` and `openspec/specs/settings-theme/spec.md:243-337`) except for the two intended edits: (1) "on the title row after the channel chip" instead of "aligned to the bottom-right of the header" in the `preferences-ui` requirement and scenario, and (2) "two metadata chips and a 'View release notes' link" / "the chips and link wrap together" / "the title, chips, or link" instead of "an extra title chip" / "the chip wraps together" / "the title or the chip" in the `settings-theme` scenario.

**Pass criterion:** The deltas contain only the intended wording changes; all other scenario names, WHEN/THEN/AND clauses, and requirement text are identical to the main specs.

- [x] 3.4 Run the repository's graph update (if `graphify-out/graph.json` exists):

```powershell
graphify update .
```

**Pass criterion:** The command completes successfully. If `graphify-out/` does not exist, skip this step.

## Stage 4: Manual smoke (user responsibility)

**Important:** Per `AGENTS.md:69-71`, this manual smoke is the user's responsibility. The implementer reports the completed XAML changes, the CHANGELOG update, and the gate results, then hands off to the user for the following checks. The orchestrator does not commit, stage, or archive until the user completes this smoke and reports the result, then grants explicit approval for commit and archive.

The user performs the following checks:

- [x] 4.1 Open the Settings window and navigate to the "Updates" page. Verify visually that the "View release notes →" link appears on the title row immediately after the "Channel: Stable" chip, with consistent spacing, and that the status chip and "Check for updates" button remain on a separate line below, centered within the header row.
- [x] 4.2 Resize the Settings window to its `MinWidth` (640 DIP). Confirm the chips and link wrap as a unit if they overflow the title column, and that the title "Updates" stays on the first line.
- [x] 4.3 Navigate to another Settings page (e.g., "General & Startup" or "Appearance"), then back to "Updates". Confirm the link reappears on the title row in the same position.
- [x] 4.4 Click the "View release notes →" link. Confirm the What's New window opens (the command binding is unchanged).
- [x] 4.5 Hover over the link. Confirm the tooltip "Open What's New" appears and the link changes color per the `LinkButton` style (accent → primary on hover).
- [x] 4.6 Tab through the Settings window's controls. Confirm the link is reachable via keyboard, and that activating it with Enter or Space opens the release notes window.
- [x] 4.7 Compare the Updates header's height against another page's header (e.g., "Projects"). Confirm the emblem stays vertically centered against the title+description block on both pages, with no visible shift (Decision 7 verification).
- [x] 4.8 Confirm the status chip and "Check for updates" button are vertically centered within the header row, not stretched or pinned to the top (Decision 8 verification).

**Smoke outcome:** The user reports whether all checks pass. If any check fails, the implementer revises the XAML; if all checks pass, the orchestrator reports to the user, then waits for the user's explicit commit and archive approval before proceeding.

## Notes

- **No test changes expected:** This is a visual rearrangement of existing XAML elements with no logic changes. The `ShowReleaseNotesCommand` binding, the `LinkButton` style, and the header-action rendering are all unchanged. If a test breaks, investigate whether it hard-coded a brittle assumption about the header structure.
- **No screenshot refresh:** Release preparation owns that (`docs/releasing.md`, step 0), not this change. Verified: `docs/images/` has no Updates page screenshot.
- **Spec deltas already written:** The delta files (`specs/preferences-ui/spec.md` and `specs/settings-theme/spec.md`) were created by the planner and validated before this `tasks.md` was finalized. The implementer does not create or edit them; Stage 3 verifies their consistency with the main specs.
- **Commit and archive are orchestrator actions:** The implementer does not run `git commit`, `git add`, or `openspec archive`. After Stage 3 gates pass and the user completes the manual smoke (Stage 4), the implementer reports the result and hands off to the orchestrator. The orchestrator then asks the user for explicit commit approval, stages only the files modified by this change (`src/CLIHub/Views/SettingsWindow.xaml`, `openspec/changes/updates-release-notes-link-placement/`, `CHANGELOG.md`), commits with the agreed message, asks for explicit archive approval, and runs `openspec archive updates-release-notes-link-placement`. This separation is required by `AGENTS.md:61-71` (Change, Commit, and Archive Policy).
