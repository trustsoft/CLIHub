## Context

`IPluginRegistry.Save` returns `void` and silently swallows write failures with `this.logger.LogWarning` (`PluginRegistryRepository.cs:97-105`). The private `TrySave` (line 182-196) returns `bool` and is used internally by the adoption path. The interface does not expose the result to callers.

Call sites that matter:

| Caller | File:Line | Risk |
|---|---|---|
| `AgentManagementViewModel.UpdatePluginRegistryEntry` | `AgentManagementViewModel.cs:1405` | Disable/enable claims a state change that did not persist; catalog reload reads old state |
| `PluginInstaller.RecordInstalledState` | `PluginInstaller.cs:512` | Files committed, backup deleted, state unwritten → next scan adopts as unknown |
| `PluginCatalog` adoption save | `PluginCatalog.cs:204` | Folder adopted in memory only; re-adopted next scan |
| `PluginSeeder` save | `PluginSeeder.cs:202` | Built-ins not recorded; re-seeded next start |

`DisablePlugin` (line 1320-1332) and `EnablePlugin` (line 1339-1350) each call `UpdatePluginRegistryEntry` → `Save`, then `catalog.ReloadPlugins()`. A failed `Save` is invisible: the reload reads the old persisted state back, and the `catch` only fires for unexpected throws. The user sees no error.

`DisabledAgentCardItem` has no `Message` property (the entire class: `DisabledAgentCardItem.cs:10-77`), while `AgentCardItem` does (`AgentCardItem.cs:367,382`). `SettingsAgentsPage.xaml:337` binds `Message` / `HasMessage` only for active cards.

`GetUserModifiedPlugins` has zero production consumers under `src/` (grep: only `IPluginCatalog.cs:60` and `PluginCatalog.cs:258`). `RecordInstalledState` unconditionally rewrites the `ContentHash` (line 506), clearing the modification detection on every install/update.

`PluginInstallResult` (line 6): doc comment says "installed and activated". Plugins are never activated (ADR-0004, ADR-0016).

`ShowInstallOutcome` (`PluginDialogService.cs:104-120`): shows success with `MessageBoxImage.Information` or failure with `MessageBoxImage.Warning`. No warning channel exists.

## Decisions

### D1 — Interface contract: `bool TrySave(PluginRegistryDocument)`

**Decision:** Replace `void Save(PluginRegistryDocument)` with `bool TrySave(PluginRegistryDocument)` on `IPluginRegistry`. The existing private `TrySave` in `PluginRegistryRepository` (line 182-196) becomes the public implementation body. The `Save` wrapper (line 97-105) is removed.

All four call sites (`UpdatePluginRegistryEntry`, `RecordInstalledState`, catalog adoption, seeder) change from `.Save(document)` to inspecting `.TrySave(document)`.

**Alternatives:**
- (a) `bool TrySave` — matches the existing private method, the repo's `Try*` convention, and the interface's already-tolerant `Load()`. Minimal change: the private method becomes public.
- (b) Throw on failure — breaks the tolerant model. `Load` degrades to empty on corrupt read; throwing on write is inconsistent and forces every caller into try/catch for a disk-full condition.
- (c) Result object — overkill; a single boolean conveys the outcome.

### D2 — Disable/Enable: surface failure, reload to truth

**Decision:** `UpdatePluginRegistryEntry` returns `bool` (the `TrySave` result). `DisablePlugin` and `EnablePlugin` inspect it. On `false`: set `card.Message = "Could not save the plugin state"`, then still reload the catalog to present the persisted truth (the old state). The existing try/catch stays for unexpected throws.

For `EnablePlugin`, `DisabledAgentCardItem` gains `Message` and `HasMessage` properties mirroring `AgentCardItem` (`AgentCardItem.cs:367-382`). `SettingsAgentsPage.xaml` binds `Message` for disabled cards.

**Alternatives:**
- (a) Add `Message` to `DisabledAgentCardItem` — consistent pattern, one XAML binding, no new dialog.
- (b) Show a MessageBox — inconsistent with `DisablePlugin`'s inline message, forces a dialog on the user for a diagnostic condition.
- (c) Log only — the current invisibility; rejected by the brief.

