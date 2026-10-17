## 1. Numeric control

- [x] 1.1 Add `CLIHub.Controls.NumericField` with a two-way `Text` dependency property, `Minimum`, `Maximum`, `Step`, `Unit`, and `Placeholder`; verify the WPF project compiles.
- [x] 1.2 Implement stepping from the chips and the Up/Down keys, clamp to the range, disable the chip at the matching bound, and seed an empty field from a numeric placeholder.

## 2. Styling

- [x] 2.1 Add the `NumericStepButton` style and the `NumericField` template to `SettingsStyles.xaml` using only palette brushes, sizing tokens, and the existing focus treatment.
- [x] 2.2 Add the `Remove` glyph to `IconGlyphs.cs` for the decrement chip; verify the markup compiles.

## 3. Settings pages

- [x] 3.1 Replace the repository scan depth input on the Projects & Paths page with a `NumericField` bounded to 1-8.
- [x] 3.2 Replace the probe TTL and timeout inputs on the CLI Agents page, keeping the default-value placeholders and moving the units into the field suffix.
- [x] 3.3 Replace the log retention and size-limit inputs on the Logs page, bounded to a minimum of 1 and 0 respectively.
- [x] 3.4 Move the unit words out of the field labels so the suffix is the only unit presentation.

## 4. Documentation and verification

- [x] 4.1 Document `NumericField` in `docs/architecture/ui.md`.
- [x] 4.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; all tests pass.
- [x] 4.3 Run `openspec validate --all`; every spec and change validates.
- [x] 4.4 Archive this change to `openspec/changes/archive/2026-10-17-settings-numeric-steppers/` and merge the delta into `openspec/specs/settings-theme/spec.md`.
