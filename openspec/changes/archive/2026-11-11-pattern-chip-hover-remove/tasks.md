# Implementation Tasks: Pattern Chip Hover-Reveal Remove Button

## Stage 1: XAML Restructure

Restructure the ignored-pattern chip DataTemplate in SettingsProjectsPage.xaml to support hover-reveal using a ContentControl with a custom ControlTemplate.

- [x] 1.1 Back up the current DataTemplate (lines ~221-246) as a comment or save it separately for reference (satisfied via design.md "Verified Current State" — the original XAML is preserved verbatim there; no commented-out block left in the source)
- [x] 1.2 Replace the DataTemplate with a ContentControl-based structure:
  - [x] 1.2.1 Wrap the chip in a `<ContentControl Content="{Binding}">` element
  - [x] 1.2.2 Define a custom `<ControlTemplate TargetType="ContentControl">` inside `ContentControl.Template`
  - [x] 1.2.3 Move the Border (chip outer) into the ControlTemplate and name it `x:Name="ChipBorder"`
  - [x] 1.2.4 Add `Focusable="True"` to ChipBorder (makes the chip keyboard-focusable)
  - [x] 1.2.5 Add `FocusVisualStyle="{x:Null}"` to ChipBorder (suppresses the default dotted focus rectangle — the revealed "×" serves as the focus indicator)
  - [x] 1.2.6 Preserve all Border properties: Margin, Padding, Background, BorderBrush, BorderThickness, CornerRadius (unchanged from current)
- [x] 1.3 Inside the Border, replace the StackPanel with a Grid:
  - [x] 1.3.1 Add a horizontal StackPanel as the first Grid child (sizing layer):
    - [x] 1.3.1.1 Move the TextBlock into this StackPanel, name it `x:Name="PatternText"`, and change `Text="{Binding}"` to `Text="{TemplateBinding Content}"`
    - [x] 1.3.1.2 Add a placeholder Border: `<Border Width="20" Height="20" Margin="4,0,0,0"/>` (reserves button space)
  - [x] 1.3.2 Add the Button as the second Grid child (overlay layer):
    - [x] 1.3.2.1 Name it `x:Name="RemoveButton"`
    - [x] 1.3.2.2 Set `HorizontalAlignment="Center"` and `VerticalAlignment="Center"`
    - [x] 1.3.2.3 Set `Visibility="Collapsed"` (hidden by default)
    - [x] 1.3.2.4 Preserve AutomationProperties.Name, Style, ToolTip, and the Close glyph TextBlock
    - [x] 1.3.2.5 Update CommandParameter binding: `CommandParameter="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Content}"` (TemplatedParent is ContentControl, Content is the pattern string)
    - [x] 1.3.2.6 Keep the Command binding unchanged: `Command="{Binding DataContext.Scanning.RemoveIgnoredPatternCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"`
- [x] 1.4 Add `<ControlTemplate.Triggers>` section after the Border:
  - [x] 1.4.1 Add first trigger: `<Trigger SourceName="ChipBorder" Property="IsMouseOver" Value="True">`
    - [x] 1.4.1.1 Inside this trigger, add `<Setter TargetName="PatternText" Property="Visibility" Value="Hidden"/>` (CRITICAL: use "Hidden" NOT "Collapsed" — Hidden reserves the layout slot while making the text invisible, preventing the chip from shrinking on hover)
    - [x] 1.4.1.2 Add `<Setter TargetName="RemoveButton" Property="Visibility" Value="Visible"/>` (show button on hover)
  - [x] 1.4.2 Add second trigger (keyboard focus): `<Trigger SourceName="ChipBorder" Property="IsKeyboardFocusWithin" Value="True">`
    - [x] 1.4.2.1 Inside this trigger, add the SAME two setters as the hover trigger: `<Setter TargetName="PatternText" Property="Visibility" Value="Hidden"/>` and `<Setter TargetName="RemoveButton" Property="Visibility" Value="Visible"/>`
  - [x] 1.4.3 DO NOT add any setter for the placeholder Border — it remains unchanged in both states
  - [x] 1.4.4 Verify the two triggers are independent (OR semantics): the button reveals on hover OR keyboard focus (verified statically: two sibling `<Trigger>` elements in ControlTemplate.Triggers, each with its own setters)

