## Context

See proposal.md - Why. The configuration token parsers `PopupScreenPositions.TryParse` and `PathDisplayStyles.Parse` accept backward-compatible alias spellings alongside their canonical tokens. These aliases are no longer needed (the app writes only canonical tokens, and no documented tooling generates aliases). The `RuntimeKinds.TryParse` cleanup in `remove-config-migrations` already removed a similar legacy alias table.

## Verified Current State

**`src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs:60-93`** — `PopupScreenPositions.TryParse` switch:
- Canonical tokens: `center`, `topCenter`, `cursorRelative`, `primary`
- Aliases: `centre`, `center-of-active-monitor` (Center group); `top-center`, `top` (TopCenter group); `cursor-relative`, `cursor` (CursorRelative group); `primary-screen` (PrimaryScreen group)
- Behavior: case-sensitive after `Trim()`, returns false for unknown values, defaults to `Center` in `Parse`
- `ToToken` (lines 108-114): returns canonical tokens only

**`src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs:43-53`** — `PathDisplayStyles.Parse` pattern match:
- Canonical tokens: `leftTrim`, `middleEllipsis`
- Aliases: `left-trim`, `left` (LeftTrim group); `middle-ellipsis`, `middle` (MiddleEllipsis group)
- Behavior: `OrdinalIgnoreCase` comparison, defaults to `Default` (which is `LeftTrim`, line 37) for null/empty/unrecognized
- `ToToken` (lines 58-62): returns canonical tokens only

**Tests:**
- `tests/CLIHub.Core.Tests/Models/Appearance/PathDisplayStyleTests.cs:16-34` — two theories explicitly pin alias acceptance (lines 17-21 for `leftTrim` aliases including `left-trim` and `left`, lines 27-30 for `middleEllipsis` aliases including `middle-ellipsis` and `middle`). These must become rejection cases that fall back to `LeftTrim` (or be replaced by explicit theories for canonical tokens and separate theories for alias rejection).
- `tests/CLIHub.Core.Tests/Models/Appearance/PopupScreenPositionsTests.cs` — currently covers only unknown/missing fallback (line 11), `ToToken` (line 21), and round-trip (line 27). No explicit canonical-token or alias-rejection coverage.

**Spec:**
- `openspec/specs/configuration-snapshot/spec.md:94-106` — Requirement "Popup screen position defaults safely" states: "The configuration boundary SHALL parse the stored popup screen position token and default to 'Active monitor — centered' for missing or unrecognized values" (line 95). The scenario "Missing or unknown token uses the default" (lines 97-100) and "Known token round-trips" (lines 102-105) document only the fallback and round-trip behavior, not alias acceptance.
- No other docs or specs promise alias acceptance.

## Goals / Non-Goals

**Goals:**
- Remove all alias cases from both parsers, keeping only the canonical tokens
- Preserve existing fallback-to-default behavior for unrecognized values (including old aliases)
- Preserve the existing casing behavior per parser (case-sensitive for `PopupScreenPositions`, `OrdinalIgnoreCase` for `PathDisplayStyles`)
- Update and expand tests to verify canonical-token parsing and alias rejection

**Non-Goals:**
- Changing the casing behavior (not adding case-insensitivity to `PopupScreenPositions`, not removing it from `PathDisplayStyles`)
- Migrating old alias values to canonical tokens (they fall back to defaults, as any unrecognized value already does)
- Deprecation warnings or logging for alias use

## Decisions

### Decision 1: Remove aliases; keep canonical tokens only

**Chosen:** Remove all alias cases from the switch in `PopupScreenPositions.TryParse` and from the pattern match in `PathDisplayStyles.Parse`. Keep only the four canonical `PopupScreenPosition` tokens and the two canonical `PathDisplayStyle` tokens.

**Rationale:** The application writes only canonical tokens (`ToToken`), and no producer in the codebase or documented tooling generates aliases. The existing `configuration-snapshot` spec documents only the canonical tokens and fallback behavior, not alias acceptance. This aligns the implementation with the documented contract and matches the cleanup already performed for `RuntimeKinds.TryParse` in `remove-config-migrations`.

**Alternatives considered:**
- **Keep the aliases:** Rejected. They add complexity and an undocumented surface for no current benefit. No known configuration files use alias spellings (the application has never written them).
- **Deprecate with warnings first:** Rejected. There is no known usage of aliases (the app never wrote them), and the fallback-to-default contract is already documented and tested. A deprecation phase would add logging noise without evidence of a migration need.

**Evidence:** `grep -r "centre\|center-of-active-monitor\|top-center\|cursor-relative\|left-trim\|middle-ellipsis" docs/` returns no promises of alias acceptance. ADR 0014 (`remove-configuration-migrations`) sets the precedent for removing legacy token aliases.

### Decision 2: Old alias values fall back to the existing defaults

