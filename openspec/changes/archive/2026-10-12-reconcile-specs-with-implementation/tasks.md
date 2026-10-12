## 1. Spec deltas

- [x] 1.1 Write the `app-lifecycle` delta correcting the dependency-injection inventory.
- [x] 1.2 Write the `launch-window-theme` and `settings-theme` deltas so the palette follows the application theme.
- [x] 1.3 Write the `agent-ui` delta removing the removed `init` command from the dynamic command-state scenario.
- [x] 1.4 Write the `logging` delta aligning the retention requirements and correcting the supported levels.
- [x] 1.5 Write the `preferences-ui` delta renaming the Logs page requirement.

## 2. Apply the deltas to the main specs

- [x] 2.1 Merge every delta into `openspec/specs/<capability>/spec.md` under a single `## Requirements` section.
- [x] 2.2 Correct the stale `## Purpose` lines in `launch-window-theme`, `settings-theme`, `agent-commands`, and `preferences-ui`.
- [x] 2.3 Confirm no delta operation headers remain in any main spec and no requirement title is duplicated.

## 3. Repository documents

- [x] 3.1 Fix the `CHANGELOG.md` parenthetical references that name no capability or change.
- [x] 3.2 Fix the stale `IProcessLauncher` invariant in `docs/architecture.md`.
- [x] 3.3 Fix the stale migration and preference statements in `docs/architecture/configuration.md`.
- [x] 3.4 Replace the Windows Terminal host statement in `docs/architecture/processes.md`.
- [x] 3.5 Fix the palette statements in `docs/architecture/ui.md`.

## 4. Verification

- [x] 4.1 Search `openspec/specs/`, `docs/`, and `CHANGELOG.md` for the stale terms (`IProcessLauncher`, `IPluginManager`, `wt.exe`, `CanInit`, `agent-process-monitoring`) and confirm none describe current behavior.
- [x] 4.2 Confirm no `src/` or `tests/` file changed.
- [x] 4.3 Archive this change to `openspec/changes/archive/2026-10-12-reconcile-specs-with-implementation/`.
