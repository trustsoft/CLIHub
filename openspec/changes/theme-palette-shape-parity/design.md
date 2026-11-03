## Context

See `proposal.md` — Why. The three palette files already declare the same 26 `SolidColorBrush` keys in the same order with the same section headers. The user has approved "Полная нормализация + guard" (full normalization plus guard): normalize the three files to an identical skeleton and enforce it with a mechanical test guard.

## Verified Current State

### Palette Files

- `src/CLIHub/Themes/Dark.xaml`: 50 lines, 26 keys in 4 sections (`Surfaces`, `Strokes and overlays`, `Text`, `Status`).
  - Header comment at lines 4-8 (5 lines total): "Dark palette. Colours only: … the launch window's styles live in LaunchWindowStyles.xaml … Values follow the approved popup design…"
  - Inline comments before `PopoverOpenBrush` (lines 14-15), `TopHighlightBrush` (line 29), `ScrollThumbBrush` (line 31), `TextPrimaryBrush` (lines 35-36), and status section header (line 45: "the single error tone (validation and failure messages on dark surfaces)").
  - Dark-specific wording: "dark-chrome windows" (line 29), "pure white reads harsh on the dark chrome" (line 35), "on dark surfaces" (line 45).

- `src/CLIHub/Themes/Light.xaml`: 44 lines, same 26 keys and 4 sections.
  - Header comment at lines 4-8 (5 lines total): "Light palette. Defines the same keys as Dark.xaml … Merged on top of the base palette only when the effective theme is light."
  - NO inline comments (blank lines at the positions where Dark/Blue have comments: lines 14, 27-28, 31).
  - Status section header at line 39: just `<!-- Status -->` (no descriptive text).

- `src/CLIHub/Themes/Blue.xaml`: 51 lines, same 26 keys and 4 sections.
  - Header comment at lines 4-9 (6 lines total): same as Light plus "Defines blue-tinted chrome and surface brushes only; the accent is application-scoped and applies independently of the theme."
  - Inline comments at lines 15-16 (PopoverOpenBrush), 30-31 (TopHighlightBrush/ScrollThumbBrush), 36-37 (TextPrimaryBrush), 46 (Status section: "the single error tone (validation and failure messages on dark surfaces)").
  - Same dark-specific wording as Dark in the inline comments.

**Key order and structure:** Already identical across all three files. Section headers appear at the same relative positions. Whitespace (4-space indentation, blank lines between sections) is consistent.

**Encoding:** All three files have no BOM (start with `3C` = `<`). Line endings are CRLF (Windows).

### Existing Test

`tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs` (50 lines):
- Enumerates `src/CLIHub/Themes/*.xaml` (non-recursive, alphabetically ordered).
- Loud-failure assertions: the three known palettes (`Dark.xaml`, `Light.xaml`, `Blue.xaml`) must be present.
- Compares key SETS using `HashSet<string>` (line 43-48: regex `x:Key="([^"]+)"`, then `Except` + `OrderBy`).
- Asserts `AccentBrush` and `AccentSelectionBrush` are NOT declared in any palette (lines 39-40).
- **Does not** check key order, file structure, or comment parity.

### Documentation

`docs/architecture/ui.md`:
- Lines 121-130: "The registered palettes … define the same brush keys" and the "To add another theme" recipe mentions copying `Dark.xaml` and defining "the same brush keys with new colors." Also: "The palette parity test enumerates every `Themes/*.xaml` … so the new palette joins the key-set check automatically."
- Lines 139: Theme Data Locations table: "Palettes … (colours only, one identical key set)".
- Lines 144-154: Static guards list, entry for `ThemePaletteParityTests.cs` — "every palette defines one identical key set" (does not mention order or shape).

## Goals / Non-Goals

**Goals:**
- Palettes differ ONLY in `Color` attribute values; everything else (header, inline comments, structure, whitespace) is identical.
- The test mechanically enforces the invariant so drift is caught at test time.
- Documentation reflects the strengthened invariant.

**Non-Goals:**
- No colour value changes.
- No new/removed brush keys.
- No runtime behavior change.
- No changes to `Common/` dictionaries, `LaunchWindowStyles.xaml`, or other theme files.

