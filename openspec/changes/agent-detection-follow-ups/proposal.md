# Proposal: agent-detection-follow-ups

## Why

The `agent-detection-settings-page` change (archived 2026-11-01) split agent detection settings into a dedicated page, leaving two deferred items in `docs/backlog.md`:

1. **Display-unused properties after the Agent Detection split** (backlog.md:605-616): `ProbePolicySummary`, `ProbePolicyDetail`, and the private `HasCustomProbePolicy` helper have no XAML binding after the DETECTION card was removed from the CLI Agents summary row. They are still asserted in two tests that currently check only these removed properties. The per-field properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutSummary`, `ProbePolicyTimeoutDetail`) drive the Agent Detection page cards instead.

2. **Agent Detection page missing from README screenshot set** (backlog.md:595-603): The page works and is shipped, but `ReadmeImageCapture.cs` does not capture it, so the README image set is incomplete. The new page should appear in the Screenshots section alongside the other Settings pages.

This change closes both items: removes the dead display code, repurposes the two tests to preserve their defaults/custom coverage using the per-field properties, adds the Agent Detection page to the capture harness and PowerShell script, updates the README Screenshots section with an image reference and caption, and documents the new image in `docs/images/README.md`.

## What changes

### 1. Remove display-unused probe policy properties

**From `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`:**

- Remove `ProbePolicySummary` (line 313), `ProbePolicyDetail` (line 319), and the private `HasCustomProbePolicy` (line 323)
- Remove the two `OnPropertyChanged` calls for these properties from `RaiseProbePolicyChanges()` (lines 338-339)
- Remove the two `OnPropertyChanged` calls for these properties from `Load()` (lines 674-675)
- **Keep** `ResolveProbeTtlMinutes()` and `ResolveProbeTimeoutSeconds()` (lines 326-334) — the per-field summary properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyTimeoutSummary`) still use them

**From `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`:**

- **Repurpose** `Load_EmptyProbeFields_ShowsBuiltInDefaults` (lines 829-838): keep the test name and scenario, but assert the per-field properties instead of the removed combined properties — `ProbePolicyCacheTtlSummary` = "15 min", `ProbePolicyCacheTtlDetail` = "Built-in default", `ProbePolicyTimeoutSummary` = "10 sec", `ProbePolicyTimeoutDetail` = "Built-in default"
- **Repurpose** `ProbeFields_PositiveValues_ShowCustomEffectivePolicy` (lines 840-851): keep the test name and scenario, but assert the per-field properties with custom values — after setting `ProbeTtlText = "30"` and `ProbeTimeoutText = "5"`, assert `ProbePolicyCacheTtlSummary` = "30 min", `ProbePolicyCacheTtlDetail` contains "Custom", `ProbePolicyTimeoutSummary` = "5 sec", `ProbePolicyTimeoutDetail` contains "Custom"

The defaults/custom coverage is preserved; the tests are repurposed, not deleted.

### 2. Add Agent Detection page to README screenshot set

**`src/CLIHub/Views/ReadmeImageCapture.cs`:**

After the CLI Agents capture (line 114), add:

```csharp
await CapturePageAsync(settingsWindow, settingsViewModel, "agent-detection", directory, "settings-agent-detection.png");
```

**`docs/images/tools/Capture-ReadmeImages.ps1`:**

- Add `'settings-agent-detection.png'` to the `$captures` array (after line 497)
- Add `Assert-ImageSize (Join-Path $OutputDirectory 'settings-agent-detection.png') -Width 1200 -Height 900` after line 513

**`README.md`:**

After the CLI Agents screenshot and caption (lines 42-44), add a new screenshot block:

```markdown
<img src="docs/images/settings-agent-detection.png" alt="CLIHub Settings: agent detection cache TTL and version check timeout" width="880">

*Settings → Agent Detection: detection cache TTL, version check timeout, effective values in the header cards, and the Reset action to restore built-in defaults.*
```

**`docs/images/README.md`:**

After the `settings-agents.png` row (line 14), add:

```markdown
| `settings-agent-detection.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Agent Detection (detection cache TTL, version check timeout, DEFAULTS card, and Reset action). |
```

### 3. Refresh the actual screenshot (manual task)

Run the PowerShell capture script to generate the new PNG:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1
```

**Important:** The app must not be running. The script seeds a throwaway configuration, renders the pages at 144 DPI with `RenderTargetBitmap`, applies rounded corners, and restores the original configuration.

If the environment cannot run the GUI capture (headless/CI/missing dependencies), the implementer must record that fact but leave the code/list changes in place — do not fabricate an image file. The capture script will fail loudly if the app is running or if dependencies are missing.

## Impact

**Files modified:**

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — remove 3 properties, 4 `OnPropertyChanged` calls
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` — repurpose 2 tests to assert per-field properties
- `src/CLIHub/Views/ReadmeImageCapture.cs` — add one `CapturePageAsync` call
- `docs/images/tools/Capture-ReadmeImages.ps1` — add one `$captures` entry, one `Assert-ImageSize` line
- `README.md` — add one screenshot block and caption
- `docs/images/README.md` — add one table row

**File created (by manual task):**

- `docs/images/settings-agent-detection.png` (1332×1031 px like the other settings captures, 12 px rounded corners; the script asserts a 1200×900 minimum)

**No breaking changes.** The removed properties had no XAML bindings and no callers outside the two repurposed tests. The per-field properties that replace them in the tests are already present and driving the live Agent Detection page.

**Verification:**

1. Build passes after property removal and test updates
2. The two repurposed tests pass with the new assertions
3. The capture script includes `settings-agent-detection.png` in its validation
4. If the manual capture task succeeds, the new PNG exists at 1332×1031 px (above the 1200×900 minimum)
5. `openspec validate --all` passes (no spec deltas; `skip_specs: true` declared)

## Non-goals

- Changing the Agent Detection page's behavior or layout (it is shipped and verified)
- Removing `ResolveProbeTtlMinutes()` or `ResolveProbeTimeoutSeconds()` — the per-field properties still use them
- Adding other Settings pages to the capture set (only the Agent Detection gap is in scope)
- Changing the screenshot capture format or tooling
- Editing archived history under `openspec/changes/archive/` (forbidden by the brief)
- Fabricating the PNG if the capture script cannot run in the environment

## Dependencies

None. The change depends only on the already-archived `agent-detection-settings-page` change.