**Chosen:** Configurations persisted with alias spellings (e.g., `"popupScreenPosition": "centre"` or `"pathDisplayStyle": "left-trim"`) will be treated as unrecognized values and fall back to `Center` and `LeftTrim` respectively.

**Rationale:** This matches the existing documented fallback contracts for unrecognized values: the popup screen position fallback in `configuration-snapshot/spec.md` lines 95-100 and the path display style fallback in `preferences-ui/spec.md` lines 153-155 ("Invalid stored value falls back to the default" — an unrecognized path display style uses left trim). No migration logic is needed; the parsers already default for any unrecognized value. Since the application has never written alias spellings, this only affects manually edited configuration files (if any exist).

**Alternatives considered:**
- **Migrate aliases to canonical tokens at load time:** Rejected. Adds migration code for a non-existent problem (the app never wrote aliases). The fallback-to-default path is simpler, already documented, and already tested.
- **Reject the configuration document entirely:** Rejected. The existing contract is forgiving (unknown values default), and there is no reason to change that.

### Decision 3: Preserve the existing casing behavior per parser

**Chosen:** Keep `PopupScreenPositions.TryParse` case-sensitive after `Trim()`, and keep `PathDisplayStyles.Parse` `OrdinalIgnoreCase`.

**Rationale:** The task is alias removal, not case normalization. Changing the casing behavior is orthogonal and was not requested. Each parser has its own casing contract, and there is no evidence either needs to change.

**Alternatives considered:**
- **Add case-insensitivity to `PopupScreenPositions`:** Rejected. Not requested, and the parser has been case-sensitive since it was introduced. No known issue with the current behavior.
- **Remove `OrdinalIgnoreCase` from `PathDisplayStyles`:** Rejected. Not requested, and the existing tolerance may have callers that rely on it (even if only in tests). No reason to tighten it.

### Decision 4: Update tests to verify canonical tokens and reject aliases

**Chosen:**
- In `PathDisplayStyleTests.cs`, replace the two alias-acceptance theories (`Parse_LeftTrimTokens_ReturnLeftTrim` and `Parse_MiddleEllipsisTokens_ReturnMiddleEllipsis`) with:
  - One theory verifying canonical-token parsing (including case variations, since `Parse` is `OrdinalIgnoreCase`)
  - One theory verifying that old aliases now fall back to `LeftTrim`
- In `PopupScreenPositionsTests.cs`, add:
  - One theory verifying canonical-token parsing (exact camelCase only, since `TryParse` is case-sensitive)
  - One theory verifying that old aliases now fall back to `Center` (return false from `TryParse`, default to `Center` from `Parse`)

**Rationale:** The tests currently pin the alias-acceptance behavior as correct. After the change, that behavior is incorrect. The tests must reflect the new contract: canonical tokens parse, aliases fall back to defaults.

**Alternatives considered:**
- **Leave existing tests unchanged and expect them to fail:** Rejected. Failing tests block verification. The tests must be updated to match the new contract.
- **Delete alias tests entirely:** Rejected. Explicit alias-rejection coverage verifies that old alias values are handled correctly (fallback to default) rather than throwing or producing unexpected results.

## Risks / Trade-offs

**[Risk] Manually edited configurations with alias spellings revert to defaults:** If a user manually edited their `config.json` to use an alias spelling (e.g., `"popupScreenPosition": "centre"`), that value will fall back to the default (`Center` or `LeftTrim`) after the change. **Mitigation:** The application has never written alias spellings, so such configurations are rare or nonexistent. The fallback-to-default behavior is already documented and tested. The impact is low (the user sees the default position/style rather than the manually edited alias).

**[Trade-off] No migration or logging for alias values:** Old alias values silently fall back to defaults without logging. **Benefit:** Simplicity; no migration code, no log noise. **Cost:** A user who manually edited an alias spelling will not be notified that it was unrecognized. Given the rarity and the benign fallback, this is acceptable.

**[Trade-off] Casing behavior unchanged:** The parsers keep their existing case sensitivity (or insensitivity). **Benefit:** Orthogonal to alias removal; no risk of breaking existing callers. **Cost:** The two parsers have inconsistent casing contracts. If case normalization is desired, it can be a separate change.

## Migration Plan

1. Remove alias cases from `PopupScreenPositions.TryParse` switch; keep only canonical tokens
2. Remove alias conditions from `PathDisplayStyles.Parse` pattern match; keep only canonical tokens
3. Update `PathDisplayStyleTests.cs`: replace alias-acceptance theories with canonical-token and alias-rejection theories
4. Add canonical-token and alias-rejection theories to `PopupScreenPositionsTests.cs`
5. Build and test: `dotnet build`, `dotnet test`
6. Verify: `openspec validate --all`, `graphify update .`

Rollback: revert code and test changes; no persisted state changes.

## Open Questions

None. The design is complete and aligned with the user's decisions.