## Decisions

### Decision 1: Unified Header Comment (No Theme-Specific Sentences)

**Chosen:** One shared header comment for all three files, with NO per-theme customization (not even a masked palette name). The header describes the palette contract generically: what keys it declares, where it is merged, and how to add a new theme.

**Exact unified header text:**

```xml
<!--
    Theme palette. Defines the application brush keys (chrome, surfaces, strokes,
    overlays, text, and status). Dark.xaml is the base palette merged by App.xaml;
    Light.xaml and Blue.xaml are swapped in at runtime by PaletteSwitcher when the
    effective theme resolves to light or blue. To add another theme, copy this file
    and replace the Color values; see docs/architecture/ui.md.
-->
```

**Rationale:**
- Factually correct for all three files: Dark IS the base (merged by `App.xaml`), Light/Blue ARE swapped in (by `PaletteSwitcher`).
- The Blue-specific accent sentence ("the accent is application-scoped…") is dropped because it is already documented in `docs/architecture/ui.md` (~line 123) and does not belong in every palette file.
- No masking needed; the header is byte-identical.

**Alternatives considered:**
- Keep the palette name in the header (e.g. "Dark palette", "Light palette") and mask it in the test → Rejected: adds masking complexity and a false-positive risk (what if "Dark" appeared in a comment elsewhere?). The unified header is simpler and equally informative.
- Separate headers with theme-specific wording → Rejected: contradicts the identical-skeleton goal.

### Decision 2: Neutral Inline Comments

**Chosen:** Every palette file declares the same inline comments at the same positions. Dark-specific wording is removed or neutralized:

1. **Before `PopoverOpenBrush` (currently Dark.xaml:14-15, Blue.xaml:15-16; missing in Light):**

   ```xml
   <!-- Open actions-menu surface: the Actions control's pressed tone (the pressed overlay over the
        pane header), used as the open menu's background so the control and menu share one fill. -->
   ```

   **Change:** "dark-chrome windows" → removed (the comment already says "pane header", which is theme-neutral).

2. **Before `TopHighlightBrush` (currently Dark.xaml:29, Blue.xaml:30; missing in Light):**

   ```xml
   <!-- 1px highlight line drawn across the top edge of windows with drawn chrome -->
   ```

   **Change:** "dark-chrome windows" → "windows with drawn chrome" (neutral; both Dark and Light have drawn chrome).

3. **Before `ScrollThumbBrush` (currently Dark.xaml:31-32, Blue.xaml:32-33; missing in Light):**

   ```xml
   <!-- Thumb of the slim application-level scrollbar -->
   ```

   **Change:** Already neutral; keep as-is.

4. **Before `TextPrimaryBrush` (currently Dark.xaml:35-36, Blue.xaml:36-37; missing in Light):**

   ```xml
   <!-- Primary text sits a hair above MenuItemBrush so list names keep a slight edge over menu entries. -->
   ```

   **Change:** Remove the dark-specific rationale "one step off pure white (pure white reads harsh on the dark chrome)"; the neutral part (the MenuItemBrush relationship) is kept. This applies to both dark and light palettes.

5. **Status section header (currently Dark.xaml:45, Blue.xaml:46: "the single error tone (validation and failure messages on dark surfaces)"; Light.xaml:39: just `<!-- Status -->`):**

   ```xml
   <!-- Status: error, success, and selection tones -->
   ```

   **Change:** "on dark surfaces" → removed; "the single error tone" → generalized to "error, success, and selection tones" (factually covers all three keys).

**Rationale:** The comments must read correctly for Dark AND Light. Dark-specific design rationale (why a color was chosen) belongs in commit messages or ADRs, not in per-brush comments that must apply to three different palettes.

