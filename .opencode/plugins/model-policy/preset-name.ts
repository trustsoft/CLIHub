// Model-policy preset names: the accepted shape of a user-preset name and the
// comparison key the preset writers share. No I/O and no writes.

import type { PresetNameCheck, Snapshot } from "./types"

/** Longest accepted user-preset name, counted in code points. */
export const PRESET_NAME_MAX = 32

// Printable text, not an identifier: a name is a JSON key and a one-line label,
// and nothing in this plugin resolves it as a path. Rejected categories are
// control (Cc: a newline breaks the row and the toast), format (Cf: bidi overrides
// and zero-width joiners make two names render alike, defeating the collision
// rule), lone surrogate (Cs: not encodable as UTF-8), private use (Co: a
// font-dependent glyph) and unassigned (Cn: the accept-set would move with an ICU
// upgrade). The `u` flag is required for property escapes; a `g` flag would make
// `.test()` stateful, so it must not be added.
const PRESET_NAME_FORBIDDEN = /[\p{Cc}\p{Cf}\p{Cs}\p{Co}\p{Cn}]/u
// Every code point Unicode calls whitespace is rejected except U+0020, which step
// 2 has already collapsed: the read path's `text()` folds all of `\s` to a space,
// so a written tab or NBSP would read back under a different key.
const PRESET_NAME_SPACE = /\p{White_Space}/u

const PRESET_NAME_DETAIL = "printable characters only; control characters and non-space whitespace are not allowed"

/** The rejection detail when a code point is not printable, else `undefined`. */
function nameProblem(name: string): string | undefined {
  for (const character of name) {
    if (character === " ") continue
    if (PRESET_NAME_FORBIDDEN.test(character)) return PRESET_NAME_DETAIL
    if (PRESET_NAME_SPACE.test(character)) return PRESET_NAME_DETAIL
  }
  return undefined
}

/** The comparison key for preset names: composed, then case-folded. */
export function nameKey(value: string): string {
  return value.normalize("NFC").toLowerCase()
}

/**
 * Validates a typed user-preset name. The returned `name` is the stored key: the
 * typed spelling trimmed, with every run of ASCII spaces collapsed to one and
 * NFC-normalized, case and script preserved. `replaces` names the existing saved
 * key the caller must confirm overwriting.
 */
export function checkPresetName(snapshot: Snapshot, raw: unknown): PresetNameCheck {
  if (typeof raw !== "string") return { ok: false, detail: "a preset name must be text" }
  const trimmed = raw.trim()
  if (!trimmed) return { ok: false, detail: "a preset name cannot be empty" }

  // Collapse runs of ASCII spaces (U+0020) only. `\s` would also fold a tab, a
  // newline and U+00A0 into a space, turning names the allowlist must reject
  // into accepted ones; and the read path's `text()` collapses whitespace, so
  // the collapsed spelling is the key that comes back.
  const collapsed = trimmed.replace(/ +/g, " ")

  // Normalize before the length check: a decomposed spelling is longer than what
  // the user sees, so counting it would reject names that fit on screen.
  const name = collapsed.normalize("NFC")
  if ([...name].length > PRESET_NAME_MAX) {
    return { ok: false, detail: `a preset name is at most ${PRESET_NAME_MAX} characters` }
  }
  const problem = nameProblem(name)
  if (problem) return { ok: false, detail: problem }

  const key = nameKey(name)
  for (const preset of snapshot.presets) {
    if (nameKey(preset) === key) {
      return { ok: false, detail: `"${preset}" is a policy preset; type another name` }
    }
  }
  for (const stored of Object.keys(snapshot.runtime.userPresets)) {
    if (nameKey(stored) === key) return { ok: true, name, replaces: stored }
  }
  return { ok: true, name }
}
