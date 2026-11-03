## Context

The Settings → Agent Detection page was introduced in `agent-detection` with field hints, summary cards, and validation messages. Subsequent changes (`agents-detection-wording`, `agent-detection-defaults-detail-copy`) refined individual strings but left minor inconsistencies: the hints repeat "Empty — use defaults" verbatim in both field cards; the CACHE TTL and TIMEOUT cards duplicate their detail line in a `DetailToolTip`; the DEFAULTS card detail says "Checking timeout" while the field title is "Version check timeout"; and validation messages use "positive number" when the minimum is 1. This change unifies the copy in one pass.

## Current State Evidence

Verified line numbers (commit 76d5306):

- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:37` — Cache TTL hint: `"How long detection and version results are cached. Empty — use defaults."`
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:48` — Version check timeout hint: `"How long a single version check may take. Empty — use defaults."`
- `src/CLIHub/Views/SettingsWindow.xaml:245` — CACHE TTL card `DetailToolTip="Custom cache TTL or the built-in default"`
- `src/CLIHub/Views/SettingsWindow.xaml:251` — TIMEOUT card `DetailToolTip="Custom version check timeout or the built-in default"`
- `src/CLIHub/Views/SettingsWindow.xaml:257` — DEFAULTS card `DetailToolTip="Built-in defaults for cache TTL and version check timeout"`
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:377` — `ProbePolicyDefaultsDetail => "Cache TTL · Checking timeout";`
- `src/CLIHub/Settings/SettingsApplicationService.cs:236` — validation error: `"Cache TTL must be a positive number of minutes, or empty for the default."`
- `src/CLIHub/Settings/SettingsApplicationService.cs:242` — validation error: `"Version check timeout must be a positive number of seconds, or empty for the default."`
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:904` — `Assert.Equal("Cache TTL · Checking timeout", fixture.ViewModel.ProbePolicyDefaultsDetail);`
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:954` — `Assert.Equal("Version check timeout must be a positive number of seconds, or empty for the default.", fixture.ViewModel.ValidationError);`
- `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:30` — `Assert.Equal("Cache TTL must be a positive number of minutes, or empty for the default.", error);`

Other string references in the codebase:
- `CHANGELOG.md:33` mentions the current `Cache TTL · Checking timeout` detail (historical).
- `openspec/changes/archive/2026-11-01-agent-detection-defaults-detail-copy/` and `openspec/changes/archive/2026-10-25-agents-detection-wording/` reference the old strings in their tasks/design files (historical context only; no action needed).
- The mockup file `agent-detection-copy-mockup.html` is untracked and explicitly excluded from this change.

No matches in `docs/glossary.md`, `docs/architecture/`, or `docs/images/README.md` for the changed strings; `README.md:48` describes the page generically without quoting the detail line.

## Goals / Non-Goals

**Goals:**
- Consistent, concise UI copy with no redundant tooltips.
- The DEFAULTS card detail matches the actual field title ("Version timeout" not "Checking timeout").
- Validation messages state the minimum value explicitly (1 minute/second) and clarify that empty means default.

**Non-Goals:**
- Changing the page description (kept as-is per the brief).
- Changing field titles, card labels, the Reset label/tooltip, watermarks, or the "Built-in default" / "Custom (default: N)" detail templates.
- Adding new validation logic or field behavior; this is a copy-only change.

## Decisions

### Decision 1: Revise field hints

**Chosen:** The Cache TTL hint becomes `"How long detection and version results are cached."` (remove the trailing "Empty — use defaults." sentence). The Version check timeout hint becomes `"Maximum time for one version check."` (replace the entire string).

**Rationale:** The placeholder watermarks (15 / 10) already show the defaults visually in the empty field. The validation messages confirm that empty means default. The trailing "Empty — use defaults." sentence is redundant in both field hints. For the timeout hint, "Maximum time" states the enforced limit rather than describing duration ("may take"); removing "single" and the default-reminder sentence keeps the hint concise and parallel to the Cache TTL hint.

**Alternatives considered:**
- Keep "How long a single version check may take.": Rejected — "may take" describes duration rather than stating the limit; "Maximum time" is more precise.

### Decision 2: Update DEFAULTS detail from "Checking timeout" to "Version timeout"

**Chosen:** `ProbePolicyDefaultsDetail` becomes `"Cache TTL · Version timeout"` (U+00B7 middle dot separator, matching the existing style).

**Rationale:** The field card title reads "Version check timeout" (singular); shortening "Version check" to "Version" for the detail line is consistent with shortening "Cache TTL" (the detail line already abbreviates "Cache Time-To-Live" to "Cache TTL"). The old "Checking timeout" was introduced before the field was renamed to "Version check timeout" and never updated.

**Alternatives considered:**
- Keep "Checking timeout": Rejected — does not match the current field title.
- Use "Version check timeout" in full: Rejected — too long for a summary card detail line.

### Decision 3: Revise DEFAULTS DetailToolTip

**Chosen:** `DetailToolTip` becomes `"Built-in values for cache TTL and version check timeout."` (changed "defaults" to "values" and added a period).

**Rationale:** "Built-in values" reads more naturally than "built-in defaults for ... timeout" (the card label already says DEFAULTS, so "values" avoids repeating the word). The period is consistent with the sentence structure.

**Alternatives considered:**
- Keep "defaults": Acceptable but slightly redundant with the card label.

### Decision 4: Remove CACHE TTL and TIMEOUT DetailToolTip attributes

**Chosen:** Remove the `DetailToolTip="..."` attributes entirely from the CACHE TTL and TIMEOUT `<controls:SummaryCard>` tags in `SettingsWindow.xaml`.

**Rationale:** The tooltip text ("Custom cache TTL or the built-in default" / "Custom version check timeout or the built-in default") repeats the visible detail line word-for-word (the detail line says "Built-in default" or "Custom (default: N min|sec)", and the tooltip offers no new information). Removing redundant tooltips reduces UI clutter.

**Alternatives considered:**
- Keep the tooltip for discoverability: Rejected — users can already read the detail line; the tooltip adds no value.

### Decision 5: Validation message precision

**Chosen:** Validation messages become:
- `"Cache TTL must be at least 1 minute. Leave empty to use the default."`
- `"Version check timeout must be at least 1 second. Leave empty to use the default."`

**Rationale:** "At least 1 minute|second" is more precise than "a positive number of minutes|seconds" (positive includes 0.5, but the UI only accepts integers ≥ 1). "Leave empty to use the default." is clearer than "or empty for the default." Both messages now match in structure.

**Alternatives considered:**
- "must be 1 minute or greater": Acceptable but less concise than "at least 1".

### Decision 6: Page description unchanged

**Chosen:** Keep the page description `"Caching and timeout behavior for agent detection."` in `SettingsNavigation.cs:80` unchanged.

**Rationale:** Per the brief, the user approved option C (keep the current string). The description is accurate and does not contradict the copy changes.

**Alternatives considered:** None; this is a user decision.

## Risks / Trade-offs

**[Risk] "Version timeout" too short?** → Mitigation: The full field title ("Version check timeout") is still visible on the page; the detail line is a summary. The parallel "Cache TTL" is equally abbreviated, so the pattern is consistent.

**[Risk] Users miss the "empty = default" note in validation messages.** → Mitigation: The watermark and the detail line ("Built-in default") already indicate this; the validation message is a third confirmation.

**[Trade-off] No tooltip on CACHE TTL/TIMEOUT cards** → Benefit: Simpler UI, no duplication. Cost: Users who hover expecting more information get nothing (but the detail line already says what the tooltip said).

## Layout Consequence

The Settings window minimum width is 640px (`SettingsWindow.xaml:17`). The Agent Detection summary cards use a horizontal `SpacedPanel` with `StretchChildren="True"`, so the three cards share the available width equally (minus the navigation sidebar and padding, each card gets ~160px). The detail text inherits `TextWrapping="Wrap"` from the `HintText` style (`SettingsControls.xaml:28`), so both the old `"Cache TTL · Checking timeout"` (29 characters) and the new `"Cache TTL · Version timeout"` (27 characters) will wrap in narrow layouts. The change shortens the detail by 2 characters but does not eliminate wrapping; the layout behavior is unchanged.

## Migration Plan

1. Update the two field hint strings in `SettingsAgentDetectionPage.xaml:37,48`.
2. Remove `DetailToolTip="..."` from the CACHE TTL and TIMEOUT `<controls:SummaryCard>` tags in `SettingsWindow.xaml:245,251`.
3. Update the DEFAULTS `DetailToolTip` in `SettingsWindow.xaml:257`.
4. Update the `ProbePolicyDefaultsDetail` literal in `SettingsViewModel.cs:377`.
5. Update the two validation messages in `SettingsApplicationService.cs:236,242`.
6. Update test assertions in `SettingsViewModelApplicationTests.cs:904,954` and `SettingsApplicationServiceTests.cs:30`.
7. Update the two MODIFIED spec delta scenarios (`preferences-ui`, `settings-theme`) for the DEFAULTS detail line change.
8. Build, test, and validate (`dotnet build`, `dotnet test`, `openspec validate --all`).

Rollback: revert the source/test/spec changes; no persisted state or data migration.

## Spec Impact

The `settings-theme` requirement prose at line 493 reads:

> The defaults card SHALL show the built-in default values for both detection parameters in the format "N min · M sec" (e.g., "15 min · 10 sec") and SHALL name the two detection parameters those default values apply to.

The new detail line `"Cache TTL · Version timeout"` names both parameters (Cache TTL and Version [check] timeout), so the requirement still holds. The requirement does not mandate the exact wording "Checking timeout", so the change is compliant.

## Open Questions

None. The copy set was user-approved, the line numbers are verified, and the layout consequence (wrapping) is documented.