**Alternatives considered:**
- Keep dark-specific comments in Dark.xaml and light-specific in Light.xaml → Rejected: contradicts the identical-skeleton invariant and makes maintenance harder.
- Remove all inline comments → Rejected: the comments explain non-obvious relationships (PopoverOpenBrush matching the pressed overlay, TextPrimaryBrush's MenuItemBrush edge) that are valuable for palette maintainers.

### Decision 3: Guard Algorithm — Ordered Keys + Normalized Shape Comparison

**Chosen:** `ThemePaletteParityTests` is strengthened with two new assertions:

1. **Ordered key comparison:** Extract keys as a `List<string>` (preserving document order) instead of `HashSet`, then compare sequences with `SequenceEqual`. This catches ordering drift.

2. **Normalized shape comparison:** For each palette file:
   - Read the full file text.
   - Apply regex masking: replace every `Color="#[0-9A-Fa-f]{6,8}"` with `Color="#FFFFFFFF"` (a fixed dummy value).
   - Trim trailing whitespace on each line (to ignore CRLF vs LF edge cases, though all three currently use CRLF).
   - Compare the normalized texts: all three must be byte-identical after normalization.

**Exact regex pattern:** `Color="#[0-9A-Fa-f]{6,8}"`

**Test structure:**

```csharp
// After the existing key-set checks...

// 1. Ordered key comparison
var referenceKeyList = ReadKeysOrdered(paletteFiles[0]);
foreach (var file in paletteFiles.Skip(1))
{
    var keyList = ReadKeysOrdered(file);
    Assert.True(
        referenceKeyList.SequenceEqual(keyList),
        $"Key order differs in {Path.GetFileName(file)}. Expected: [{string.Join(", ", referenceKeyList)}]. Actual: [{string.Join(", ", keyList)}].");
}

// 2. Normalized shape comparison
// xUnit's Assert.Equal has no overload with a custom message, so use Assert.True.
var referenceNormalized = NormalizeShape(File.ReadAllText(paletteFiles[0]));
Assert.True(referenceNormalized.Contains("x:Key"), "Normalization must preserve key declarations.");
foreach (var file in paletteFiles.Skip(1))
{
    var normalized = NormalizeShape(File.ReadAllText(file));
    Assert.True(normalized.Contains("x:Key"), "Normalization must preserve key declarations.");
    Assert.True(
        string.Equals(referenceNormalized, normalized, StringComparison.Ordinal),
        $"File shape differs in {Path.GetFileName(file)} after masking Color attributes. " +
        "The three palettes must have identical structure (comments, whitespace, key order) " +
        "differing only in Color values. Check the normalized diff.");
}

// Helper methods
private static List<string> ReadKeysOrdered(string path)
{
    var text = File.ReadAllText(path);
    return Regex.Matches(text, @"x:Key=""([^""]+)""")
        .Select(match => match.Groups[1].Value)
        .ToList();
}

private static string NormalizeShape(string xaml)
{
    // Mask all Color="#..." attributes with a fixed dummy value
    var normalized = Regex.Replace(xaml, @"Color=""#[0-9A-Fa-f]{6,8}""", @"Color=""#FFFFFFFF""");
    
    // Split on both line-ending styles and rejoin with Environment.NewLine, so a CRLF/LF
    // mismatch cannot fail the shape comparison; trailing whitespace is trimmed per line.
    var lines = normalized.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    return string.Join(Environment.NewLine, lines.Select(line => line.TrimEnd()));
}
```

**Rationale:**
- **Ordered keys catch ordering drift** that the current `HashSet` comparison misses.
- **Normalized shape comparison** is the mechanical enforcement of the identical-skeleton invariant: if a comment is added/removed/changed in one file but not the others, the test fails.
- **Regex safety:** The pattern `Color="#[0-9A-Fa-f]{6,8}"` is anchored by `Color="` and `"`, so it cannot match arbitrary text. The 6-8 hex digit range covers `#RRGGBB` (6) and `#AARRGGBB` (8), which are the only two formats used in WPF.
- **False-positive risk mitigation:** The test includes a sanity check that the normalized text still contains `x:Key` declarations (proves the regex did not accidentally delete the whole file). Also, the regex is tested separately in a unit test (one test input with real palette content, one with edge cases like lowercase hex, 6-digit vs 8-digit).

**Alternatives considered:**
- Parse XAML and compare DOM trees → Rejected: overkill; the regex approach is simpler, faster, and sufficient.
- Mask the palette name in the header comment → Rejected: see Decision 1 (unified header, no masking needed).
- Line-by-line diff with colour-value skipping → Rejected: same result, more code.

**Existing assertions preserved:**
- File existence (loud failure on rename).
- Non-empty key sets.
- Accent key prohibition (`AccentBrush`, `AccentSelectionBrush` must NOT be declared).

### Decision 4: Documentation Update

**Chosen:** Update `docs/architecture/ui.md`:

1. **Line 121 area (palette description):** Add one sentence after "define the same brush keys": "The three files differ ONLY in colour values; their structure (comments, key order, whitespace) is identical and mechanically guarded by the parity test."

2. **Line 126 (add-a-theme recipe, step 1):** Change "define the same brush keys with new colors" → "preserve the exact file structure and replace only the Color attribute values (the parity test enforces this)."

3. **Line 139 (Theme Data Locations table, Palettes row):** Change "colours only, one identical key set" → "identical structure and key order, differing only in Color values."

4. **Lines 146 (static guards list, `ThemePaletteParityTests` entry):** Change "every palette defines one identical key set" → "palettes have identical structure (key order, comments, whitespace) differing only in Color values."

**Rationale:** The docs should reflect the strengthened invariant so future maintainers understand what the test enforces.

**Alternatives considered:** Leave the docs as-is → Rejected: the current wording ("one identical key set") under-describes what the test now enforces.

## Risks / Trade-offs

**[Risk] Regex masking false-positive** → **Mitigation:** The pattern `Color="#[0-9A-Fa-f]{6,8}"` is tightly scoped (quotes, `Color` prefix, exact hex digit count). A sanity check asserts the normalized text still contains `x:Key` (proves the file was not accidentally emptied). A separate unit test covers regex edge cases.

**[Risk] CRLF vs LF line-ending mismatch** → **Mitigation:** `NormalizeShape` splits on both `\r\n` and `\n` and rejoins with `Environment.NewLine`, so line-ending differences are normalized before comparison; trailing whitespace is trimmed per line. All three palette files currently use CRLF with no BOM (verified).

**[Trade-off] Inline comment wording is more generic** → **Benefit:** The comments apply to all three palettes. **Cost:** Dark-specific design rationale (e.g. "pure white reads harsh") is removed. This rationale can be found in commit messages (e.g. the commit that introduced the dark palette) or in ADRs if needed.

**[Trade-off] Test is slightly slower** → **Negligible:** Three 50-line files are read twice (once for keys, once for shape). The regex runs on ~150 total lines. Estimated: <10ms on a typical machine.

## Migration Plan

1. **Normalize the three palette files** (edit header comments, add/neutralize inline comments; NO colour value changes).
2. **Gate:** `dotnet build CLIHub.sln` (0 warnings/errors).
3. **Strengthen `ThemePaletteParityTests`** (add ordered-key comparison, add normalized-shape comparison).
4. **Gate:** `dotnet test CLIHub.sln` (all tests pass, including the new shape-parity assertion).
5. **Update `docs/architecture/ui.md`** (four locations: description, recipe, table, guards list).
6. **Final gates:**
   - `dotnet build CLIHub.sln` (0 warnings/errors).
   - `dotnet test CLIHub.sln` (all tests pass).
   - `openspec validate --all` (0 errors).
   - `graphify update .` (refresh the knowledge graph).
   - **Manual check:** Run the test helper's `NormalizeShape` on all three files and confirm the results are identical (can be automated as part of the test).

**Rollback:** Revert code and docs; no persisted state changes.

## Open Questions

None. The design is complete and verified against the current codebase. Implementation is ready to proceed.

## Deviations from Approved Direction

None. The approved direction was "Полная нормализация + guard" (full normalization plus guard), which is exactly what this design specifies.

## Implementation Notes (recorded after implementation)

1. The `PopoverOpenBrush` comment in `Dark.xaml` and `Blue.xaml` already matched the target text verbatim, so only `Light.xaml` needed the addition (tasks 1.2/1.12 concern `TopHighlightBrush` only). No deviation — the design text and the files agree.
2. The design's `NormalizeShape` snippet contained one stray trailing-whitespace line after `Regex.Replace`; the implemented file follows the repo's whitespace discipline and does not reproduce it.
