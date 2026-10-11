## Context

See `proposal.md` — Why. The page label is defined once in `SettingsViewModel.AllPages` (the `SettingsPage` record's `Title`) and shown in the sidebar and the page header (`SettingsTelemetryPage.xaml`). The page key stays `telemetry`.

## Goals / Non-Goals

**Goals:**

- Rename the label to "Logs" in the sidebar and the page header.

**Non-Goals:**

- Changing the page key, its settings, or any behavior; renaming other pages.

## Decisions

### D1 — Change the label and rename the view

Update the `Title` in `AllPages` to "Logs", set the `PageHeader` text to "Logs", and rename the view `SettingsTelemetryPage` → `SettingsLogsPage` (XAML + code-behind), updating the Settings window reference. **Why:** the page key is a stable routing key and stays `telemetry`, but the view name should match the page; nothing else references "Telemetry & Logs". **Alternative rejected:** changing the page key — not user-facing and has no benefit.

## Risks / Trade-offs

- None; a label-only change.

## Migration Plan

- None.

## Open Questions

- None.