**Reload-after-save behavior:** The current code reloads the catalog regardless of save outcome. When the save succeeds, this makes the UI reflect the new persisted state. When it fails, reloading reads the old state back — which IS the correct outcome (the UI shows what's actually on disk). The message tells the user why the change didn't stick.

### D3 — Install: warning channel, not rollback

**Decision:** `PluginInstallResult` gains `IReadOnlyList<string> Warnings`. `RecordInstalledState` checks `TrySave` and, on failure, appends a warning: "Plugin files were installed, but the plugin state could not be saved. The plugin will appear as Unknown origin on the next catalog scan." The result still reports `Success = true`. `ShowInstallOutcome` renders warnings after the success message with `MessageBoxImage.Warning`.

Fix the doc comment: "installed and activated" → "installed" (line 6).

**Alternatives:**
- (a) Warning channel — files are committed (backup deleted, staging moved), so the install succeeded. A state-write failure is non-fatal and recoverable (next scan adopts as unknown). The warning tells the user what degraded.
- (b) Report failure after file commit — contradicts the "Atomic installation with rollback" spec (`plugin-installation` requirement 4): backup is already deleted, there is nothing to roll back to. Reporting failure would mislead the user into thinking no files changed.
- (c) Roll back files on state-write failure — incorrect: deleting already-committed files would lose a successful install over a disk-full transient. The registry is recoverable; the files are not.
- (d) Keep `void Save` for the installer only — inconsistent; asks callers to know which save is tolerant.

### D4 — Seed and catalog adoption: fail-soft with context

**Decision:** `PluginSeeder` and `PluginCatalog` use `TrySave` and log their own context on failure. No UI change.

- Seeder: `this.logger.LogWarning("Could not write the plugin registry after seeding; seeded built-ins will be re-applied on next start")`
- Catalog: `this.logger.LogWarning("Could not write the plugin registry after adopting folders; adopted entries are in memory only")`

**Rationale:** These paths run at startup without user intent. A write failure here is transient (disk full), and the operations retry on the next start. The existing policy of fail-soft logging is correct; only the log message gains specificity.

### D5 — Local modification overwritten on update: record, do not change

**Decision:** Add one sentence to `docs/architecture/plugins.md` under "Scan Verification and Reconciliation": "An install or update replaces the installed descriptor and rewrites the stored `contentHash` by design; the user-modified flag is a diagnostic of drift since the last CLIHub write, not a protection mechanism, and any member of the user-modified collection is cleared by the next install or update that touches that plugin."

Add one scenario to the `plugin-installation` spec under "SHA-256 content integrity": "Local modification cleared by install."

No code changes. `GetUserModifiedPlugins` stays the diagnostic surface. The seeder's three-hash preservation rule for built-ins is unchanged.

**Alternatives:**
- (a) Document + spec only — matches the existing code behavior exactly. The brief states this is the product decision.
- (b) Add a confirmation dialog before overwriting — rejected by the brief ("Do NOT add a confirmation dialog").
- (c) Remove `GetUserModifiedPlugins` — rejected by the brief ("Do NOT remove `GetUserModifiedPlugins`").

## Deviations from the brief

- The brief's `EnablePlugin` catch (line 1346-1349) only logs a warning and does not set a message on `DisabledAgentCardItem`. The brief says to use the "existing `card.Message` / notification pattern" — we add `Message` to `DisabledAgentCardItem` to make this possible. The alternative (a dialog) breaks the pattern.
- The brief asks whether to reload or skip the reload after a failed save. We reload to the persisted truth: it is the safe default (no phantom state) and the message tells the user what happened.

## Open Questions

1. **Should `DisabledAgentCardItem.Message` also bind in the `SettingsAgentsPage.xaml` disabled section?** Yes — the XAML section that renders disabled cards needs `Text="{Binding Message}"` with `Visibility="{Binding HasMessage, ...}"` mirroring the active-card pattern at line 337. The precise XAML placement depends on the current disabled-section template structure, which the implementation session reads from the file.

2. **Should `PluginInstallResult.Warnings` carry the same pluralized form as `Errors` (`IReadOnlyList<string>`)?** Yes — consistent shape, zero warnings on the happy path, and `ShowInstallOutcome` iterates them the same way.

3. **Does the `PluginInstallResult` primary constructor syntax (`record` with `(bool Success, ...)`) need changing?** No — `Warnings` is an added property (not a primary-constructor parameter), initialized to `[]` in the `Succeeded` factory and left empty in `Failed`. The existing `Succeeded(string)` and `Failed(IReadOnlyList<string>)` factories gain a `warnings` parameter or default.

4. **Does the `PluginRegistryRepositoryTests` suite need new tests for the public `TrySave`?** The existing tests already call `Save` which delegates to `TrySave` — the atomic-write tests pass through the same code path. The implementation session adds at least one direct `TrySave` test for the return value and one "TrySave returns false when the file system is unwritable" test (mocked).