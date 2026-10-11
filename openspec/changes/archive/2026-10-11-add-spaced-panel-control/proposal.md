## Why

CLIHub's WPF views repeatedly express simple gaps between vertically or horizontally arranged elements through child-specific `Margin` values, making spacing harder to review and adjust consistently. A reusable spacing panel should centralize these gaps while preserving predictable WPF stack measurement and rejecting invalid spacing values.

## What Changes

- Add a reusable `SpacedPanel` control to the `CLIHub` WPF project.
- Support vertical and horizontal arrangement through an `Orientation` dependency property.
- Support a non-negative inter-child `Spacing` dependency property that can be bound to shared sizing resources.
- Implement WPF measure and arrange behavior that preserves the available cross-axis constraint and gives the stack axis the same unbounded measurement semantics as `StackPanel`.
- Ignore collapsed children when calculating gaps and arranging visible children.
- Add focused tests for measurement, arrangement, collapsed children, orientation, and spacing validation.
- Document intended adoption in simple settings and toolbar groups while leaving grid-based pane and list layouts unchanged.

## Capabilities

### New Capabilities

- `spaced-panel-layout`: Provides a reusable WPF panel for consistent gaps between visible child elements in a vertical or horizontal stack.

### Modified Capabilities

- None.

## Impact

- Affected production area: `src/CLIHub`, including a new control under the WPF UI project and optional XAML usage in simple settings or toolbar layouts.
- Affected test area: `tests/CLIHub.Tests` for WPF layout behavior.
- Affected shared UI resources: `src/CLIHub/Themes/Sizing.xaml` may gain spacing tokens, without changing the palette or configuration model.
- No changes to `CLIHub.Core`, plugin loading, persistence, process execution, or public application workflows.