**Gate 1.1 (Build):** Run `dotnet build CLIHub.sln` and verify zero errors. If the build fails, fix XAML syntax errors (malformed bindings, missing namespace prefixes, unclosed tags) before proceeding.

**Gate 1.2 (Manual Smoke - Visual Check):** Run the application, open Settings → Project Scanning, verify:
- Ignored-pattern chips render without errors (no blank chips, no binding errors in Output window)
- At rest (no hover): chips show pattern text, no "×" button visible
- Hover one chip: "×" button appears centered, pattern text hidden, chip size unchanged
- Hover off: button disappears, text reappears
- Hover a different chip: only that chip's button appears, others unaffected
- **If any visual state is incorrect, diagnose the trigger/binding issue and fix before proceeding to Stage 2.**

## Stage 2: Functional Verification

Verify the remove button's command binding and interaction behavior.

- [x] 2.1 Manual test: hover a chip, click the "×" button, verify the pattern is removed from the list and the collection updates
- [x] 2.2 Verify the Command still fires correctly (check ViewModel RemoveIgnoredPattern method is called with the correct pattern string)
- [x] 2.3 Test with multiple chips: remove one, verify others remain and their hover states work independently
- [x] 2.4 Test edge case: hover a chip, then move mouse to another chip without leaving the first — verify only the currently hovered chip shows its button

**Gate 2.1 (Functional):** All remove operations succeed; no binding errors in Output window; Command fires with correct pattern parameter. If the Command does not fire or the parameter is null/wrong, check the RelativeSource and TemplatedParent binding in the CommandParameter.

## Stage 3: Keyboard Accessibility and Edge Cases

Verify keyboard accessibility behavior and edge cases.

- [x] 3.1 Keyboard navigation to chip:
  - [x] 3.1.1 Tab through the Settings page until focus reaches an ignored-pattern chip (ChipBorder becomes focused)
  - [x] 3.1.2 Verify "×" button reveals when the chip receives focus (same visual treatment as hover: text hides, button appears centered)
  - [x] 3.1.3 Verify chip dimensions do NOT change on focus (chip width stable, no WrapPanel reflow) — CRITICAL: if the chip shrinks, PatternText was set to "Collapsed" instead of "Hidden" (go back and fix the trigger)
- [x] 3.2 Keyboard navigation to button:
  - [x] 3.2.1 With a chip focused (button visible), press Tab again
  - [x] 3.2.2 Verify focus moves to the RemoveButton itself (the button is tabbable when Visible)
  - [x] 3.2.3 Verify "×" remains visible while the button is focused (IsKeyboardFocusWithin remains True because the button is a descendant of ChipBorder)
- [x] 3.3 Keyboard removal:
  - [x] 3.3.1 With the RemoveButton focused, press Enter or Space
  - [x] 3.3.2 Verify the pattern is removed from the list immediately (chip disappears)
  - [x] 3.3.3 Verify focus moves to the next tab stop (next chip, or next focusable element if no chips remain) — standard WPF behavior when the focused element is removed
- [x] 3.4 Focus blur:
  - [x] 3.4.1 Tab to a chip (button reveals), then Shift+Tab (or Tab away without pressing Enter)
  - [x] 3.4.2 Verify "×" collapses and pattern text reappears when focus leaves the chip
- [x] 3.5 Mouse click on chip body (not button):
  - [x] 3.5.1 Click the chip body (the Border area, not the button)
  - [x] 3.5.2 Verify the chip receives focus and "×" reveals (consistent focus-indicator behavior)
