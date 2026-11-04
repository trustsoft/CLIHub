## Stage 1: Normalize Palette Files

**Goal:** Make the three palette files structurally identical (header, inline comments, whitespace) while preserving every `Color` attribute value.

- [x] 1.1 Replace the header comment in `src/CLIHub/Themes/Dark.xaml` (lines 4-8) with the unified header from `design.md` Decision 1, preserving the existing blank line before `<!-- Surfaces -->`.
- [x] 1.2 Neutralize the inline comment before `TopHighlightBrush` in `Dark.xaml` (line 29): change "dark-chrome windows" to "windows with drawn chrome".
- [x] 1.3 Neutralize the inline comment before `TextPrimaryBrush` in `Dark.xaml` (lines 35-36): remove the dark-specific rationale sentence ("Primary text is one step off pure white..."), keep only "it sits a hair above MenuItemBrush so list names keep a slight edge over menu entries" → becomes the one-line comment from `design.md` Decision 2 point 4.
- [x] 1.4 Neutralize the Status section header comment in `Dark.xaml` (line 45): change "the single error tone (validation and failure messages on dark surfaces)" to "error, success, and selection tones" → becomes `<!-- Status: error, success, and selection tones -->`.
- [x] 1.5 Replace the header comment in `src/CLIHub/Themes/Light.xaml` (lines 4-8) with the unified header from `design.md` Decision 1.
- [x] 1.6 Add the inline comment before `PopoverOpenBrush` in `Light.xaml` (after line 13, before line 14): the 2-line comment from `design.md` Decision 2 point 1.
- [x] 1.7 Add the inline comment before `TopHighlightBrush` in `Light.xaml` (after line 26, before line 27): the 1-line comment from `design.md` Decision 2 point 2.
- [x] 1.8 Add the inline comment before `ScrollThumbBrush` in `Light.xaml` (after line 27, before line 28): the 1-line comment from `design.md` Decision 2 point 3.
- [x] 1.9 Add the inline comment before `TextPrimaryBrush` in `Light.xaml` (after line 30, before line 31): the 1-line comment from `design.md` Decision 2 point 4.
- [x] 1.10 Replace the Status section header in `Light.xaml` (line 39) with the unified Status header from `design.md` Decision 2 point 5: `<!-- Status: error, success, and selection tones -->`.
- [x] 1.11 Replace the header comment in `src/CLIHub/Themes/Blue.xaml` (lines 4-9) with the unified header from `design.md` Decision 1.
- [x] 1.12 Neutralize the inline comment before `TopHighlightBrush` in `Blue.xaml` (line 30): change "dark-chrome windows" to "windows with drawn chrome".
- [x] 1.13 Neutralize the inline comment before `TextPrimaryBrush` in `Blue.xaml` (lines 36-37): apply the same change as Dark (remove the theme-specific rationale, keep the MenuItemBrush relationship) → becomes the 1-line comment from `design.md` Decision 2 point 4.
- [x] 1.14 Neutralize the Status section header comment in `Blue.xaml` (line 46): change "the single error tone (validation and failure messages on dark surfaces)" to "error, success, and selection tones" → `<!-- Status: error, success, and selection tones -->`.
- [x] 1.15 **Verification pass:** For each palette file, confirm:
  - Header comment is the unified header from `design.md` Decision 1, byte-identical in all three files.
  - Inline comments appear at the same line numbers in all three files (relative to key declarations).
  - All 26 `Color="#..."` values are unchanged from the original file.
  - File encoding is still UTF-8 without BOM (starts with `3C`, not `EF BB BF`).
  - Line endings are still CRLF (Windows).
- [x] 1.16 **Gate:** `dotnet build CLIHub.sln` (0 warnings, 0 errors). If build fails, identify the cause (syntax error in XAML?) and fix it before proceeding.

## Stage 2: Strengthen Parity Test

**Goal:** Enforce the identical-skeleton invariant mechanically.

