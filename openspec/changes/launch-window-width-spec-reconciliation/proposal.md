# Launch window width spec reconciliation

## Why

`openspec/specs/launch-window-layout/spec.md` makes two contradictory claims about the launch
window's width:

- Requirement "Agents pane and row sizing", scenario "Window fits its content" (`:232-234`):
  "WHEN the panes' content changes THEN the window's width follows the panes, so the window never
  keeps empty space beside its content."
- Requirement "Window chrome", scenario "Fixed width and content height" (`:252-254`): "WHEN the
  panes' content changes THEN the window height follows its content and its width does not
  change."

Both scenarios share the same WHEN ("the panes' content changes") and assert opposite THENs for
the window's width. The actual behavior matches "Window fits its content": `LaunchWindow.xaml:20`
sets `SizeToContent="WidthAndHeight"`, and the pane grid's three columns are all `Auto`
(`LaunchWindow.xaml:114,116,118`), so the window's measured width is the sum of its panes' desired
widths — it grows or shrinks whenever a pane's content does. `LaunchWindow.xaml:18` sets
`ResizeMode="NoResize"`, so the window cannot be resized by the user; that is a different fact from
the window's width being constant, and the spec conflates the two.

The contradiction became concrete, not just theoretical, in the archived change
`openspec/changes/archive/2026-11-01-agent-row-inset-alignment/`: adding 4 DIP of agent row padding
widened the launch window from 596 DIP (894 px at 1.5x scaling minus the border) to 600 DIP — a
width change the "Fixed width and content height" scenario forbids outright. The project is also
tracking this as a known defect in `docs/backlog.md`, section "UI and theming", entry
"`launch-window-layout` spec contradicts itself about the window width" (`:607-622`).

## What Changes

- **`openspec/specs/launch-window-layout/spec.md`**, Requirement "Window chrome": keep the scenario
  named "Fixed width and content height" (OpenSpec's `MODIFIED` delta mechanism cannot rename or
  drop a scenario within a requirement — see `design.md`, "Tooling constraint") but rewrite its THEN
  clause to state only the width-independent facts this requirement actually owns: the window's
  height follows its content, and the window has no OS-level or user-driven way to set its own size
  (no resize border, `ResizeMode="NoResize"`). The new body does not assert that the width stays
  constant. Every other scenario under "Window chrome" is kept with its name and body unchanged:
  "No title bar or frame controls", "Window outline", "Rounded corners", "Window cannot be moved or
  resized by dragging".
- The requirement's own SHALL statement is reviewed for consistency with the revised scenario body
  (see `design.md`); no other requirement in the file changes.
- **`docs/backlog.md`**: mark the "`launch-window-layout` spec contradicts itself about the window
  width" entry done, naming this change's id.
- **`CHANGELOG.md`**: one `### Changed` bullet under `## Unreleased` naming this change's id and the
  `launch-window-layout` capability.
- **`docs/architecture/ui.md`**: reviewed for any claim about the window's sizing behavior; see
  `design.md` for the result (no claim found, so no edit is planned unless a later read finds one).

No XAML, C#, or test file changes. This change corrects documentation to match already-shipped,
already-correct behavior; it does not change how the window is sized.

## Impact

- Affected spec: `launch-window-layout` (one requirement's scenario set).
- Affected docs: `docs/backlog.md`, `CHANGELOG.md`, and `docs/architecture/ui.md` only if review
  finds a contradicting claim there.
- No affected source files, no affected tests, no behavior change — the window continues to be
  sized by `SizeToContent="WidthAndHeight"` with `ResizeMode="NoResize"`, exactly as it is today.
- No manual smoke task: nothing observable in the running application changes.

## Non-goals

- Not making the launch window fixed-width. `Auto` pane columns, `SizeToContent`, the "Pane fits its
  list" and "List width follows the widest row with a 300px default" scenarios, and the draggable
  splitter all depend on the width tracking content; freezing the width would contradict all of
  them and require source changes this change does not make.
- Not rewriting "Window fits its content". That scenario is the accurate statement of the window's
  width behavior and the one other requirements already depend on; this change keeps it verbatim.
- Not deleting the "Fixed width and content height" scenario outright. Deleting it would silently
  drop its true half (the window's height follows its content) and leave the "Window chrome"
  requirement saying nothing about height at all. It is also not deletable through OpenSpec's
  `MODIFIED` delta mechanism without recreating it under the same name anyway (see `design.md`).
- Not renaming the scenario to a name that better fits its corrected body (e.g. "Height follows
  content, size not user-adjustable"). OpenSpec's `MODIFIED Requirements` delta replaces a
  requirement's whole scenario set and is validated against the current spec's scenario *names*; it
  has no `RENAMED`-at-scenario-level operation (confirmed against two precedents:
  `openspec/changes/archive/2026-10-27-independent-card-actions/design.md:175`, "OpenSpec RENAMED
  works only at requirement level, so no scenario may be renamed or removed", and
  `openspec/changes/archive/2026-10-12-reconcile-specs-with-implementation/design.md:19`, "Keep
  requirement titles and modify bodies... because the archive uses MODIFIED exclusively and never
  RENAMED"). The same constraint applies one level down, to scenario names inside a requirement:
  `openspec validate` rejected a first draft of this delta that dropped the old scenario name in
  favor of a new one, with "omits scenario(s) the current spec still has... Copy the omitted
  scenarios into the MODIFIED block." The scenario keeps its name; only its body changes.
- Not touching `RELEASE-NOTES.md` or adding a new version heading in `CHANGELOG.md`.
- No code, test, or build changes of any kind.

## Dependencies

None on other open changes. Builds on the already-archived `agent-row-inset-alignment`, which is
the change whose window-width side effect exposed the contradiction (cited as evidence, not
modified).

## Deviation from the brief

The brief asked for the false scenario to be "replaced by one whose THEN states the true invariant"
and named it as if it would carry a new title. `openspec validate` rejects a `MODIFIED` block that
drops an existing scenario name, even when a differently-named replacement scenario is added in its
place — see `design.md`, "Tooling constraint", for the exact validator error and the two archived
precedents that independently establish the same rule at the requirement level. The scenario named
"Fixed width and content height" is kept, with its body rewritten to the corrected text the brief
specified; its name is the one piece of the brief's literal wording this change could not carry out
as asked, because the tool does not support it. The corrected body fulfills the brief's substantive
intent: it states the height-follows-content and non-user-resizable facts and does not claim the
width is constant.
