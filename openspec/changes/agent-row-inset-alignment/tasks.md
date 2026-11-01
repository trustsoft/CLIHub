# Tasks: agent-row-inset-alignment

Implementation happens in a fresh session; `design.md` carries the full rationale, arithmetic, and
file references. Tasks are checked off as they land. The change is XAML-resource-only; no C# code,
ViewModel, or configuration changes are needed, and no test is expected to need an update (confirmed
in `design.md`, Concern 3).

## 1. Update the sizing token

- [x] 1.1 Read `src/CLIHub/Themes/Sizing.xaml:76` to verify the current `Sizing.AgentRow.Padding`
      value (`"10,0,10,0"`)
- [x] 1.2 Edit `Sizing.AgentRow.Padding` from `"10,0,10,0"` to `"12,0,12,0"` — both horizontal
      insets become 12, matching `Sizing.ProjectRow.Padding`'s left/right values
      (`src/CLIHub/Themes/Sizing.xaml:92`)
- [x] 1.3 Build: `dotnet build CLIHub.sln` — green with no XAML parsing errors
- [x] 1.4 Test: `dotnet test CLIHub.sln` — green (no assertion is expected to reference this token;
      if one unexpectedly fails, stop and reconcile before continuing — do not weaken the
      assertion to pass)
- [x] 1.5 Re-wrap the `Sizing.AgentRow.Padding` comment to the file's three-line convention (at most
      ~100 characters per line, same indentation as the neighbouring comments), keeping the meaning:
      equal 12px leading and trailing content insets, so the agent row's content starts and ends the
      same distance from the pane divider and the window border as the project row's content does
- [x] 1.6 Build: `dotnet build CLIHub.sln` — green (0 warnings, 0 errors) after the comment re-wrap
- [x] 1.7 Align the agent row's inter-row separator hairline with the row content: the `Rectangle`'s
      `Margin` in `AgentRowContainer` (`src/CLIHub/Themes/LaunchWindowStyles.xaml:290-293`) changes
      from `10,0,12,0` to `12,0,12,0`, so the hairline starts and ends at the row content's 12 DIP
      insets; the `Rectangle`'s height, brush, and visibility binding are unchanged
- [x] 1.8 Test: `dotnet test CLIHub.sln` — green (1334 passed, 0 failed) after the hairline change
- [x] 1.9 Correct the stale `PaneList` scroll-standoff comment above the `PART_VerticalScrollBar`
      visibility `Trigger` (`src/CLIHub/Themes/LaunchWindowStyles.xaml:371-374`): the 4px standoff is
      added on top of the row's own 12 DIP trailing padding (both rows are 12 since
      `project-row-trailing-inset` and this change), so while the bar is visible the rows end 16 DIP
      from the list edge — the old "10px" and the "same" cross-pane claim are gone; the `Trigger`, the
      `0,0,4,0` value, and every other line of that template are unchanged
- [x] 1.10 Build: `dotnet build CLIHub.sln` — green (0 warnings, 0 errors) after the comment correction

## 2. Documentation

- [x] 2.1 Re-read `docs/architecture/ui.md` lines 109-116 (the project row marker-rail paragraph)
      and confirm no sentence there claims a specific agent-row inset value or a cross-pane
      relationship that this change would change the truth of. If a sentence is found to be
      invalidated, add the minimal correction without touching the rest of the rail paragraph. If
      none is found (expected, per `design.md` Concern 4), make no edit to this file.
- [x] 2.2 In `docs/backlog.md`, section "UI and theming", mark the entry "Project and agent rows no
      longer end symmetrically about the pane divider" (currently around lines 586-598) done in the
      file's existing style: add `- **[done 2026-11-01: agent-row-inset-alignment]**` to the entry,
      and correct the "Proposed remedy" line's suggested value from `10,0,12,0` to the value this
      change actually ships, `12,0,12,0` (the backlog's draft had the divider-facing side backwards —
      see `design.md`, "Backlog's draft value was wrong"). Follow the pattern used by nearby
      `[done ...]`/`[closed ...]` entries in the same file (e.g. lines 215-227, 229-243) — keep the
      evidence paragraph, append the resolution, do not delete the history.
