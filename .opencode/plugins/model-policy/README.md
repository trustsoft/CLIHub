# model-policy (OpenCode CLI plugin)

A control panel for the CLIHub subagent model policy. It shows the active preset,
the model each role resolves to, models that are currently exhausted, and the
runtime session log — and it can change the preset and per-role overrides, and save,
load and delete the user's own stage-to-model presets.

The sidebar shows the roles, the last observed model, and the exhausted models.
The session log is **not** rendered in the sidebar; it is available in the
`/policy` details view, where the full history and the resolved paths are listed.

The plugin is a **policy editor, not a model switcher**. It writes the runtime
selection file; the orchestrator reads that file at dispatch time. There is no
plugin API to change the host model (`ctx.ui.model` exposes `current()` and
`variant.set()` only), so the panel never attempts to.

- Sidebar (`sidebar.content`): a bold `Model Policy` header with a `▼`/`▶` chevron
  (the active preset shown in muted parentheses only when collapsed, cut to 20
  characters), one row per role with the resolved model, a `preset` row with the
  active preset, the last model observation reported by the event stream, and up to
  two exhausted models with the remaining back-off. The model name uses the
  informational colour when the user pinned it by hand; a model that comes from the active
  user preset uses the base colour, because the `preset` row above and the details view
  already name the preset it came from. No text suffix is rendered. Each role row is
  clickable and opens that role's model picker; the `preset` row opens the preset picker.
  The trailing `›` marks the affordance on both. The `preset` row shows the active user
  preset, otherwise the stored policy preset; it shows `custom` when the resolved set no
  longer matches the selected preset, and `no preset` when neither is known. The block is
  collapsible:
  click the `Model Policy` header (or its `▶`/`▼` chevron) to
  collapse it to that single line, which shows the same name (`custom` included). The
  collapsed state is remembered per project when the
  host offers `ctx.storage`.
- The sidebar is compact: the role is a three-letter code, the provider is a
  two-letter code, the effort is one letter, and the redundant `-free` suffix is
  dropped for the `opencode` provider (every `opencode` model in the catalogue is a
  free one). Nothing is truncated at the current catalogue size.

  | Full | Shown | |
  | --- | --- | --- |
  | `planner` | `pln` | roles: `pln` `imp` `ver` `exp` `res` `mch` |
  | `zai-coding-plan` | `za` | providers: `za` `rf` `ds` `oc` `lm` |
  | `relayfast` | `rf` | `ds` is `deepseek`, `oc` is `opencode`, `lm` is `lmstudio` |
  | `opencode/glm-5.3-flash-free` | `oc/glm-5.3-flash` | the `-free` suffix is dropped |
  | `relayfast/claude-sonnet-5` | `rf/cl-sonnet-5` | the vendor word is shortened |
  | `lmstudio/qwen/qwen3-coder-next` | `lm/q3-coder-next` | the vendor word is shortened too |
  | `#high` | `#h` | effort: `n` `i` `l` `m` `h` `x` `M` |

  These abbreviations are **read from the versioned policy and nowhere else**
  (`<project>/docs/agent-model-policy.json`, the `display` section): the plugin
  keeps no built-in table, so the panel and the documentation cannot drift apart
  and a policy edit always decides what is shown. A policy without the section
  renders full ids (`opencode/glm-5.3-flash-free`), a role as its first three
  characters (`pla`), the effort verbatim (`high`), and a `none` legend. A partial
  section abbreviates only what it declares — nothing is merged in. The legend in
  the details dialog is generated from the same table, and reading it never
  writes the policy or the runtime document.

  Model names keep the version and the tier; only the vendor word is
  shortened (`cl` claude, `ds` deepseek, `mm` minimax, `nemo` nemotron,
  `q3` qwen).

  The full model id is always kept in the data and in dialog values. The
  `/policy` details dialog shows the full ids and also prints the legend
  above.

  The model picker lists the short label in the title (`rf/cl-sonnet-5#h`, the
  effort the role would run it with) and the exact id in the description
  (`relayfast/claude-sonnet-5 · paid · high`), so the list stays short and the
  value that will be written is still visible. A short id typed by hand in the
  custom-model prompt is expanded back to the full id before it is written.
  The `/policy` menu follows the same rule for its per-role entries.