- [ ] 3.6 Verify screen reader behavior (if testing tools available):  â€” not exercised: no screen-reader tooling in this session (UIA behavior documented in design.md behavioral notes)
  - [ ] 3.6.1 The button is not announced when collapsed
  - [ ] 3.6.2 When revealed (hover or focus), the button is announced as "Remove pattern, Button"
- [x] 3.7 Test with long pattern text: verify text truncates (if longer than chip width), and on hover/focus the button is centered regardless of text length
- [x] 3.8 Test WrapPanel layout stability: add multiple patterns, hover and tab to chips in different positions, verify the WrapPanel does not reflow (chip dimensions stable)
- [ ] 3.9 Test theme switching: apply Dark, Light, Deep Blue, and System themes; verify the chip colors (ChipBgBrush, FieldStrokeBrush) and button hover overlay (HoverOverlayBrush) render correctly in all themes â€” waived: chips reuse the same DynamicResource brushes as the original template; no theme-dependent resource changed

**Gate 3.1 (Accessibility & Polish):** Keyboard accessibility works (tab to chip → button reveals, tab to button → focus lands on button, Enter removes pattern); no visual glitches; WrapPanel layout stable; themes render correctly. CRITICAL: Verify the chip dimensions do NOT change on hover OR focus — if the chip shrinks, PatternText was set to "Collapsed" instead of "Hidden" (go back and fix the trigger).

## Stage 4: Verification Against Other Chips

Verify that ONLY the ignored-pattern chips have hover-reveal, and all other ChipRemoveButton usages remain always-visible.

- [x] 4.1 Open Settings → Project Scanning, verify watched-folder rows (Rescan, Remove icon buttons) are always visible (no change)
- [x] 4.2 Open Settings → Projects, verify project rows with actions are always visible (no chip removal buttons here, but verify the page renders normally)
- [x] 4.3 Open Settings → CLI Agents, verify agent state chips have no remove buttons and render normally (no change)
- [x] 4.4 Verify no other XAML file was accidentally modified: run `git status` and confirm only SettingsProjectsPage.xaml is changed

**Gate 4.1 (Regression Check):** All other pages and chip usages are unaffected; only ignored-pattern chips have hover-reveal. If any other page's layout changed, revert accidental edits.

## Stage 5: Build, Test, and Validate

Run the full project build, automated tests, and OpenSpec validation.

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify zero errors and zero warnings (or only pre-existing warnings)
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass (no new failures introduced)
- [x] 5.3 Run `openspec validate --all` and verify zero errors (all spec deltas and artifacts are valid)

**Gate 5.1 (Build & Test):** Build succeeds, all tests pass, OpenSpec validation passes. If tests fail, check if any XAML change broke a UI test (unlikely for this change, but verify). If OpenSpec validation fails, fix spec delta errors before proceeding.

## Stage 6: Manual Smoke Testing (Final)

Perform a final end-to-end smoke test of the hover-reveal feature.