- [x] 2.3 Add a `Fixed` bullet under the current unreleased/next heading in `CHANGELOG.md` recording
      the cross-pane inset alignment (`agent-row-inset-alignment`, `launch-window-layout`), following
      the style of the entry the prior change added (see
      `openspec/changes/archive/2026-11-01-project-row-trailing-inset/tasks.md:21` for the pattern —
      check the actual heading/format already in `CHANGELOG.md` before adding).
- [x] 2.4 Build: `dotnet build CLIHub.sln` — green after the documentation updates (doc-only edits
      should not affect the build, but this gate catches an accidental source edit)
- [x] 2.5 Correct the `CHANGELOG.md` placement: `v1.0.4` is already tagged, so version 1.0.4 has
      shipped and neither row-inset bullet belongs under `## 1.0.4 — 2026-11-01`. Added an
      `## Unreleased` heading with a `### Fixed` group between the intro paragraphs and the 1.0.4
      heading, moved both bullets there verbatim (the `project-row-trailing-inset` project-row bullet
      and this change's `agent-row-inset-alignment` agent-row bullet), and removed them from the 1.0.4
      `### Fixed` group, which keeps its three remaining entries in place; `RELEASE-NOTES.md` and every
      other file are untouched, per `docs/releasing.md:38-41`
- [x] 2.6 Build: `dotnet build CLIHub.sln` — green (0 warnings, 0 errors) after the changelog correction

## 3. Manual smoke test

The user must perform this smoke test to verify the visual outcome before the change is archived.

- [x] 3.1 Run: `dotnet run --project src/CLIHub/CLIHub.csproj`
- [x] 3.2 Open the launch window (system tray → launch or hotkey)
- [x] 3.3 Confirm the agents pane's content (agent logo on the left, action buttons on the right)
      starts and ends the same distance from the pane divider as the project row's content
      (logo on the left, marker rail on the right) — both should read as 22 DIP from the divider
- [x] 3.4 Confirm the agents pane's content starts and ends the same distance from the window's
      outer borders as the project row's content does from its side — both should read as 22 DIP
      from the window's left/right borders respectively
- [x] 3.5 Confirm the agent row's action buttons (Launch, Resume) still sit flush at the row's
      trailing edge, with the same visible gap before them as before (the 24 DIP
      `Sizing.AgentRow.TextMargin` gap is unaffected by this change)
- [x] 3.6 Confirm nothing is clipped in either pane at the window's default width, and resize the
      Projects pane (drag the splitter) to confirm nothing clips in the Agents pane as its available
      width is affected indirectly
- [x] 3.7 Switch themes (Settings → Appearance → Dark/Light/Blue) and verify the equalized insets
      hold in each theme
- [x] 3.8 Report the smoke result back before requesting archive/commit approval, per
      `AGENTS.md`'s change/commit/archive policy

Confirmed by the user ("approve" after the smoke question, 2026-11-01). Orchestrator evidence from the
running build at 1.5x scaling: agent content 22 DIP from the pane edge (was 20), logo +2 DIP from the
divider, inter-row hairline 12/12 DIP, project row unchanged at 12/12 DIP, window 900x806 px (was
894x806, +4 DIP from the content-sized Agents pane).

## 4. Final validation

- [x] 4.1 Build: `dotnet build CLIHub.sln` — green with no warnings
- [x] 4.2 Tests: `dotnet test CLIHub.sln` — green (no test changes are expected; the change is
      XAML/resource-only)
- [x] 4.3 Validate: `openspec validate --all` — green (no spec deltas are produced; `skip_specs: true`
      is set in `.openspec.yaml`)
- [x] 4.4 Update graph: `graphify update .` — keep the knowledge graph current after the XAML/docs
      edits
- [x] 4.5 Quote the exact `openspec validate --all` output in the final report
- [x] 4.6 Wait for the user's smoke confirmation (step 3.8) before asking for archive/commit
      approval; do not stage or commit files before that approval is given
