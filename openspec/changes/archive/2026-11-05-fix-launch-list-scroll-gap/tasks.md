# Tasks: Fix Launch List Scroll Gap

## Stage 1: Pixel-unit scrolling

- [x] 1.1 In `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`, add
      `<Setter Property="VirtualizingPanel.ScrollUnit" Value="Pixel"/>` to the `PaneList` style (near the
      existing `VirtualizingPanel.VirtualizationMode="Standard"` setter, currently line 177). Leave
      `ScrollViewer.CanContentScroll` unset (its effective value stays `True`) so virtualization stays on.
- [x] 1.2 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors.

## Stage 2: Zero-height agent row divider

- [x] 2.1 In `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`, rewrite the `AgentRowContainer`
      `ControlTemplate` (currently lines 146-158) so the divider `Rectangle` and the row `Border` (named
      `Bd`) are overlapping children of a `Grid` instead of sequential children of a `StackPanel`. Keep the
      divider's existing properties unchanged: `Height="1"`, `Fill="{DynamicResource RowDividerBrush}"`,
      `Margin="12,0,12,0"`, and the `IndexToDividerVisibility` binding on `ItemsControl.AlternationIndex`.
      Give the divider `VerticalAlignment="Top"` so it paints along the row's top edge without occupying
      layout height. Confirm the `Border`'s `Padding="{StaticResource Sizing.AgentRow.Padding}"` and its
      `IsMouseOver` → `HoverOverlayBrush` trigger (currently lines 160-162) are preserved unchanged.
- [x] 2.2 Resolve open question 1 from `design.md`: verify (by inspection of the resulting visual tree order,
      or a before/after screenshot comparison in both a themed build and at least one palette) that the
      divider is not visually hidden by the row's hover/selection fill in a way that changes today's
      appearance. Adjust `Panel.ZIndex` or child order only if needed to preserve the current look.
- [x] 2.3 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors.

## Stage 3: Comment correction

- [x] 3.1 In `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`, rewrite the `PaneList` comment (currently
      lines 178-183) to describe only: (a) the custom template avoids the default `ListBox` template's 1px
      border eating into the viewport, and (b) `MaxHeight` (434 DIP) is seven rows at the shared 62 DIP row
      pitch (both panes, after Stage 2). Remove the implication that row-height arithmetic is what makes the
      scrolled end position land flush; that property now comes from `VirtualizingPanel.ScrollUnit="Pixel"`
      (Stage 1).
- [x] 3.2 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors.

## Stage 4: Spec delta

- [x] 4.1 Confirm `openspec/changes/fix-launch-list-scroll-gap/specs/launch-window-layout/spec.md` (already
      created alongside this proposal) correctly MODIFIES "Scrolling lists" and "Agents pane and row sizing",
      preserving every original scenario name and only extending THEN clauses / adding new scenarios.
- [x] 4.2 Run `openspec validate --all` from the repository root and verify zero errors.

## Stage 5: Automated verification (UI Automation)

- [x] 5.1 Build the application in Release or Debug configuration (`dotnet build CLIHub.sln`) and locate the
      output `CLIHub.exe`.
- [x] 5.2 Write a small, throwaway UI Automation check (a standalone script or a temporary test using
      `System.Windows.Automation` / `FlaUI`, whichever is faster to wire up — no reusable harness exists in
      this repo today, see `design.md` "Test coverage") that:
      1. Starts `CLIHub.exe` and opens the launch window with at least 8 registered agents (seed test data if
         the running environment has fewer).
      2. Finds the Agents pane `ListBox` (`AutomationId`/control type `List`) and invokes
         `ScrollPattern.SetScrollPercent(ScrollPattern.NoScroll, 100)`.
      3. Reads the bounding rectangle of the last realized `ListItem` and the `ListBox`'s own bounding
         rectangle, and asserts the `ListItem`'s bottom edge is within 2-3 px of the `ListBox`'s bottom edge
         (matching the "flush, no gap" result the Projects list already produces — see `design.md`'s measured
         table: pre-fix gap was 83 px / 55 DIP at 150% DPI).
      4. With exactly 7 agents, asserts the Agents `ListBox`'s `ScrollPattern.VerticallyScrollable` is `false`
         (no scrollbar), matching the Projects list's behavior at 7 projects.
- [x] 5.3 Run the check against the pre-fix build (optional, for contrast) and the post-fix build; record the
      measured gap and scrollable-state values in the stage report. Expected post-fix result: gap ≤ 2-3 px
      (flush) and `VerticallyScrollable = false` at 7 agents, both matching the Projects list's existing
      numbers from `design.md`.
- [x] 5.4 Delete or clearly mark any throwaway script/test created only for this check; it is not meant to
      become a permanent part of the test suite unless the user asks for one.

## Stage 6: Full gate run

- [x] 6.1 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors.
- [x] 6.2 Run `dotnet test CLIHub.sln` from the repository root and verify all tests pass (no existing test
      reads XAML or layout, so none should newly fail or newly need updating — if one does, treat that as a
      finding to report, not something to silently patch around).
- [x] 6.3 Run `openspec validate --all` from the repository root and verify zero errors.
- [x] 6.4 Run `graphify update .` from the repository root to refresh the knowledge graph after source edits.

## Stage 7: Manual smoke test (user-performed)

- [x] 7.1 Open CLIHub with 8 or more registered agents. Scroll the Agents list to the end with the mouse
      wheel or the scrollbar thumb; confirm the last row ends flush with the pane's bottom edge, with no
      blank band below it.
- [x] 7.2 Scroll the Projects list to its end (with enough projects to require scrolling); confirm it still
      ends flush, matching its pre-fix behavior (no regression).
- [x] 7.3 With exactly 7 registered agents, confirm the Agents list shows no vertical scrollbar (all 7 rows
      fit without scrolling), matching the Projects list's behavior at exactly 7 projects.
- [x] 7.4 Confirm the agent row divider still looks like a thin hairline between rows (not above the first
      row), in both dark and light themes, and that hovering a row still shows the expected hover highlight
      without a visual seam or doubled line at the divider.
- [x] 7.5 Scroll both lists with the mouse wheel and confirm the wheel-scroll feel is still acceptable
      (see `design.md` open question 2: pixel-unit scrolling can change the per-click scroll distance from
      the previous item-based behavior).
