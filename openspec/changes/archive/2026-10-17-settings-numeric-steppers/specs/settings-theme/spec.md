## ADDED Requirements

### Requirement: Numeric input fields

A numeric setting SHALL render as an editable number on the themed input surface with a decrement control on the leading edge and an increment control on the trailing edge. A numeric field SHALL accept an optional unit suffix shown in the muted style next to the number, and stepped values SHALL stay within the field's allowed range.

#### Scenario: Stepping with the controls
- **WHEN** the user activates the decrement or increment control
- **THEN** the displayed number changes by one step and remains within the field's minimum and maximum

#### Scenario: Range limits
- **WHEN** the value reaches the field's minimum or maximum
- **THEN** the control that would move the value beyond that bound is disabled

#### Scenario: Keyboard stepping
- **WHEN** a numeric field has focus
- **THEN** pressing Up or Down changes the value by one step within the allowed range

#### Scenario: Typed values stay editable
- **WHEN** the user types a number into a numeric field
- **THEN** the typed text is kept and the existing form validation applies when the form is saved

#### Scenario: Empty field with a default
- **WHEN** a numeric field is empty and shows a default placeholder
- **THEN** the first step starts from that default value

#### Scenario: Unit suffix
- **WHEN** a numeric setting declares a unit such as minutes, seconds, days, or megabytes
- **THEN** the unit appears as a muted suffix next to the number on the input surface
