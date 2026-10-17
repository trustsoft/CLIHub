## Why

The numeric settings fields can be stepped with the chips and the Up/Down keys, but the mouse wheel — the pointer is often already over the field while scanning the settings pages — does nothing. Wheel stepping is the expected behavior of a numeric spinner and should complete the control's input model.

## What Changes

- One mouse-wheel notch over a numeric field SHALL change the value by one step, upward for wheel-up and downward for wheel-down.
- Wheel stepping SHALL use the same clamping as the chips and keyboard, so a value cannot leave the field's minimum and maximum.
- The wheel event SHALL be consumed while the pointer is over the field, so the settings page does not scroll and change the value at the same time.
- Wheel stepping applies while the pointer is over the field regardless of keyboard focus, matching the chips.

## Capabilities

### New Capabilities
<!-- None: wheel input extends the existing numeric field requirement. -->

### Modified Capabilities
- `settings-theme`: the numeric input fields requirement gains mouse-wheel stepping.

## Impact

- `src/CLIHub/Controls/NumericField.cs` (wheel handling), `docs/architecture/ui.md`, and the `settings-theme` spec.
- No change to the settings view model, validation, persistence, or the configuration schema.