- [x] 6.1 Close and restart the application (cold start)
- [x] 6.2 Open Settings → Project Scanning
- [x] 6.3 Add 3-5 ignored patterns with varying text lengths (short, medium, long)
- [x] 6.4 Verify at-rest state: no "×" buttons visible, pattern text shown
- [x] 6.5 Hover each chip sequentially and verify:
  - [x] 6.5.1 "×" button appears centered on hover
  - [x] 6.5.2 Pattern text is hidden while hovered (invisible but the chip's width is unchanged — this confirms the text uses Visibility="Hidden" not "Collapsed")
  - [x] 6.5.3 Chip outer dimensions do not change (no WrapPanel reflow) — CRITICAL: measure or visually confirm the chip width stays constant; if it shrinks on hover, PatternText was incorrectly set to "Collapsed" instead of "Hidden"
  - [x] 6.5.4 Moving mouse off the chip: button disappears, text reappears
- [x] 6.6 Test keyboard interaction:
  - [x] 6.6.1 Tab to a chip → verify "×" reveals and text hides (chip width unchanged)
  - [x] 6.6.2 Tab again → verify focus moves to the "×" button (button remains visible)
  - [x] 6.6.3 Press Enter → verify pattern is removed and focus moves to next tab stop
  - [x] 6.6.4 Tab to another chip, then Shift+Tab away → verify "×" collapses and text reappears
- [x] 6.7 Hover one chip, click "×", verify:
  - [x] 6.7.1 Pattern is removed from the list immediately
  - [x] 6.7.2 Remaining chips re-layout in the WrapPanel correctly
  - [x] 6.7.3 Hovering the remaining chips still works
- [x] 6.8 Close Settings, reopen, verify the patterns persisted correctly (removed patterns are gone, remaining patterns are still there)
- [x] 6.9 Verify watched-folder row buttons (Rescan, Remove) on the same page are still always-visible (unchanged)

**Gate 6.1 (Smoke Test Pass):** All manual verification steps pass; hover-reveal works correctly (mouse and keyboard); remove action works; no regressions on other chips. If any step fails, diagnose and fix before reporting completion.

## Stage 7: Documentation and Completion

Update project artifacts and report completion.

- [x] 7.1 Run `graphify update .` to refresh the project knowledge graph (if graphify is in use)
- [x] 7.2 Verify `git status` shows only expected changes: SettingsProjectsPage.xaml modified, openspec/changes/pattern-chip-hover-remove/ added
- [x] 7.3 Report completion: list files modified, validation results, any open questions or deviations from the design

**Gate 7.1 (Completion):** All tasks completed, all gates passed, ready for user review and commit approval.

## Stage 8: Revision — Remove Reserved Button Space

User decision after reviewing the running app: remove the permanently reserved button space on the right side of each chip. The chip must now hug its text with symmetric padding.

- [x] 8.1 Open `src/CLIHub/Views/SettingsProjectsPage.xaml` and locate the ignored-pattern chip ControlTemplate (the one with ChipBorder, PatternText, RemoveButton, and two triggers)
- [x] 8.2 Delete the placeholder Border entirely:
  - [x] 8.2.1 Find the StackPanel in the Grid's first layer (comment: "Sizing layer: TextBlock + placeholder for button space" or similar)
  - [x] 8.2.2 Remove the `<Border Width="20" Height="20" Margin="4,0,0,0"/>` element
  - [x] 8.2.3 Remove the StackPanel wrapper if it now contains only the TextBlock — place the TextBlock directly in the Grid
- [x] 8.3 Change ChipBorder Padding from "8,4,4,4" to "8,4" (symmetric horizontal padding)
- [x] 8.4 Verify no other sizing element exists in the Grid's first layer (only PatternText should determine the chip's content size)
- [x] 8.5 Verify the RemoveButton remains in the Grid's overlay layer with `HorizontalAlignment="Center"`, `VerticalAlignment="Center"`, and `Visibility="Collapsed"` by default (unchanged)
- [x] 8.6 Verify both triggers (IsMouseOver, IsKeyboardFocusWithin) are unchanged and still set PatternText `Visibility="Hidden"` and RemoveButton `Visibility="Visible"`
- [x] 8.7 Run `dotnet build CLIHub.sln` and verify zero errors
- [x] 8.8 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 8.9 Run `openspec validate --all` and verify zero errors
- [x] 8.10 Note: Interactive smoke testing for this revision is the user's responsibility (they reviewed the running app and requested the change)

**Gate 8.1 (Revision Complete):** Placeholder Border deleted, Padding changed to "8,4", build/test/validate all pass. The chip now hugs its text; size stability is still guaranteed by PatternText `Visibility="Hidden"` on hover/focus.

## Stage 9: Revision — Constant Chip Height via Hidden Button

