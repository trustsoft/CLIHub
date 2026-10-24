# Proposal

## Why

The capability that contracts the launch window's two-pane layout is named `main-window-layout`, but the window it describes is `LaunchWindow` — the only window registered in DI (`src/CLIHub/Composition/ServiceRegistration.cs:113`) and the window assigned as the application main window (`src/CLIHub/Startup/ApplicationBootstrapper.cs:68`). `MainWindow` (`src/CLIHub/Views/MainWindow.xaml.cs:25`) is an unregistered, never-constructed legacy reference. The name collision is recorded as Medium documentation debt in `docs/backlog.md:227-233` ("`main-window-layout` names the wrong window") and misleads every reader who maps capability names onto code. The spec text drifts the same way: eight scenario WHEN lines and the Purpose say "the main window" while the sibling `launch-window-theme` capability and the newer scenarios in this same spec already say "the launch window".

## What Changes

- **Capability rename**: retire `main-window-layout` and create `launch-window-layout` with the same twelve requirements and all sixty-four scenarios. Requirement names and scenario names are byte-identical; only the window noun in the text changes.
- **Wording fix**: inside the renamed capability, "the main window" becomes "the launch window" in the eight scenario WHEN lines (`openspec/specs/main-window-layout/spec.md` lines 14, 34, 42, 46, 50, 58, 170, 178) and the Purpose is rewritten to name the launch window explicitly.
- **Terminological note**: the new Purpose records that the legacy `MainWindow` class is an unregistered build-only reference, not the subject of the capability.
- **Cross-reference follow-up**: update the live references to the old capability id — `ui/mockups/README.md:71` and `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md:81` — and resolve the backlog item (`docs/backlog.md:227-233`).
- Spec and docs only: no source, test, or configuration changes.

## Capabilities

### New Capabilities

- `launch-window-layout`: adds the launch window's layout contract — the twelve requirements of the retired capability with the window noun corrected and an explicit Purpose.

### Removed Capabilities

- `main-window-layout`: removed — its content moves to `launch-window-layout`; the capability is retired and its spec deleted on archive.

## Impact

- **Specs**: `openspec/specs/main-window-layout/spec.md` is replaced by `openspec/specs/launch-window-layout/spec.md` when this change is archived. The `.openspec.yaml` of this change declares `retire_capabilities: true`, which authorizes `openspec archive` to delete the emptied old spec.
- **Docs**: `ui/mockups/README.md` (capability id in the delivered-features list), `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md` (capability id in the scenario-to-test table), `docs/backlog.md` (resolve the item that motivated this change). `docs/architecture.md`, `docs/architecture/ui.md`, `docs/project-context.md`, and `README.md` were checked and contain no reference to the old capability id or "main window" prose; they need no edit.
- **Code**: none. `LaunchWindow`, `MainWindow`, DI registration, and startup wiring are untouched.
- **Non-goals**: deleting or registering the legacy `MainWindow` class (backlog option C, stays parked); rewording "main window" prose inside other capabilities (`app-lifecycle` lines 23, 99, 103, 107 and `preferences-ui` line 96 — same drift, different capabilities, out of scope); rewriting historical `CHANGELOG.md` entries or the completed `openspec/plans/2026-10-10-settings-window-roadmap.md` snapshot, which record the id that was current at the time.

## Dependencies

None. This change does not build on any other open change.
