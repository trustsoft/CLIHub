## Why

Numeric settings on the Settings pages (repository scan depth, agent probe TTL and timeout, log retention and size limit) are plain text boxes, so the user has to select and retype a number to change it by one. The settings theme already defines the input surface but has no numeric control, so a reference design with decrement/increment controls cannot be expressed consistently.

## What Changes

- Add a reusable `NumericField` WPF control to `CLIHub.Controls`: an editable number on the themed input surface with decrement and increment chips, an optional muted unit suffix, and a step that is clamped to the field's minimum and maximum.
- Support an optional placeholder that both hints the effective default of an empty field and seeds the first step from that default.
- Change the stepped value with the Up and Down keys and disable the chip that would move the value beyond the allowed range.
- Style the control with the existing palette and sizing tokens only; no new colors or configuration fields.
- Adopt the control for the numeric settings on the Projects & Paths, CLI Agents, and Logs pages, and move their unit labels into the field suffix.
- Document the control in the UI architecture notes.

## Capabilities

### New Capabilities
<!-- None: the control refines the existing settings input surface. -->

### Modified Capabilities
- `settings-theme`: numeric settings render as themed numeric fields with decrement/increment controls, a unit suffix, and range-clamped stepping.

## Impact

- `src/CLIHub/Controls/NumericField.cs`, `src/CLIHub/Themes/SettingsStyles.xaml`, `src/CLIHub/Themes/IconGlyphs.cs`.
- `src/CLIHub/Views/SettingsProjectsPage.xaml`, `SettingsAgentsPage.xaml`, `SettingsLogsPage.xaml`.
- `docs/architecture/ui.md` and the `settings-theme` spec.
- No change to `CLIHub.Core`, the configuration schema, the settings view-model validation, or persistence.