- [x] 2.1 In `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs`, add a new private helper method `ReadKeysOrdered` that returns `List<string>` (document order preserved) instead of `HashSet` — implementation from `design.md` Decision 3.
- [x] 2.2 After the existing key-set comparison loop (line ~35), add an ordered-key comparison loop using `SequenceEqual` — exact code from `design.md` Decision 3 (referenceKeyList, keyList, assertion message includes both expected and actual for debugging).
- [x] 2.3 Add a new private helper method `NormalizeShape(string xaml)` — implementation from `design.md` Decision 3 (regex masking `Color="#[0-9A-Fa-f]{6,8}"` → `Color="#FFFFFFFF"`, then trim trailing whitespace per line).
- [x] 2.4 After the ordered-key comparison, add a normalized-shape comparison loop — exact code from `design.md` Decision 3 (referenceNormalized, normalized, assertion message says "Check the normalized diff").
- [x] 2.5 Add a sanity check inside the normalized-shape comparison: `Assert.True(normalized.Contains("x:Key"), "Normalization must preserve key declarations.")` for the reference file and every compared file (xUnit has no `Assert.Contains` overload with a custom message).
- [x] 2.6 Add one edge-case `[Fact]` in the same test class covering `NormalizeShape` (a private static helper of the class, callable from another fact in the same class): masks `#RRGGBB` and `#AARRGGBB` including lowercase hex; leaves hex-looking text outside a `Color="..."` attribute untouched; preserves non-Color content verbatim.
- [x] 2.7 **Gate:** `dotnet test CLIHub.sln` — the `ThemePalettes_AllPaletteFilesDefineOneIdenticalKeySet` test MUST pass. If it fails:
  - Check the failure message: does it report a key-order difference or a shape difference?
  - If key-order: verify Stage 1 did not accidentally reorder keys (it should not have; only comments were changed).
  - If shape: diff the normalized outputs from two files to find the discrepancy (whitespace? missed comment?), then fix the palette files in Stage 1 and re-run.
- [x] 2.8 **Gate:** Run the full test suite `dotnet test CLIHub.sln` (all tests pass, not just the parity test). The normalization should not affect any other tests (no runtime behavior changed), but verify.

## Stage 3: Update Documentation

**Goal:** Reflect the strengthened invariant in `docs/architecture/ui.md`.

- [x] 3.1 At `docs/architecture/ui.md` line ~121 (after "define the same brush keys"), add one sentence: "The three files differ ONLY in colour values; their structure (comments, key order, whitespace) is identical and mechanically guarded by the parity test."
- [x] 3.2 At line ~126 (add-a-theme recipe, step 1, after "define the same brush keys with new colors"), replace that phrase with: "preserve the exact file structure and replace only the Color attribute values (the parity test enforces this)."
- [x] 3.3 At line ~139 (Theme Data Locations table, Palettes row, currently "colours only, one identical key set"), change to: "identical structure and key order, differing only in Color values."
- [x] 3.4 At line ~146 (static guards list, `ThemePaletteParityTests` entry, currently "every palette defines one identical key set"), change to: "palettes have identical structure (key order, comments, whitespace) differing only in Color values."
- [x] 3.5 **Verification pass:** Re-read the four edited locations in `ui.md` and confirm the wording is factually correct and consistent with the test's new behavior.

## Stage 4: Final Verification

**Goal:** Confirm all gates pass and the change is ready for commit/archive.

- [x] 4.1 **Gate:** `dotnet build CLIHub.sln` (0 warnings, 0 errors).
- [x] 4.2 **Gate:** `dotnet test CLIHub.sln` (all tests pass, no new failures, no skipped tests).
- [x] 4.3 **Gate:** `openspec validate --all` (0 errors). Quote the exact output in the task completion report.
- [x] 4.4 **Gate:** `graphify update .` (refresh the knowledge graph; no behavior change, but structural edits warrant a refresh).
- [x] 4.5 **Manual verification:** Run a helper script or an inline test snippet that calls `NormalizeShape` on all three palette files and diffs the results. Confirm they are byte-identical. This is a redundant check (the test already does it), but serves as a final confidence gate.
- [x] 4.6 Review the git diff for the three palette files: confirm ONLY comments changed, no `Color="#..."` values differ, and no keys were added/removed/reordered.
- [x] 4.7 Review the git diff for `ThemePaletteParityTests.cs`: confirm the new assertions were added and no existing assertions were weakened or removed.
- [x] 4.8 Review the git diff for `docs/architecture/ui.md`: confirm the four documented locations were updated and the wording is accurate.

## Implementation Notes

- **Task granularity:** Stage 1 tasks (1.1-1.16) are small and can be done in any order (Dark, Light, Blue are independent). Stage 2 tasks (2.1-2.8) must be done sequentially (helper methods before assertions). Stage 3 tasks (3.1-3.5) are independent. Stage 4 tasks (4.1-4.8) must be done in order (build → test → validate → graph → manual diff reviews).
- **No manual app smoke:** the palette edits change comments only — no `Color` value, key, or structural element — so the compiled resources are unchanged. Build (BAML compile), the full test suite, and the shape-parity assertion are the gates; an application run is optional user-side confidence, not a task of this change.
- **Rollback:** If any gate fails and cannot be fixed in the same session, revert the palette files, test, and docs to their original state. No persisted state changes; all changes are in versioned files.
- **Encoding preservation:** All edits must preserve UTF-8 without BOM and CRLF line endings. Use a text editor or tool that respects the existing encoding.
- **Whitespace discipline:** The palette files use 4-space indentation and blank lines between sections. Do not introduce tabs or extra blank lines.
- **Atomic edits:** Each palette file edit (1.1-1.14) must change ONLY the specified comment lines; do not touch any other lines (keys, Color values, closing tags).
