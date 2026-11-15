## 1. Interface contract: `TrySave` returns `bool`

- [x] 1.1 Change `IPluginRegistry.Save` to `bool TrySave(PluginRegistryDocument document)`. Update the XML doc.
- [x] 1.2 In `PluginRegistryRepository`, remove the `Save` wrapper (line 97-105); make `TrySave` (line 182-196) `public` and the interface implementation. Keep the private log line at line 193 â€” `TrySave` logs the warning and returns `false`, it does not log on success.
- [x] 1.3 Update `InMemoryPluginRegistry` (test fake) to implement `TrySave` returning `true` (configurable for failure tests).
- [x] 1.4 Sweep all four call sites to use `TrySave` and inspect the result:
  - `AgentManagementViewModel.UpdatePluginRegistryEntry` â†’ return `bool`.
  - `PluginInstaller.RecordInstalledState` â†’ record result for warnings (see stage 2).
  - `PluginCatalog` adoption save â†’ log context on `false`.
  - `PluginSeeder` save â†’ log context on `false`.
- **Gate:** `dotnet build CLIHub.sln` 0 warnings; `dotnet test CLIHub.sln` all green.

## 2. Disable/Enable: surface save failure

- [x] 2.1 Make `UpdatePluginRegistryEntry` return `bool` (the `TrySave` result).
- [x] 2.2 `DisablePlugin`: inspect the `bool`. On `false`, set `card.Message = "Could not save the plugin state"` (existing `card.Message` property). Still reload the catalog to present the persisted truth. Existing try/catch stays.
- [x] 2.3 `EnablePlugin`: inspect the `bool`. `DisabledAgentCardItem` has no `Message` â€” add `Message` and `HasMessage` properties mirroring `AgentCardItem.cs:367-382`. On `false`, set `card.Message = "Could not save the plugin state"`. Still reload the catalog. Existing try/catch stays.
- [x] 2.4 Bind `DisabledAgentCardItem.Message` in `SettingsAgentsPage.xaml` (read the disabled-section template structure to find the right place; mirror the active-card pattern at `SettingsAgentsPage.xaml:337-339`).
- [x] 2.5 Add/update unit tests in `AgentManagementViewModelTests.Plugins.cs`:
  - `DisablePlugin_SaveFails_SetsMessage` â€” mock `TrySave` returning `false`, assert `card.Message` is set, assert catalog reload still called.
  - `EnablePlugin_SaveFails_SetsMessage` â€” same for disabled card.
  - Existing enable/disable tests keep their assertions; only the mock setup changes (`Save` â†’ `TrySave(true)`).
- **Gate:** `dotnet build CLIHub.sln` 0 warnings; `dotnet test CLIHub.sln` all green.

## 3. Install: warning channel on state-write failure

- [x] 3.1 Add `IReadOnlyList<string> Warnings` property to `PluginInstallResult`. Initialize to `[]` in both `Succeeded` and `Failed` factories. The `Succeeded` factory gains an optional `IReadOnlyList<string>? warnings` parameter (default `null` â†’ `[]`).
- [x] 3.2 Fix the doc comment on `PluginInstallResult`: "installed and activated" â†’ "installed".
- [x] 3.3 `PluginInstaller.RecordInstalledState`: call `TrySave` instead of `Save`. On `false`, log a warning and collect it. Pass the collected warnings into `PluginInstallResult.Succeeded`.
- [x] 3.4 `PluginInstaller.CommitInstall` (line 295-309): after `RecordInstalledState`, if warnings are non-empty, return `Succeeded(pluginId, warnings)`. The install is still a success.
- [x] 3.5 `PluginDialogService.ShowInstallOutcome`: after the success message, render each warning line with `MessageBoxImage.Warning` (or append to the success text). Read the current implementation at `PluginDialogService.cs:104-120` for the exact flow.
- [x] 3.6 Add/update unit tests in `PluginInstallerTests.cs`:
  - `Install_StateSaveFails_ReportsSuccessWithWarning` â€” mock `TrySave` returning `false`, assert `result.Success` is `true`, assert `result.Warnings` contains the expected text.
  - Existing install tests keep their assertions; only the mock setup changes (`Save` â†’ `TrySave(true)`).
