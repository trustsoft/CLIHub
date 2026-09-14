## 1. Document models and generic store

- [x] 1.1 Implement `JsonDocumentStore<T>` (filename parameter, load-with-defaults, atomic temp-file replace, corrupt `.bak` backup, unknown-field round-trip via extension data); verify unit tests for missing file, unparseable file, unknown-field survival, and no temp leftover
- [x] 1.2 Add `SettingsDocument`, `ProjectsDocument`, `AgentsDocument` (each with `schemaVersion` and extension data); verify each round-trips its sections through the store in unit tests
- [x] 1.3 Add `SettingsStore` exposing runtime default, hotkey, probe tuning, and update flag reads; verify unit tests for defaults when `settings.json` is missing or unparseable

## 2. Migration

- [x] 2.1 Implement the split migrator: when `settings.json` is absent and legacy `config.json` exists, write the three owner files (unknown top-level fields into `settings.json`), then rename `config.json` to `config.json.migrated`; verify unit tests for first-run split, unknown-field placement, one-time gating on `settings.json`, and corrupt legacy file being skipped without crash

## 3. Service rewiring

- [x] 3.1 Rewire `ProjectRegistry` to own `projects.json` (drop cached-aggregate saves, drop `Runtime`/`Hotkey` exposure); verify `ProjectRegistryTests` pass against `projects.json`
- [x] 3.2 Rewire `AgentDetector` to read probe tuning from `SettingsStore` and persist the cache to `agents.json` once per round; verify `AgentDetectorTests` pass with probe settings sourced from `settings.json`
- [x] 3.3 Rewire `UpdateService` to read `update.*` from `SettingsStore`; verify `UpdateServiceTests` pass against `settings.json`
- [x] 3.4 Rewire the composition root in `App.xaml.cs`: build the stores, run the migrator before first store read, pass each owner its store, feed the popup the runtime default from settings; verify `dotnet build CLIHub.sln` succeeds

## 4. End-to-end verification

- [x] 4.1 Run `dotnet test CLIHub.sln` and confirm all tests pass
- [x] 4.2 Manual: with the existing `%AppData%\CLIHub\config.json`, launch the app, confirm the three files appear with correct contents and `config.json.migrated` remains; relaunch and confirm no re-split; hotkey, popup add/remove project, and agent list behave as before

## 5. Documentation

- [x] 5.1 Update `docs/vision.md`, `docs/ui.md`, `README.md`, `AGENTS.md` to describe the three-file layout and the migration; verify no stale `config.json` references remain (`grep config.json` over docs hits only migration-legacy mentions)
- [x] 5.2 Update the `app-config` Purpose in `openspec/specs/app-config/spec.md` to the generic document-persistence wording; verify `openspec validate` passes for the change
