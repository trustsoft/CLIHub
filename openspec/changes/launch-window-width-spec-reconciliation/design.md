# Design: launch-window-width-spec-reconciliation

## Current state (verified in this session at HEAD `739e920`)

| Concern | Today | File:Line |
|---|---|---|
| Contradictory scenario A | "Window fits its content" — WHEN the panes' content changes, THEN the window's width follows the panes, so the window never keeps empty space beside its content | `openspec/specs/launch-window-layout/spec.md:232-234`, under Requirement "Agents pane and row sizing" (`:212-214`) |
| Contradictory scenario B | "Fixed width and content height" — WHEN the panes' content changes, THEN the window height follows its content and its width does not change | `openspec/specs/launch-window-layout/spec.md:252-254`, under Requirement "Window chrome" (`:236-238`) |
| Window sizing attribute | `SizeToContent="WidthAndHeight"` | `src/CLIHub/Views/LaunchWindow.xaml:20` |
| Window resize attribute | `ResizeMode="NoResize"` | `src/CLIHub/Views/LaunchWindow.xaml:18` |
| Projects pane column | `<ColumnDefinition Width="Auto" MinWidth="{StaticResource Sizing.ProjectPane.MinWidth}"/>` | `src/CLIHub/Views/LaunchWindow.xaml:114` |
| Splitter column | `<ColumnDefinition Width="Auto"/>` | `src/CLIHub/Views/LaunchWindow.xaml:116` |
| Agents pane column | `<ColumnDefinition Width="Auto" MinWidth="{StaticResource Sizing.AgentPane.MinWidth}"/>` | `src/CLIHub/Views/LaunchWindow.xaml:118` |
| Existing drag-resize scenario | "Window cannot be moved or resized by dragging" — WHEN the user drags anywhere on the window surface, THEN the window is neither moved nor resized | `openspec/specs/launch-window-layout/spec.md:256-258`, also under "Window chrome" |
| Concrete regression evidence | `agent-row-inset-alignment` widened the launch window from 596 to 600 DIP (900×806 px at 1.5x scaling minus the 1px border on each side, per that change's `tasks.md:91-94`, by adding 4 DIP of agent row padding (`Sizing.AgentRow.Padding` `10,0,10,0` → `12,0,12,0`) | `openspec/changes/archive/2026-11-01-agent-row-inset-alignment/tasks.md:91-94`, `design.md:48` |
| Backlog tracking entry | "`launch-window-layout` spec contradicts itself about the window width" — severity Low, documentation drift, no runtime behavior | `docs/backlog.md:607-622` |
| CHANGELOG `Unreleased` section today | Has `### Fixed` with two bullets; no `### Changed` group exists yet | `CHANGELOG.md:11-16` |
| `docs/architecture/ui.md` window-sizing claims | None found: the file discusses resource organization, theming, `SpacedPanel`, Settings page layout, footer insets, Settings numeric fields, and project row/rail insets (`:109-116`) — no sentence states or implies whether the launch window's width is fixed, content-driven, or user-resizable | Read in full, `docs/architecture/ui.md:1-154` |
| `README.md` width mentions | One hit, an unrelated `<img>` HTML attribute (`width="880"` on the launch-window screenshot), not a behavior claim | `README.md:30` |
| `docs/vision.md`, `docs/glossary.md` | No matches for width/sizing/resize/`SizeToContent` terms | grep, zero hits in both files |

Working tree is clean at `739e920` (`chore(openspec): archive agent-row-inset-alignment`), matching
the brief.

## The contradiction

Both scenarios share the identical WHEN clause — "the panes' content changes" — and assert opposite
facts about the same variable:

- Scenario A (Agents pane and row sizing → "Window fits its content"): width **follows** the panes.
- Scenario B (Window chrome → "Fixed width and content height"): width **does not change**.

They cannot both be true for the same trigger. The code settles which one is actually true:
`SizeToContent="WidthAndHeight"` combined with three `Auto` grid columns means WPF measures the
window's natural width from its content every layout pass. Scenario A is the accurate one. Scenario
B's width clause is simply wrong; its height clause ("the window height follows its content") is
correct and is not duplicated anywhere else in the file.

`ResizeMode="NoResize"` is a separate, true fact: the user cannot drag an OS resize border because
WPF draws none (`WindowStyle="None"`, `:26`) and no resize affordance is wired. That fact is already
captured by the existing scenario "Window cannot be moved or resized by dragging" (`:256-258`), whose
WHEN ("the user drags anywhere on the window surface") and THEN ("the window is neither moved nor
resized") already cover drag-driven resizing. Scenario B's width clause was likely an attempt to
restate "the user can't resize it" in width-specific language, but it picked the wrong invariant
(constant width) instead of the true one (no user-settable size) and now collides with Scenario A.

## Decision

Keep "Window fits its content" (Scenario A) verbatim — it is the accurate, authoritative statement
about the window's width, and other requirements already depend on its truth (see "Dependents"
below). Correct Scenario B ("Fixed width and content height") in place, under the same "Window
chrome" requirement, so its THEN states only the facts the requirement is actually responsible for:
the window's height follows its content, and the window's size is not settable by the user (no OS
resize affordance, `ResizeMode="NoResize"`). The corrected body does not assert anything about the
width's constancy.

### Tooling constraint: the scenario name cannot change

A first draft of this delta replaced the scenario under a new name ("Height follows content, size
not user-adjustable"), on the theory that the old name — asserting the false "Fixed width" half —
should not survive even with a corrected body. `openspec validate --all` rejected that draft:

```
✗ [ERROR] launch-window-layout/spec.md: MODIFIED "Window chrome" omits scenario(s) the current spec
still has: "Fixed width and content height". The modified block has 5 scenarios; the current spec
has 5 scenarios. It adds 1 scenario not in the current spec: "Height follows content, size not
user-adjustable". Copy the omitted scenarios into the MODIFIED block (a MODIFIED requirement
replaces the whole block, so archive refuses to drop them).
```

A `MODIFIED Requirements` block is validated against the *names* of the current spec's scenarios
under that requirement; it has no scenario-level rename or delete operation. Two archived changes
independently establish the same rule, one explicitly at the requirement level and one by directly
naming the scenario-level restriction:

- `openspec/changes/archive/2026-10-27-independent-card-actions/design.md:175`: "OpenSpec `RENAMED`
  works only at requirement level, so no scenario may be renamed or removed."
- `openspec/changes/archive/2026-10-12-reconcile-specs-with-implementation/design.md:19`: "`openspec
  validate` matches `MODIFIED` requirements by title. Where a title is now imprecise... the
  requirement body and scenarios are corrected rather than renaming the requirement, because the
  archive uses `MODIFIED` exclusively and never `RENAMED`."

The second precedent's rule — correct an imprecise title's *body* in place rather than renaming it —
is the one this change follows, one level down: the scenario keeps the name "Fixed width and content
height" and only its body is corrected. This is a deviation from the brief's literal wording (which
asked for a scenario "replaced by one" with implied new naming); it is recorded here and in
`proposal.md`'s "Deviation from the brief" section because the brief could not be carried out as
asked once the tool rejected it, and the available conforming alternative (keep the name, fix the
body) is both tool-accepted and faithful to the brief's substantive intent — a THEN that states the
true invariant and does not claim the width is constant.

### Exact scenario text shipped

```
#### Scenario: Fixed width and content height
- **WHEN** the panes' content changes
- **THEN** the window height follows its content, and the window's size is never settable by
  the user — there is no OS resize border and `ResizeMode` is `NoResize`
```

The name is unchanged from the current spec; only the THEN clause is rewritten. The four sibling
scenarios ("No title bar or frame controls", "Window outline", "Rounded corners", "Window cannot be
moved or resized by dragging") are kept with both their names and bodies unchanged, per the brief.

### Requirement text consistency

The "Window chrome" requirement's SHALL statement (`:238`): "The launch window SHALL be presented
without operating-system window chrome, delineated instead by its own border and shape." This
statement is about the *absence of OS chrome* (title bar, frame controls, resize border) — it does
not claim anything about width or height, so no edit to the requirement text is needed; the new
scenario is a direct instance of "no OS resize border" plus the orthogonal, already-true fact that
height tracks content. The requirement text and its five scenarios (four kept, one replaced) stay
mutually consistent: none of them claims the width is constant, and "Window fits its content" in the
sibling requirement remains the sole word on the width's behavior.

### No duplication with "Window cannot be moved or resized by dragging"

That scenario's WHEN is "the user drags anywhere on the window surface" (an interaction trigger); the
new scenario's WHEN is "the panes' content changes" (a content trigger). They test different
triggers and would need different test setups if automated, so there is no overlap to collapse. The
new scenario's size-not-user-adjustable clause is a restatement of the same `ResizeMode="NoResize"`
fact from a different angle (content-driven measurement vs. user drag input) — both are true and
both are worth keeping as separate, independently-triggerable scenarios, matching the existing style
of the file (e.g., "Pane headers aligned" / "Pane bodies end at the footer" state related but
distinct facts about the same requirement rather than merging into one scenario).

### Rejected alternatives

1. **Delete the false scenario outright**, leaving "Window chrome" with four scenarios. Rejected:
   this loses the true half of the deleted scenario (the window's height follows its content), which
   is not asserted anywhere else in the file — the capability would go from over-claiming on width to
   staying silent on height, trading one gap for another instead of closing it.
2. **Make the launch window fixed-width** (add an explicit `Width` and drop `SizeToContent`'s width
   component, e.g. `SizeToContent="Height"`), to match the literal text of the old scenario. Rejected:
   this contradicts `Auto` pane columns (`LaunchWindow.xaml:114,116,118`), the draggable splitter
   requirement ("Dragging changes pane widths": "the window follows the panes so no empty space is
   left over", `:16-18`), "Pane fits its list" (`:216-218`), and "List width follows the widest row
   with a 300px default" (`:124-126`) — all of which require the window's width to track pane content.
   It would also require a source change, which this change (spec-only) is not scoped to make, and
   would reverse the already-shipped, already-correct `SizeToContent="WidthAndHeight"` behavior for no
   product benefit.
3. **Rewrite "Window fits its content" instead**, treating Scenario B as authoritative. Rejected: that
   scenario is the accurate one and other scenarios in the same requirement group ("Pane fits its
   list", "Rows fill the list width") already assume content-driven pane and window sizing; rewriting
   it to match the false "fixed width" claim would make the spec wrong instead of right, and would
   contradict the verified `SizeToContent="WidthAndHeight"` + `Auto` column behavior at
   `LaunchWindow.xaml:20,114,116,118`.

## Dependents on "Window fits its content" staying true

Confirmed by reading the full spec file: no other scenario names "Window fits its content" directly
(OpenSpec scenarios do not cross-reference each other by name), but several scenarios in the same
file assume the same underlying width-follows-content behavior and would become internally
inconsistent if that scenario were weakened:

- "Dragging changes pane widths" (`:16-18`): "...the window follows the panes so no empty space is
  left over."
- "Pane fits its list" (`:216-218`): "the Agents pane is as wide as its widest agent row requires, so
  no empty space is left beside the list."
- "Projects pane hugs the list" (`:116-118`): "the Projects pane is exactly as wide as its list
  requires... and the Agents pane takes the remaining width."
- "List width follows the widest row with a 300px default" (`:124-126`).

These all stay unchanged by this change; they are cited here only to confirm the decision to keep
"Window fits its content" verbatim is consistent with the rest of the capability, not to modify them.

## Check of every other place describing the window's width

| Location | Claim found | Action |
|---|---|---|
| `openspec/specs/launch-window-layout/spec.md:232-234` ("Window fits its content") | Width follows panes | Kept verbatim (authoritative) |
| `openspec/specs/launch-window-layout/spec.md:252-254` ("Fixed width and content height") | Width does not change | Replaced (see above) |
| `src/CLIHub/Views/LaunchWindow.xaml:18,20` | `ResizeMode="NoResize"`, `SizeToContent="WidthAndHeight"` | Source of truth; not edited (no source changes in this change) |
| `README.md:30` | `<img ... width="880">` — an HTML image-display width, unrelated to the application window's sizing behavior | No fix needed; not a behavior claim |
| `docs/vision.md` | No width/sizing/resize/`SizeToContent` text found | No fix needed |
| `docs/glossary.md` | No width/sizing/resize/`SizeToContent` text found | No fix needed |
| `docs/architecture/ui.md` (full read, 154 lines) | No sentence claims the launch window's width is fixed, content-driven, or user-resizable | No fix needed; confirmed by full read, not just grep |
| `docs/backlog.md:607-622` | Already correctly describes the contradiction (not a claim to fix, but the tracking entry to close) | Mark done with this change's id |
| `CHANGELOG.md:11-16` | No width claim; only unrelated `Fixed` bullets for other changes | Add a `### Changed` bullet recording this change |

No additional occurrence of the false "width does not change" (or equivalent) claim exists outside
the spec file itself.

## `docs/architecture/ui.md` resolution

The brief asks to correct this file "minimally" only if it states a sizing claim; it does not. The
file's only pane/row-sizing content is the project row's marker-rail paragraph (`:109-116`), which
describes 12 DIP content insets within a row — not the window's or a pane's overall width behavior —
and is unaffected by (and consistent with) either scenario. No edit is planned for this file.

## CHANGELOG placement

`CHANGELOG.md`'s `## Unreleased` section (`:11-16`) currently has only a `### Fixed` group with two
bullets (`project-row-trailing-inset`, `agent-row-inset-alignment`). This change is a documentation
correction to an existing spec, which the file's own groupings (`Added`, `Changed`, `Fixed`,
`Removed`) and the brief classify as `Changed` — a new `### Changed` group is inserted under
`## Unreleased`, after the existing heading and before `### Fixed` (matching the group order used
under `## 1.0.4 — 2026-11-01`: `Added`, `Changed`, `Fixed`, `Removed`, `:18-42`), with one bullet
naming this change's id and the `launch-window-layout` capability. `RELEASE-NOTES.md` is not touched
(no new version heading, no user-facing behavior change — the window behaves exactly as it already
did).

## Open questions

None block implementation. The change is a single scenario-body correction in one spec file plus
two documentation bookkeeping edits (`docs/backlog.md`, `CHANGELOG.md`); `docs/architecture/ui.md`
needs no edit per the verified review above, and no source, test, or build artifact is affected.
The scenario's name stays "Fixed width and content height" — slightly imprecise against its
corrected body, which no longer claims the width is fixed — because OpenSpec's tooling forced that
choice (see "Tooling constraint" above); flag this to the user if a future change wants a true
rename, which would need to go through `openspec` the one way it does support renames (a
`RENAMED Requirements` pair at the whole-requirement level, not a scenario-level rename, and only if
the requirement itself is being renamed for an unrelated reason).
