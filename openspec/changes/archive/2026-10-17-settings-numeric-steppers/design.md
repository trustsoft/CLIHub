## Context

See `proposal.md` — Why. The Settings window already has a themed input surface (`SettingsInput`) and palette/sizing tokens in `SettingsStyles.xaml`. Numeric settings are edited as strings (`ScanDepthText`, `ProbeTtlText`, `ProbeTimeoutText`, `LogRetentionDaysText`, `LogFileSizeLimitText`) and are parsed and range-checked by `SettingsApplicationService` only on save. The settings pages are plain `UserControl`s that bind directly to the view model.

## Goals / Non-Goals

**Goals:**

- One reusable WPF control for numeric settings that follows the existing settings input styling.
- Stepped changes respect the same bounds the save validation enforces.
- Existing text bindings and validation keep working without touching the view model or Core.
- An empty field keeps its "use the default" meaning and can be stepped from the displayed default.

**Non-Goals:**

- Changing validation messages, persistence, or configuration fields.
- Decimal or formatted numbers, drag-to-change, or wheel stepping.
- New palette colors or replacing the remaining free-text inputs (accent hex, ignored patterns).

## Decisions

### D1 — `NumericField` exposes the value as a string

`NumericField : Control` keeps `Text` as a `string` dependency property, two-way by default. The existing view-model properties stay unchanged, so `SettingsApplicationService` keeps owning parsing, bounds messages, and the "empty means default" rule. **Alternative rejected:** an `int` value property would force the view model and its save path to change, and would lose the distinction between an empty field and a zero.

### D2 — Template parts wired in code-behind

The template contains `PART_DecreaseButton`, `PART_IncreaseButton`, and `PART_TextBox`. `OnApplyTemplate` detaches and re-attaches click handlers and the text box's preview-key handler, then refreshes the chip enabled state. Stepping parses the current text, falls back to `Placeholder` when the field is empty, clamps the result to `Minimum`/`Maximum`, and writes the invariant number back to `Text` so the change flows through the normal binding. Up and Down do the same as the chips. **Why:** the behavior needs both buttons and the embedded text box, which a pure XAML style cannot coordinate.

### D3 — Style on the existing palette and sizing tokens only

The control reuses `PopoverBgBrush`, `FieldStrokeBrush`, `ThumbBgBrush`, `HoverOverlayBrush`, `PressedOverlayBrush`, `AccentBrush`, `TextPrimaryBrush`, `TextMutedBrush`, `DisabledTextBrush`, and `Sizing.CornerRadius.Chip` / `Sizing.FontSize.*`. The step chips are `RepeatButton`s so holding one repeats the step. The accent border marks keyboard focus, matching `SettingsInput`. **Alternative rejected:** introducing bespoke numeric-field colors would break the shared palette contract.

### D4 — Unit suffix and placeholder carry the existing hints

`Unit` renders a muted suffix next to the number (`min`, `sec`, `days`, `MB`), so the page labels can drop their inline units. `Placeholder` keeps the probe defaults visible while the field is empty and doubles as the starting value for the first step; when the placeholder is not a number, the first increment starts at the minimum. **Why:** keeps the "empty uses the built-in default" semantics observable without adding new view-model properties.

### D5 — Adoption is limited to numeric settings

The control replaces the repository scan depth, agent probe TTL/timeout, and log retention/size fields. The accent hex and ignored-pattern inputs stay `SettingsInput` because they are not numeric.

## Risks / Trade-offs

- Typed text remains free-form until save → acceptable; existing validation reports the same errors as before, and the chips only ever produce in-range numbers.
- The editable text box sizes to its content inside a centred group, so unusually long typed text can visually crowd the chips → numeric settings are short; the save validation rejects impossible values.
- Parsing is invariant-culture while the view model parses with the current culture on save → both accept plain ASCII digits, which is what the fields and the step chips produce; no localized number formats are offered.
- No automated UI test for the new control → the WPF project compiles the template and markup, and the existing settings tests continue to cover validation; deeper control tests can be added if the stepper grows behavior.