- `/policy` (slash) or the "Model policy control panel" palette command: a menu whose
  first entry is `Presets: <active>` (the saved-preset count is in its description),
  plus per-role model, details/session log, "clear overrides", "re-read".
- `/preset` (slash), the "Switch the subagent model-policy preset" palette command and
  the sidebar `preset` row all open the same combined preset picker: every policy preset,
  every saved user preset, "Save current as a new preset…" and — once a preset is saved
  — "Delete a saved preset…".
- Both files are polled every 2 seconds, so orchestrator writes and policy edits
  appear without restarting OpenCode.

The host discovers the plugin at `<project>\.opencode\plugins\model-policy\tui.tsx`
(plugin id `sidebar-suite.policy`); nothing registers it, and no configuration
document lists it.

## Data contract

Both layers are JSON and are read directly with `node:fs`. No YAML is parsed and
no catalogue is mirrored.

| File | Role |
| --- | --- |
| `<project>/docs/agent-model-policy.json` | versioned, human-curated policy (read-only here) |
| `<project>/.opencode/model-policy.json` | runtime selection (partially owned by this plugin) |

`<project>` is the session's `location.directory`, falling back to the plugin
location, the default location, then the process cwd. The resolver and gate
scripts for those documents are versioned in this repository
(`project/.opencode/scripts/`) and deployed per project by `install.ps1`;
on the origin machine the project was `D:\Projects\CLIHub`, i.e. exactly the two
contract paths. Two environment
variables override the paths (used by `verify.ts`):

```text
OPENCODE_MODEL_POLICY_FILE           absolute path to the policy JSON
OPENCODE_MODEL_POLICY_RUNTIME_FILE   absolute path to the runtime JSON
```

Policy keys consumed: `version`, `active_preset`, `roles` (`agents`, `tier`,
`on_exhaustion`), `models` (`billing`, `tier`, `limits`, `max_concurrent`),
`presets`, `overrides` (`preset`, `valid_from`, `valid_until`).

Runtime keys consumed: `schemaVersion`, `activePreset`, `activeUserPreset`,
`userPresets` (name → role → `model` or `model#variant`), `roleOverrides`,
`exhaustedUntil`, `sessionLog` (`at`, `role`, `from`, `to`, `reason`). Snake-case
aliases (`active_preset`, `role_overrides`, `user_presets`, `active_user_preset`, …)
are accepted on read; writes always use the camelCase contract spelling.

### Resolution shown in the panel

