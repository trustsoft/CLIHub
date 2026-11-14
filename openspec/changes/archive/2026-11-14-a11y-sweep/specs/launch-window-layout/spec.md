# Launch Window Layout (Delta)

## ADDED Requirements

### Requirement: Launch window actions menu entries expose an accessible name and tooltip

Actions menu entries in the Launch window SHALL bind optional tooltip and accessible-name properties from MenuAction, with a fallback to the label, ensuring consistency with the Settings window.

#### Scenario: Launch window actions menu entries are announced with accessible names

**WHEN** the user navigates an Actions menu in the Launch window with a screen reader  
**THEN** each menu entry is announced with its effective accessible name (either the explicit AccessibleName or the Label fallback)  
**AND** tooltips are available on hover or focus

#### Scenario: Launch window decorative glyphs in actions menus are not announced

**WHEN** the user reads an Actions menu in the Launch window with a screen reader  
**THEN** the glyph TextBlock preceding each label is not announced  
**AND** only the label and accessible name are announced
