## Why

The configuration token parsers `PopupScreenPositions.TryParse` and `PathDisplayStyles.Parse` still accept backward-compatible alias spellings (`centre`, `center-of-active-monitor`, `top-center`, `top`, `cursor-relative`, `cursor`, `primary-screen`, `left-trim`, `left`, `middle-ellipsis`, `middle`) alongside their canonical camelCase tokens. These aliases were introduced during early development for user convenience but are no longer needed:

- The application writes only canonical tokens (`ToToken` methods).
- No producer in the current codebase or documented tooling generates alias spellings.
- The existing spec (`configuration-snapshot`) documents only the canonical tokens and fallback behavior, not alias acceptance.
- The `RuntimeKinds.TryParse` cleanup in the `remove-config-migrations` change already removed similar legacy aliases.

Keeping the alias tables complicates the parsers and creates an undocumented surface. Removing them simplifies maintenance and aligns the implementation with the documented contract.

## What Changes

- Remove all alias cases from `PopupScreenPositions.TryParse` (in `src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs`), keeping only the four canonical tokens: `center`, `topCenter`, `cursorRelative`, `primary`.
- Remove all alias cases from `PathDisplayStyles.Parse` (in `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs`), keeping only the two canonical tokens: `leftTrim`, `middleEllipsis`.
- Old alias values (and any other unrecognized values) fall back to the existing documented defaults: `Center` for `PopupScreenPositions.Parse` and `LeftTrim` for `PathDisplayStyles.Parse`.
- Update tests in `tests/CLIHub.Core.Tests/Models/Appearance/PathDisplayStyleTests.cs` to verify canonical-token parsing and alias rejection (aliases now fall back to `LeftTrim`).
- Add tests in `tests/CLIHub.Core.Tests/Models/Appearance/PopupScreenPositionsTests.cs` to verify canonical-token parsing (including exact camelCase) and alias rejection (aliases now fall back to `Center`).
- Preserve the existing casing behavior: `PopupScreenPositions.TryParse` remains case-sensitive after `Trim()`, and `PathDisplayStyles.Parse` keeps its `OrdinalIgnoreCase` tolerance.
- No changes to `ToToken` methods, enum values, default values, Settings UI labels, or persisted property names.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This change aligns the implementation with the existing specs that document the fallback contracts: the popup screen position fallback in `configuration-snapshot` and the path display style fallback in `preferences-ui`. Both specs document only the canonical tokens and fallback-to-default behavior for unrecognized values; neither promises alias acceptance. No spec-level behavior change is required, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

**Modified code:**
- `src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs` — remove alias cases from the switch, keep canonical tokens only
- `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs` — remove alias conditions from the pattern match, keep canonical tokens only
- `tests/CLIHub.Core.Tests/Models/Appearance/PathDisplayStyleTests.cs` — replace alias-acceptance cases with alias-rejection cases
- `tests/CLIHub.Core.Tests/Models/Appearance/PopupScreenPositionsTests.cs` — add canonical-token and alias-rejection cases

**Behavior for old alias values:**
Configurations persisted with alias spellings (e.g., `"popupScreenPosition": "centre"` or `"pathDisplayStyle": "left-trim"`) will fall back to the documented defaults (`Center` and `LeftTrim` respectively) when loaded after this change. This matches the existing fallback contract documented in `configuration-snapshot/spec.md` for unrecognized values. The application has never written alias spellings, so this only affects manually edited configuration files.

**No breaking changes:** The fallback-to-default behavior is already documented and tested. Applications that persisted canonical tokens (the overwhelming majority) are unaffected.

## Non-Goals

- Changing the casing behavior (adding case-insensitivity to `PopupScreenPositions` or removing it from `PathDisplayStyles`): this change removes aliases only, not case normalization.
- Migrating old alias values to canonical tokens at load time: unrecognized values simply fall back to defaults, consistent with the existing contract.
- Adding deprecation warnings or logging when an alias is encountered: the parsers silently default, as they already do for any unrecognized value.

## Dependencies

None. This change is independent of all other work.
