## Context

See `proposal.md` - Why. The Settings window renders fixed summary cards from page-specific `DataTemplate`s in the header area of `SettingsWindow.xaml`. Project Discovery uses `ProjectsSummaryCards` bound to `SettingsViewModel` scanner state, and Projects uses `RegistrySummaryCards` bound to `ProjectRegistryViewModel` so the counts follow manual adds and removals. The CLI Agents page is backed by `AgentManagementViewModel`, which owns the agent cards, the installed flags, the default-agent selection, and the supported/installed counts. The probe TTL and timeout are draft fields on `SettingsViewModel`.

## Goals / Non-Goals

**Goals:**

- Reuse the existing three-card visual language and the fixed page-header slot.
- Keep the agent-derived values in the agent section so test/update refresh them without reopening the window.
- Show the effective probe policy, including the built-in defaults, instead of duplicating the raw field text.

**Non-Goals:**

- No new persistence or configuration.
- No changes to the Project Discovery or Projects cards.
- No per-card navigation or click actions.

## Decisions

### Agent counts live in the agent section

Add `InstalledAgentCount`, `SupportedAgentCount`, `AvailabilitySummary`, `DefaultAgentName`, `DefaultAgentDetail`, and `HasDefaultAgent` to `IAgentManagementSection`. `RefreshAvailabilitySummary` recomputes the counts and the availability summary whenever the cards are rebuilt or a probe completes. The default-agent setters raise the default display properties.

- **Why:** the section owns the cards, the installed flags, and the selected default; recomputation there stays correct after "Test" flips an agent's installed state.
- **Alternative rejected:** reusing `SettingsViewModel.AgentsInstalledCount`, which only refreshes on `Load` and would go stale after a probe.

### Probe policy is derived from the draft fields

Add `ProbePolicySummary` and `ProbePolicyDetail` to `SettingsViewModel`. The summary uses the entered positive values when present and the built-in defaults otherwise (`AgentVersionService.DefaultTtl` and `DefaultProbeTimeout`), formatted as "15 min · 10 s". The detail reads "Built-in defaults" while both fields are empty and "Custom probe settings" otherwise. Both properties are raised from the TTL/timeout setters and from `Load`.

- **Why:** the page edits a draft, so the header should reflect what the user currently sees in the fields; showing the resolved defaults keeps the card meaningful while the fields are empty.
- **Alternative rejected:** binding the raw field text, which shows an empty value and two separate numbers without units or default resolution.

### Header template keyed by page

Add an `AgentsSummaryCards` `DataTemplate` next to the existing templates and extend the summary `ContentControl` style with a trigger on `SelectedPage.Key = "agents"`. The template binds the AGENTS and DEFAULT cards to `AgentsState.*` and the PROBE card to the settings view-model properties.

### Terminology and the obsolete metrics line

The documentation defines the terms "summary card" (one block) and "summary row" (the group) and names the per-page templates as the canonical code shape. Because the page body no longer shows a supported/installed line, `IAgentManagementSection.Metrics` and `RefreshMetrics` are removed rather than left as dead UI state.

- **Why:** the page body duplicated the AGENTS card, and keeping a `Metrics` string would preserve a competing term for the same information.
- **Alternatives rejected:** keeping `Metrics` for tests only, which leaves stale XML documentation and a second source of truth for the counts.

### Tone and icon vocabulary

- AGENTS: `Robot` glyph, success tone; the big number is the installed count.
- DEFAULT: `Robot` glyph, accent; big text is the agent name, trimmed like the DAEMON state, muted when "None".
- PROBE: `Diagnostic` glyph, muted; big text is the resolved TTL/timeout pair.

## Risks / Trade-offs

- **A long default-agent name grows the card** -> the name is trimmed with a tooltip, and all three cards stretch to the same height.
- **The probe card shows draft values that Save may reject** -> positive parsing is used for the summary; an invalid field still fails validation on Save as before.
- **Availability summary changes width** -> the hint wraps, matching the existing cards.

## Migration Plan

No data or configuration migration. Rolling back removes the template, the header trigger, and the new summary properties.
