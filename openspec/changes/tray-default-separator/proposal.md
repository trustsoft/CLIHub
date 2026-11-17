## Why

In the tray project submenus, the configured default agent is always followed by a `Separator`
(`src/CLIHub/Tray/TrayMenuBuilder.cs:156`, inside `PopulateProjectSubmenu`). When the default is the
only item a project submenu lists — a favorite project where no other agent is initialized — that
separator is a trailing, meaningless divider hanging under a single row. The user wants it hidden in
that case: the separator should appear only when at least one other agent item actually follows the
default.

The default-only case is real, not a corner: the default agent is pinned first in every project
submenu even when it is not initialized for the project
(`openspec/specs/tray-menu/spec.md:103-108`), so a project with no detected agents still shows the
pinned default plus the stray separator.

## What Changes

- `PopulateProjectSubmenu` (`src/CLIHub/Tray/TrayMenuBuilder.cs:143-177`) keeps the default agent as
  the first item of every project submenu, but adds the separator after it **only when the submenu
  also lists at least one other initialized agent** (after de-duplicating the default, which today is
  skipped at `src/CLIHub/Tray/TrayMenuBuilder.cs:162-165`). Mechanically: materialize the non-default
  initialized agents first, add the default item, add the separator conditionally, then append the
  others.
- Default only (no other initialized agents in the submenu): the submenu contains **exactly** the
  default item — no separator, and no `(no agents)` placeholder (the default is a real, launch-bound
  item, so the placeholder rule at `src/CLIHub/Tray/TrayMenuBuilder.cs:173-176` is untouched).
- No default configured, or the configured id is not in the catalog: unchanged — no default item, no
  separator.
- Everything else is unchanged: labels, MDL2 icons, the `(no agents)` rule, click/launch behavior,
  the top-level menu structure including the separator before `Exit`, and the state/projection shape.
- Spec delta: one MODIFIED requirement, "Default agent is first in every project submenu"
  (`openspec/specs/tray-menu/spec.md:103-141`). All six original scenario names are preserved; only
  the THEN clauses that promise the separator are qualified, and one new scenario covers the
  default-only case.

## Impact

- `src/CLIHub/Tray/TrayMenuBuilder.cs` — `PopulateProjectSubmenu` only; a private static helper with
  no other callers of the affected logic. No signature, state, or projection changes.
- `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs` — three new default-only tests; the existing
  separator-present tests (`:80-130`, `:133-155`) keep passing unchanged because they always include
  a second agent.
- `openspec/specs/tray-menu/spec.md` — after archive, the modified requirement text replaces the
  unconditional separator promise.

`skip_specs` is **not** set: this is an observable tray-menu behavior change with a MODIFIED delta at
`specs/tray-menu/spec.md`.

## Non-Goals

- Not changing when or how the default agent is resolved (`TrayStateProjection.ResolveDefaultAgent`)
  or the fact that it is pinned first even when not initialized.
- Not changing the `(no agents)` placeholder rule; it still appears only when there is no default and
  no initialized agents.
- Not touching the top-level tray menu separator before `Exit` or any other menu structure, label, or
  icon rule.
- Not adding a preference for separator visibility, and not reordering or de-duplicating agents in
  the launch window.
- Not changing `TrayMenuState`/`TrayProjectAgents` shape or the projection; the fix is builder-local.

## Dependencies

None. Self-contained inside `TrayMenuBuilder` and its tests; no other change is assumed.
