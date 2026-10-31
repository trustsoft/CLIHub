# Design: agent-detection-follow-ups

## Current State

### Display-unused properties after the Agent Detection split

**Evidence from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`:**

The three properties in question:

```csharp
Line 313: public string ProbePolicySummary =>
Line 314:     $"{this.ResolveProbeTtlMinutes()} min · {this.ResolveProbeTimeoutSeconds()} s";

Line 319: public string ProbePolicyDetail => this.HasCustomProbePolicy
Line 320:     ? "Custom detection settings"
Line 321:     : "Built-in defaults";

Line 323: private bool HasCustomProbePolicy =>
Line 324:     !string.IsNullOrWhiteSpace(this.probeTtlText) || !string.IsNullOrWhiteSpace(this.probeTimeoutText);
```

The helper methods they use:

```csharp
Line 326: private int ResolveProbeTtlMinutes() =>
Line 327:     int.TryParse(this.probeTtlText.Trim(), out var minutes) && minutes > 0
Line 328:         ? minutes
Line 329:         : (int)AgentVersionService.DefaultTtl.TotalMinutes;

Line 331: private int ResolveProbeTimeoutSeconds() =>
Line 332:     int.TryParse(this.probeTimeoutText.Trim(), out var seconds) && seconds > 0
Line 333:         ? seconds
Line 334:         : (int)AgentVersionService.DefaultProbeTimeout.TotalSeconds;
```

The per-field properties that replaced them (Agent Detection page uses these):

```csharp
Line 361: public string ProbePolicyCacheTtlSummary => $"{this.ResolveProbeTtlMinutes()} min";
Line 366: public string ProbePolicyCacheTtlDetail => this.HasCustomProbeTtl ? ... : "Built-in default";
Line 373: public string ProbePolicyTimeoutSummary => $"{this.ResolveProbeTimeoutSeconds()} sec";
Line 378: public string ProbePolicyTimeoutDetail => this.HasCustomProbeTimeout ? ... : "Built-in default";
```

**Notification calls to remove:**

From `RaiseProbePolicyChanges()`:

```csharp
Line 338: this.OnPropertyChanged(nameof(this.ProbePolicySummary));
Line 339: this.OnPropertyChanged(nameof(this.ProbePolicyDetail));
```

From `Load()`:

```csharp
Line 674: this.OnPropertyChanged(nameof(this.ProbePolicySummary));
Line 675: this.OnPropertyChanged(nameof(this.ProbePolicyDetail));
```

**XAML binding verification:**

```
$ grep -r "ProbePolicySummary\|ProbePolicyDetail" src/CLIHub/Views --include="*.xaml"
(no results)
```

Confirmed: no XAML bindings exist. The properties were bound to the DETECTION summary card that was removed in `agent-detection-settings-page`.

**Tests to repurpose:**

From `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`:

```csharp
Line 829: [Fact]
Line 830: public void Load_EmptyProbeFields_ShowsBuiltInDefaults()
Line 831: {
Line 832:     var fixture = new SettingsViewModelFixture();
Line 833:
Line 834:     fixture.ViewModel.Load();
Line 835:
Line 836:     Assert.Equal("15 min · 10 s", fixture.ViewModel.ProbePolicySummary);
Line 837:     Assert.Equal("Built-in defaults", fixture.ViewModel.ProbePolicyDetail);
Line 838: }

