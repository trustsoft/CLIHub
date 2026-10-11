## Context

See `proposal.md` for motivation and `specs/spaced-panel-layout/spec.md` for the behavior contract. CLIHub is a WPF application whose shared sizing metrics are defined in `src/CLIHub/Themes/Sizing.xaml`; the new layout control belongs in the UI project and must not introduce a WPF dependency into `CLIHub.Core`.

## Goals / Non-Goals

**Goals:**
- Provide a small XAML-friendly panel for simple one-dimensional layouts.
- Match the constraint behavior expected of a stack layout while adding a uniform gap.
- Make invalid spacing values harmless and deterministic.
- Make the control testable without introducing a third-party UI package.

**Non-Goals:**
- Replace `Grid`, `DockPanel`, `WrapPanel`, or virtualizing item panels.
- Migrate all existing `StackPanel` instances or standardize every margin in the application.
- Add wrapping, justification, per-child spacing, or cross-axis alignment modes.
- Change the visual theme or any user-facing workflow.

## Decisions

### Place the control in the WPF application project

Implement the panel under `src/CLIHub/Controls/` with a `CLIHub.Controls` namespace. This keeps WPF-specific layout code within the UI boundary and avoids contaminating `CLIHub.Core`.

Alternative considered: placing it in Core or a shared non-UI library. Rejected because `Panel`, dependency properties, and WPF `UIElement` are inherently presentation-layer types.

### Keep the API intentionally small

Expose `Orientation` as a dependency property defaulting to `Vertical`, and `Spacing` as a dependency property defaulting to zero. Use a dependency-property validation/coercion policy that prevents negative, `NaN`, and infinite values from entering layout. Property changes must invalidate measure, which also schedules arrange.

Alternative considered: using a nonzero default spacing. Rejected because zero is the least surprising behavior for a panel replacing a normal `StackPanel`; call sites opt into a gap explicitly, usually via a sizing resource.

### Follow StackPanel measurement semantics

In the measure pass, give each child an infinite constraint on the stacking axis while preserving the parent's finite constraint on the cross axis. Sum visible desired sizes and add one spacing interval per adjacent visible pair. In the arrange pass, retain each child's desired size along the stacking axis and use the panel's final cross-axis extent for the other dimension.

This is required for text wrapping and other constrained children to behave naturally. The panel does not attempt to compress or overlap children when the parent provides less stacking space than their desired total.

Alternative considered: measuring every child with the full available size on both axes. Rejected because a vertical child would then receive a finite height constraint (and a horizontal child a finite width), changing stack behavior and potentially causing premature clipping or wrapping.

### Exclude collapsed children consistently

Skip collapsed children in both measure and arrange passes. Compute spacing based on the count of participating children rather than raw child indices, so no phantom gap remains where a collapsed child was declared.

### Adopt selectively and use sizing tokens

Use the panel only in simple settings-page stacks or compact horizontal groups where the gap is purely between siblings. Add narrowly named shared spacing tokens only where multiple call sites share the same rhythm. Retain margins for outer separation, card padding, and one-off optical alignment; retain Grid for launch panes, list rows, and layouts with star sizing or text-width constraints.

Alternative considered: broad migration of all StackPanels. Rejected because many current margins encode non-spacing semantics and the launch-window grids have explicit sizing behavior.

### Test behavior at the WPF layout boundary

Add focused tests in `tests/CLIHub.Tests` using small deterministic child elements to verify orientation, measure constraints, desired size, arrange rectangles, collapsed-child gaps, property invalidation/validation, and overflow without overlap. Avoid snapshot or theme tests; the contract is layout geometry.

## Risks / Trade-offs

- [A custom panel can diverge from subtle WPF layout expectations] -> Keep its API narrow, align constraints with `StackPanel`, and cover both axes and finite cross-axis constraints in tests.
- [A spacing token may be mistaken for outer margin or control padding] -> Name tokens by their role and use the panel only where the gap is strictly between siblings.
- [Tests that depend on an STA dispatcher can be fragile] -> Follow the existing WPF test setup and keep panel tests isolated from windows, application resources, and theme initialization.

## Migration Plan

Add the control and tests, then adopt it in a small set of representative settings layouts where it removes repeated sibling-gap margins without changing outer spacing. Existing XAML remains valid and no persisted data migration is required. Rollback consists of replacing each use with `StackPanel` plus the previous child margins and removing the control/resources/tests.