- [x] 3.7 Add/update unit tests in `PluginDialogServiceTests` for warning rendering (if a test file exists; otherwise test through the `PluginInstaller` integration).
- **Gate:** `dotnet build CLIHub.sln` 0 warnings; `dotnet test CLIHub.sln` all green.

## 4. Seed and catalog adoption: fail-soft with context

- [x] 4.1 `PluginSeeder` (line 202): `if (stateDirty) { this.stateRepository.TrySave(state); }` â€” inspect the result. On `false`, log: "Could not write the plugin registry after seeding; seeded built-ins will be re-applied on next start".
- [x] 4.2 `PluginCatalog` adoption save (line 204): `if (stateDirty) { this.stateRepository!.TrySave(state); }` â€” inspect the result. On `false`, log: "Could not write the plugin registry after adopting folders; adopted entries are in memory only".
- [x] 4.3 Update `PluginSeederTests` and `PluginCatalogTests`: add cases for `TrySave` returning `false` â€” assert the log message is emitted, assert the operation continues (does not throw).
- **Gate:** `dotnet build CLIHub.sln` 0 warnings; `dotnet test CLIHub.sln` all green.

## 5. Spec deltas

- [x] 5.1 Write delta `specs/plugin-registry/spec.md`:
  - **MODIFIED "Single local registry document"** â€” add scenario "Failed write reported to caller" (the write is atomic; a failure returns `false` and is never presented as persisted).
  - **MODIFIED "Verify content and reconcile on catalog load"** â€” add scenario "Catalog adoption write failure logged" (folder-adoption save failure is logged with context; adopted entries stay in memory).
- [x] 5.2 Write delta `specs/plugin-installation/spec.md`:
  - **MODIFIED "Disable and enable plugins"** â€” add scenario "Failed save after disable surfaces message" and "Failed save after enable surfaces message" (preserving original Disable/Enable scenario names).
  - **MODIFIED "Plugin lifecycle state tracking"** â€” add scenario "State write failure after install adds warning" (files installed, install reports success with warning).
  - **MODIFIED "Atomic installation with rollback"** â€” add scenario "State write failure after move is a warning not a rollback" (the state write is post-commit; rollback is only for staging/pre-move failures).
  - **MODIFIED "SHA-256 content integrity"** â€” add scenario "Local modification cleared by install" (when a plugin is installed over a locally modified descriptor, the installed content and hash replace the modified ones and the plugin is no longer reported as user-modified).
- **Gate:** `openspec validate --all` passes.

## 6. Documentation and backlog

- [x] 6.1 Add one sentence to `docs/architecture/plugins.md` under the "Scan Verification and Reconciliation" section: "An install or update replaces the installed descriptor and rewrites the stored `contentHash` by design; the user-modified flag is a diagnostic of drift since the last CLIHub write, not a protection mechanism, and any member of the user-modified collection is cleared by the next install or update that touches that plugin."
- [x] 6.2 Update `docs/backlog.md`:
  - "Locally modified plugins are reported but never consulted" â†’ `**Status: Resolved by plugin-state-integrity (Part B â€” documented as accepted behavior).**`
  - "Registry save failures are invisible" â†’ `**Status: Resolved by plugin-state-integrity (Part A â€” TrySave returns bool, callers surface failure).**`
- **Gate:** `dotnet build CLIHub.sln` 0 warnings (doc-only changes, no code impact).

## 7. Final verification

- [x] 7.1 `dotnet build CLIHub.sln` â€” 0 warnings.
- [x] 7.2 `dotnet test CLIHub.sln` â€” all green.
- [x] 7.3 `openspec validate --all` â€” 0 errors.
- [x] 7.4 `graphify update .` â€” graph refreshed.
- [x] 7.5 `node .gitnexus/run.cjs detect-changes --scope all --repo .` â€” clean.
- **Gate:** all four checks pass.