Line 840: [Fact]
Line 841: public void ProbeFields_PositiveValues_ShowCustomEffectivePolicy()
Line 842: {
Line 843:     var fixture = new SettingsViewModelFixture();
Line 844:     fixture.ViewModel.Load();
Line 845:
Line 846:     fixture.ViewModel.ProbeTtlText = "30";
Line 847:     fixture.ViewModel.ProbeTimeoutText = "5";
Line 848:
Line 849:     Assert.Equal("30 min · 5 s", fixture.ViewModel.ProbePolicySummary);
Line 850:     Assert.Equal("Custom detection settings", fixture.ViewModel.ProbePolicyDetail);
Line 851: }
```

### Agent Detection page missing from README screenshot set

**Evidence from `src/CLIHub/Views/ReadmeImageCapture.cs`:**

The Settings page captures:

```csharp
Line 95:  await CapturePageAsync(settingsWindow, settingsViewModel, "appearance", directory, "settings-appearance.png");
Line 113: await CapturePageAsync(settingsWindow, settingsViewModel, "projects", directory, "settings-discovery.png");
Line 114: await CapturePageAsync(settingsWindow, settingsViewModel, "agents", directory, "settings-agents.png");
```

No `agent-detection` capture exists after line 114.

**Evidence from `docs/images/tools/Capture-ReadmeImages.ps1`:**

The `$captures` array:

```powershell
Line 493: $captures = @(
Line 494:     'launch-window.png',
Line 495:     'settings-appearance.png',
Line 496:     'settings-discovery.png',
Line 497:     'settings-agents.png'
Line 498: )
```

The `Assert-ImageSize` validations:

```powershell
Line 510: Assert-ImageSize (Join-Path $OutputDirectory 'launch-window.png') -Width 800 -Height 600
Line 511: Assert-ImageSize (Join-Path $OutputDirectory 'settings-appearance.png') -Width 1200 -Height 900
Line 512: Assert-ImageSize (Join-Path $OutputDirectory 'settings-discovery.png') -Width 1200 -Height 900
Line 513: Assert-ImageSize (Join-Path $OutputDirectory 'settings-agents.png') -Width 1200 -Height 900
```

No `settings-agent-detection.png` entry exists.

**Evidence from `README.md`:**

The Settings screenshots section:

```markdown
Line 34: <img src="docs/images/settings-appearance.png" alt="CLIHub Settings: theme, accent color, and path display" width="880">
Line 36: *Settings → Appearance: theme, accent color, and path display. Theme and accent changes apply immediately after Save, and the System theme follows the Windows light/dark mode live.*
Line 38: <img src="docs/images/settings-discovery.png" alt="CLIHub Settings: watched directories and scanning rules" width="880">
Line 40: *Settings → Project Scanning: watched roots, scan depth and scanning rules, and the background watcher.*
Line 42: <img src="docs/images/settings-agents.png" alt="CLIHub Settings: installed CLI agents and the default agent" width="880">
Line 44: *Settings → CLI Agents: installed agents and the default agent.*
```

No Agent Detection screenshot exists after the CLI Agents block.

**Evidence from `docs/images/README.md`:**

The image table:

```markdown
Line 12: | `settings-appearance.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Appearance (theme, accent color, and path display). |
Line 13: | `settings-discovery.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Project Scanning (watched roots, scanning rules, watcher status). |
Line 14: | `settings-agents.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → CLI Agents (installed agents and the default agent). |
```

No `settings-agent-detection.png` row exists after line 14.

## Decisions

### 1. Remove the display-unused properties

**Decision:** Remove `ProbePolicySummary` (line 313), `ProbePolicyDetail` (line 319), and `HasCustomProbePolicy` (line 323) from `SettingsViewModel.cs`, plus their four `OnPropertyChanged` calls.

**Rationale:**
- No XAML bindings exist (verified with grep)
- The per-field properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutSummary`, `ProbePolicyTimeoutDetail`) replaced them in the Agent Detection page
- Only the two tests reference them, and those tests will be repurposed

**Keep `ResolveProbeTtlMinutes()` and `ResolveProbeTimeoutSeconds()`:**

The per-field summary properties still use them:

```csharp
Line 361: public string ProbePolicyCacheTtlSummary => $"{this.ResolveProbeTtlMinutes()} min";
Line 373: public string ProbePolicyTimeoutSummary => $"{this.ResolveProbeTimeoutSeconds()} sec";
```

Removing them would break the live Agent Detection page. They stay.

### 2. Repurpose the two tests to preserve their coverage

**Decision:** Keep the test names and scenarios (`Load_EmptyProbeFields_ShowsBuiltInDefaults`, `ProbeFields_PositiveValues_ShowCustomEffectivePolicy`) but assert the per-field properties instead of the removed combined properties.

**Rationale:**
- The tests verify that empty fields show built-in defaults and custom values are reflected — that behavior is still present via the per-field properties
- Deleting the tests would lose that coverage
- The test names are still accurate: "empty probe fields" and "positive values" describe the scenario, not the specific properties asserted

**New assertions for `Load_EmptyProbeFields_ShowsBuiltInDefaults`:**

```csharp
Assert.Equal("15 min", fixture.ViewModel.ProbePolicyCacheTtlSummary);
Assert.Equal("Built-in default", fixture.ViewModel.ProbePolicyCacheTtlDetail);
Assert.Equal("10 sec", fixture.ViewModel.ProbePolicyTimeoutSummary);
Assert.Equal("Built-in default", fixture.ViewModel.ProbePolicyTimeoutDetail);
```

**New assertions for `ProbeFields_PositiveValues_ShowCustomEffectivePolicy`:**

After setting `ProbeTtlText = "30"` and `ProbeTimeoutText = "5"`:

```csharp
Assert.Equal("30 min", fixture.ViewModel.ProbePolicyCacheTtlSummary);
Assert.Contains("Custom", fixture.ViewModel.ProbePolicyCacheTtlDetail);
Assert.Equal("5 sec", fixture.ViewModel.ProbePolicyTimeoutSummary);
Assert.Contains("Custom", fixture.ViewModel.ProbePolicyTimeoutDetail);
```

Use `Assert.Contains("Custom", ...)` because the detail string is `"Custom (default: 15 min)"`, not just `"Custom"`.

### 3. Add Agent Detection page to README screenshot set

**Decision:** Add the `agent-detection` capture call after the CLI Agents capture (line 114), add the PNG to the `$captures` array and `Assert-ImageSize` validations, add the screenshot block to `README.md` after the CLI Agents block (lines 42-44), and add the image row to `docs/images/README.md` after the `settings-agents.png` row (line 14).

**Rationale:**
- The Agent Detection page is a shipped Settings page; the capture harness captures every other Settings page
- The README Screenshots section is incomplete without it
- The capture harness already has the infrastructure (page navigation, `CapturePageAsync`, rounded corners)

**Caption text (README.md):**

> *Settings → Agent Detection: detection cache TTL, version check timeout, effective values in the header cards, and the Reset action to restore built-in defaults.*

Matches the existing caption style: page name, primary content, notable UI elements.

**Image table row (docs/images/README.md):**

| `settings-agent-detection.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Agent Detection (detection cache TTL, version check timeout, DEFAULTS card, and Reset action). |

