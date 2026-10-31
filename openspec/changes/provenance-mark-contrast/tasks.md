# Tasks: provenance-mark-contrast

## 1. Add shared style to Controls.xaml

- [x] 1.1 Add the `ProjectProvenanceMark` style to `src/CLIHub/Themes/Controls.xaml` after the existing typography styles (after line 56, before the drawn chrome section at line 58). Use `TextPrimaryBrush` for manual projects (`CanRemove=True`) and `TextMutedBrush` default for discovered. Verify the solution compiles with 0 warnings/errors.

## 2. Remove duplicated definitions

- [x] 2.1 Delete the local `ProjectProvenanceMark` style from `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` (lines 10-24). Keep the `<UserControl.Resources>` tags and the `ProjectMissingChip` style that follows. The `Style="{StaticResource ProjectProvenanceMark}"` reference at lines 152-155 remains unchanged. Verify the solution compiles.

- [x] 2.2 Delete the duplicate `ProjectProvenanceMark` style and its comment from `src/CLIHub/Themes/LaunchWindowStyles.xaml` (lines 251-266). The `Style="{StaticResource ProjectProvenanceMark}"` reference in `LaunchWindow.xaml:252` remains unchanged. Verify the solution compiles with 0 warnings/errors.

## 3. Update backlog

- [x] 3.1 Mark the backlog item at `docs/backlog.md:243` as resolved by changing the heading to `### Provenance-mark style is duplicated across two project surfaces - **[done 2026-10-31]**` and replacing the proposed-remedy line with a resolution line: `- Resolved (2026-10-31) by `provenance-mark-contrast`: promoted to `Controls.xaml` with the contrast-safe brush rule (manual `TextPrimaryBrush`, discovered `TextMutedBrush`).` Do not add a commit sha; the orchestrator records it after the commit.

## 4. Build and test gates

- [x] 4.1 Run `dotnet build CLIHub.sln` from the repository root and verify 0 warnings/errors.

- [x] 4.2 Run `dotnet test CLIHub.sln` and verify all tests pass (no behavioral change, so test count and pass rate should be unchanged).

## 5. Manual smoke verification

- [x] 5.1 Launch the application. Navigate to Settings → Projects. Observe that:
  - Manual projects (`M` mark) use a strong, high-contrast foreground (readable in the current theme)
  - Discovered projects (`D` mark) use a muted but readable foreground
  - The mark positioning and alignment are unchanged

- [x] 5.2 From Settings → Appearance, switch to Light theme. Verify the manual mark is readable (no longer nearly invisible). Screenshot or note the observation.

- [x] 5.3 Switch to Dark theme. Verify both marks are readable with appropriate contrast.

- [x] 5.4 Switch to Blue theme. Verify both marks are readable.

- [x] 5.5 Return to the launch window (main popup). Select a project. Observe that:
  - Manual projects (`M` mark, right marker rail) use the same strong foreground as on the Settings page
  - Discovered projects (`D` mark) use the same muted foreground
  - The mark positioning and alignment are unchanged
  - Both surfaces are visually consistent

- [x] 5.6 With the launch window open, cycle through Light, Dark, and Blue themes again (via Settings → Appearance). Verify the launch-window marks remain readable in all themes.

## 6. Validation

- [x] 6.1 Run `npx --yes openspec validate provenance-mark-contrast` from the repository root and verify it reports 0 errors.

---

**Verification notes:**
- Smoke tasks 5.1-5.6 require actual application launch and theme switching; they cannot be automated.
- A screenshot showing the mark in Light theme (both surfaces) is sufficient visual evidence.
- The validation command targets this change only (not `--all`) because a sibling planner may be writing a concurrent change.
