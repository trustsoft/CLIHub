## Purpose

Provides a reusable layout capability for arranging visible WPF child elements in one direction with a consistent, configurable gap. The capability keeps simple spacing separate from child margins while preserving predictable layout under finite constraints.

## ADDED Requirements

### Requirement: Directional child arrangement

The panel SHALL arrange its visible children in sequence along a configurable vertical or horizontal orientation. It SHALL expose the orientation as a bindable WPF property and default to vertical orientation.

#### Scenario: Default orientation is vertical
- **WHEN** a panel is created without an orientation value
- **THEN** its visible children are arranged from top to bottom

#### Scenario: Horizontal orientation
- **WHEN** the panel orientation is set to horizontal
- **THEN** its visible children are arranged from left to right in child order

#### Scenario: Orientation changes after layout
- **WHEN** the orientation changes after the panel has been measured or arranged
- **THEN** the panel recalculates and applies the layout using the new orientation

### Requirement: Configurable non-negative spacing

The panel SHALL expose a bindable spacing value, measured in device-independent units, that is applied only between consecutive visible children. Spacing SHALL default to zero and SHALL NOT accept negative, `NaN`, or infinite values.

#### Scenario: Spacing appears only between children
- **WHEN** two or more children are visible and spacing is positive
- **THEN** the configured gap appears between each consecutive visible pair with no leading or trailing gap

#### Scenario: Zero spacing
- **WHEN** spacing is zero
- **THEN** consecutive visible children have no panel-added gap

#### Scenario: Invalid spacing value
- **WHEN** a caller assigns a negative, `NaN`, or infinite spacing value
- **THEN** the panel rejects or coerces the value according to its documented validation behavior and never lays out children using an invalid gap

#### Scenario: Spacing changes after layout
- **WHEN** spacing changes after the panel has been measured or arranged
- **THEN** the panel recalculates and applies layout with the new spacing

### Requirement: Collapsed children do not participate in layout

Children with `Visibility="Collapsed"` SHALL be excluded from the panel's desired size, placement, and inter-child spacing. Other children SHALL retain their relative order.

#### Scenario: Collapsed child between visible children
- **WHEN** a collapsed child is between two visible children
- **THEN** it occupies no layout size and the configured spacing is applied once between the visible children

#### Scenario: All children are collapsed
- **WHEN** every child is collapsed
- **THEN** the panel reports no desired size and arranges no visible child

### Requirement: Constraint-aware layout

The panel SHALL measure children with the available constraint preserved on the cross axis and unbounded on the stacking axis. Its desired size SHALL be the sum of visible child sizes and inter-child gaps on the stacking axis, and the maximum visible child size on the cross axis. During arrangement, children SHALL retain their desired stacking-axis size and SHALL be arranged within the final cross-axis extent.

#### Scenario: Vertical measurement under finite width
- **WHEN** the panel is vertically oriented and receives finite available width
- **THEN** each child is measured with that width constraint and an unbounded height constraint

#### Scenario: Horizontal measurement under finite height
- **WHEN** the panel is horizontally oriented and receives finite available height
- **THEN** each child is measured with an unbounded width constraint and that height constraint

#### Scenario: Desired size includes exactly the required gaps
- **WHEN** multiple children are visible
- **THEN** the desired stacking-axis size includes one gap per adjacent visible pair and the cross-axis size is the maximum desired cross-axis child size

#### Scenario: Children exceed the final stacking extent
- **WHEN** the total desired stacking-axis size exceeds the final size supplied by the parent
- **THEN** children remain arranged in order at their desired stacking-axis sizes and the panel does not overlap them to force them into the available extent