### 4. Manual screenshot capture task

**Decision:** The implementer must run the PowerShell capture script to generate the new PNG, but if the environment cannot run the GUI capture (headless, missing dependencies, app already running), the implementer records that fact and leaves the code/list changes in place. Do not fabricate an image file.

**Rationale:**
- The capture script requires the Debug build, a specific throwaway configuration, and a GUI environment
- The script will fail loudly if prerequisites are missing
- The code/list changes are still correct even if the PNG cannot be generated in this session
- A later session or a different machine can run the capture script and produce the PNG

## Alternatives Rejected

**Alternative 1:** Keep `ProbePolicySummary` and `ProbePolicyDetail` as convenience properties

Rejected: they have no callers outside the two tests, and the per-field properties already exist. Keeping dead code "just in case" makes the API surface larger than necessary.

**Alternative 2:** Delete the two tests instead of repurposing them

Rejected: the tests verify that defaults are shown when fields are empty and custom values are reflected when entered — that coverage is still valuable. The per-field properties provide the same data in a different shape.

**Alternative 3:** Capture the Agent Detection page in a separate follow-up task

Rejected: the capture harness already exists, and adding one page is a small incremental change. The README image set is incomplete without it.

**Alternative 4:** Fabricate a placeholder image if the capture script cannot run

Rejected: the brief explicitly says "do not fabricate an image." A placeholder would be misleading and would need to be replaced later.

## Open Questions

None. The removed properties have no XAML bindings, the per-field properties are already present and driving the live Agent Detection page, the tests' coverage is preserved by repurposing them, and the capture harness has all the infrastructure needed to add one page.

## Deviations from Brief

None. The brief specifies:

1. Remove `ProbePolicySummary`, `ProbePolicyDetail`, `HasCustomProbePolicy`, and their notification calls from `SettingsViewModel.cs` — done
2. Keep `ResolveProbeTtlMinutes()` and `ResolveProbeTimeoutSeconds()` because the per-field properties use them — done
3. Repurpose the two tests to assert the per-field properties — done
4. Add the Agent Detection page to `ReadmeImageCapture.cs`, the `$captures` array, the `Assert-ImageSize` validations, the README Screenshots section, and `docs/images/README.md` — done
5. Include a manual task for the PowerShell capture script, with an explicit note that if the environment cannot run it, the implementer must record that and leave the code/list changes in place — done
6. Declare `skip_specs: true` in `.openspec.yaml` (no spec deltas) — done

The brief also says "check whether any other docs/spec references to `ProbePolicySummary`/`ProbePolicyDetail` outside `openspec/changes/archive/` need updating; archived history must not be edited."

**Verification:**

```bash
$ grep -r "ProbePolicySummary\|ProbePolicyDetail" docs --include="*.md"
docs/backlog.md:605:### `ProbePolicySummary` and `ProbePolicyDetail` are display-unused after the Agent Detection split
docs/backlog.md:608:- Evidence: `ProbePolicySummary` (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:312`) and
docs/backlog.md:609:  `ProbePolicyDetail` (`:318`) have no XAML binding after the DETECTION card was removed from the CLI
```

The only reference is the backlog item this change closes. After the removal, that item must be deleted from `docs/backlog.md` (lines 605-616).