User report after running the app: chip height differs between hovered and non-hovered states (at-rest chip is shorter; hovered chip grows taller). Root cause: the inner Grid measures the max of its visible children — at rest RemoveButton is Collapsed (excluded), so Grid measures ~16px text; on hover RemoveButton becomes Visible (20px), so Grid measures 20px. User decision revision 3: achieve constant height by making RemoveButton default to `Visibility="Hidden"` (layout slot preserved) instead of `Collapsed`, NOT via Grid MinHeight="20". Stage 9 (MinHeight approach) was never implemented; the source still has Collapsed and no MinHeight.

- [x] 9.1 Open `src/CLIHub/Views/SettingsProjectsPage.xaml` and locate the ignored-pattern chip ControlTemplate's RemoveButton (the Button with x:Name="RemoveButton")
- [x] 9.2 Change RemoveButton's default `Visibility="Collapsed"` to `Visibility="Hidden"` (Hidden keeps the element in layout measurement while rendering nothing, so the Grid measures the 20×20px button in both at-rest and hovered/focused states)
- [x] 9.3 Verify the inner Grid does NOT have `MinHeight="20"` attribute (the MinHeight approach was documented but never implemented; the Hidden button alone achieves constant height)
- [x] 9.4 Verify both triggers (IsMouseOver, IsKeyboardFocusWithin) are unchanged and still set RemoveButton `Visibility="Visible"` and PatternText `Visibility="Hidden"`
- [x] 9.5 Verify no other changes are needed: ChipBorder Padding remains "8,4", ChipBorder Focusable="True" and FocusVisualStyle="{x:Null}", triggers unchanged, bindings unchanged
- [x] 9.6 Confirm behavioral notes:
  - [x] 9.6.1 Hidden elements are skipped by tab traversal (same as Collapsed) — keyboard flow is unchanged: button not tabbable at rest, becomes tabbable when triggers set it to Visible (verified against WPF framework behavior as documented in design.md)
  - [x] 9.6.2 Hidden elements are not hit-testable — the invisible button cannot steal clicks (WPF framework behavior)
  - [x] 9.6.3 Hidden elements remain in UIA tree as offscreen (unlike Collapsed) — mild automation discoverability improvement, no regression
  - [x] 9.6.4 For very short pattern text, the reserved 20px button slot may make the chip marginally wider than text+padding alone — accepted (max-of-both-states sizing, consistent with constant-height goal)
- [x] 9.7 Calculate expected chip outer height: 20px content (Grid measures max of text ~16px and button 20px = 20px) + 8px vertical padding + 2px border = 30px in both states (equals the original pre-change chip height)
- [x] 9.8 Run `dotnet build CLIHub.sln` and verify zero errors
- [x] 9.9 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 9.10 Run `openspec validate --all` and verify zero errors
- [x] 9.11 Note: Interactive check (at-rest vs hovered chip heights are equal) is the user's responsibility

**Gate 9.1 (Constant Height Revision Complete):** RemoveButton default Visibility changed Collapsed→Hidden, inner Grid has no MinHeight, build/test/validate all pass. Chip height is now constant at ~30px (the original familiar height) in both at-rest and hovered/focused states. Both PatternText (on hover/focus) and RemoveButton (at rest) use `Visibility="Hidden"` to preserve layout slots for width and height stability respectively.

---

**Total estimated stages:** 9 (7 original + 2 revisions)  
**Manual smoke testing required:** Yes (Gates 1.2, 6.1; Stage 8 smoke by user; Stage 9 interactive height check by user)  
**Automated gates:** Build (1.1, 5.1, 8.1, 9.1), Test (5.1, 8.1, 9.1), OpenSpec validation (5.1, 8.1, 9.1)

## Smoke Record

Manual smoke confirmed by the user on 2026-11-11 across three running-app reviews: hover/focus reveal (centered X, text hidden), constant chip dimensions (width via revisions 1+3, height via revision 3 Hidden button), keyboard flow (Tab to chip, Tab to button, Enter removal), click removal, short-pattern rendering, watched-folder rows unchanged, persistence across app restarts. Items 3.6 and 3.9 were not interactively exercised (see inline notes).

