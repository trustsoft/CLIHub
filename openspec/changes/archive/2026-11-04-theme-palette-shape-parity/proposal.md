## Why

The three WPF theme palette files (`Dark.xaml`, `Light.xaml`, `Blue.xaml`) already declare the same 26 brush keys in the same order with the same section headers. However, they differ in ways that are not colour values:

1. **Header comment blocks** differ in length and content (4-8 lines in Dark, 4-8 lines in Light, 4-9 lines in Blue), with theme-specific wording.
2. **Inline comments** exist only in Dark and Blue; Light has none. Some Dark/Blue comments use dark-specific language ("dark-chrome windows", "on dark surfaces", "pure white reads harsh on the dark chrome") that is inaccurate for Light.
3. **Whitespace and ordering** are already identical.

The existing `ThemePaletteParityTests` compares key SETS (unordered `HashSet`), so it catches missing/extra keys but not ordering drift, comment divergence, or structural inconsistencies. The repository documentation (`docs/architecture/ui.md` lines ~121-139) states palettes should be "colours only" and "one identical key set", but the enforcement is weak.

This inconsistency makes adding a new palette error-prone (which comments to copy? which header format?) and allows silent drift in ordering or structure.

## What Changes

Normalize the three palette files to an **identical skeleton** — same header comment, same inline comments, same whitespace — differing ONLY in `Color="#..."` attribute values. Then strengthen `ThemePaletteParityTests` to enforce this invariant mechanically: after masking colour values, the normalized file texts must be byte-identical.

**Specific changes:**

1. **Unified header comment** (neutral wording, factually correct for all three palettes).
2. **Neutral inline comments** (wording that applies to dark AND light palettes; dark-specific rationale removed or generalized).
3. **Identical structure** (26 keys, 4 sections, same order, same spacing).
4. **Stronger test guard** that compares:
   - Ordered key sequences (not unordered sets)
   - Normalized file shape after masking `Color` attributes (same skeleton assertion)
5. **Documentation update** to `docs/architecture/ui.md` stating the invariant and guard.

## Capabilities

### New Capabilities

None. This is a mechanical normalization with no runtime behavior change.

### Modified Capabilities

None. No user-facing behavior changes; the same 26 brushes are declared with the same keys and resolved identically. This change opts out of specs (`skip_specs: true` in `.openspec.yaml`) because it is a pure structural/documentation refactor with no spec-level contract change.

## Impact

**Modified code:**
- `src/CLIHub/Themes/Dark.xaml` — header comment and inline comments normalized
- `src/CLIHub/Themes/Light.xaml` — header comment added, inline comments added (matching Dark/Blue structure)
- `src/CLIHub/Themes/Blue.xaml` — header comment normalized, inline comments neutralized
- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs` — add ordered-key comparison and normalized-shape comparison
- `docs/architecture/ui.md` — update Theme Data Locations table and guard list to state the shape-parity invariant

**No breaking changes.** All 26 brush keys remain declared with identical `Color` values before and after. The only differences are comment text and test strictness.

**Reuses:**
- Existing `ThemePaletteParityTests` structure (file enumeration, loud-failure asserts)
- Existing palette merge and resolution logic (untouched)

**Risk:**
- Regex masking in the test could theoretically false-pass if the palette name token appears inside a `Color` value string (e.g. `"#FF444444"` containing substring "Dark"). Mitigation: the design specifies exact regex boundaries and the test includes a sanity check.

## Non-Goals

- **No colour value changes** — every `Color="#..."` stays exactly as it was.
- **No new/removed brush keys** — still 26 keys; no renames.
- **No runtime behavior change** — palette resolution, merging, and theming logic untouched.
- **No changes to `Common/` dictionaries** or other theme files.
- **No OpenSpec spec deltas** — this is structural normalization, so `skip_specs: true`.

## Dependencies

None. This change is self-contained and does not depend on or block other OpenSpec changes.
