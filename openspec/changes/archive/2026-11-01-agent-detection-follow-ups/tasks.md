# Tasks: agent-detection-follow-ups

## 1. Remove display-unused probe policy properties

- [x] 1.1 Remove `ProbePolicySummary` property (line 313-314) from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`.
- [x] 1.2 Remove `ProbePolicyDetail` property (line 319-321) from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`.
- [x] 1.3 Remove the private `HasCustomProbePolicy` property (line 323-324) from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`.
- [x] 1.4 Remove the two `OnPropertyChanged` calls for `ProbePolicySummary` and `ProbePolicyDetail` from `RaiseProbePolicyChanges()` (lines 338-339) in `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`.
- [x] 1.5 Remove the two `OnPropertyChanged` calls for `ProbePolicySummary` and `ProbePolicyDetail` from `Load()` (lines 674-675) in `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`.
- [x] 1.6 **Build gate:** Run `dotnet build CLIHub.sln` and verify zero errors.

## 2. Repurpose tests to preserve coverage

- [x] 2.1 Update `Load_EmptyProbeFields_ShowsBuiltInDefaults` test (lines 829-838) in `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`: keep the test name and the `fixture.ViewModel.Load()` call, but replace the two assertions with four new assertions for the per-field properties: `Assert.Equal("15 min", fixture.ViewModel.ProbePolicyCacheTtlSummary)`, `Assert.Equal("Built-in default", fixture.ViewModel.ProbePolicyCacheTtlDetail)`, `Assert.Equal("10 sec", fixture.ViewModel.ProbePolicyTimeoutSummary)`, `Assert.Equal("Built-in default", fixture.ViewModel.ProbePolicyTimeoutDetail)`.
- [x] 2.2 Update `ProbeFields_PositiveValues_ShowCustomEffectivePolicy` test (lines 840-851) in `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`: keep the test name and the field assignments (`ProbeTtlText = "30"`, `ProbeTimeoutText = "5"`), but replace the two assertions with four new assertions for the per-field properties: `Assert.Equal("30 min", fixture.ViewModel.ProbePolicyCacheTtlSummary)`, `Assert.Contains("Custom", fixture.ViewModel.ProbePolicyCacheTtlDetail)`, `Assert.Equal("5 sec", fixture.ViewModel.ProbePolicyTimeoutSummary)`, `Assert.Contains("Custom", fixture.ViewModel.ProbePolicyTimeoutDetail)`. Use `Assert.Contains` because the detail text is `"Custom (default: N unit)"`, not just `"Custom"`.
- [x] 2.3 **Test gate:** Run `dotnet test CLIHub.sln` and verify the two repurposed tests pass. All tests must pass (no new failures).

## 3. Delete the closed backlog item

- [x] 3.1 Delete lines 605-616 (`### ProbePolicySummary and ProbePolicyDetail are display-unused after the Agent Detection split`) from `docs/backlog.md`. The two items closed by this change are (1) this item and (2) the Agent Detection screenshot gap (lines 595-603), which will be closed after the capture task.

## 4. Add Agent Detection page to README screenshot set

- [x] 4.1 Add `await CapturePageAsync(settingsWindow, settingsViewModel, "agent-detection", directory, "settings-agent-detection.png");` after line 114 in `src/CLIHub/Views/ReadmeImageCapture.cs`.
- [x] 4.2 Add `'settings-agent-detection.png'` to the `$captures` array after line 497 in `docs/images/tools/Capture-ReadmeImages.ps1`.
- [x] 4.3 Add `Assert-ImageSize (Join-Path $OutputDirectory 'settings-agent-detection.png') -Width 1200 -Height 900` after line 513 in `docs/images/tools/Capture-ReadmeImages.ps1`.
- [x] 4.4 Add the screenshot block and caption to `README.md` after lines 42-44 (the CLI Agents screenshot block):
  ```markdown
  <img src="docs/images/settings-agent-detection.png" alt="CLIHub Settings: agent detection cache TTL and version check timeout" width="880">
  
  *Settings → Agent Detection: detection cache TTL, version check timeout, effective values in the header cards, and the Reset action to restore built-in defaults.*
  ```
- [x] 4.5 Add the image table row to `docs/images/README.md` after line 14 (the `settings-agents.png` row):
  ```markdown
  | `settings-agent-detection.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Agent Detection (detection cache TTL, version check timeout, DEFAULTS card, and Reset action). |
  ```
- [x] 4.6 **Build gate:** Run `dotnet build CLIHub.sln` and verify zero errors.

## 5. Manual screenshot capture

- [x] 5.1 **Manual task:** Run `powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1` to generate `docs/images/settings-agent-detection.png`. The app must not be running. If the environment cannot run the GUI capture (headless, missing dependencies, app already running), record that fact and leave the code/list changes from step 4 in place. Do not fabricate an image file.
- [x] 5.2 **Validation gate (if capture succeeded):** Verify `docs/images/settings-agent-detection.png` exists and is 1200×900 px (the script's `Assert-ImageSize` will have checked this).
- [x] 5.3 Delete the Agent Detection screenshot gap backlog item (lines 595-603 from `docs/backlog.md`) only if the capture succeeded and the PNG exists. If the capture could not run, leave the backlog item open with a note about the environment constraint.

## 6. Final validation

- [x] 6.1 **Test gate:** Run `dotnet test CLIHub.sln` and verify all tests pass (no new failures).
- [x] 6.2 **Validation gate:** Run `openspec validate --all` and verify zero errors (skip_specs: true declared, so no spec deltas to validate).
- [x] 6.3 Review the changes: 3 properties removed, 4 notification calls removed, 2 tests repurposed to assert per-field properties, Agent Detection page added to capture harness/script/README/image table, backlog items deleted.
- [x] 6.4 **Update graphify (if required by repository tooling):** Run `graphify update .` to refresh the knowledge graph after source modifications.

## Notes

- `ResolveProbeTtlMinutes()` and `ResolveProbeTimeoutSeconds()` are **kept** — the per-field summary properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyTimeoutSummary`) still use them.
- The two tests are **repurposed**, not deleted — their coverage of defaults/custom values is preserved by asserting the per-field properties.
- The manual capture task (step 5.1) is **explicit about failure handling** — if the environment cannot run the GUI capture, the implementer records that and leaves the code/list changes in place. Do not fabricate an image.
- The second backlog item (Agent Detection screenshot gap, lines 595-603) is deleted only if the capture succeeds. If the capture cannot run, leave it open with a note.
- No spec deltas — `skip_specs: true` is declared in `.openspec.yaml`.

## Execution notes

- The capture (5.1) ran successfully: the script built the Debug app, rendered every page, and passed its own
  `Assert-ImageSize` checks. `docs/images/settings-agent-detection.png` measures 1332×1031 px, the same pixel
  size as the existing `settings-*.png` captures; `Assert-ImageSize` asserts a minimum of 1200×900, not an exact
  match, so 5.2 is satisfied per the script's own gate. No image was fabricated.
- The capture run also regenerated the six existing README images (`launch-window.png`,
  `settings-appearance.png`, `settings-discovery.png`, `settings-agents.png`, `banner.png`,
  `social-preview.png`). Those files were restored byte-for-byte to their committed state so this change stays
  scoped to the planned file list; refreshing the older captures is separate follow-up work.
