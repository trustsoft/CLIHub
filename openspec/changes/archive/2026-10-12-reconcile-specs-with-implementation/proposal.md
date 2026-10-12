## Why

Several durable capability specs and repository documents still describe behavior the shipped implementation no longer has, so the behavior contract contradicts the code. `app-lifecycle` names retired dependency-injection services (`IPluginManager`, `IProcessLauncher`); the launch and settings theme specs assert a dark-only look although a light palette ships; the agent specs still reference the removed `init` command; and the logging spec describes retention as days while the logger keeps a file count. The implementation is correct in every case; the specs and docs must be reconciled with it.

## What Changes

- Correct `app-lifecycle`'s dependency-injection inventory to the services the composition root actually registers (`IPluginCatalog`, `IInteractiveProcessRunner`, `IProcessOutputRunner`; no `IPluginManager`/`IProcessLauncher`).
- Correct `launch-window-theme` and `settings-theme` so the windows use the shared application theme palette (dark by default, light when the effective theme is light) instead of asserting a dark-only look; keep the drawn chrome, layout, and card requirements.
- Remove the deleted `init` command from the `agent-commands` Purpose and from the `agent-ui` dynamic command-state scenario.
- Correct `logging`: the retention requirements agree on days, and the supported levels include `Verbose` and `Fatal`.
- Correct the `preferences-ui` "Telemetry and Logs page options" requirement name to "Logs page options".
- Fix the stale capability references in `CHANGELOG.md` and the stale statements in `docs/architecture.md`, `docs/architecture/configuration.md`, `docs/architecture/processes.md`, and `docs/architecture/ui.md`.

## Capabilities

### New Capabilities
<!-- None: this change only corrects existing capability specs. -->

### Modified Capabilities
- `app-lifecycle`: dependency-injection inventory.
- `launch-window-theme`: window palette follows the application theme.
- `settings-theme`: chrome palette follows the application theme.
- `agent-ui`: dynamic command-state scenario no longer references the removed `init` command.
- `logging`: retention unit and supported log levels.
- `preferences-ui`: Logs page requirement name.

## Impact

- `openspec/specs/` for the capabilities above (through this change's deltas and in-place Purpose corrections).
- `CHANGELOG.md`, `docs/architecture.md`, `docs/architecture/configuration.md`, `docs/architecture/processes.md`, `docs/architecture/ui.md`.
- No production or test code change; no `config.json` change.

## Non-goals

- Changing runtime behavior, the configuration schema, or any WPF/XAML.
- Renaming capability directories or restructuring the spec set.