1. `roleOverrides[role]` (source `override`, or `user` when it equals the active
   user preset's entry — the preset row names it, so the row is not marked), else
2. the first model of the **effective** preset's chain for that role whose
   `exhaustedUntil` is not in the future (source `preset`), else
3. if that preset has no chain for the role, the `default` preset's chain
   (source `default`), else nothing (source `none`).

The effective preset is the first `overrides[]` window containing `now`, else the
runtime `activePreset` when the policy declares a preset by that name, else the
policy `active_preset` when it is declared, else the first preset in `presets`.
A stored name the policy does not declare is ignored: the row, the header and
the details view then name the preset dispatch uses, which is the one
`resolve-model.mjs` resolves for the same documents. The panel shows both the
stored preset and the effective one when they differ. Reading never rewrites the
runtime document, so an undeclared `activePreset` stays on disk until the next
write. Expired `exhaustedUntil` entries are ignored; a chain whose every model is
exhausted is flagged `starved` and rendered in the warning colour.

An override wins while its model is usable: the resolver applies the same
`exhaustedUntil` filter to `roleOverrides[role]` as to the chain, so a loaded — or
saved — user preset no longer pins dispatch to a model that is backing off; the role
falls back through the chain until the mark expires and then pins again. The sidebar
still resolves the row to the pinned model and marks such a role in the warning colour
(as it does for an exhausted chain), while dispatch returns the chain fallback — the
resolver reports it with the `exhausted skip` reason and names the skipped override.
Switching to a policy preset or clearing all role overrides is the way back to the
chain.

### User presets

A saved preset is one model per role, exactly what `roleOverrides` can express:

```json
"userPresets": {
  "cheap planning": {
    "planner": "relayfast/claude-sonnet-5#high",
    "mechanical": "opencode/mimo-v2.6-flash-free"
  }
},
"activeUserPreset": "cheap planning"
```

- **Save** stores, for every role that resolves to a model **and is declared in the
  policy's `roles` block**, `model` or `model#variant` under `userPresets[<name>]`, and
  makes that set current in the same write: `roleOverrides` is replaced by exactly the
  saved map and `activeUserPreset` becomes the saved name. A role whose resolution is
  `none` and a role only a preset chain names are omitted and counted in the toast; if
  nothing applies, nothing is written. `activePreset` is untouched and therefore still
  a policy preset, which is what the CLIHub policy check requires. Saving a set pins
  every resolved role — an override outranks the chain at dispatch while its model is
  not exhausted; an exhausted pin falls back through the chain until its mark expires —
  so the set you just saved is the set in force, and a later preset switch
  unpins it (see the last bullet).
- **Load** replaces `roleOverrides` (it does not merge) and sets `activeUserPreset`
  in the same write. `activePreset` is untouched and therefore still a policy preset,
  which is what the CLIHub policy check requires. A manual override for a role the
  preset does not mention is lost by design: the active user preset is the complete
  answer for every role.
- **Stale entries** are filtered against the current policy before the write.
  "Declared" means listed in the policy's `roles` block: a role the block does not
  declare — including a role only a preset chain names, which still renders a row — is
  skipped and reported, as is a model that left the catalogue. A variant the model no
  longer lists is dropped and the model kept, and an entry ending in `#` with no
  variant text is applied as the bare model id, with no separator written back. If
  nothing applies, the load is refused and the file is untouched.
- **Names** are trimmed, then each run of ASCII spaces (U+0020 only) is collapsed to
  one space, then Unicode-normalized to NFC, so the stored key is the key the picker
  reads back. A name is 1–32 code points of printable text: letters, digits, marks,
  punctuation and symbols in any script (`ds/ds flash`, `deepseek / all`,
  `plan (free)`, `50% + free!`, `plan 🚀`, `бесплатный план`). Control characters
  (a newline, a tab), format characters (bidi overrides, zero-width spaces and joiners),
  lone surrogates, private-use and unassigned code points, and every whitespace other
  than U+0020 (U+00A0, U+3000) are rejected. A name that matches a policy preset
  ignoring case is refused, and a name that matches an existing saved preset asks for
  confirmation and then replaces it, in the typed spelling after normalization.
  Composed and decomposed spellings of one word remain a single preset.
- **Selection changes**: a manual role override keeps `activeUserPreset` — the changed role
  uses the informational colour and every role that still names its saved model remains
  attributed to the preset. While the set differs, the `preset` row and the collapsed
  header show `custom`; `custom` is display-only and is never written to `activePreset` or
  `activeUserPreset`. "Clear all role overrides" clears the marker and the pins. A
  policy-preset switch clears the marker, always empties `roleOverrides` (a chosen preset
  must be the set in force) and leaves `userPresets` untouched. Deleting the preset the
  marker names clears the marker and leaves the overrides it applied in place.
- Presets live in the per-project runtime file, so they are not shared between
  projects and not visible to CLIHub's policy check (which ignores unknown keys).

## Ownership and writes

| Writer | Fields |
| --- | --- |
| this plugin | `activePreset`, `activeUserPreset`, `userPresets`, `roleOverrides` |
| orchestrator | `exhaustedUntil`, `sessionLog` |

`activeUserPreset` is the plugin's own marker for "the current `roleOverrides`
came from this saved user preset": loading a saved preset and saving one both set it,
in the same write that sets the overrides. A manual role override keeps it — the changed
role then uses the informational colour while every unchanged role stays attributed to the
preset — and "clear all role overrides" or a policy-preset switch clears it. A
policy-preset switch also empties `roleOverrides` unconditionally (a chosen preset must be
the set in force) and leaves `userPresets` untouched. `userPresets` is written only by
saving and deleting a preset;
no other path touches it.

Every write:

1. re-reads the runtime document,
2. assigns only the owned keys (unknown/future keys are preserved verbatim),
3. writes a unique temp file in the same directory (`.model-policy.json.<pid>.<n>.tmp`,
   opened with `wx`) and renames it over the destination, retrying transient
   Windows `EPERM`/`EACCES`/`EBUSY` up to four times.

Consequences:

- The plugin never rewrites `exhaustedUntil` or `sessionLog`, so an orchestrator
  write between the plugin's read and rename is preserved (verified).
- A malformed or unreadable runtime file is **never** overwritten, because the
  orchestrator fields cannot be reconstructed. The panel offers an explicit,
  confirmed "Recreate the runtime file with defaults" action instead.
- If the runtime file is missing, the first sidebar render creates it from policy
  defaults (`schemaVersion: 1`, `activePreset` from `active_preset`, empty
  `userPresets`/`roleOverrides`/`exhaustedUntil`/`sessionLog`). Write paths do the same
  if the file disappears between renders. "Recreate the runtime file with defaults"
  therefore drops every saved user preset as well.

If the policy file is missing or unparsable the panel shows the reason and the
expected path; controls refuse to write a preset or role that the policy does not
declare.

## Controls and the host dialog API

OpenCode 2.0.32 has no button/menu primitive in the low-level JSX, and adding
`onClick` handlers is not supported. Controls therefore use `ctx.ui.dialog`:

- `select({ title, placeholder, current, options, ... })` returns a Promise that
  resolves to the chosen option's `value` (or `undefined` on close). The plugin
  awaits it; it does not pass a dialog-level `onSelect`, because the host installs
  its own resolver and would replace it. It lists policy presets and user presets
  together, and it is also the picker for the preset to delete.
- `prompt({ title, description, placeholder, value })` for a custom model id and for
  the name of a preset being saved.
- `confirm(...)` before overwriting or deleting a saved preset, before clearing
  overrides or recreating the runtime file.
- `alert(...)` for the details view (paths, preset, user presets, roles, exhaustion,
  session log, seen models).

Each option carries both `title` (what this build renders) and `name` (the
documented spelling) so either host revision displays labels.

Save is `prompt` (its description states the printable-character rule) → name check →
optional overwrite `confirm` → one write, then one toast:
`Saved preset "<name>" and made it active (N role(s))`, plus
`, M role(s) not saved (no model or not declared)` when roles were left out. Delete is
`select` → `confirm` → one write. Every dialog is awaited before the next opens
(a second concurrent dialog is refused), and a throwing dialog is reported as a toast
instead of propagating.

## Files

| File | Purpose |
| --- | --- |
| `tui.tsx` | plugin entry: `Plugin.define({ id: "sidebar-suite.policy", setup })`, slot + commands |
| `state.ts` + 11 modules | `state.ts` is a barrel; the layers are `types`/`util` (leaves), `paths`/`display`/`policy`/`preset-name`/`observe`, `io`, `runtime`, `snapshot`, `presets` |
| `verify.ts` | round-trip / merge-ownership / resolution tests (not loaded by OpenCode) |
| `README.md` | this file |

The shared sidebar helpers — colors, collapse, session id, toast, polling, and
the header row — live at `<project>\.opencode\shared\sidebar-ui.tsx`; the entry
imports them as `../../shared/sidebar-ui`.

Canonical sources live in the opencode-sidebar-suite repository, and
`install.ps1` copies them into the project's `.opencode` directory.

## Verification

Commands actually run from `C:\Users\Maxim\.config\opencode\plugins\model-policy`
(Bun 1.4.2):

```powershell
bun run verify.ts
bun build tui.tsx --target=bun --external "*" --outfile "$env:TEMP\mp-build-shared-b.js"
```

`verify.ts` output (12 assertion groups, all passing):

```text
paths: default project resolution OK
live policy: 4 presets, 6 roles
ensureRuntime: created with policy defaults, no temp leftovers
merge ownership: activePreset/roleOverrides writes never clobber orchestrator fields
resolution: runtime activePreset overrides the policy default
atomicWriteJson: round-trip OK
resolution: exhausted models skipped, starved chains flagged, log parsed
overrides: window selects the effective preset without changing activePreset
malformed runtime: refused write, explicit recreate repairs
user presets: schema, name rules, save/apply/delete, marker clearing OK
observation: event extraction and role attribution OK
verify: all assertions passed
```

The test writes only into `%TEMP%\opencode-model-policy-verify-<pid>` and asserts
that the live runtime file is byte-identical before and after the run.

`bun build` prints `Bundled 1 module in 5ms` and
`mp-build-shared-b.js  23.80 KB  (entry point)`, exit code 0; it parses the TSX
and transpiles it (syntax and JSX check; OpenCode itself compiles JSX with
`@opentui/solid`, not Bun's React transform). The reported time varies between
runs; the bundle shrank from 26.66 KB to 23.80 KB when the shared helpers moved
out of the entry.

Type-check (strict, no `@types/node`, `@opentui`, or `solid-js` installed, so a
temporary ambient shim in `%TEMP%\mp-typecheck` declares only the used surface):

```powershell
node C:\Users\Maxim\AppData\Roaming\npm\node_modules\kanban\node_modules\typescript\lib\tsc.js -p %TEMP%\mp-typecheck\tsconfig.json
```

Result: `TYPECHECK_OK` against TypeScript 7.0.2, `strict: true`.

Headless load/render check (`%TEMP%\mp-load-check`): the plugin is bundled with
stubs for `@opencode/plugin/tui`, `solid-js` and the JSX runtime, then imported
and executed with a fake `ctx` and a scratch project containing a copy of the
live policy. The check is re-run against the *current* sources — refresh the
copies first, or the transcript below describes a stale revision. The copies
must mirror the real layout, because the entry imports `../../shared/sidebar-ui`:
the plugin files go under `<root>\plugins\model-policy\` next to
`<root>\shared\sidebar-ui.tsx`.

```powershell
$root = "$env:TEMP\mp-load-check"
New-Item -ItemType Directory -Force "$root\plugins\model-policy", "$root\shared" | Out-Null
Copy-Item C:\Users\Maxim\.config\opencode\plugins\model-policy\* "$root\plugins\model-policy\" -Force
Copy-Item C:\Users\Maxim\.config\opencode\shared\sidebar-ui.tsx      "$root\shared\sidebar-ui.tsx"
cd "$root\plugins\model-policy"
bun build .\tui.tsx --target=bun --outfile "$root\tui.bundle.js"
bun run "$root\check.ts"
```

The transcript below was observed before the shared-module move, with the older
two-file layout, and has not been re-run since.

Observed output:

```text
render: sidebar resolved all roles and created the runtime file
render: expanded header shows the down chevron, the bold title, and no preset summary
render: collapsed header shows the right chevron and the muted preset summary
render: exhausted model visible, session log absent from the sidebar
render: exhausted block renders between the role rows and the hint line
commands: preset switch writes atomically, preserves orchestrator fields, cancel is a no-op
commands: per-role override written from the menu, orchestrator fields preserved
render: observation line renders above the exhausted block, no session log
verify-tui: all assertions passed
```

That check asserts the entry shape (`id === "sidebar-suite.policy"`, `setup` returns a
cleanup function), one `sidebar.content` slot, one global keymap layer with the
`policy`/`preset` commands, the rendered sidebar text, the collapsible header
contract (a row with `gap={1}`, the `▼`/`▶` chevron in its own text node, the
bold title, and the muted preset summary that appears only while collapsed), the
render order (role rows, observation line, exhausted models, hint line), the
absence of the session log in the sidebar, the preset write path through the
real command body, and preservation of orchestrator fields.

Registration: the plugin directory was listed as `local` by `opencode plugin list`
(directory discovery; no `tui.json` entry is involved).

## Deviations and limits

- **Option shape.** The brief documents `options: [{ value, name }]`; the 2.0.32
  host renders `title` + `description` (the bundled `select` implementation maps
  options through and the list item reads `title`). Both keys are supplied.
- **`select` completion.** The host replaces any dialog-level `onSelect` with its
  own resolver and resolves the returned Promise with the option's `value`. The
  plugin relies on the Promise; per-option `onSelect` is not used. The result is
  mapped back through the pure `matchOptionValue` helper — an exact `value`, then
  the rendered `title`, then `name` — because the picker's titles (`policy · x`)
  no longer equal its values (`policy:x`): a host that resolved the label instead
  would otherwise close the dialog and do nothing. On the documented host `value`
  wins and the fallback is never taken, and a result that matches no option counts
  as a cancelled dialog (no write). `verify.ts` asserts every branch.
- **Picker `current` marking.** The active preset is marked in the option title
  (`policy · x (active)` / `user · x (active)`) *and* passed as `current`, because
  this host build's `current` matching is unverified (see below). A host that ignores
  `current` still shows the marker.
- **Event observation.** `ctx.data.listen(({ details }) => ...)` is used when the
  host exposes it; otherwise the v1 `ctx.event.on("message.updated" | ...)` is
  tried. Both are wrapped in try/catch: observation only decorates the panel and
  is dropped silently if the channel is absent. The installed
  `@opencode-ai/plugin@1.18.30` type package is the v1 API and is not used.
- **Header parity with the built-in MCP sidebar block.** The header mirrors
  `packages/tui/src/feature-plugins/sidebar/mcp.tsx`: a row with `gap={1}`, the
  `▼`/`▶` chevron in its own `<text>` in the base colour, a bold title, and a
  muted ` (summary)` shown only while collapsed. The `<b>` inline tag is the same
  construct the built-in block uses inside `<text>`, so the TUI JSX runtime
  supports it. Three deliberate differences: the toggle uses `onMouseUp` (the
  built-in uses `onMouseDown`, but this plugin uses `onMouseUp` for every
  clickable row); the title keeps `wrapMode="none"` because the sidebar is
  narrow; and the chevron is always visible, since the built-in hides it only
  when it has two or fewer entries while this block always has six roles.
- **Cannot be verified here:** live rendering in the real TUI (the headless
  check above uses stubs, not the real OpenTUI renderer), the visual layout at
  narrow sidebar widths, and the event-observation line against real message
  events. The running TUI did create
  `D:\Projects\CLIHub\.opencode\model-policy.json` with the documented defaults
  right after `tui.tsx` was written, which indicates the plugin loaded and the
  sidebar effect ran, but that is inferred from the file and its timestamp.

## Activation and rollback

Activation: restart the OpenCode CLI (or let the host reload plugins) so the
project tree is re-read.

Rollback: run `.\uninstall.ps1 -ProjectDir <project>` (it removes exactly the
managed names, including this plugin directory and `shared\sidebar-ui.tsx` when no
other entry imports it), or delete
`<project>\.opencode\plugins\model-policy\` by hand. The runtime file
`.opencode\model-policy.json` can stay (the orchestrator owns most of it) or be
deleted; it is recreated with defaults on the next load if the plugin is still
registered.

## Known risks

- The TUI plugin surface is version-specific (built against OpenCode 2.0.32);
  `@opencode/plugin/tui`, slot names, or the JSX runtime may change.
- The sidebar width is host-owned; role models are truncated to 30 characters.
  With the current catalogue nothing reaches that limit (the longest is 19
  characters, `rf/kimi-k2.7-code#l`); a new model with a longer id would be cut.
  A saved preset name may be any printable text up to 32 code points — punctuation
  (`ds/ds flash`), symbols, emoji and any script — and the collapsed suffix is cut to
  20 UTF-16 units: a CJK or astral-script name can overflow that suffix and the
  `preset` column grid (`truncate` slices code units). Cyrillic — the target case — is
  single-width and unaffected; the stored key is never truncated.
- The preset picker lists one option per policy preset plus one per saved preset
  plus two actions. The host's list scrolling for a long list has not been verified;
  the limit here is a single user's runtime file.
- The collapsed state is persisted through `ctx.storage.memory("collapsed", false)`
  and `ctx.storage.store("collapsed", value)`. The built-in MCP block uses an
  object-shaped store (`ctx.storage.store("view", { initial: { open: true } })`),
  so the scalar shape has not been validated against a real host session. If the
  host ignores it, the block simply starts expanded on every launch — a
  cosmetic-only failure.
- `docs/agent-model-policy.json` must remain valid JSON for the panel to work.
- The details view is a plain-text `alert`, not a scrollable panel; the session
  log is capped at 12 lines there (20 entries are parsed).
- Polling is every 2 seconds. A non-atomic *orchestrator* write can briefly show
  the runtime as malformed until the rename completes.
- The policy's `_free_rule`/`_comment` prose keys are ignored by design; only the
  structured keys listed above drive the UI.
