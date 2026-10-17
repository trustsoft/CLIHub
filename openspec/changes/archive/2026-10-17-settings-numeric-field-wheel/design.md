## Context

See `proposal.md` — Why. `NumericField` already centralizes stepping through `StepBy`/`ResolveNextValue`, and the settings pages are hosted in a `ScrollViewer`, so the wheel event must be routed to the control before the page consumes it.

## Goals / Non-Goals

**Goals:**

- Wheel-up and wheel-down step the value by one step with the existing clamping.
- The page does not scroll while the wheel edits a numeric field.

**Non-Goals:**

- Changing the step size, the bounds, focus behavior, or the chip/keyboard input.
- Making the wheel a global setting or configurable per page.

## Decisions

### D1 — Handle the tunneling preview event on the control

`OnPreviewMouseWheel` steps the value and marks the event handled. The preview route reaches `NumericField` before the bubbling wheel reaches the surrounding `ScrollViewer`, so one wheel notch cannot both edit the value and scroll the page. **Alternative rejected:** handling the bubbling `MouseWheel` — the `ScrollViewer` may already have consumed it, and using both events risks double stepping.

### D2 — Hover-based, one notch per step

The wheel is handled whenever the pointer is over the field, without requiring keyboard focus, matching the chips which also do not need focus. One notch is one `Step` value; multi-notch deltas are divided by the system's wheel delta and clamped to at least one step.

## Risks / Trade-offs

- Scrolling the page with the pointer over a numeric field edits the value instead of scrolling → accepted; the behavior is the point of the request, and a field at its bound simply does nothing rather than scrolling unexpectedly.
- A high-resolution wheel may report deltas smaller than one notch → they are treated as one step, so the value never changes by a fraction.
